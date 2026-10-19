using System.ComponentModel;
using System.IO.Ports;
using OBLSequenceStudio.Localization;
using OBLSequenceStudio.Services;

namespace OBLSequenceStudio.Controls;

/// <summary>
/// Guides firmware selection, bootloader preparation, image review and programming of a connected board.
/// </summary>
/// <remarks>
/// The designer partial owns all reusable controls. Runtime page changes only change their placement
/// and visibility; the wizard owns the bootloader session until navigation, completion or disposal.
/// </remarks>
public sealed partial class FirmwareUpdateWizard : Form
{
    private readonly UiHints _hints = new();
    private FirmwareImage? _image;
    private FirmwareUpdateSession? _session;
    private string? _preferredPort;
    // Page order also indexes the localized heading keys in ShowPage:
    // 0 = file selection, 1 = bootloader preparation, 2 = review, 3 = programming/result.
    private int _page;
    private bool _busy, _success;
    private readonly string _firmwareDirectory;

    /// <summary>Creates the wizard without a preferred port and supports Visual Studio designer activation.</summary>
    public FirmwareUpdateWizard() : this(null, AppContext.BaseDirectory) { }

    /// <summary>Creates the wizard with a port hint from the application's previous board connection.</summary>
    /// <param name="preferredPort">Serial port name to select when available, or <see langword="null"/>.</param>
    public FirmwareUpdateWizard(string? preferredPort) : this(preferredPort, AppContext.BaseDirectory) { }

    /// <summary>Initializes the layout and runtime workflow using a configurable firmware search location.</summary>
    /// <param name="preferredPort">Serial port hint; no connection is opened by the constructor.</param>
    /// <param name="executionDirectory">Base directory containing the optional <c>firmware</c> subdirectory.</param>
    internal FirmwareUpdateWizard(string? preferredPort, string executionDirectory)
    {
        InitializeComponent();
        _firmwareDirectory = Path.Combine(executionDirectory, "firmware");
        _preferredPort = preferredPort;
        // Do not inspect firmware files or serial ports while the designer is loading.
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
        ScrollableWorkspace.Attach(this, wizardLayout, new Size(804, 581));
        DarkTitleBar.Attach(this);
        Text = T("FirmwareUpdate");
        _cancel.Text = T("Cancel");
        _back.Text = T("WizardBack");
        _browse.Text = T("FirmwareBrowse");
        _refreshPorts.Text = T("FirmwareRefreshPorts");
        ButtonVisuals.Configure(_cancel, "No");
        ButtonVisuals.Configure(_back, "FirmwareUpdate");
        ButtonVisuals.Configure(_next, "FirmwareUpdate");
        ButtonVisuals.Configure(_browse, "Open");
        ButtonVisuals.Configure(_refreshPorts, "Open");
        _hints.Set(_browse, "FirmwareBrowse");
        _hints.Set(_refreshPorts, "FirmwareRefreshPorts");
        _browse.Click += (_, _) => BrowseFirmware();
        _refreshPorts.Click += (_, _) => RefreshPorts();
        _cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        _back.Click += (_, _) => { _session?.Dispose(); _session = null; _page--; ShowPage(); };
        _next.Click += async (_, _) => await NextAsync();
        FormClosing += (_, e) => { if (_busy) e.Cancel = true; };
        _choices.Resize += (_, _) => _file.Width = Math.Max(_choices.LogicalToDeviceUnits(100), _choices.ClientSize.Width - _choices.LogicalToDeviceUnits(12));
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
    /// <summary>
    /// Uses a bundled image only when exactly one binary is available and can be validated.
    /// Otherwise, leaves the wizard on manual file selection.
    /// </summary>
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
    /// <summary>Formats localized wizard text with line endings suitable for Windows text boxes.</summary>
    private static string T(string key, params object[] args) => Localizer.Text(key, args).ReplaceLineEndings(Environment.NewLine);

    /// <summary>Selects a binary and invalidates any image previously validated for the former file.</summary>
    private void BrowseFirmware()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Firmware (*.bin)|*.bin",
            CheckFileExists = true,
            InitialDirectory = Directory.Exists(_firmwareDirectory) ? _firmwareDirectory : AppContext.BaseDirectory
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) { _file.Text = dialog.FileName; _image = null; }
    }
    /// <summary>Enumerates serial ports, preserving the selection or preferred port when still available.</summary>
    private void RefreshPorts()
    {
        string? selected = _ports.SelectedItem as string ?? _preferredPort;
        _ports.Items.Clear(); _ports.Items.AddRange(SerialPort.GetPortNames().OrderBy(p => p).Cast<object>().ToArray());
        if (selected is not null && _ports.Items.Contains(selected)) _ports.SelectedItem = selected;
        else if (_ports.Items.Count > 0) _ports.SelectedIndex = 0;
    }
    /// <summary>Reuses designer-created controls to display the current page and its navigation actions.</summary>
    /// <remarks>The review page requires a validated image and an open bootloader session.</remarks>
    private void ShowPage()
    {
        // Detached controls are reused on later pages and must remain alive until Dispose.
        _choices.Controls.Clear();
        _back.Visible = _page is 1 or 2; _cancel.Visible = _page < 3;
        _next.Enabled = true; _next.Text = T(_page == 2 ? "FirmwareProgram" : _page == 3 ? "WizardFinish" : "WizardNext");
        _hints.Set(_next, _page == 2 ? "FirmwareProgram" : _page == 3 ? "WizardFinish" : "WizardNext");
        _heading.Text = T(new[] { "FirmwareSelect", "FirmwareBoot", "FirmwareReview", "FirmwareResult" }[_page]);
        _progress.Visible = _page == 3;
        if (_page == 0)
        {
            _instructions.Text = T("FirmwareSelectHelp"); _file.Width = Math.Max(_choices.LogicalToDeviceUnits(100), _choices.ClientSize.Width - _choices.LogicalToDeviceUnits(12)); _file.Dock = DockStyle.None;
            _choices.Controls.Add(_file);
            _choices.Controls.Add(_browse);
        }
        else if (_page == 1)
        {
            _instructions.Text = BootInstructions(); _prepared.Text = T("FirmwarePrepared"); _prepared.Checked = false;
            RefreshPorts(); _choices.Controls.Add(_ports); _choices.Controls.Add(_refreshPorts); _choices.Controls.Add(_prepared);
        }
        else if (_page == 2)
            _instructions.Text = T("FirmwareReviewHelp", Path.GetFileName(_file.Text), _image!.FileLength, _image.Sha256, _preferredPort!, _session!.ProductId, _session.FlashSize / 1024);
    }
    /// <summary>Combines the selected image name with the board's manual bootloader preparation instructions.</summary>
    private string BootInstructions() => T("FirmwareSelected", Path.GetFileName(_file.Text)) + Environment.NewLine + Environment.NewLine + T("FirmwareBootHelp");

    /// <summary>Prevents navigation and window closure while an asynchronous bootloader operation is running.</summary>
    private void SetBusy(bool busy)
    {
        _busy = busy; _next.Enabled = _back.Enabled = _cancel.Enabled = _choices.Enabled = !busy;
        UseWaitCursor = busy;
    }
    /// <summary>
    /// Validates the image, probes the selected port, programs the reviewed image, or closes the result page.
    /// </summary>
    /// <remarks>
    /// Programming starts only after the review page is confirmed. Before programming, errors allow correction
    /// on the current page; programming errors stay on the result page and release the bootloader session.
    /// </remarks>
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

    /// <summary>Releases the bootloader session, tooltips and reusable controls, including detached page controls.</summary>
    /// <param name="disposing">Whether managed resources should also be released.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _hints.Dispose(); _session?.Dispose(); _session = null; _file.Dispose(); _ports.Dispose(); _prepared.Dispose(); _browse.Dispose(); _refreshPorts.Dispose(); }
        base.Dispose(disposing);
    }
}
