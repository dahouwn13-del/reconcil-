using System.Data;
using ExcelDataReader;
using CIEL.Reconciliation.Models;

namespace CIEL.Reconciliation.Services;

public static class BookingExcelReader
{
    public static List<BookingRecord> Read(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var data = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
        });
        if (data.Tables.Count == 0) throw new InvalidDataException("No worksheet found in Booking.com file.");
        var table = data.Tables[0];
        var map = table.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName.Trim().ToLowerInvariant(), c => c.ColumnName);
        string Col(params string[] names)
        {
            foreach (var n in names) if (map.TryGetValue(n, out var actual)) return actual;
            throw new InvalidDataException($"Missing required Booking.com column: {names[0]}");
        }
        var numberCol = Col("book number", "booking number");
        var guestCol = Col("guest name(s)", "guest name");
        var inCol = Col("check-in", "check in");
        var outCol = Col("check-out", "check out");
        var statusCol = Col("status");

        var result = new List<BookingRecord>();
        foreach (DataRow row in table.Rows)
        {
            var guest = Convert.ToString(row[guestCol])?.Trim() ?? "";
            var number = Convert.ToString(row[numberCol])?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(guest) && string.IsNullOrWhiteSpace(number)) continue;
            result.Add(new BookingRecord
            {
                BookingNumber = number.EndsWith(".0") ? number[..^2] : number,
                GuestName = guest,
                Arrival = ParseDate(row[inCol]),
                Departure = ParseDate(row[outCol]),
                Status = (Convert.ToString(row[statusCol]) ?? "").Trim().ToLowerInvariant(),
                NormalizedName = NameTools.Normalize(guest)
            });
        }
        return result;
    }

    private static DateTime? ParseDate(object value)
    {
        if (value is DateTime dt) return dt.Date;
        if (value is double d) return DateTime.FromOADate(d).Date;
        return DateTime.TryParse(Convert.ToString(value), out var parsed) ? parsed.Date : null;
    }
}
