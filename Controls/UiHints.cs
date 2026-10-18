using OBLSequenceStudio.Localization;

namespace OBLSequenceStudio.Controls;

internal sealed class UiHints : IDisposable
{
    private readonly ToolTip _tips = new() { ShowAlways = true, InitialDelay = 450, ReshowDelay = 150, AutoPopDelay = 15000 };
    private readonly Dictionary<Control, string> _keys = new();
    private readonly HashSet<Control> _parents = new();
    private Control? _disabledHover;
    public static string Text(string key)
    {
        if (key == "TimelineHelp") key = "Timeline";
        return Localizer.Text(Localizer.Keys.Contains("Tip" + key) ? "Tip" + key : "TipArea");
    }
    public void Set(Control control, string key)
    {
        if (!_keys.ContainsKey(control))
        {
            control.ParentChanged += ParentChanged;
            control.Disposed += ControlDisposed;
        }
        _keys[control] = key;
        SetText(control, Text(key));
        WatchParent(control.Parent);
        // NumericUpDown contains separate edit and arrow windows.
        if (control is NumericUpDown)
            foreach (Control child in control.Controls) Set(child, key);
    }
    public void SetText(Control control, string text)
    {
        if (_tips.GetToolTip(control) != text) _tips.SetToolTip(control, text);
    }
    public void Refresh()
    {
        foreach (var (control, key) in _keys) SetText(control, Text(key));
    }
    private void ParentChanged(object? sender, EventArgs e) => WatchParent((sender as Control)?.Parent);
    private void WatchParent(Control? parent)
    {
        if (parent is null || !_parents.Add(parent)) return;
        parent.MouseMove += ParentMouseMove;
        parent.MouseLeave += ParentMouseLeave;
        parent.Disposed += ParentDisposed;
    }
    private void ParentMouseMove(object? sender, MouseEventArgs e)
    {
        if (sender is not Control parent) return;
        var target = parent.Controls.Cast<Control>().FirstOrDefault(c => c.Visible && !c.Enabled && c.Bounds.Contains(e.Location) && _keys.ContainsKey(c));
        if (ReferenceEquals(target, _disabledHover)) return;
        _tips.Hide(parent); _disabledHover = target;
        if (target is not null) _tips.Show(_tips.GetToolTip(target), parent, e.X + 12, e.Y + 20, 15000);
    }
    private void ParentMouseLeave(object? sender, EventArgs e) { if (sender is Control parent) _tips.Hide(parent); _disabledHover = null; }
    private void ControlDisposed(object? sender, EventArgs e) { if (sender is Control control) _keys.Remove(control); }
    private void ParentDisposed(object? sender, EventArgs e) { if (sender is Control parent) _parents.Remove(parent); }
    public void Dispose()
    {
        foreach (var control in _keys.Keys) { control.ParentChanged -= ParentChanged; control.Disposed -= ControlDisposed; }
        foreach (var parent in _parents) { parent.MouseMove -= ParentMouseMove; parent.MouseLeave -= ParentMouseLeave; parent.Disposed -= ParentDisposed; }
        _keys.Clear(); _parents.Clear(); _tips.Dispose();
    }
}
