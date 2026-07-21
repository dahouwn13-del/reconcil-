using CIEL.Reconciliation.Models;
using CIEL.Reconciliation.Services;

namespace CIEL.Reconciliation;

public sealed class MainForm : Form
{
    private readonly TextBox _bookingPath = new() { ReadOnly = true, Dock = DockStyle.Fill };
    private readonly TextBox _operaPath = new() { ReadOnly = true, Dock = DockStyle.Fill };
    private readonly Button _run = new() { Text = "Generate Reconciliation Excel", Height = 46, Dock = DockStyle.Top };
    private readonly Label _status = new() { Text = "Select the Booking.com Excel file and Opera Arrivals: Detailed PDF.", AutoSize = true };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells };
    private readonly FlowLayoutPanel _cards = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, FlowDirection = FlowDirection.LeftToRight };
    private List<ResultRecord> _results = new();

    public MainForm()
    {
        Text = "CIEL Reconciliation Suite";
        Width = 1380;
        Height = 820;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 650);
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(246, 244, 239);
        AllowDrop = true;

        var title = new Label
        {
            Text = "CIEL RECONCILIATION SUITE",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 22, FontStyle.Bold),
            ForeColor = Color.FromArgb(11, 79, 108),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), RowCount = 5, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);
        root.Controls.Add(title, 0, 0);
        root.Controls.Add(BuildUploadPanel(), 0, 1);
        root.Controls.Add(_cards, 0, 2);
        root.Controls.Add(_status, 0, 3);
        root.Controls.Add(_grid, 0, 4);

        _run.Click += async (_, _) => await RunAsync();
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        AddEmptyCards();
    }

    private Control BuildUploadPanel()
    {
        var box = new GroupBox { Text = "Upload & run reconciliation", Dock = DockStyle.Fill, Padding = new Padding(16) };
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 3 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        box.Controls.Add(table);

        table.Controls.Add(new Label { Text = "Booking.com Excel", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
        table.Controls.Add(_bookingPath, 1, 0);
        var b1 = new Button { Text = "Browse", Dock = DockStyle.Fill };
        b1.Click += (_, _) => BrowseBooking();
        table.Controls.Add(b1, 2, 0);

        table.Controls.Add(new Label { Text = "Opera PDF", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
        table.Controls.Add(_operaPath, 1, 1);
        var b2 = new Button { Text = "Browse", Dock = DockStyle.Fill };
        b2.Click += (_, _) => BrowseOpera();
        table.Controls.Add(b2, 2, 1);

        table.SetColumnSpan(_run, 3);
        table.Controls.Add(_run, 0, 2);
        return box;
    }

    private void BrowseBooking()
    {
        using var dlg = new OpenFileDialog { Filter = "Booking.com Excel (*.xls;*.xlsx)|*.xls;*.xlsx" };
        if (dlg.ShowDialog(this) == DialogResult.OK) _bookingPath.Text = dlg.FileName;
    }

    private void BrowseOpera()
    {
        using var dlg = new OpenFileDialog { Filter = "Opera PDF (*.pdf)|*.pdf" };
        if (dlg.ShowDialog(this) == DialogResult.OK) _operaPath.Text = dlg.FileName;
    }

    private async Task RunAsync()
    {
        if (!File.Exists(_bookingPath.Text) || !File.Exists(_operaPath.Text))
        {
            MessageBox.Show(this, "Please select both source files.", "Files required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _run.Enabled = false;
        _status.Text = "Reading files and reconciling bookings...";
        UseWaitCursor = true;
        try
        {
            var data = await Task.Run(() =>
            {
                var bookings = BookingExcelReader.Read(_bookingPath.Text);
                var opera = OperaPdfReader.Read(_operaPath.Text);
                var results = ReconciliationEngine.Run(bookings, opera);
                return (bookings, opera, results);
            });
            _results = data.results;
            _grid.DataSource = _results;
            UpdateCards(data.bookings.Count, data.opera.Count, _results);
            _status.Text = $"Ready: {data.bookings.Count} Booking.com rows, {data.opera.Count} Opera rows. Choose where to save the Excel report.";

            using var save = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"Booking_Reconciliation_{DateTime.Now:yyyy-MM-dd_HHmm}.xlsx"
            };
            if (save.ShowDialog(this) == DialogResult.OK)
            {
                await Task.Run(() => ExcelExporter.Save(save.FileName, _results, data.bookings.Count, data.opera.Count));
                _status.Text = $"Completed: {save.FileName}";
                var open = MessageBox.Show(this, "Reconciliation Excel created successfully. Open it now?", "Completed", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (open == DialogResult.Yes)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(save.FileName) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            _status.Text = "Reconciliation failed.";
            MessageBox.Show(this, ex.Message, "Reconciliation error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
            _run.Enabled = true;
        }
    }

    private void AddEmptyCards() => UpdateCards(0, 0, Array.Empty<ResultRecord>());

    private void UpdateCards(int bookingCount, int operaCount, IReadOnlyList<ResultRecord> rows)
    {
        _cards.Controls.Clear();
        AddCard("Booking.com", bookingCount);
        AddCard("Opera", operaCount);
        AddCard("Perfect Match", rows.Count(r => r.Result == "Perfect Match"));
        AddCard("Date Mismatch", rows.Count(r => r.Result == "Date Mismatch"));
        AddCard("Missing in Opera", rows.Count(r => r.Result == "Missing in Opera"));
        AddCard("Missing in Booking", rows.Count(r => r.Result == "Missing in Booking.com"));
        AddCard("Manual Review", rows.Count(r => r.Result == "Manual Review"));
    }

    private void AddCard(string label, int value)
    {
        var panel = new Panel { Width = 170, Height = 78, Margin = new Padding(0, 4, 12, 4), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        panel.Controls.Add(new Label { Text = value.ToString(), Font = new Font("Segoe UI Semibold", 18, FontStyle.Bold), AutoSize = false, Height = 38, Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.FromArgb(11, 79, 108) });
        panel.Controls.Add(new Label { Text = label, AutoSize = false, Height = 30, Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleCenter });
        _cards.Controls.Add(panel);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        var files = e.Data?.GetData(DataFormats.FileDrop) as string[];
        if (files == null) return;
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext is ".xls" or ".xlsx") _bookingPath.Text = file;
            else if (ext == ".pdf") _operaPath.Text = file;
        }
    }
}
