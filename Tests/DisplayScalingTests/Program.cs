using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using OBLSequenceStudio;
using OBLSequenceStudio.Controls;
using OBLSequenceStudio.Localization;

namespace DisplayScalingTests;

/// <summary>Exercises WinForms layout with synthetic DPI notifications and physical viewport sizes.</summary>
internal static class Program
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> _failures = new();
    private static readonly List<object> _results = new();
    private static string _output = "";
    private static int _checks;

    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.SetDefaultFont(new Font("Segoe UI", 9.5f));
        _output = Path.GetFullPath(args.FirstOrDefault() ?? "artifacts/display-scaling");
        Directory.CreateDirectory(_output);

        foreach (Size display in new[] { new Size(1920, 1080), new Size(3840, 2160) })
        {
            foreach (int dpi in new[] { 96, 120, 144, 192 })
            {
                VerifyDisplay(display, dpi);
            }
        }
        VerifyDpiTransitions();
        File.WriteAllText(Path.Combine(_output, "results.json"),
            JsonSerializer.Serialize(new { Method = "Synthetic WM_DPICHANGED; not physical monitor certification", Checks = _checks, Results = _results, Failures = _failures },
                new JsonSerializerOptions { WriteIndented = true }));
        foreach (string failure in _failures)
        {
            Console.WriteLine("FAIL: " + failure);
        }
        Console.WriteLine($"Completed 8 display cases; {_checks} assertions; {_failures.Count} failures.");
        return _failures.Count == 0 ? 0 : 1;
    }

    private static void VerifyDisplay(Size display, int dpi)
    {
        string name = $"{display.Width}x{display.Height}-{dpi * 100 / 96}percent";
        using var form = new MainForm();
        form.WindowState = FormWindowState.Normal;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-20000, -20000);
        form.Show();
        Application.DoEvents();
        SetDpi(form, dpi);
        // Reserve physical space for the taskbar, title bar and window border.
        form.ClientSize = new Size(display.Width - 16, display.Height - 80 * dpi / 96);
        form.PerformLayout();
        Application.DoEvents();
        foreach (Localizer.Language language in Localizer.Languages)
        {
            Localizer.SetLanguage(language.Code);
            typeof(MainForm).GetMethod("ApplyLanguage", PrivateInstance)!.Invoke(form, null);
            form.PerformLayout();
            Application.DoEvents();
            VerifyLayout(form, dpi, name + "-" + language.Code);
        }
        Localizer.SetLanguage("it");
        typeof(MainForm).GetMethod("ApplyLanguage", PrivateInstance)!.Invoke(form, null);
        var timeline = GetField<TimelineControl>(form, "_timeline");
        int selected = 0;
        timeline.ChannelSelected += channel => selected = channel;
        typeof(TimelineControl).GetMethod("OnMouseDown", PrivateInstance)!.Invoke(timeline,
            new object[] { new MouseEventArgs(MouseButtons.Left, 1, 20 * dpi / 96, 57 * dpi / 96, 0) });
        Check(selected == 1, name + ": timeline hit testing must select channel 1.");
        int expectedHeight = (40 + 28 * 35 + 28) * dpi / 96;
        Check(Math.Abs(timeline.Height - expectedHeight) <= 1,
            name + $": timeline height {timeline.Height}, expected {expectedHeight}.");
        VerifyTimelineDrag(form, dpi, name);
        using var image = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
        image.Save(Path.Combine(_output, name + ".png"));
        _results.Add(new { Name = name, Dpi = form.DeviceDpi, Client = form.ClientSize.ToString(),
            TimelineHeight = timeline.Height, Workspace = ((Panel)form.Controls[0]).Controls[0].Size.ToString(), HorizontalScroll = ((Panel)form.Controls[0]).HorizontalScroll.Visible, VerticalScroll = ((Panel)form.Controls[0]).VerticalScroll.Visible });
        Console.WriteLine($"{name}: DeviceDpi={form.DeviceDpi}, timeline={timeline.Height}, failures={_failures.Count}");
        var viewport = (Panel)form.Controls[0];
        var status = GetField<Control>(form, "statusBar");
        viewport.ScrollControlIntoView(status);
        Application.DoEvents();
        Rectangle statusBounds = viewport.RectangleToClient(status.RectangleToScreen(status.ClientRectangle));
        Check(statusBounds.Bottom <= viewport.ClientSize.Height + 2, name + ": status bar cannot be scrolled into view.");
        if (dpi == 192 && display.Width == 1920)
        {
            using var scrolledImage = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(scrolledImage, new Rectangle(Point.Empty, scrolledImage.Size));
            scrolledImage.Save(Path.Combine(_output, name + "-scrolled.png"));
        }
        VerifyFirmwareWizard(display, dpi, name);
        form.Close();
    }

    private static void VerifyLayout(MainForm form, int dpi, string name)
    {
        var viewport = (Panel)form.Controls[0];
        Control content = viewport.Controls[0];
        Check(content.Width >= 1150 * dpi / 96 && content.Height >= 760 * dpi / 96,
            name + ": minimum workspace must remain reachable by scrolling.");
        foreach (string key in new[] { "toolbar", "hardware" })
        {
            var panel = GetField<FlowLayoutPanel>(form, key);
            foreach (Control child in panel.Controls)
            {
                Check(child.Bottom + child.Margin.Bottom <= panel.ClientSize.Height + 2,
                    name + $": {key}/{child.Name} clipped vertically ({child.Bottom}/{panel.ClientSize.Height}).");
            }
        }
        var stepToolbar = GetField<FlowLayoutPanel>(form, "rightTop");
        Check(!stepToolbar.VerticalScroll.Visible, name + ": step toolbar has an unnecessary vertical scrollbar.");
        Check(!stepToolbar.HorizontalScroll.Visible, name + ": step toolbar has an unnecessary horizontal scrollbar.");
        foreach (Control control in stepToolbar.Controls)
        {
            Check(control.Bottom + control.Margin.Bottom <= stepToolbar.ClientSize.Height,
                name + ": step toolbar control is clipped vertically: " + control.Name);
        }
        var button = GetField<Button>(form, "_saveButton");
        Check(button.Image?.Width == 20 * dpi / 96, name + ": action icon has incorrect DPI size.");
        Check(form.DeviceDpi == dpi, name + ": synthetic DPI not applied.");
        Check(viewport.DisplayRectangle.Width >= content.Width && viewport.DisplayRectangle.Height >= content.Height,
            name + ": workspace extends beyond the scrollable area.");
        var grid = GetField<DataGridView>(form, "_grid");
        Check(grid.Columns.Count == 7, name + ": editor must retain exactly seven columns.");
        Check((grid.DefaultCellStyle.Font?.Size ?? grid.Font.Size) == grid.Font.Size, name + ": grid font does not follow DPI scaling.");
        Check(grid.Columns[6].MinimumWidth == 180 * dpi / 96, name + ": grid column minimum width does not follow DPI.");
        Check(grid.ClientSize.Width > 0 && grid.ClientSize.Height > 0, name + ": grid has no usable area.");
    }

    private static void VerifyTimelineDrag(MainForm form, int dpi, string name)
    {
        var timeline = GetField<TimelineControl>(form, "_timeline");
        MethodInfo mapTime = typeof(TimelineControl).GetMethod("X", PrivateInstance)!;
        int endpoint = (int)mapTime.Invoke(timeline, new object[] { 1000.0 })!;
        int destination = (int)mapTime.Invoke(timeline, new object[] { 1200.0 })!;
        (int Channel, int Step, long Time, int Value)? moved = null;
        void RecordMove(int channel, int step, long time, int value) => moved = (channel, step, time, value);
        timeline.PointMoved += RecordMove;
        typeof(TimelineControl).GetMethod("OnMouseDown", PrivateInstance)!.Invoke(timeline,
            new object[] { new MouseEventArgs(MouseButtons.Left, 1, endpoint * dpi / 96, 45 * dpi / 96, 0) });
        typeof(TimelineControl).GetMethod("OnMouseMove", PrivateInstance)!.Invoke(timeline,
            new object[] { new MouseEventArgs(MouseButtons.Left, 0, destination * dpi / 96,
                (int)Math.Round(57.5 * dpi / 96), 0) });
        typeof(TimelineControl).GetMethod("OnMouseUp", PrivateInstance)!.Invoke(timeline,
            new object[] { new MouseEventArgs(MouseButtons.Left, 1, destination * dpi / 96, 58 * dpi / 96, 0) });
        timeline.PointMoved -= RecordMove;
        Check(moved is { Channel: 1, Step: 0 } && Math.Abs(moved.Value.Time - 1200) <= 5
            && Math.Abs(moved.Value.Value - 50) <= 3, name + ": timeline endpoint drag does not match rendered coordinates.");
        // The form's normal event handlers edited this temporary model; suppress its save prompt.
        typeof(MainForm).GetField("_dirty", PrivateInstance)!.SetValue(form, false);
    }
    private static void VerifyFirmwareWizard(Size display, int dpi, string name)
    {
        foreach (Localizer.Language language in Localizer.Languages)
        {
            Localizer.SetLanguage(language.Code);
            using var wizard = new FirmwareUpdateWizard();
            wizard.StartPosition = FormStartPosition.Manual;
            wizard.Location = new Point(-20000, -20000);
            wizard.Show();
            Application.DoEvents();
            SetDpi(wizard, dpi);
            wizard.ClientSize = new Size(Math.Min(804 * dpi / 96, display.Width - 16),
                Math.Min(581 * dpi / 96, display.Height - 80 * dpi / 96));
            Application.DoEvents();
            Check(wizard.ClientSize.Width <= display.Width && wizard.ClientSize.Height <= display.Height, name + ": firmware window exceeds display.");
            // The review page depends on a physical bootloader connection; never start it here.
            foreach (int page in new[] { 0, 1, 3 })
            {
                typeof(FirmwareUpdateWizard).GetField("_page", PrivateInstance)!.SetValue(wizard, page);
                typeof(FirmwareUpdateWizard).GetMethod("ShowPage", PrivateInstance)!.Invoke(wizard, null);
                Application.DoEvents();
                var viewport = (Panel)wizard.Controls[0];
                var navigation = GetField<Control>(wizard, "navigationButtons");
                viewport.ScrollControlIntoView(navigation);
                Application.DoEvents();
                Rectangle bounds = viewport.RectangleToClient(navigation.RectangleToScreen(navigation.ClientRectangle));
                Check(bounds.Bottom <= viewport.ClientSize.Height + 2,
                    name + "-" + language.Code + $": firmware page {page} navigation is unreachable.");
                foreach (Control button in navigation.Controls)
                {
                    if (button.Visible)
                    {
                        Check(button.Top >= 0 && button.Bottom <= navigation.ClientSize.Height + 2,
                            name + "-" + language.Code + $": firmware page {page} button clipped vertically.");
                    }
                }
                if (page == 1 && language.Code == "it")
                {
                    using var bitmap = new Bitmap(wizard.Width, wizard.Height);
                    wizard.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(Path.Combine(_output, name + "-firmware.png"));
                }
            }
            wizard.Close();
        }
        Localizer.SetLanguage("it");
    }

    private static void VerifyDpiTransitions()
    {
        using var form = new MainForm();
        form.WindowState = FormWindowState.Normal;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-20000, -20000);
        form.Show();
        Application.DoEvents();
        foreach (int dpi in new[] { 120, 144, 192, 144, 120, 96 })
        {
            SetDpi(form, dpi);
            form.ClientSize = new Size(1904, 1000);
            Application.DoEvents();
            VerifyLayout(form, dpi, "DPI round trip " + dpi);
            var timeline = GetField<TimelineControl>(form, "_timeline");
            int channel = 0;
            void SelectChannel(int selected) => channel = selected;
            timeline.ChannelSelected += SelectChannel;
            typeof(TimelineControl).GetMethod("OnMouseDown", PrivateInstance)!.Invoke(timeline,
                new object[] { new MouseEventArgs(MouseButtons.Left, 1, 20 * dpi / 96, 92 * dpi / 96, 0) });
            timeline.ChannelSelected -= SelectChannel;
            Check(channel == 2, "DPI round trip: channel hit testing drifted at " + dpi);
        }
        using var largerFont = new Font(form.Font.FontFamily, form.Font.Size * 1.5f);
        form.Font = largerFont;
        Application.DoEvents();
        VerifyLayout(form, 96, "150 percent application font");
        Console.WriteLine("DPI round trip and enlarged application font completed.");
        form.Close();
    }
    private static T GetField<T>(object target, string name) =>
        (T)target.GetType().GetField(name, PrivateInstance)!.GetValue(target)!;

    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition)
        {
            _failures.Add(message);
        }
    }

    private static void SetDpi(Form form, int dpi)
    {
        // Windows does not change the physical monitor DPI in this test. Set the child
        // cached DPI and deliver the parent's real message path to exercise WinForms scaling.
        var dpiProperty = typeof(Control).GetProperty("DeviceDpiInternal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        Control[] descendants = Descendants(form).ToArray();
        foreach (Control control in descendants)
        {
            dpiProperty.SetValue(control, dpi);
        }
        var rectangle = new NativeRectangle { Left = -20000, Top = -20000, Right = -18500, Bottom = -19000 };
        nint pointer = Marshal.AllocHGlobal(Marshal.SizeOf<NativeRectangle>());
        try
        {
            Marshal.StructureToPtr(rectangle, pointer, false);
            SendMessage(form.Handle, 0x02E0, (nint)(dpi | dpi << 16), pointer);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
        foreach (Control control in descendants)
        {
            typeof(Control).GetMethod("OnDpiChangedAfterParent", PrivateInstance)!
                .Invoke(control, new object[] { EventArgs.Empty });
        }
        Application.DoEvents();
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (Control descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
}
