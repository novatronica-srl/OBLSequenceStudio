using System.Runtime.InteropServices;

namespace OBLSequenceStudio.Controls;

internal static class DarkTitleBar
{
    // https://learn.microsoft.com/windows/apps/desktop/modernize/apply-windows-themes
    private const int UseImmersiveDarkMode = 20;
    private const int CaptionColor = 35;
    private const int TextColor = 36;

    public static void Attach(Form form)
    {
        form.HandleCreated += Apply;
        if (form.IsHandleCreated) Apply(form, EventArgs.Empty);
    }

    private static void Apply(object? sender, EventArgs e)
    {
        if (sender is not Form form || !OperatingSystem.IsWindows() || SystemInformation.HighContrast) return;
        int enabled = 1;
        // Unsupported attributes simply return an HRESULT; retain the native frame.
        _ = DwmSetWindowAttribute(form.Handle, UseImmersiveDarkMode, ref enabled, sizeof(int));
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            int caption = ColorRef(Color.FromArgb(16, 21, 32));
            int text = ColorRef(Color.WhiteSmoke);
            _ = DwmSetWindowAttribute(form.Handle, CaptionColor, ref caption, sizeof(int));
            _ = DwmSetWindowAttribute(form.Handle, TextColor, ref text, sizeof(int));
        }
    }

    private static int ColorRef(Color color) => color.R | color.G << 8 | color.B << 16;

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
