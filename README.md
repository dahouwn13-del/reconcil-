# CIEL Reconciliation Suite — Version 1.1

Native Windows desktop application for reconciling a Booking.com Excel export with the Opera **Arrivals: Detailed** PDF.

## Version 1.1 fix

The Opera reader now parses the report using the printed word coordinates instead of relying on the PDF text order. This supports the actual 17-page CIEL report layout and extracts guest name, arrival date, departure date, Opera confirmation number, room number and reservation status.

## Build

Push the project to GitHub. The workflow at `.github/workflows/build-windows-exe.yml` builds a self-contained Windows x64 executable and uploads it as the `CIEL-Reconciliation-Windows` artifact.

## Use

1. Select the Booking.com `.xls` or `.xlsx` export.
2. Select the Opera `Arrivals: Detailed` PDF filtered to `Travel Agent Booking.Com`.
3. Click **Generate Reconciliation Excel**.
4. Choose where to save the output workbook.

No Python installation is required for the finished EXE.
