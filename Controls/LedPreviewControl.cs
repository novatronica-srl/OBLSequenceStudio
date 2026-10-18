using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using OBLSequenceStudio.Localization;
using OBLSequenceStudio.Models;
using OBLSequenceStudio.Services;

namespace OBLSequenceStudio.Controls;

/// <summary>Displays channel LED colors with either local simulated brightness or live hardware PWM readings.</summary>
public sealed class LedPreviewControl : UserControl
{
    private readonly UiHints _hints = new();
    private readonly LedPreviewEngine _engine = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 33 };
    private readonly Stopwatch _clock = new();
    private readonly Button _play = new() { AutoSize = true, ForeColor = SystemColors.ControlText }, _stop = new() { AutoSize = true, ForeColor = SystemColors.ControlText };
    private readonly DarkCheckBox _firmware = new() { AutoSize = true, Checked = false, Enabled = false };
    private readonly Label _title = new() { AutoSize = true }, _time = new() { AutoSize = true };
    private readonly LedCanvas _canvas = new() { Dock = DockStyle.Fill };
    private readonly Button _color = new() { AutoSize = true, MinimumSize = new Size(100, 30) };
    private readonly Label _colorChannel = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private DeviceSequences? _device;
    private long _position;
    private int[] _hardwarePwm = new int[28];
    /// <summary>Enables hardware preview when connected and clears stale PWM readings when disconnected.</summary>
    public void SetOnline(bool online) { _firmware.Enabled = online; _firmware.Checked = online; if (!online) _hardwarePwm = new int[28]; RenderFrame(); }
    /// <summary>Copies and clamps channel PWM percentages before refreshing the displayed frame.</summary>
    public void SetHardwareValues(int[] pwm) { _hardwarePwm = pwm.Select(v => Math.Clamp(v, 0, 100)).ToArray(); RenderFrame(); }
    /// <summary>Reports local playback position in milliseconds after timer ticks or seeking.</summary>
    public event Action<long>? TimeChanged;
    /// <summary>Reports a clicked LED using its one-based channel number.</summary>
    public event Action<int>? ChannelSelected;
    /// <summary>Reports a changed preview color so the editor can mark its configuration dirty.</summary>
    public event Action? ConfigurationChanged;
    /// <summary>Current local playback position in milliseconds, including time since the most recent resume.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public long Position => _position + _clock.ElapsedMilliseconds;
    /// <summary>Whether the local simulation clock is advancing.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsPlaying => _clock.IsRunning;

    /// <summary>Creates playback controls and the LED canvas and attaches preview interaction handlers.</summary>
    public LedPreviewControl()
    {
        ButtonVisuals.Configure(_play, "Play");
        ButtonVisuals.Configure(_stop, "Stop");
        ButtonVisuals.Configure(_color, "Color");
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(6) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        foreach (int height in new[] { 24, 36, 28, 36 }) header.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        header.Controls.Add(_title, 0, 0);
        var playback = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        playback.Controls.Add(_play); playback.Controls.Add(_stop); playback.Controls.Add(_time);
        header.Controls.Add(playback, 0, 1);
        header.Controls.Add(_firmware, 0, 2);
        var colors = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
        colors.Controls.Add(_colorChannel); colors.Controls.Add(_color);
        header.Controls.Add(colors, 0, 3);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.Dock = DockStyle.Fill; header.Margin = Padding.Empty;
        _canvas.Margin = Padding.Empty;
        layout.Controls.Add(header, 0, 0); layout.Controls.Add(_canvas, 0, 1);
        Controls.Add(layout);
        _play.Click += (_, _) =>
        {
            if (IsPlaying) Pause(); else Play();
        };
        _stop.Click += (_, _) => Seek(0);
        _firmware.CheckedChanged += (_, _) => { if (_firmware.Checked) Pause(); _play.Enabled = _stop.Enabled = !_firmware.Checked; _engine.Reset(); RenderFrame(); };
        _timer.Tick += (_, _) => { RenderFrame(); TimeChanged?.Invoke(Position); };
        _canvas.ChannelSelected += channel => ChannelSelected?.Invoke(channel);
        _color.Click += (_, _) =>
        {
            if (_device is null) return;
            using var picker = new ColorDialog { Color = GetLedColor(_canvas.SelectedChannel), FullOpen = true };
            if (picker.ShowDialog(this) == DialogResult.OK) SetLedColor(_canvas.SelectedChannel, picker.Color);
        };
        ApplyLanguage();
        _hints.Set(_title, "LedPreview");
        _hints.Set(_time, "PreviewTime");
        _hints.Set(_firmware, "HardwarePwm");
        _hints.Set(_color, "ChooseLedColor");
        _hints.Set(_colorChannel, "LedChannel");
        _hints.Set(_canvas, "LedPreview");
        _canvas.MouseMove += (_, e) =>
        {
            if (_canvas.Width <= 0 || _canvas.Height <= 0) return;
            int index = Math.Clamp(e.Y * 4 / _canvas.Height, 0, 3) * 7 + Math.Clamp(e.X * 7 / _canvas.Width, 0, 6);
            _hints.SetText(_canvas, Localizer.Text("TipLedChannelValue", index + 1, _canvas.Values[index]));
        };
    }

    /// <summary>Binds a configuration, resetting playback only when the configuration instance changes.</summary>
    public void SetDevice(DeviceSequences device)
    {
        if (!ReferenceEquals(device, _device)) { _device = device; Seek(0); }
        RenderFrame();
    }
    /// <summary>Highlights a one-based channel and updates its preview-color action.</summary>
    public void SelectChannel(int channel) { _canvas.SelectedChannel = channel; UpdateColorButton(); _canvas.Invalidate(); }
    /// <summary>Resumes local simulation when a device is bound and hardware PWM preview is not selected.</summary>
    public void Play() { if (_device is null || IsPlaying || _firmware.Checked) return; _clock.Start(); _timer.Start(); RenderFrame(); ApplyLanguage(); }
    /// <summary>Stops local simulation while preserving its accumulated playback position.</summary>
    public void Pause() { _position = Position; _clock.Reset(); _timer.Stop(); RenderFrame(); ApplyLanguage(); }
    /// <summary>Resolves a channel preview color or the default amber color when no matching channel is bound.</summary>
    public Color GetLedColor(int channel)
    {
        string value = _device?.Channels.FirstOrDefault(c => c.ChannelNumber == channel)?.LedColor ?? "#FFB328";
        return ColorTranslator.FromHtml(value);
    }
    /// <summary>Updates a channel preview-only RGB color and notifies the editor when its value changes.</summary>
    public void SetLedColor(int channel, Color color)
    {
        var sequence = _device?.Channels.FirstOrDefault(c => c.ChannelNumber == channel);
        if (sequence is null) return;
        string value = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        if (string.Equals(sequence.LedColor, value, StringComparison.OrdinalIgnoreCase)) return;
        sequence.LedColor = value;
        RenderFrame(); UpdateColorButton(); ConfigurationChanged?.Invoke();
    }
    private void UpdateColorButton()
    {
        _colorChannel.Text = Localizer.Text("ChannelShort", _canvas.SelectedChannel);
        _color.Text = Localizer.Text("ChooseLedColor");
        var color = GetLedColor(_canvas.SelectedChannel);
        _color.BackColor = color;
        _color.ForeColor = color.GetBrightness() > 0.5f ? Color.Black : Color.White;
        _color.UseVisualStyleBackColor = false;
        _color.FlatAppearance.MouseOverBackColor = color;
        _color.FlatAppearance.MouseDownBackColor = color;
        ButtonVisuals.SetIcon(_color, "Color");
    }
    /// <summary>Pauses playback and clears fire history before evaluating the requested position.</summary>
    /// <param name="position">Playback time in milliseconds; negative values are clamped to zero.</param>
    public void Seek(long position)
    {
        _timer.Stop(); _clock.Reset(); _position = Math.Max(0, position); _engine.Reset();
        RenderFrame(); ApplyLanguage(); TimeChanged?.Invoke(Position);
    }
    /// <summary>Refreshes preview captions, tooltips and play/pause imagery for the current language and playback state.</summary>
    public void ApplyLanguage()
    {
        _hints.Refresh();
        _hints.Set(_play, IsPlaying ? "PausePreview" : "PlayPreview");
        _hints.Set(_stop, "StopPreview");
        _title.Text = Localizer.Text("LedPreview");
        _play.Text = Localizer.Text(_clock.IsRunning ? "PausePreview" : "PlayPreview");
        ButtonVisuals.SetIcon(_play, _clock.IsRunning ? "Pause" : "Play");
        _stop.Text = Localizer.Text("StopPreview");
        _firmware.Text = Localizer.Text("HardwarePwm");
        _time.Text = _firmware.Checked ? Localizer.Text("HardwareLive") : Localizer.Text("PreviewTime", Position / 1000.0);
        _canvas.Invalidate();
        UpdateColorButton();
    }
    /// <summary>Selects real PWM or simulated values and redraws the canvas without changing hardware playback.</summary>
    private void RenderFrame()
    {
        _canvas.Values = _firmware.Checked ? _hardwarePwm : _engine.Evaluate(_device, Position);
        _canvas.Colors = Enumerable.Range(1, 28).Select(GetLedColor).ToArray();
        _time.Text = _firmware.Checked ? Localizer.Text("HardwareLive") : Localizer.Text("PreviewTime", Position / 1000.0);
        _canvas.Invalidate();
    }
    /// <summary>Stops the local clock and releases timer and tooltip resources.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _hints.Dispose(); _timer.Dispose(); _clock.Stop(); }
        base.Dispose(disposing);
    }

    /// <summary>Paints preview LEDs and performs pointer hit testing for channel selection.</summary>
    private sealed class LedCanvas : Control
    {
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int[] Values { get; set; } = new int[28];
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectedChannel { get; set; } = 1;
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color[] Colors { get; set; } = Enumerable.Repeat(Color.Orange, 28).ToArray();
        public event Action<int>? ChannelSelected;
        public LedCanvas() { DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float width = ClientSize.Width / 7f, height = ClientSize.Height / 4f;
            for (int i = 0; i < 28; i++)
            {
                float x = i % 7 * width, y = i / 7 * height;
                float size = Math.Max(4, Math.Min(28, Math.Min(width - 10, height - 28)));
                float intensity = Values[i] / 100f;
                var circle = new RectangleF(x + (width - size) / 2, y + 3, size, size);
                using var brush = new SolidBrush(Color.FromArgb((int)(Colors[i].R * intensity), (int)(Colors[i].G * intensity), (int)(Colors[i].B * intensity)));
                e.Graphics.FillEllipse(brush, circle);
                using var border = new Pen(i + 1 == SelectedChannel ? Color.Cyan : Color.FromArgb(80, 90, 100), i + 1 == SelectedChannel ? 2 : 1);
                e.Graphics.DrawEllipse(border, circle);
                string label = Localizer.Text("ChannelShort", i + 1) + $"\n{Values[i]}%";
                using var text = new SolidBrush(ForeColor);
                using var format = new StringFormat { Alignment = StringAlignment.Center };
                e.Graphics.DrawString(label, Font, text, new RectangleF(x, y + size + 4, width, Math.Max(1, height - size - 4)), format);
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || Width <= 0 || Height <= 0) return;
            int column = Math.Clamp(e.X * 7 / Width, 0, 6), row = Math.Clamp(e.Y * 4 / Height, 0, 3);
            ChannelSelected?.Invoke(row * 7 + column + 1);
        }
    }
}

