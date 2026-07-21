using System.Globalization;
using System.Text.RegularExpressions;
using CIEL.Reconciliation.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace CIEL.Reconciliation.Services;

/// <summary>
/// Reads the Opera "Arrivals: Detailed" PDF by using the printed word positions.
/// Opera prints every reservation on a main line and its confirmation number on
/// the immediately following detail line. Reading by coordinates is much more
/// reliable than using Page.Text, whose column order can vary between PDFs.
/// </summary>
public static class OperaPdfReader
{
    private static readonly Regex DateRegex = new(@"^\d{2}-\d{2}-\d{2}$", RegexOptions.Compiled);
    private static readonly Regex ConfirmationRegex = new(@"^\d{4,10}$", RegexOptions.Compiled);

    public static List<OperaRecord> Read(string path)
    {
        var records = new List<OperaRecord>();

        using var document = PdfDocument.Open(path);
        foreach (var page in document.GetPages())
        {
            var lines = BuildLines(page.GetWords());

            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];

                // The report header also contains Booking.Com, therefore a real
                // reservation line must additionally contain arrival and departure dates.
                if (!line.Words.Any(w => w.Text.Equals("Booking.Com", StringComparison.OrdinalIgnoreCase)))
                    continue;

                var dates = line.Words
                    .Where(w => DateRegex.IsMatch(w.Text))
                    .OrderBy(w => w.Left)
                    .Select(w => w.Text)
                    .ToList();

                if (dates.Count < 2)
                    continue;

                // In the Opera report the guest-name column starts near x=35 and
                // ends before the Travel Agent column near x=180.
                var guest = JoinWords(line.Words.Where(w => w.Left >= 34 && w.Left < 178));
                if (string.IsNullOrWhiteSpace(guest))
                    continue;

                var confirmation = FindConfirmation(lines, index);
                var status = JoinWords(line.Words.Where(w => w.Left >= 530 && w.Left < 580));
                var roomNumber = JoinWords(line.Words.Where(w => w.Left < 31 && IsDigits(w.Text)));

                records.Add(new OperaRecord
                {
                    OperaConf = confirmation,
                    GuestName = guest,
                    Arrival = ParseOperaDate(dates[0]),
                    Departure = ParseOperaDate(dates[1]),
                    Status = status,
                    RoomNumber = roomNumber,
                    NormalizedName = NameTools.Normalize(guest)
                });
            }
        }

        if (records.Count == 0)
        {
            throw new InvalidDataException(
                "No Booking.com reservations were detected in the Opera PDF. " +
                "Please select the Opera Arrivals: Detailed report filtered by Travel Agent Booking.Com.");
        }

        return records;
    }

    private static List<PdfLine> BuildLines(IEnumerable<Word> words)
    {
        // PDF coordinates start at the bottom of the page. Words on one printed
        // line have almost the same baseline; 2 points safely absorbs small font differences.
        const double tolerance = 2.0;
        var result = new List<PdfLine>();

        foreach (var word in words
                     .Where(w => !string.IsNullOrWhiteSpace(w.Text))
                     .OrderByDescending(w => w.BoundingBox.Bottom)
                     .ThenBy(w => w.BoundingBox.Left))
        {
            var y = word.BoundingBox.Bottom;
            PdfLine? target = null;

            // Only recent lines need checking because input is already sorted by Y.
            for (var i = result.Count - 1; i >= 0 && i >= result.Count - 4; i--)
            {
                if (Math.Abs(result[i].Y - y) <= tolerance)
                {
                    target = result[i];
                    break;
                }
            }

            if (target == null)
            {
                target = new PdfLine(y);
                result.Add(target);
            }

            target.Add(new PdfWord(word.Text.Trim(), word.BoundingBox.Left));
        }

        foreach (var line in result)
            line.Words.Sort((a, b) => a.Left.CompareTo(b.Left));

        return result.OrderByDescending(l => l.Y).ToList();
    }

    private static string FindConfirmation(IReadOnlyList<PdfLine> lines, int mainLineIndex)
    {
        var mainY = lines[mainLineIndex].Y;

        // The confirmation-detail row is normally the next line, roughly 14 points below.
        // Check up to three following lines to tolerate occasional wrapped text.
        for (var i = mainLineIndex + 1; i < lines.Count && i <= mainLineIndex + 3; i++)
        {
            var candidateLine = lines[i];
            var verticalDistance = mainY - candidateLine.Y;
            if (verticalDistance > 27)
                break;

            var candidate = candidateLine.Words.FirstOrDefault(w =>
                w.Left >= 30 && w.Left < 80 && ConfirmationRegex.IsMatch(w.Text));

            if (candidate != null)
                return candidate.Text;
        }

        return string.Empty;
    }

    private static string JoinWords(IEnumerable<PdfWord> words) =>
        string.Join(" ", words.OrderBy(w => w.Left).Select(w => w.Text)).Trim();

    private static bool IsDigits(string value) => value.All(char.IsDigit);

    private static DateTime? ParseOperaDate(string value) =>
        DateTime.TryParseExact(value, "dd-MM-yy", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var dt) ? dt.Date : null;

    private sealed class PdfLine
    {
        private double _totalY;
        private int _count;

        public PdfLine(double y)
        {
            Y = y;
            _totalY = y;
            _count = 1;
        }

        public double Y { get; private set; }
        public List<PdfWord> Words { get; } = new();

        public void Add(PdfWord word)
        {
            Words.Add(word);
            // Keep a stable average baseline for lines with minor glyph variations.
            // The first value is already represented in _totalY/_count.
            if (Words.Count > 1)
            {
                _totalY += Y;
                _count++;
                Y = _totalY / _count;
            }
        }
    }

    private sealed record PdfWord(string Text, double Left);
}
