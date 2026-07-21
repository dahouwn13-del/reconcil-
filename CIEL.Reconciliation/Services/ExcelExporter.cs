using ClosedXML.Excel;
using CIEL.Reconciliation.Models;

namespace CIEL.Reconciliation.Services;

public static class ExcelExporter
{
    public static void Save(string path, IReadOnlyList<ResultRecord> rows, int bookingCount, int operaCount)
    {
        using var wb = new XLWorkbook();
        AddSummary(wb, rows, bookingCount, operaCount);
        AddSheet(wb, "All Results", rows);
        foreach (var group in new[] { "Perfect Match", "Missing in Opera", "Missing in Booking.com", "Date Mismatch", "Manual Review", "Excluded / Cancelled" })
            AddSheet(wb, SafeName(group), rows.Where(r => r.Result == group).ToList());
        wb.SaveAs(path);
    }

    private static void AddSummary(XLWorkbook wb, IReadOnlyList<ResultRecord> rows, int bookingCount, int operaCount)
    {
        var ws = wb.AddWorksheet("Summary");
        ws.Cell("A1").Value = "CIEL Booking.com Reconciliation";
        ws.Range("A1:D1").Merge().Style.Font.SetBold().Font.SetFontSize(18).Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#0B4F6C")).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        ws.Cell("A3").Value = "Booking.com Records"; ws.Cell("B3").Value = bookingCount;
        ws.Cell("A4").Value = "Opera Records"; ws.Cell("B4").Value = operaCount;
        ws.Cell("A6").Value = "Result"; ws.Cell("B6").Value = "Count";
        var results = rows.GroupBy(r => r.Result).OrderBy(g => g.Key).ToList();
        for (var i = 0; i < results.Count; i++) { ws.Cell(7 + i, 1).Value = results[i].Key; ws.Cell(7 + i, 2).Value = results[i].Count(); }
        ws.RangeUsed().CreateTable();
        ws.Columns().AdjustToContents();
    }

    private static void AddSheet(XLWorkbook wb, string name, IReadOnlyList<ResultRecord> rows)
    {
        var ws = wb.AddWorksheet(name);
        var headers = new[] { "Booking.com Number", "Booking.com Guest", "Booking.com Arrival", "Booking.com Departure", "Booking.com Status", "Opera Conf No.", "Opera Guest", "Opera Arrival", "Opera Departure", "Opera Status", "Match Score", "Match Method", "Result", "Reason" };
        for (var c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];
        for (var r = 0; r < rows.Count; r++)
        {
            var x = rows[r]; var row = r + 2;
            ws.Cell(row, 1).Value = x.BookingNumber; ws.Cell(row, 2).Value = x.BookingGuest;
            if (x.BookingArrival.HasValue) ws.Cell(row, 3).Value = x.BookingArrival.Value;
            if (x.BookingDeparture.HasValue) ws.Cell(row, 4).Value = x.BookingDeparture.Value;
            ws.Cell(row, 5).Value = x.BookingStatus; ws.Cell(row, 6).Value = x.OperaConf; ws.Cell(row, 7).Value = x.OperaGuest;
            if (x.OperaArrival.HasValue) ws.Cell(row, 8).Value = x.OperaArrival.Value;
            if (x.OperaDeparture.HasValue) ws.Cell(row, 9).Value = x.OperaDeparture.Value;
            ws.Cell(row, 10).Value = x.OperaStatus; ws.Cell(row, 11).Value = x.MatchScore; ws.Cell(row, 12).Value = x.MatchMethod; ws.Cell(row, 13).Value = x.Result; ws.Cell(row, 14).Value = x.Reason;
        }
        var used = ws.RangeUsed();
        if (used is not null)
        {
            used.CreateTable();
            ws.Row(1).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(XLColor.FromHtml("#0077B6"));
            ws.Columns(3, 4).Style.DateFormat.Format = "dd-mmm-yyyy";
            ws.Columns(8, 9).Style.DateFormat.Format = "dd-mmm-yyyy";
            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents(5, 45);
            ws.Column(14).Width = 45;
        }
    }

    private static string SafeName(string name) => name.Replace("Booking.com", "Booking").Replace(" / ", "-")[..Math.Min(31, name.Replace("Booking.com", "Booking").Replace(" / ", "-").Length)];
}
