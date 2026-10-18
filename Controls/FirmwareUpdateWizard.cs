using System.IO.Ports;
using OBLSequenceStudio.Localization;
using OBLSequenceStudio.Services;

namespace OBLSequenceStudio.Controls;

public sealed class FirmwareUpdateWizard : Form
{
    private readonly UiHints _hints = new();
    private readonly Label _heading = new() { AutoSize = true, Font = new Font("Segoe UI", 13, FontStyle.Bold) };
    private readonly TextBox _instructions = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.Vertical, BackColor = Color.FromArgb(27, 34, 48), ForeColor = Color.WhiteSmoke, TabStop = false };
    private readonly TextBox _file = new() { ReadOnly = true, Dock = DockStyle.Fill, ForeColor = SystemColors.WindowText, BackColor = SystemColors.Window };
    private readonly ComboBox _ports = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, ForeColor = SystemColors.WindowText, BackColor = SystemColors.Window };
    private readonly CheckBox _prepared = new() { AutoSize = true };
    private readonly FlowLayoutPanel _choices = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Maximum = 100 };
    private readonly Button _back = new() { AutoSize = true }, _next = new() { AutoSize = true }, _cancel = new() { AutoSize = true };
    private FirmwareImage? _image;
    private FirmwareUpdateSession? _session;
    private string? _preferredPort;
    private int _page;
    private bool _busy, _success;
    private readonly string _firmwareDirectory;

    public FirmwareUpdateWizard(string? preferredPort = null) : this(preferredPort, AppContext.BaseDirectory) { }

    internal FirmwareUpdateWizard(string? preferredPort, string executionDirectory)
    {
        DarkTitleBar.Attach(this);
        _firmwareDirectory = Path.Combine(executionDirectory, "firmware");
        _preferredPort = preferredPort;
        Text = T("FirmwareUpdate"); StartPosition = FormStartPosition.CenterParent;
        Size = new Size(820, 620); MinimumSize = new Size(760, 560);
        BackColor = Color.FromArgb(27, 34, 48); ForeColor = Color.WhiteSmoke; Font = new Font("Segoe UI", 10);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 5 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (var row in new[] { new RowStyle(SizeType.Absolute, 45), new RowStyle(SizeType.Percent, 100), new RowStyle(SizeType.Absolute, 140), new RowStyle(SizeType.Absolute, 25), new RowStyle(SizeType.Absolute, 55) }) layout.RowStyles.Add(row);
        layout.Controls.Add(_heading, 0, 0); layout.Controls.Add(_instructions, 0, 1); layout.Controls.Add(_choices, 0, 2); layout.Controls.Add(_progress, 0, 3);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 10, 0, 0) };
        foreach (var b in new[] { _cancel, _next, _back }) { ButtonVisuals.Configure(b, b == _cancel ? "No" : "FirmwareUpdate"); buttons.Controls.Add(b); }
        layout.Controls.Add(buttons, 0, 4); Controls.Add(layout);
        _cancel.Text = T("Cancel"); _back.Text = T("WizardBack");
        _cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        _back.Click += (_, _) => { _session?.Dispose(); _session = null; _page--; ShowPage(); };
        _next.Click += async (_, _) => await NextAsync();
        FormClosing += (_, e) => { if (_busy) e.Cancel = true; };
        _choices.Resize += (_, _) => _file.Width = Math.Max(100, _choices.ClientSize.Width - 12);
        _hints.Set(_heading, "FirmwareHeading");
        _hints.Set(_instructions, "FirmwareInstructions");
        _hints.Set(_file, "FirmwareFile");
        _hints.Set(_ports, "FirmwarePorts");
        _hints.Set(_prepared, "FirmwarePrepared");
        _hints.Set(_progress, "FirmwareProgress");
        _hints.Set(_cancel, "Cancel");
        _hints.Set(_back, "WizardBack");
        TryUseBundledFirmware();
        ShowPage();
    }
    private void TryUseBundledFirmware()
    {
        try
        {
            if (!Directory.Exists(_firmwareDirectory)) return;
            var files = Directory.EnumerateFiles(_firmwareDirectory)
                .Where(path => string.Equals(Path.GetExtension(path), ".bin", StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
            // An ambiguous folder requires the user to choose the intended image.
            if (files.Length != 1) return;
            _image = FirmwareImage.Load(files[0]);
            _file.Text = files[0];
            _page = 1;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        { System.Diagnostics.Debug.WriteLine(ex); }
    }
    private static string T(string key, params object[] args) => Localizer.Text(key, args).ReplaceLineEndings(Environment.NewLine);
    private Button Choice(string key, Action action)
    {
        var button = new Button { Text = T(key), AutoSize = true };
        ButtonVisuals.Configure(button, "Open"); _hints.Set(button, key); button.Click += (_, _) => action(); return button;
    }
    private void RefreshPorts()
    {
        string? selected = _ports.SelectedItem as string ?? _preferredPort;
        _ports.Items.Clear(); _ports.Items.AddRange(SerialPort.GetPortNames().OrderBy(p => p).Cast<object>().ToArray());
        if (selected is not null && _ports.Items.Contains(selected)) _ports.SelectedItem = selected;
        else if (_ports.Items.Count > 0) _ports.SelectedIndex = 0;
    }
    private void ShowPage()
    {
        // Remove controls without disposing reusable inputs.
        var old = _choices.Controls.Cast<Control>().ToArray();
        _choices.Controls.Clear();
        foreach (var control in old) if (control != _file && control != _ports && control != _prepared) control.Dispose();
        _back.Visible = _page is 1 or 2; _cancel.Visible = _page < 3;
        _next.Enabled = true; _next.Text = T(_page == 2 ? "FirmwareProgram" : _page == 3 ? "WizardFinish" : "WizardNext");
        _hints.Set(_next, _page == 2 ? "FirmwareProgram" : _page == 3 ? "WizardFinish" : "WizardNext");
        _heading.Text = T(new[] { "FirmwareSelect", "FirmwareBoot", "FirmwareReview", "FirmwareResult" }[_page]);
        _progress.Visible = _page == 3;
        if (_page == 0)
        {
            _instructions.Text = T("FirmwareSelectHelp"); _file.Width = Math.Max(100, _choices.ClientSize.Width - 12); _file.Dock = DockStyle.None;
            _choices.Controls.Add(_file);
            _choices.Controls.Add(Choice("FirmwareBrowse", () =>
            {
                using var dialog = new OpenFileDialog
                {
                    Filter = "Firmware (*.bin)|*.bin",
                    CheckFileExists = true,
                    InitialDirectory = Directory.Exists(_firmwareDirectory) ? _firmwareDirectory : AppContext.BaseDirectory
                };
                if (dialog.ShowDialog(this) == DialogResult.OK) { _file.Text = dialog.FileName; _image = null; }
            }));
        }
        else if (_page == 1)
        {
            _instructions.Text = BootInstructions(); _prepared.Text = T("FirmwarePrepared"); _prepared.Checked = false;
            RefreshPorts(); _choices.Controls.Add(_ports); _choices.Controls.Add(Choice("FirmwareRefreshPorts", RefreshPorts)); _choices.Controls.Add(_prepared);
        }
        else if (_page == 2)
            _instructions.Text = T("FirmwareReviewHelp", Path.GetFileName(_file.Text), _image!.FileLength, _image.Sha256, _preferredPort!, _session!.ProductId, _session.FlashSize / 1024);
    }
    private string BootInstructions() => T("FirmwareSelected", Path.GetFileName(_file.Text)) + Environment.NewLine + Environment.NewLine + T("FirmwareBootHelp");
    private void SetBusy(bool busy)
    {
        _busy = busy; _next.Enabled = _back.Enabled = _cancel.Enabled = _choices.Enabled = !busy;
        UseWaitCursor = busy;
    }
    private async Task NextAsync()
    {
        if (_busy) return;
        if (_page == 3) { DialogResult = _success ? DialogResult.OK : DialogResult.Cancel; Close(); return; }
        SetBusy(true);
        try
        {
            if (_page == 0) { _image = FirmwareImage.Load(_file.Text); _page = 1; ShowPage(); }
            else if (_page == 1)
            {
                if (!_prepared.Checked || _ports.SelectedItem is not string port) throw new InvalidOperationException(T("FirmwarePrepareRequired"));
                _instructions.Text = T("FirmwareProbing"); _preferredPort = port;
                _session = await FirmwareUpdateSession.OpenAsync(port);
                _page = 2; ShowPage();
            }
            else
            {
                _page = 3; ShowPage(); _next.Enabled = false;
                var progress = new Progress<FirmwareProgress>(p => { if (!_busy || _success || IsDisposed) return; _progress.Value = Math.Clamp(p.Percent, 0, 100); _instructions.Text = T(p.StageKey); });
                await _session!.ProgramAsync(_image!, progress);
                _success = true; _progress.Value = 100; _instructions.Text = T("FirmwareSuccess");
            }
        }
        catch (Exception ex)
        {
            if (_page == 3) { _success = false; _instructions.Text = T("FirmwareFailure", ex.Message); }
            else
            {
                if (_page == 1) _instructions.Text = BootInstructions();
                MessageBox.Show(this, T("FirmwareError", ex.Message), T("FirmwareUpdate"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally { if (_page == 3) { _session?.Dispose(); _session = null; } SetBusy(false); }
    }

    private void InitializeComponent()
    {

    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _hints.Dispose(); _session?.Dispose(); _session = null; _file.Dispose(); _ports.Dispose(); _prepared.Dispose(); }
        base.Dispose(disposing);
    }
}
