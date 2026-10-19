using System.ComponentModel;
using System.Drawing.Drawing2D;
using OBLSequenceStudio.Localization;
using OBLSequenceStudio.Models;

namespace OBLSequenceStudio.Controls;

/// <summary>Draws 28 channel timelines and translates pointer gestures into channel, step and endpoint edits.</summary>
public sealed class TimelineControl : Control
{
    /// <summary>Runtime configuration to draw; excluded from designer serialization.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DeviceSequences? Device { get; set; }

    /// <summary>One-based channel highlighted in the timeline.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedChannel { get; set; } = 1;

    /// <summary>Playback cursor position in milliseconds from the start of the timeline.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public long CursorMs { get; set; }

    /// <summary>Requests a seek to the emitted timeline position in milliseconds.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public event Action<long>? CursorChanged;

    /// <summary>Reports selection using a one-based channel number.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public event Action<int>? ChannelSelected;
    /// <summary>Reports a dragged endpoint as channel number, zero-based step index, absolute milliseconds and brightness percentage.</summary>
    public event Action<int, int, long, int>? PointMoved;
    /// <summary>Reports a selected block as a one-based channel number and zero-based step index.</summary>
    public event Action<int, int>? StepSelected;
    private int _dragChannel = -1, _dragStep = -1;
    private long _dragMaxTime;
    /// <summary>Enables buffered timeline painting and redraw when the control is resized.</summary>
    public TimelineControl()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(18, 24, 37);
        ForeColor = Color.FromArgb(217, 226, 240);
        SetStyle(ControlStyles.ResizeRedraw, true);
    }
    private const int Header = 40, Left = 80, RowHeight = 35, Bottom = 28;
    private float DpiScale => DeviceDpi / 96f;
    private int DrawingWidth => (int)Math.Floor(ClientSize.Width / DpiScale);

    /// <summary>Returns the scaled height needed to display every channel and the time header.</summary>
    public override Size GetPreferredSize(Size proposedSize)
    {
        return new Size(proposedSize.Width, LogicalToDeviceUnits(Header + (Device?.Channels.Count ?? 28) * RowHeight + Bottom));
    }

    private MouseEventArgs ToLogicalCoordinates(MouseEventArgs e)
    {
        return new MouseEventArgs(e.Button, e.Clicks,
            (int)Math.Round(e.X / DpiScale), (int)Math.Round(e.Y / DpiScale), e.Delta);
    }
    private long MaxTime => _dragStep >= 0 ? _dragMaxTime : Math.Max(1000, Device?.Channels.Select(c => c.TotalDuration).DefaultIfEmpty(1000).Max() ?? 1000);
    private double Scale => Math.Max(1, DrawingWidth - Left - 24) / (double)MaxTime;
    private int X(double ms) => Left + (int)Math.Round(ms * Scale);
    private long TimeAt(int x) => Math.Clamp((long)Math.Round((x - Left) / Scale), 0, MaxTime);
    private static Color ChannelColor(int index) => index switch
    {
        0 => Color.FromArgb(59, 200, 240),
        _ => Color.FromArgb(89 + (index * 41) % 144, 120 + (index * 57) % 116, 145 + (index * 29) % 108)
    };
    /// <summary>Draws the time scale, channel rows, effect blocks and playback cursor from runtime data.</summary>
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.ScaleTransform(DpiScale, DpiScale);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);
        if (Device is null) return;
        var channels = Device.Channels;
        int chartBottom = Header + channels.Count * RowHeight;
        using var gridPen = new Pen(Color.FromArgb(49, 62, 80));
        using var textBrush = new SolidBrush(ForeColor);
        using var dimBrush = new SolidBrush(Color.FromArgb(146, 161, 182));
        using var labelFont = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var smallFont = new Font("Segoe UI", 32f / 3f, FontStyle.Regular, GraphicsUnit.Pixel);
        long max = MaxTime;
        long tick = NiceTick(max / 10.0);
        for (long time = 0; time <= max; time += tick)
        {
            int px = X(time);
            g.DrawLine(gridPen, px, Header - 3, px, chartBottom);
            g.DrawString(time >= 1000 ? $"{time / 1000.0:0.##} s" : $"{time} ms", smallFont, dimBrush, px + 3, 7);
            if (time > max - tick) break;
        }
        for (int i = 0; i < channels.Count; i++)
        {
            var channel = channels[i];
            int top = Header + i * RowHeight;
            int bot = top + RowHeight;
            if (channel.ChannelNumber == SelectedChannel)
                using (var selBrush = new SolidBrush(Color.FromArgb(35, 83, 117)))
                    g.FillRectangle(selBrush, 0, top, DrawingWidth, RowHeight);
            g.DrawLine(gridPen, 0, bot, DrawingWidth, bot);
            g.DrawString(Localizer.Text("ChannelShort", channel.ChannelNumber), labelFont, textBrush, 13, top + 9);
            double t = 0;
            var color = channel.ChannelNumber == SelectedChannel ? Color.FromArgb(76, 222, 255) : ChannelColor(i);
            using var linePen = new Pen(color, channel.ChannelNumber == SelectedChannel ? 2.6f : 1.7f);
            float MapY(double value) => (float)(top + 5 + (100 - value) * (RowHeight - 10) / 100.0);
            foreach (var s in channel.Steps)
            {
                if (s.Duration < 10) continue;
                int x0 = X(t), x1 = X(t + s.Duration);
                using (var blockBrush = new SolidBrush(Color.FromArgb(28, color)))
                    g.FillRectangle(blockBrush, x0, top + 1, Math.Max(1, x1 - x0), RowHeight - 2);
                if (s.Mode == StepMode.Ramp)
                    g.DrawLine(linePen, x0, MapY(s.Start), x1, MapY(s.End));
                else
                {
                    int samples = Math.Clamp(x1 - x0, 2, 500);
                    var points = Enumerable.Range(0, samples + 1).Select(n => new PointF(
                        x0 + (x1 - x0) * n / (float)samples, MapY(s.ValueAt(s.Duration * n / (double)samples)))).ToArray();
                    g.DrawLines(linePen, points);
                }
                if (x1 - x0 > 55)
                {
                    var bounds = new RectangleF(x0 + 3, top, x1 - x0 - 6, 14);
                    using var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                    g.DrawString(Localizer.Text("Mode" + s.Mode), smallFont, textBrush, bounds, format);
                }
                t += s.Duration;
                using (var pointBrush = new SolidBrush(color))
                    g.FillEllipse(pointBrush, X(t) - 4, MapY(s.ValueAt(s.Duration)) - 4, 8, 8);
                // La discontinuità con il prossimo step viene evidenziata in verticale.
                if (channel.Steps.IndexOf(s) + 1 < channel.Steps.Count)
                {
                    var next = channel.Steps[channel.Steps.IndexOf(s) + 1];
                    if (s.ValueAt(s.Duration) != next.ValueAt(0))
                        using (var jumpPen = new Pen(Color.FromArgb(255, 183, 97), 1.5f) { DashStyle = DashStyle.Dash })
                            g.DrawLine(jumpPen, X(t), MapY(s.ValueAt(s.Duration)), X(t), MapY(next.ValueAt(0)));
                }
            }
        }
        int cursorX = X(CursorMs);
        using var cursorPen = new Pen(Color.FromArgb(255, 196, 93), 2);
        g.DrawLine(cursorPen, cursorX, Header - 2, cursorX, chartBottom);
        g.FillPolygon(Brushes.Orange, new[] { new Point(cursorX - 6, Header - 5), new Point(cursorX + 6, Header - 5), new Point(cursorX, Header + 2) });
    }
    private static long NiceTick(double raw)
    {
        if (raw <= 0) return 1000;
        double p = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double n = raw / p;
        return Math.Max(1, (long)((n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10) * p));
    }
    /// <summary>Begins endpoint dragging or reports a channel, block or cursor selection based on hit testing.</summary>
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        e = ToLogicalCoordinates(e);
        if (e.Button != MouseButtons.Left) return;
        if (Device is not null && e.Y >= Header && e.Y < Header + Device.Channels.Count * RowHeight)
        {
            int row = (e.Y - Header) / RowHeight;
            var channel = Device.Channels[row];
            long time = 0;
            for (int i = 0; i < channel.Steps.Count; i++)
            {
                var step = channel.Steps[i];
                time += step.Duration;
                float y = (float)(Header + row * RowHeight + 5 + (100 - step.ValueAt(step.Duration)) * (RowHeight - 10) / 100.0);
                if (Math.Abs(e.X - X(time)) > 7 || Math.Abs(e.Y - y) > 7) continue;
                _dragMaxTime = MaxTime;
                _dragChannel = row;
                _dragStep = i;
                Capture = true;
                SelectedChannel = channel.ChannelNumber;
                ChannelSelected?.Invoke(SelectedChannel);
                StepSelected?.Invoke(SelectedChannel, i);
                Invalidate();
                return;
            }
            SelectedChannel = channel.ChannelNumber;
            ChannelSelected?.Invoke(SelectedChannel);
            long clicked = TimeAt(e.X);
            long end = 0;
            for (int i = 0; e.X >= Left && i < channel.Steps.Count; i++)
            {
                end += channel.Steps[i].Duration;
                if (clicked < end) { StepSelected?.Invoke(SelectedChannel, i); break; }
            }
        }
        if (Device is not null && e.Y >= Header && e.Y < Header + (Device.Channels.Count * RowHeight) && e.X < Left)
        {
            int rowIndex = (e.Y - Header) / RowHeight;
            if (rowIndex >= 0 && rowIndex < Device.Channels.Count)
            {
                SelectedChannel = Device.Channels[rowIndex].ChannelNumber;
                ChannelSelected?.Invoke(SelectedChannel);
            }
        }
        else if (e.X >= Left)
        {
            CursorMs = TimeAt(e.X);
            CursorChanged?.Invoke(CursorMs);
        }
        Invalidate();
    }
    /// <summary>Reports endpoint edits while preserving the time scale captured at the start of the drag.</summary>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        e = ToLogicalCoordinates(e);
        if (_dragStep >= 0 && Device is not null && e.Button == MouseButtons.Left)
        {
            int top = Header + _dragChannel * RowHeight;
            int value = (int)Math.Clamp(Math.Round(100 - (e.Y - top - 5) * 100.0 / (RowHeight - 10)), 0, 100);
            long time = Math.Max(0, (long)Math.Round((e.X - Left) / Scale));
            PointMoved?.Invoke(Device.Channels[_dragChannel].ChannelNumber, _dragStep, time, value);
            Invalidate();
            return;
        }
        if (e.Button == MouseButtons.Left && e.X >= Left)
        {
            CursorMs = TimeAt(e.X);
            CursorChanged?.Invoke(CursorMs);
            Invalidate();
        }
    }
    /// <summary>Ends the active pointer gesture and releases mouse capture.</summary>
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragStep = -1;
        _dragChannel = -1;
        Capture = false;
        Invalidate();
    }

    /// <summary>Clears drag state if another window or control takes mouse capture.</summary>
    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        if (!Capture) { _dragStep = -1; _dragChannel = -1; Invalidate(); }
    }
}
