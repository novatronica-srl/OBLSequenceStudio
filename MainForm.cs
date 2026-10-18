using System.ComponentModel;
using OBLSequenceStudio.Controls;
using OBLSequenceStudio.Localization;
using OBLSequenceStudio.Models;
using OBLSequenceStudio.Services;

namespace OBLSequenceStudio;

public sealed class MainForm : Form
{
    private DeviceSequences _device;
    private readonly UiHints _hints = new();
    private int _selected = 1;
    private string? _filename;
    private bool _dirty, _loading;
    private readonly TimelineControl _timeline = new();
    private readonly DataGridView _grid = new();
    private readonly ListBox _channels = new();
    private readonly Label _summary = new(), _status = new(), _cursor = new();
    private readonly NumericUpDown _destination = new() { Minimum = 1, Maximum = 28, Value = 2, Width = 65 };
    private readonly BindingList<SequenceStep> _rows = new();
    private readonly Color _bg = Color.FromArgb(16, 21, 32);
    private readonly Color _panel = Color.FromArgb(27, 34, 48);
    private readonly Color _accent = Color.FromArgb(40, 167, 219);
    private readonly ComboBox _language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 115, ForeColor = SystemColors.WindowText, BackColor = SystemColors.Window };
    private readonly List<(Control Control, string Key)> _localizedControls = new();
    private string _statusKey = "Ready";
    private object[] _statusArgs = Array.Empty<object>();
    private readonly StepEditorControl _stepEditor = new() { Dock = DockStyle.Right, Width = 350 };
    private readonly Label _repeat = new() { AutoSize = true, Margin = new Padding(8, 12, 3, 0) };
    private readonly LedPreviewControl _ledPreview = new() { Dock = DockStyle.Fill };
    private readonly Button _connect = new() { AutoSize = true, Margin = new Padding(4) };
    private readonly CheckBox _live = new() { AutoSize = true, Checked = true, ForeColor = Color.WhiteSmoke, Margin = new Padding(5, 10, 8, 0) };
    private readonly System.Windows.Forms.Timer _writeTimer = new() { Interval = 400 };
    private CancellationTokenSource? _search;
    private BoardConnection? _connection;
    private bool _writing;
    private bool _readingTelemetry;
    private readonly System.Windows.Forms.Timer _telemetryTimer = new() { Interval = 300 };
    private readonly ComboBox _hardwareTarget = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 145, ForeColor = SystemColors.WindowText, BackColor = SystemColors.Window };
    private readonly List<Button> _hardwareButtons = new();
    private readonly Label _boardState = new() { AutoSize = true, Margin = new Padding(5) };
    private Button? _firmwareButton;

    public MainForm()
    {
        DarkTitleBar.Attach(this);
        Localizer.Initialize();
        _device = DeviceSequences.CreateDefault();
        MinimumSize = new Size(1150, 760);
        Size = new Size(1480, 950);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = _bg;
        ForeColor = Color.WhiteSmoke;
        Font = new Font("Segoe UI", 9.5f);
        BuildUi();
        WireEvents();
        BindDevice();
        ApplyLanguage();
        WindowState = FormWindowState.Maximized;
        Disposed += (_, _) => _hints.Dispose();

        this.Shown += MainForm_Shown;
    }

    private void MainForm_Shown(object sender, EventArgs e)
    {
        // choose a fraction that matches desired initial layout, e.g. 60% for top panel
        //vertical.SplitterDistance = (int)(vertical.ClientSize.Height * 0.6);
    }

    private Button ActionButton(string title, Action action)
    {
        var b = new Button
        {
            Text = Localizer.Text(title),
            AutoSize = true,
            Height = 34,
            Padding = new Padding(8, 3, 8, 3),
            FlatStyle = FlatStyle.Flat,
            BackColor = _panel,
            ForeColor = Color.WhiteSmoke,
            Margin = new Padding(4)
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(65, 79, 98);
        ButtonVisuals.Configure(b, title);
        _hints.Set(b, title);
        b.Click += (_, _) => action();
        _localizedControls.Add((b, title));
        return b;
    }

    private T Translate<T>(T control, string key) where T : Control
    {
        control.Text = Localizer.Text(key);
        _hints.Set(control, key);
        _localizedControls.Add((control, key));
        return control;
    }

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

    private void SetStatus(string key, params object[] args)
    {
        _statusKey = key;
        _statusArgs = args;
        _status.Text = Localizer.Text(key, args);
    }

    private void BuildUi()
    {
        var outer = new TableLayoutPanel
        { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        Controls.Add(outer);

        var toolbar = new FlowLayoutPanel
        { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, BackColor = _bg, Margin = Padding.Empty };
        var languagePanel = new FlowLayoutPanel
        { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = Padding.Empty };
        languagePanel.Controls.Add(Translate(new Label { AutoSize = true, Margin = new Padding(12, 11, 3, 0) }, "Language"));
        _language.Items.AddRange(Localizer.Languages.Cast<object>().ToArray());
        _language.SelectedItem = Localizer.Languages.First(l => l.Code == Localizer.Code);
        _language.Margin = new Padding(3, 8, 0, 0);
        languagePanel.Controls.Add(_language);
        toolbar.Controls.Add(ActionButton("New", NewFile));
        toolbar.Controls.Add(ActionButton("Open", OpenFile));
        toolbar.Controls.Add(ActionButton("Save", () => SaveFile(false)));
        var saveAs = ActionButton("SaveAs", () => SaveFile(true));
        saveAs.Margin = new Padding(4, 4, 16, 4);
        toolbar.Controls.Add(saveAs);
        ButtonVisuals.Configure(_connect, "Connect");
        toolbar.Controls.Add(_connect);
        toolbar.Controls.Add(Translate(_live, "LiveChanges"));
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
        _firmwareButton = ActionButton("FirmwareUpdate", OpenFirmwareWizard);
        toolbar.Controls.Add(_firmwareButton);
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.Controls.Add(toolbar, 0, 0);
        header.Controls.Add(languagePanel, 1, 0);
        var hardware = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, Margin = Padding.Empty };
        _hardwareTarget.Margin = new Padding(4, 8, 4, 0);
        hardware.Controls.Add(_hardwareTarget);
        foreach (var item in new[] { ("ApplyBoard", (BoardCommand?)null), ("StartBoard", (BoardCommand?)BoardCommand.Start), ("PauseBoard", (BoardCommand?)BoardCommand.Pause), ("ResumeBoard", (BoardCommand?)BoardCommand.Resume), ("StopBoard", (BoardCommand?)BoardCommand.Stop), ("RestartBoard", (BoardCommand?)BoardCommand.Restart), ("SaveBoard", (BoardCommand?)BoardCommand.Save) })
        {
            var command = item.Item2;
            var button = ActionButton(item.Item1, async () => await HardwareActionAsync(command));
            _hardwareButtons.Add(button); hardware.Controls.Add(button);
        }
        var bars = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, RowCount = 3, ColumnCount = 1, Margin = Padding.Empty };
        bars.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bars.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        bars.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        bars.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _boardState.Margin = new Padding(5, 2, 0, 2);
        _boardState.Visible = false;
        void FitToolbarRow(FlowLayoutPanel panel, int row)
        {
            float height = 44 + (panel.HorizontalScroll.Visible ? SystemInformation.HorizontalScrollBarHeight : 0);
            if (bars.RowStyles[row].Height != height) bars.RowStyles[row].Height = height;
        }
        toolbar.Layout += (_, _) => FitToolbarRow(toolbar, 0);
        hardware.Layout += (_, _) => FitToolbarRow(hardware, 1);
        bars.Controls.Add(header, 0, 0); bars.Controls.Add(hardware, 0, 1); bars.Controls.Add(_boardState, 0, 2);
        outer.Controls.Add(bars, 0, 0);
        _telemetryTimer.Tick += async (_, _) => await ReadTelemetryAsync();

        var vertical = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 7,
            BackColor = _bg,
            Panel1MinSize = 260,
            Panel2MinSize = 60
        };
        vertical.Size = new Size(outer.ClientSize.Width - outer.Padding.Horizontal, 600); // or a chosen size
        outer.Controls.Add(vertical, 0, 1);
        vertical.SplitterDistance = 450;

        var previewLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350));
        previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        vertical.Panel1.Controls.Add(previewLayout);
        previewLayout.Controls.Add(_ledPreview, 1, 0);
        _timeline.AutoScrollOffset = new Point(0, 0);
        // La timeline disegna 28 righe: lo scroll verticale è gestito dal contenitore.
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = _bg, Margin = Padding.Empty };
        previewLayout.Controls.Add(scroll, 0, 0);
        scroll.Controls.Add(_timeline);
        _timeline.Dock = DockStyle.Top;
        _timeline.Height = 40 + 28 * 35 + 28;
        scroll.Resize += (_, _) =>
        {
            _timeline.Width = Math.Max(scroll.ClientSize.Width - 20, 800);
            _timeline.Invalidate();
        };

        var bottom = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterWidth = 7,
            BackColor = _bg,
            Panel1MinSize = 165
        };
        vertical.Panel2.Controls.Add(bottom);
        bottom.SplitterDistance = 200;

        var chPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5), BackColor = _panel };
        chPanel.Controls.Add(_channels);
        chPanel.Controls.Add(Translate(new Label
        {
            Dock = DockStyle.Top,
            Height = 36,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.FromArgb(137, 208, 244)
        }, "Channels"));
        _channels.Dock = DockStyle.Fill;
        _channels.BackColor = _panel;
        _channels.ForeColor = Color.WhiteSmoke;
        _channels.BorderStyle = BorderStyle.None;
        _channels.IntegralHeight = false;
        bottom.Panel1.Controls.Add(chPanel);

        var detail = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8), BackColor = _panel };
        bottom.Panel2.Controls.Add(detail);
        var rightTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 88, WrapContents = true, AutoScroll = true };
        _summary.AutoSize = true;
        _summary.Font = new Font(Font.FontFamily, 12, FontStyle.Bold);
        _summary.Margin = new Padding(5, 12, 25, 0);
        rightTop.Controls.Add(_summary);
        rightTop.SetFlowBreak(_summary, true);
        rightTop.Controls.Add(ActionButton("AddStep", AddStep));
        rightTop.Controls.Add(ActionButton("DeleteStep", DeleteStep));
        rightTop.Controls.Add(ActionButton("Clear", ClearSteps));
        rightTop.Controls.Add(ActionButton("Copy", CopyTo));
        rightTop.Controls.Add(ActionButton("SingleStep", KeepSingleStep));
        rightTop.Controls.Add(Translate(_repeat, "AlwaysCyclic"));
        _destination.Margin = new Padding(3, 8, 3, 0);
        rightTop.Controls.Add(_destination);
        detail.Controls.Add(_grid);
        detail.Controls.Add(_stepEditor);
        detail.Controls.Add(rightTop);

        _grid.Dock = DockStyle.Fill;
        _grid.BackgroundColor = _panel;
        _grid.GridColor = Color.FromArgb(61, 75, 92);
        _grid.BorderStyle = BorderStyle.None;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 53, 73);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.DefaultCellStyle.BackColor = Color.FromArgb(29, 39, 56);
        _grid.DefaultCellStyle.ForeColor = Color.WhiteSmoke;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(24, 110, 153);
        _grid.DefaultCellStyle.SelectionForeColor = Color.White;
        _grid.RowHeadersVisible = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.EditMode = DataGridViewEditMode.EditOnEnter;
        _grid.ColumnHeadersHeight = 38;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _grid.RowTemplate.Height = 33;
        _grid.AutoGenerateColumns = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        { Name = "idx", HeaderText = Localizer.Text("Step"), ReadOnly = true, FillWeight = 55 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        { Name = "start", HeaderText = Localizer.Text("Start"), DataPropertyName = nameof(SequenceStep.Start) });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        { Name = "end", HeaderText = Localizer.Text("End"), DataPropertyName = nameof(SequenceStep.End) });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        { Name = "duration", HeaderText = Localizer.Text("Duration"), DataPropertyName = nameof(SequenceStep.Duration) });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "position", HeaderText = Localizer.Text("Position") });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "mode", HeaderText = Localizer.Text("Mode"), ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "parameters", HeaderText = Localizer.Text("Parameters"), ReadOnly = true, FillWeight = 180 });
        _grid.Columns[1].Visible = false;
        _grid.Columns[2].Visible = false;
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

        var statusBar = new Panel { Dock = DockStyle.Fill, BackColor = _bg };
        _status.Dock = DockStyle.Fill;
        _status.ForeColor = Color.FromArgb(159, 178, 199);
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _cursor.Dock = DockStyle.Right;
        _cursor.Width = 240;
        _cursor.TextAlign = ContentAlignment.MiddleRight;
        _cursor.ForeColor = Color.FromArgb(255, 194, 94);
        statusBar.Controls.Add(_status);
        statusBar.Controls.Add(_cursor);
        outer.Controls.Add(statusBar, 0, 2);

        this.Shown += (s, e) =>
        {
            // vertical and bottom are fields or accessible variables
            // safely set splitter distances after layout
            vertical.SplitterDistance = 450; // or compute/clamp based on vertical.Height
            bottom.SplitterDistance = 200; // or compute/clamp based on bottom.Width
        };
    }

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

    private DeviceSequences Snapshot() => new() { Channels = _device.Channels.Select(c => c.Clone(c.ChannelNumber)).ToList() };

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

    private void OpenFirmwareWizard()
    {
        if (_writing || _search is not null || !CommitGrid()) return;
        string? port = _connection?.PortName;
        _ledPreview.Pause(); DisconnectBoard();
        using var wizard = new FirmwareUpdateWizard(port);
        wizard.ShowDialog(this);
    }

    private void QueueOnlineWrite()
    {
        if (_connection is null || !_live.Checked || _loading) return;
        _writeTimer.Stop(); _writeTimer.Start();
    }

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

    private void InitializeComponent()
    {

    }

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

