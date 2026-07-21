using CIEL.Reconciliation.Models;
using FuzzySharp;

namespace CIEL.Reconciliation.Services;

public static class ReconciliationEngine
{
    private static readonly HashSet<string> ActiveStatuses = new(StringComparer.OrdinalIgnoreCase) { "ok", "no_show" };

    public static List<ResultRecord> Run(IReadOnlyList<BookingRecord> bookings, IReadOnlyList<OperaRecord> opera)
    {
        var active = bookings.Where(b => ActiveStatuses.Contains(b.Status)).ToList();
        var excluded = bookings.Where(b => !ActiveStatuses.Contains(b.Status)).ToList();
        var remaining = new HashSet<int>(Enumerable.Range(0, opera.Count));
        var output = new List<ResultRecord>();

        foreach (var b in active)
        {
            int? chosen = null;
            int score = 0;
            string method = "";
            var exact = remaining.Where(i => opera[i].NormalizedName == b.NormalizedName).ToList();
            if (exact.Count > 0)
            {
                chosen = exact.OrderBy(i => DateDistance(b, opera[i])).First();
                score = 100;
                method = "Exact name";
            }
            else if (!string.IsNullOrWhiteSpace(b.NormalizedName))
            {
                foreach (var i in remaining)
                {
                    var s = Fuzz.TokenSortRatio(b.NormalizedName, opera[i].NormalizedName);
                    if (s > score) { score = s; chosen = i; }
                }
                if (score >= 78) method = "Similar name";
                else chosen = null;
            }

            var rr = NewBookingResult(b);
            rr.MatchScore = score;
            rr.MatchMethod = method;
            if (chosen is int idx)
            {
                var o = opera[idx];
                remaining.Remove(idx);
                rr.OperaConf = o.OperaConf;
                rr.OperaGuest = o.GuestName;
                rr.OperaArrival = o.Arrival;
                rr.OperaDeparture = o.Departure;
                rr.OperaStatus = o.Status;
                var arrOk = b.Arrival == o.Arrival;
                var depOk = b.Departure == o.Departure;
                if (arrOk && depOk && score >= 90)
                {
                    rr.Result = "Perfect Match";
                    rr.Reason = "Guest name and stay dates match.";
                }
                else if (!arrOk || !depOk)
                {
                    rr.Result = "Date Mismatch";
                    var parts = new List<string>();
                    if (!arrOk) parts.Add("arrival date differs");
                    if (!depOk) parts.Add("departure date differs");
                    rr.Reason = char.ToUpper(parts[0][0]) + string.Join("; ", parts)[1..] + ".";
                }
                else
                {
                    rr.Result = "Manual Review";
                    rr.Reason = "Dates match, but the guest-name similarity requires review.";
                }
            }
            output.Add(rr);
        }

        foreach (var idx in remaining.OrderBy(i => opera[i].Arrival).ThenBy(i => opera[i].GuestName))
        {
            var o = opera[idx];
            output.Add(new ResultRecord
            {
                OperaConf = o.OperaConf, OperaGuest = o.GuestName, OperaArrival = o.Arrival,
                OperaDeparture = o.Departure, OperaStatus = o.Status,
                Result = "Missing in Booking.com", Reason = "Opera reservation was not matched to an active Booking.com booking."
            });
        }

        output.AddRange(excluded.Select(b => new ResultRecord
        {
            BookingNumber = b.BookingNumber, BookingGuest = b.GuestName, BookingArrival = b.Arrival,
            BookingDeparture = b.Departure, BookingStatus = b.Status, MatchMethod = "Excluded",
            Result = "Excluded / Cancelled", Reason = "Booking.com status is not active, so it is excluded from missing-booking totals."
        }));

        var order = new Dictionary<string, int> { ["Missing in Opera"] = 1, ["Date Mismatch"] = 2, ["Manual Review"] = 3, ["Missing in Booking.com"] = 4, ["Perfect Match"] = 5, ["Excluded / Cancelled"] = 6 };
        return output.OrderBy(r => order.GetValueOrDefault(r.Result, 99)).ThenBy(r => r.BookingArrival).ThenBy(r => r.BookingGuest).ToList();
    }

    private static ResultRecord NewBookingResult(BookingRecord b) => new()
    {
        BookingNumber = b.BookingNumber, BookingGuest = b.GuestName, BookingArrival = b.Arrival,
        BookingDeparture = b.Departure, BookingStatus = b.Status,
        Result = "Missing in Opera", Reason = "No sufficiently similar Opera reservation was found."
    };

    private static int DateDistance(BookingRecord b, OperaRecord o)
    {
        var a = b.Arrival.HasValue && o.Arrival.HasValue ? Math.Abs((b.Arrival.Value - o.Arrival.Value).Days) : 999;
        var d = b.Departure.HasValue && o.Departure.HasValue ? Math.Abs((b.Departure.Value - o.Departure.Value).Days) : 999;
        return a + d;
    }
}
