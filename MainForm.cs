using System.ComponentModel;
using OBLSequenceStudio.Controls;
using OBLSequenceStudio.Localization;
using OBLSequenceStudio.Models;
using OBLSequenceStudio.Services;

namespace OBLSequenceStudio;

/// <summary>
/// Coordinates sequence editing, LED previews, file operations and the board connection.
/// </summary>
/// <remarks>
/// The designer partial owns the visual layout. Runtime initialization and device operations
/// belong here so that opening the form in Visual Studio does not start application workflows.
/// </remarks>
public sealed partial class MainForm : Form
{
    // Initialized only at runtime; designer initialization must not access the device model.
    private DeviceSequences _device = null!;
    private readonly UiHints _hints = new();
    private int _selected = 1;
    private string? _filename;
    private bool _dirty, _loading;

    // Editable copies of the selected channel's steps; PersistRows commits them to the model.
    private readonly BindingList<SequenceStep> _rows = new();

    private readonly List<(Control Control, string Key)> _localizedControls = new();
    private string _statusKey = "Ready";
    private object[] _statusArgs = Array.Empty<object>();

    // Restarted after each edit to group live changes into one write after 400 ms of inactivity.
    private readonly System.Windows.Forms.Timer _writeTimer = new() { Interval = 400 };
    private CancellationTokenSource? _search;
    private BoardConnection? _connection;
    private bool _writing;
    private bool _readingTelemetry;
    // Poll every 300 ms; _readingTelemetry prevents overlapping reads across asynchronous ticks.
    private readonly System.Windows.Forms.Timer _telemetryTimer = new() { Interval = 300 };

    private readonly List<Button> _hardwareButtons = new();

    /// <summary>
    /// Creates the design surface and, outside the designer, initializes the editor and language.
    /// </summary>
    public MainForm()
    {
        InitializeComponent();
        // Keep data binding, user preferences and device operations out of the designer.
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
        _device = DeviceSequences.CreateDefault();
        DarkTitleBar.Attach(this);
        Localizer.Initialize();
        ScrollableWorkspace.Attach(this, outer, new Size(1150, 760));
        ConfigureRuntimeUi();
        WireEvents();
        BindDevice();
        ApplyLanguage();
        WindowState = FormWindowState.Maximized;
        Disposed += (_, _) => _hints.Dispose();
    }

    /// <summary>
    /// Registers a control's resource key for both its caption and localized tooltips.
    /// </summary>
    /// <param name="control">Control whose text is managed by runtime localization.</param>
    /// <param name="key">Key in the embedded localization resources.</param>
    /// <returns>The same control, allowing registration during UI configuration.</returns>
    private T Translate<T>(T control, string key) where T : Control
    {
        control.Text = Localizer.Text(key);
        _hints.Set(control, key);
        _localizedControls.Add((control, key));
        return control;
    }

    /// <summary>
    /// Refreshes localized captions, grid formatting and channel descriptions without rebuilding the layout.
    /// </summary>
    private void ApplyLanguage()
    {
        SuspendLayout();
        try
        {
            foreach (var (control, key) in _localizedControls) control.Text = Localizer.Text(key);
            string[] headers = { "Step", "Start", "End", "Duration", "Position", "Mode", "Parameters" };
            for (int i = 0; i < headers.Length; i++)
            {
                _grid.Columns[i].HeaderText = Localizer.Text(headers[i]);
                _grid.Columns[i].HeaderCell.ToolTipText = UiHints.Text(headers[i]);
            }
            _hints.Refresh();
            _grid.DefaultCellStyle.FormatProvider = Localizer.Culture;
            _destination.ThousandsSeparator = false;
            for (int channel = 1; channel <= 28; channel++) UpdateChannelLabel(channel);
            foreach (DataGridViewRow row in _grid.Rows) row.ErrorText = "";
            UpdateTitle();
            RefreshTimeline();
            _status.Text = Localizer.Text(_statusKey, _statusArgs);
            _grid.Invalidate();
            _stepEditor.ApplyLanguage();
            _ledPreview.ApplyLanguage();
            int target = Math.Max(0, _hardwareTarget.SelectedIndex);
            _hardwareTarget.Items.Clear();
            _hardwareTarget.Items.Add(Localizer.Text("AllChannels"));
            for (int ch = 1; ch <= 28; ch++) _hardwareTarget.Items.Add(Localizer.Text("ChannelShort", ch));
            _hardwareTarget.SelectedIndex = target;
            UpdateConnectionButton();
        }
        finally { ResumeLayout(true); }
    }

    /// <summary>
    /// Retains the status resource key and arguments so a language change can translate the current message.
    /// </summary>
    private void SetStatus(string key, params object[] args)
    {
        _statusKey = key;
        _statusArgs = args;
        _status.Text = Localizer.Text(key, args);
    }

    /// <summary>
    /// Attaches actions, localization, data binding and adaptive layout behavior to designer-created controls.
    /// </summary>
    /// <remarks>Called once by the runtime constructor; repeated calls would duplicate event handlers.</remarks>
    private void ConfigureRuntimeUi()
    {
        ConfigureActionButton(_newButton, "New", NewFile);
        ConfigureActionButton(_openButton, "Open", OpenFile);
        ConfigureActionButton(_saveButton, "Save", () => SaveFile(false));
        ConfigureActionButton(_saveAsButton, "SaveAs", () => SaveFile(true));
        ConfigureActionButton(_firmwareButton, "FirmwareUpdate", OpenFirmwareWizard);
        ConfigureActionButton(_addStepButton, "AddStep", AddStep);
        ConfigureActionButton(_deleteStepButton, "DeleteStep", DeleteStep);
        ConfigureActionButton(_clearButton, "Clear", ClearSteps);
        ConfigureActionButton(_copyButton, "Copy", CopyTo);
        ConfigureActionButton(_singleStepButton, "SingleStep", KeepSingleStep);
        ConfigureActionButton(_applyBoardButton, "ApplyBoard", async () => await HardwareActionAsync(null));
        ConfigureActionButton(_startBoardButton, "StartBoard", async () => await HardwareActionAsync(BoardCommand.Start));
        ConfigureActionButton(_pauseBoardButton, "PauseBoard", async () => await HardwareActionAsync(BoardCommand.Pause));
        ConfigureActionButton(_resumeBoardButton, "ResumeBoard", async () => await HardwareActionAsync(BoardCommand.Resume));
        ConfigureActionButton(_stopBoardButton, "StopBoard", async () => await HardwareActionAsync(BoardCommand.Stop));
        ConfigureActionButton(_restartBoardButton, "RestartBoard", async () => await HardwareActionAsync(BoardCommand.Restart));
        ConfigureActionButton(_saveBoardButton, "SaveBoard", async () => await HardwareActionAsync(BoardCommand.Save));
        ButtonVisuals.Configure(_connect, "Connect");
        _hardwareButtons.Add(_applyBoardButton);
        _hardwareButtons.Add(_startBoardButton);
        _hardwareButtons.Add(_pauseBoardButton);
        _hardwareButtons.Add(_resumeBoardButton);
        _hardwareButtons.Add(_stopBoardButton);
        _hardwareButtons.Add(_restartBoardButton);
        _hardwareButtons.Add(_saveBoardButton);
        Translate(_languageLabel, "Language");
        Translate(_channelsLabel, "Channels");
        Translate(_live, "LiveChanges");
        Translate(_repeat, "AlwaysCyclic");
        _language.Items.AddRange(Localizer.Languages.Cast<object>().ToArray());
        _language.SelectedItem = Localizer.Languages.First(l => l.Code == Localizer.Code);

        _connect.Click += async (_, _) => await ToggleConnectionAsync();
        _live.CheckedChanged += (_, _) =>
        {
            if (_live.Checked && _connection is not null) QueueOnlineWrite();
            else _writeTimer.Stop();
        };
        _language.SelectedIndexChanged += (_, _) =>
        {
            if (_language.SelectedItem is not Localizer.Language language || language.Code == Localizer.Code) return;
            if (!CommitGrid())
            {
                _language.SelectedItem = Localizer.Languages.First(l => l.Code == Localizer.Code);
                return;
            }
            Localizer.SetLanguage(language.Code);
            ApplyLanguage();
            if (!Localizer.SavePreference()) SetStatus("PreferenceError");
        };

        void FitToolbarRow(FlowLayoutPanel panel, int row)
        {
            int contentHeight = panel.Controls.Cast<Control>()
                .Where(control => control.Visible)
                .Select(control => control.GetPreferredSize(Size.Empty).Height + control.Margin.Vertical)
                .DefaultIfEmpty(0).Max();
            int scrollHeight = panel.HorizontalScroll.Visible
                ? SystemInformation.GetHorizontalScrollBarHeightForDpi(panel.DeviceDpi) : 0;
            float height = Math.Max(panel.LogicalToDeviceUnits(44), contentHeight + panel.Padding.Vertical) + scrollHeight;
            if (bars.RowStyles[row].Height != height)
            {
                bars.RowStyles[row].Height = height;
            }
        }
        // Keep every wrapped command row visible instead of scrolling inside the step toolbar.
        rightTop.AutoScroll = false;
        rightTop.AutoSize = true;
        rightTop.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        rightTop.WrapContents = true;

        toolbar.Layout += (_, _) => FitToolbarRow(toolbar, 0);
        hardware.Layout += (_, _) => FitToolbarRow(hardware, 1);

        _telemetryTimer.Tick += async (_, _) => await ReadTelemetryAsync();

        void FitTimeline()
        {
            _timeline.Width = Math.Max(scroll.ClientSize.Width, _timeline.LogicalToDeviceUnits(800));
            _timeline.Height = _timeline.GetPreferredSize(Size.Empty).Height;
            _timeline.Invalidate();
        }
        scroll.Resize += (_, _) => FitTimeline();
        _timeline.DpiChangedAfterParent += (_, _) => FitTimeline();
        FitTimeline();
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        void FitGridColumns()
        {
            int[] widths = { 55, 70, 70, 85, 100, 90, 180 };
            for (int index = 0; index < widths.Length; index++)
            {
                _grid.Columns[index].MinimumWidth = _grid.LogicalToDeviceUnits(widths[index]);
            }
        }
        _grid.DpiChangedAfterParent += (_, _) => FitGridColumns();
        FitGridColumns();

        _grid.DataSource = _rows;
        _grid.CellToolTipTextNeeded += (_, e) =>
        {
            string[] keys = { "Step", "Start", "End", "Duration", "Position", "Mode", "Parameters" };
            if (e.ColumnIndex >= 0 && e.ColumnIndex < keys.Length) e.ToolTipText = UiHints.Text(keys[e.ColumnIndex]);
        };
        _hints.Set(_language, "Language");
        _hints.Set(_hardwareTarget, "HardwareTarget");
        _hints.Set(_destination, "Destination");
        _hints.Set(_channels, "Channels");
        _hints.Set(_timeline, "Timeline");
        _hints.Set(_grid, "Grid");
        _hints.Set(_stepEditor, "StepEditor");
        _hints.Set(_summary, "Summary");
        _hints.Set(_status, "Status");
        _hints.Set(_cursor, "Cursor");
        _hints.Set(_boardState, "BoardState");
        _hints.Set(vertical, "ResizePanels");
        _hints.Set(bottom, "ResizePanels");

    }

    /// <summary>
    /// Applies a button's runtime icon, localized caption and click action while preserving its designer layout.
    /// </summary>
    private void ConfigureActionButton(Button button, string key, Action action)
    {
        ButtonVisuals.Configure(button, key);
        Translate(button, key);
        button.Click += (_, _) => action();
    }

    /// <summary>
    /// Connects editor changes and preview navigation, and arranges timer and connection cleanup on close.
    /// </summary>
    private void WireEvents()
    {
        _channels.SelectedIndexChanged += (_, _) =>
        {
            if (!_loading && _channels.SelectedIndex >= 0) SelectChannel(_channels.SelectedIndex + 1);
        };
        _timeline.ChannelSelected += channel => SelectChannel(channel);
        _timeline.CursorChanged += t =>
        {
            _ledPreview.Seek(t);
            _cursor.Text = Localizer.Text("Cursor", t / 1000.0);
            _timeline.Invalidate();
        };
        _ledPreview.ChannelSelected += channel => SelectChannel(channel);
        _ledPreview.ConfigurationChanged += MarkDirty;
        _ledPreview.TimeChanged += time =>
        {
            long max = Math.Max(1000, _device.Channels.Select(c => c.TotalDuration).DefaultIfEmpty(1000).Max());
            _timeline.CursorMs = Math.Min(time, max);
            _timeline.Invalidate();
            _cursor.Text = Localizer.Text("Cursor", time / 1000.0);
        };
        _timeline.PointMoved += MovePoint;
        _timeline.StepSelected += (channel, step) =>
        {
            SelectChannel(channel);
            if (_selected == channel && step >= 0 && step < _grid.Rows.Count)
                _grid.CurrentCell = _grid.Rows[step].Cells[3];
            BindStepEditor();
        };
        _grid.SelectionChanged += (_, _) => { if (!_loading) BindStepEditor(); };
        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count) return;
            // TextBox cells require a string when FormattingApplied is true.
            if (e.ColumnIndex == 0) e.Value = (e.RowIndex + 1).ToString();
            else if (e.ColumnIndex == 4) e.Value = _rows.Take(e.RowIndex + 1).Sum(s => (long)s.Duration).ToString();
            else if (e.ColumnIndex == 5) e.Value = Localizer.Text("Mode" + _rows[e.RowIndex].Mode);
            else if (e.ColumnIndex == 6) e.Value = SequenceService.ParameterSummary(_rows[e.RowIndex]);
            else return;
            e.FormattingApplied = true;
        };
        _grid.CellValidating += (_, e) =>
        {
            if (_loading || e.RowIndex < 0 || e.RowIndex >= _rows.Count || e.ColumnIndex < 1 || e.ColumnIndex > 4) return;
            int previousTime = _rows.Take(e.RowIndex).Sum(s => s.Duration);
            int min = e.ColumnIndex == 4 ? previousTime + 10 : e.ColumnIndex == 3 ? 10 : 0;
            int max = e.ColumnIndex == 4 ? previousTime + 60000 : e.ColumnIndex == 3 ? 60000 : 100;
            if (!int.TryParse(Convert.ToString(e.FormattedValue), out int value) || value < min || value > max)
            {
                e.Cancel = true;
                _grid.Rows[e.RowIndex].ErrorText = Localizer.Text("AllowedRange", min, max);
            }
            else _grid.Rows[e.RowIndex].ErrorText = "";
        };
        _grid.CellEndEdit += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < _rows.Count)
            {
                if (e.ColumnIndex == 4 && int.TryParse(Convert.ToString(_grid.Rows[e.RowIndex].Cells[4].Value),
                        out int position))
                    _rows[e.RowIndex].Duration = position - _rows.Take(e.RowIndex).Sum(s => s.Duration);
                _grid.Rows[e.RowIndex].ErrorText = "";
            }

            _grid.Invalidate();
            RefreshTimeline();
        };
        _grid.DataError += (_, e) =>
        {
            e.ThrowException = false;
            e.Cancel = true;
            System.Diagnostics.Debug.WriteLine($"Grid DataError ({e.RowIndex}, {e.ColumnIndex}, {e.Context}): {e.Exception}");
            if (e.RowIndex >= 0 && e.RowIndex < _grid.Rows.Count)
                _grid.Rows[e.RowIndex].ErrorText = Localizer.Text("CellError");
        };
        _rows.ListChanged += (_, _) =>
        {
            if (!_loading) PersistRows();
        };
        FormClosing += (_, e) =>
        {
            if (!ConfirmDiscard()) e.Cancel = true;
        };
        _writeTimer.Tick += async (_, _) =>
        {
            if (_writing) return;
            _writeTimer.Stop();
            await ApplyOnlineAsync();
        };
        FormClosed += (_, _) => { _search?.Cancel(); _writeTimer.Dispose(); _telemetryTimer.Dispose(); _hints.Dispose(); _connection?.Dispose(); _connection = null; };
    }

    /// <summary>
    /// Normalizes cyclic playback and binds the current device configuration without treating loading as an edit.
    /// </summary>
    private void BindDevice()
    {
        SequenceService.EnsureCyclic(_device);
        _loading = true;
        _channels.Items.Clear();
        foreach (var c in _device.Channels) _channels.Items.Add(Localizer.Text("ChannelList", c.ChannelNumber, c.Steps.Count));
        _loading = false;
        _timeline.Device = _device;
        SelectChannel(_selected, false);
        if (_connection is null) SetStatus("Ready");
    }

    /// <summary>
    /// Switches the editor to a channel using cloned step rows, preserving pending valid edits when requested.
    /// </summary>
    /// <param name="channel">One-based channel number, from 1 to 28.</param>
    /// <param name="commit">Whether to commit the current grid before changing selection.</param>
    private void SelectChannel(int channel, bool commit = true)
    {
        if (_loading || channel is < 1 or > 28) return;
        if (commit && !CommitGrid()) return;
        if (commit && channel == _selected) return;
        _stepEditor.BindStep(null);
        _selected = channel;
        _loading = true;
        _rows.Clear();
        foreach (var s in Current.Steps) _rows.Add(s.Clone());
        _channels.SelectedIndex = channel - 1;
        _timeline.SelectedChannel = channel;
        _loading = false;
        BindStepEditor();
        RefreshTimeline();
    }

    private ChannelSequence Current => _device.Channels[_selected - 1];

    /// <summary>
    /// Copies changed editor rows into the selected channel and refreshes dirty state and previews.
    /// </summary>
    private void PersistRows()
    {
        if (Current.Steps.Count == _rows.Count && Current.Steps.Zip(_rows).All(pair =>
                pair.First.SameValues(pair.Second)))
            return;
        Current.Steps = _rows.Select(x => x.Clone()).ToList();
        MarkDirty();
        RefreshTimeline();
        _grid.Invalidate();
    }

    private void MarkDirty()
    {
        _dirty = true;
        UpdateTitle();
        QueueOnlineWrite();
    }

    private void UpdateTitle() => Text =
        $"OBL Sequence Studio  |  {(_filename is null ? Localizer.Text("Untitled") : Path.GetFileName(_filename))}{(_dirty ? " *" : "")}";

    private void RefreshTimeline()
    {
        _ledPreview.SetDevice(_device);
        _ledPreview.SelectChannel(_selected);
        _timeline.Invalidate();
        _summary.Text = Localizer.Text("Summary", _selected, _rows.Count, Current.TotalDuration / 1000.0);
        UpdateChannelLabel(_selected);
        _cursor.Text = Localizer.Text("Cursor", _timeline.CursorMs / 1000.0);
    }

    /// <summary>
    /// Updates a channel description while suppressing selection events raised by the list refresh.
    /// </summary>
    private void UpdateChannelLabel(int channel)
    {
        if (_channels.Items.Count != 28) return;
        string label = Localizer.Text("ChannelList", channel, _device.Channels[channel - 1].Steps.Count);
        if (Equals(_channels.Items[channel - 1], label)) return;
        bool wasLoading = _loading;
        _loading = true;
        try
        {
            _channels.Items[channel - 1] = label;
        }
        finally
        {
            _loading = wasLoading;
        }
    }

    /// <summary>
    /// Converts a dragged timeline endpoint into a step duration and, for ramps, matching boundary values.
    /// </summary>
    /// <param name="channel">One-based channel number.</param>
    /// <param name="step">Zero-based step index within the channel.</param>
    /// <param name="time">Endpoint position in milliseconds from the start of the channel sequence.</param>
    /// <param name="value">Ramp brightness percentage at the endpoint.</param>
    private void MovePoint(int channel, int step, long time, int value)
    {
        if (!CommitGrid()) return;
        if (_selected != channel) SelectChannel(channel);
        if (_selected != channel || step < 0 || step >= _rows.Count) return;
        long start = _rows.Take(step).Sum(s => (long)s.Duration);
        _rows[step].Duration = (int)Math.Clamp(time - start, 10, 60000);
        if (_rows[step].Mode == StepMode.Ramp)
        {
            _rows[step].End = value;
            if (step + 1 < _rows.Count && _rows[step + 1].Mode == StepMode.Ramp) _rows[step + 1].Start = value;
        }
        _grid.Invalidate();
    }

    private void BindStepEditor()
    {
        int index = _grid.CurrentRow?.Index ?? -1;
        _stepEditor.BindStep(index >= 0 && index < _rows.Count ? _rows[index] : null);
    }

    private void KeepSingleStep()
    {
        if (!CommitGrid()) return;
        int index = _grid.CurrentRow?.Index ?? -1;
        if (index < 0 || index >= _rows.Count) return;
        if (_rows.Count > 1 && Dialogs.Show(Localizer.Text("SingleStepConfirm"), Localizer.Text("Confirm"), MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        var step = _rows[index].Clone();
        _loading = true;
        try { _stepEditor.BindStep(null); _rows.Clear(); _rows.Add(step); Current.Repeat = true; }
        finally { _loading = false; }
        PersistRows();
        MarkDirty();
        BindStepEditor();
    }

    /// <summary>Ends grid editing and persists rows only when the current cell can be committed.</summary>
    /// <returns>Whether it is safe to continue an action that depends on the edited values.</returns>
    private bool CommitGrid()
    {
        if (!_grid.EndEdit()) return false;
        if (!_grid.IsCurrentCellDirty)
        {
            PersistRows();
            return true;
        }

        return false;
    }

    private void AddStep()
    {
        if (!CommitGrid()) return;
        if (_rows.Count >= 10)
        {
            Dialogs.Show(Localizer.Text("MaxSteps"));
            return;
        }

        int prev = _rows.Count > 0 ? _rows[_rows.Count - 1].End : 0;
        _rows.Add(new SequenceStep { Start = prev, End = prev, Duration = 1000 });
        if (_grid.Rows.Count > 0) _grid.CurrentCell = _grid.Rows[_grid.Rows.Count - 1].Cells[3];
        BindStepEditor();
    }

    private void DeleteStep()
    {
        if (!CommitGrid()) return;
        int idx = _grid.CurrentRow?.Index ?? -1;
        if (idx >= 0 && idx < _rows.Count) _rows.RemoveAt(idx);
    }

    private void ClearSteps()
    {
        if (Dialogs.Show(Localizer.Text("ClearConfirm", _selected), Localizer.Text("Confirm"), MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            _rows.Clear();
    }

    private void CopyTo()
    {
        if (!CommitGrid()) return;
        int dest = (int)_destination.Value;
        if (dest == _selected)
        {
            Dialogs.Show(Localizer.Text("DifferentChannel"));
            return;
        }

        if (Dialogs.Show(Localizer.Text("CopyConfirm", dest, _selected), Localizer.Text("CopyTitle"),
                MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        _device.Channels[dest - 1] = Current.Clone(dest);
        MarkDirty();
        UpdateChannelLabel(dest);
        RefreshTimeline();
    }

    private bool ConfirmDiscard() => !_dirty ||
                                     Dialogs.Show(Localizer.Text("DiscardConfirm"),
                                         Localizer.Text("Unsaved"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) ==
                                     DialogResult.Yes;

    private void NewFile()
    {
        if (!ConfirmDiscard()) return;
        DisconnectBoard();
        _device = DeviceSequences.CreateDefault();
        _filename = null;
        _dirty = false;
        _selected = 1;
        BindDevice();
        UpdateTitle();
    }

    private void OpenFile()
    {
        if (!ConfirmDiscard()) return;
        using var dlg = new OpenFileDialog { Filter = Localizer.Text("JsonFilter"), Title = Localizer.Text("Open") };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            var loaded = SequenceService.Load(dlg.FileName);
            DisconnectBoard();
            _device = loaded;
            _filename = dlg.FileName;
            _selected = 1;
            _dirty = false;
            BindDevice();
            UpdateTitle();
        }
        catch (Exception ex)
        {
            Dialogs.Show(Localizer.Error(ex, dlg.FileName), Localizer.Text("LoadError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveFile(bool asNew = false)
    {
        if (!CommitGrid()) return;
        var path = _filename;
        if (asNew || path is null)
        {
            using var dlg = new SaveFileDialog
            { Filter = Localizer.Text("JsonFilter"), Title = Localizer.Text("SaveAs"), FileName = "sequences_28_channels.json" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            path = dlg.FileName;
        }

        try
        {
            SequenceService.Save(path, _device);
            _filename = path;
            _dirty = false;
            UpdateTitle();
            SetStatus("Saved", path);
        }
        catch (Exception ex)
        {
            Dialogs.Show(Localizer.Error(ex, path), Localizer.Text("SaveError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportProgrammer()
    {
        if (!ConfirmDiscard()) return;
        using var dialog = new OpenFileDialog { Filter = Localizer.Text("ProgrammerFilter"), Title = Localizer.Text("ImportProgrammer") };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        try
        {
            var device = ProgrammerConfigurationImporter.Load(dialog.FileName);
            DisconnectBoard();
            _device = device;
            _filename = null;
            _selected = 1;
            BindDevice();
            MarkDirty();
        }
        catch (Exception ex) { Dialogs.Show(Localizer.Error(ex, dialog.FileName), Localizer.Text("LoadError")); }
    }

    private void UpdateConnectionButton()
    {
        _connect.Text = Localizer.Text(_search is not null ? "CancelSearch" : _connection is null ? "Connect" : "Disconnect");
        _hints.Set(_connect, _search is not null ? "CancelSearch" : _connection is null ? "Connect" : "Disconnect");
        ButtonVisuals.SetIcon(_connect, _connection is null ? "Connect" : "Disconnect");
        bool online = _connection is not null && _search is null && !_writing;
        foreach (var button in _hardwareButtons) button.Enabled = online;
        _hardwareTarget.Enabled = online;
        _connect.Enabled = !_writing;
        if (_firmwareButton is not null) _firmwareButton.Enabled = !_writing && _search is null;
    }

    private void SetSearchUi(bool searching)
    {
        foreach (var (control, _) in _localizedControls)
            if (control is Button && control != _connect) control.Enabled = !searching;
        _language.Enabled = _grid.Enabled = _stepEditor.Enabled = _channels.Enabled = _timeline.Enabled = _ledPreview.Enabled = _repeat.Enabled = !searching;
        UpdateConnectionButton();
    }

    /// <summary>
    /// Cancels an active search, disconnects the current board, or discovers a board and loads its configuration.
    /// </summary>
    private async Task ToggleConnectionAsync()
    {
        if (_search is not null) { _search.Cancel(); return; }
        if (_connection is not null) { DisconnectBoard(); return; }
        if (!CommitGrid() || !ConfirmDiscard()) return;
        _ledPreview.Pause();
        using var search = new CancellationTokenSource();
        _search = search;
        SetSearchUi(true);
        SetStatus("SearchingBoard", "");
        try
        {
            var progress = new Progress<string>(description =>
            {
                if (!IsDisposed && ReferenceEquals(_search, search) && !search.IsCancellationRequested) SetStatus("SearchingBoard", description);
            });
            var result = await BoardConnection.FindAsync(progress, search.Token);
            if (IsDisposed || search.IsCancellationRequested)
            {
                result?.Dispose();
                if (!IsDisposed) SetStatus("SearchCancelled");
                return;
            }
            if (result is null) { SetStatus("BoardNotFound"); Dialogs.Show(Localizer.Text("ConnectionFailureDetails", Localizer.Text("BoardNotFound"), ConnectionDiagnostics.LogPath), Localizer.Text("Connect")); return; }
            foreach (var channel in result.Configuration.Channels)
                channel.LedColor = _device.Channels.FirstOrDefault(c => c.ChannelNumber == channel.ChannelNumber)?.LedColor ?? "#FFB328";
            _connection = result;
            _device = result.Configuration;
            _filename = null; _dirty = false; _selected = 1;
            BindDevice(); UpdateTitle();
            SetStatus("BoardConnected", result.Description); _ledPreview.SetOnline(true); _telemetryTimer.Start();
        }
        catch (OperationCanceledException) { if (!IsDisposed) SetStatus("SearchCancelled"); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            if (!IsDisposed)
            {
                SetStatus("ConnectionFailed");
                Dialogs.Show(Localizer.Text("ConnectionFailureDetails", ex.Message, ConnectionDiagnostics.LogPath), Localizer.Text("Connect"));
            }
        }
        finally
        {
            _search = null;
            if (!IsDisposed) SetSearchUi(false);
        }
    }

    /// <summary>
    /// Stops live writes and telemetry before releasing the board connection and updating offline UI state.
    /// </summary>
    private void DisconnectBoard()
    {
        _telemetryTimer.Stop(); _ledPreview.SetOnline(false); _boardState.Text = ""; _boardState.Visible = false;
        _writeTimer.Stop();
        var connection = _connection;
        _connection = null;
        connection?.Dispose();
        UpdateConnectionButton();
        SetStatus("BoardDisconnected");
    }

    /// <summary>Clones the configuration so asynchronous board writes do not observe subsequent editor changes.</summary>
    private DeviceSequences Snapshot() => new() { Channels = _device.Channels.Select(c => c.Clone(c.ChannelNumber)).ToList() };

    /// <summary>
    /// Serializes a board action with configuration writes and applies the required configuration before execution.
    /// </summary>
    /// <param name="command">Board action, or <see langword="null"/> to apply the configuration only.</param>
    private async Task HardwareActionAsync(BoardCommand? command)
    {
        var connection = _connection;
        if (connection is null || _writing || !CommitGrid()) return;
        _writeTimer.Stop(); _writing = true; UpdateConnectionButton();
        try
        {
            int target = Math.Max(0, _hardwareTarget.SelectedIndex);
            if (command is null || command is BoardCommand.Start or BoardCommand.Restart or BoardCommand.Save)
                await connection.ApplyAsync(Snapshot(), command is null or BoardCommand.Save, command == BoardCommand.Save ? 0 : target);
            if (command == BoardCommand.Save) await connection.SaveAsync();
            else if (command.HasValue) await connection.CommandAsync(command.Value, target);
            SetStatus(command == BoardCommand.Save ? "BoardSaved" : command is null ? "OnlineUpdated" : "BoardCommandDone");
        }
        catch (Exception ex) when (ex is InvalidDataException or BoardCommandException) { SetStatus("OnlineValidation", ex.Message); }
        catch (Exception) { DisconnectBoard(); SetStatus("ConnectionLost"); }
        finally { _writing = false; UpdateConnectionButton(); }
    }

    /// <summary>
    /// Reads PWM and playback state without overlapping another telemetry read or a configuration write.
    /// </summary>
    /// <remarks>Results from a disposed form or a replaced connection are ignored.</remarks>
    private async Task ReadTelemetryAsync()
    {
        var connection = _connection;
        if (connection is null || _writing || _readingTelemetry) return;
        _readingTelemetry = true;
        try
        {
            var telemetry = await connection.ReadTelemetryAsync();
            if (IsDisposed || !ReferenceEquals(connection, _connection)) return;
            _ledPreview.SetHardwareValues(telemetry.Pwm);
            int index = _selected - 1;
            string[] states = { "BoardStopped", "BoardRunning", "BoardPaused", "BoardFinished" };
            string state = telemetry.States[index] < states.Length ? Localizer.Text(states[telemetry.States[index]]) : "?";
            _boardState.Text = Localizer.Text("BoardState", _selected, state, telemetry.Steps[index], telemetry.Pwm[index],
                Localizer.Text(telemetry.SaveState == 1 ? "BoardSaving" : telemetry.Dirty ? "BoardUnsaved" : "BoardClean"));
            _boardState.Visible = true;
        }
        catch (Exception ex)
        {
            ConnectionDiagnostics.Write($"TELEMETRY FAILED {ex.GetType().Name}: {ex.Message}");
            if (!IsDisposed && ReferenceEquals(connection, _connection)) { DisconnectBoard(); SetStatus("ConnectionLost"); }
        }
        finally { _readingTelemetry = false; }
    }

    /// <summary>
    /// Releases the normal board connection before opening the bootloader wizard with the previous port as a hint.
    /// </summary>
    private void OpenFirmwareWizard()
    {
        if (_writing || _search is not null || !CommitGrid()) return;
        string? port = _connection?.PortName;
        _ledPreview.Pause(); DisconnectBoard();
        using var wizard = new FirmwareUpdateWizard(port);
        wizard.ShowDialog(this);
    }

    /// <summary>Restarts the live-write delay when connected, live updates are enabled and no model load is active.</summary>
    private void QueueOnlineWrite()
    {
        if (_connection is null || !_live.Checked || _loading) return;
        _writeTimer.Stop(); _writeTimer.Start();
    }

    /// <summary>
    /// Sends a configuration snapshot to the current board and distinguishes validation errors from connection loss.
    /// </summary>
    private async Task ApplyOnlineAsync()
    {
        var connection = _connection;
        if (connection is null || !_live.Checked || _search is not null) return;
        _writing = true;
        UpdateConnectionButton();
        try
        {
            var snapshot = new DeviceSequences { Channels = _device.Channels.Select(c => c.Clone(c.ChannelNumber)).ToList() };
            await connection.ApplyAsync(snapshot);
            if (!IsDisposed && ReferenceEquals(_connection, connection))
                SetStatus("OnlineUpdated");
        }
        catch (Exception ex) when (ex is InvalidDataException or BoardCommandException)
        {
            if (!IsDisposed && ReferenceEquals(_connection, connection)) SetStatus("OnlineValidation", ex.Message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            if (!IsDisposed && ReferenceEquals(_connection, connection))
            {
                DisconnectBoard();
                SetStatus("ConnectionLost");
            }
        }
        finally { _writing = false; if (!IsDisposed) UpdateConnectionButton(); }
    }

    /// <summary>Exports committed editor values using the Modbus register CSV representation.</summary>
    private void ExportCsv()
    {
        if (!CommitGrid()) return;
        using var dlg = new SaveFileDialog { Filter = Localizer.Text("CsvFilter"), Title = Localizer.Text("Export"), FileName = "modbus_registers.csv" };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try
        {
            SequenceService.ExportCsv(dlg.FileName, _device);

            SetStatus("Exported", dlg.FileName);
        }
        catch (Exception ex)
        {
            Dialogs.Show(Localizer.Error(ex, dlg.FileName), Localizer.Text("ExportError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

