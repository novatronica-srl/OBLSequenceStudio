using System.ComponentModel;
using OBLSequenceStudio.Localization;
using OBLSequenceStudio.Models;

namespace OBLSequenceStudio.Controls;

public sealed class StepEditorControl : UserControl
{
    private readonly UiHints _hints = new();
    private readonly TableLayoutPanel _fields = new() { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(8) };
    private SequenceStep? _step;
    private bool _binding;
    private readonly Dictionary<string, NumericUpDown> _numbers = new();

    public StepEditorControl()
    {
        AutoScroll = true;
        _fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        _fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        Controls.Add(_fields);
    }

    public void BindStep(SequenceStep? step)
    {
        if (ReferenceEquals(step, _step)) return;
        if (_step is not null) _step.PropertyChanged -= StepChanged;
        _step = step;
        if (_step is not null) _step.PropertyChanged += StepChanged;
        Rebuild();
    }

    private void StepChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_binding) return;
        if (e.PropertyName == nameof(SequenceStep.Mode)) { Rebuild(); return; }
        _binding = true;
        try
        {
            foreach (var (property, control) in _numbers)
                control.Value = Math.Clamp((int)typeof(SequenceStep).GetProperty(property)!.GetValue(_step)!, (int)control.Minimum, (int)control.Maximum);
        }
        finally { _binding = false; }
    }

    public void ApplyLanguage() => Rebuild();

    private void Rebuild()
    {
        _binding = true;
        _fields.SuspendLayout();
        try
        {
            foreach (Control control in _fields.Controls.Cast<Control>().ToArray()) control.Dispose();
            _fields.Controls.Clear();
            _fields.RowStyles.Clear();
            _fields.RowCount = 0;
            _numbers.Clear();
            void Row(string key, Control control)
            {
                int row = _fields.RowCount++;
                _fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                var label = new Label { Text = Localizer.Text(key), AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 7, 3, 7) };
                _fields.Controls.Add(label, 0, row);
                _hints.Set(label, key);
                _hints.Set(control, key);
                control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                _fields.Controls.Add(control, 1, row);
            }
            if (_step is null)
            {
                Row("SelectStep", new Label { AutoSize = true });
                return;
            }
            var mode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, ForeColor = SystemColors.WindowText, BackColor = SystemColors.Window };
            mode.Items.AddRange(Enum.GetValues<StepMode>().Select(m => new ModeOption(m, Localizer.Text("Mode" + m))).Cast<object>().ToArray());
            mode.SelectedItem = mode.Items.Cast<ModeOption>().First(m => m.Mode == _step.Mode);
            mode.SelectedIndexChanged += (_, _) => { if (!_binding && _step is not null && mode.SelectedItem is ModeOption option) SetMode(option.Mode); };
            Row("Mode", mode);
            void Number(string property, string key, int min, int max)
            {
                var info = typeof(SequenceStep).GetProperty(property)!;
                var number = new NumericUpDown { Minimum = min, Maximum = max, Width = 100, ForeColor = SystemColors.WindowText, BackColor = SystemColors.Window, Value = Math.Clamp((int)info.GetValue(_step)!, min, max) };
                number.ValueChanged += (_, _) => { if (!_binding && _step is not null) info.SetValue(_step, (int)number.Value); };
                _numbers[property] = number;
                Row(key, number);
            }
            switch (_step.Mode)
            {
                case StepMode.Ramp:
                    Number(nameof(SequenceStep.Start), "Start", 0, 100);
                    Number(nameof(SequenceStep.End), "End", 0, 100);
                    break;
                case StepMode.Fixed:
                    Number(nameof(SequenceStep.Brightness), "Brightness", 0, 100);
                    break;
                case StepMode.Pulse:
                case StepMode.Flash:
                    Number(nameof(SequenceStep.Minimum), "Minimum", 0, 100);
                    Number(nameof(SequenceStep.Maximum), "Maximum", 0, 100);
                    Number(nameof(SequenceStep.Cycle), "Cycle", 100, 10000);

                    if (_step.Mode == StepMode.Flash) Number(nameof(SequenceStep.Duty), "Duty", 0, 100);
                    break;
                case StepMode.Fire:
                    var presets = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, ForeColor = SystemColors.WindowText, BackColor = SystemColors.Window };
                    presets.Items.AddRange(new object[] { Localizer.Text("Custom"), Localizer.Text("Candle"), Localizer.Text("Fireplace"), Localizer.Text("Torch") });
                    presets.SelectedIndex = 0;
                    presets.SelectedIndexChanged += (_, _) => { if (!_binding && presets.SelectedIndex > 0) ApplyFirePreset(presets.SelectedIndex); };
                    Row("Preset", presets);
                    Number(nameof(SequenceStep.Mean), "Mean", 0, 100);
                    Number(nameof(SequenceStep.StdDev), "StdDev", 0, 100);
                    Number(nameof(SequenceStep.Filter), "Filter", 0, 100);
                    Number(nameof(SequenceStep.Refresh), "Refresh", 1, 10000);
                    break;
            }
            Number(nameof(SequenceStep.Duration), "Duration", 10, 60000);
        }
        finally { _fields.ResumeLayout(true); _binding = false; }
    }

    public void ApplyFirePreset(int preset)
    {
        if (_step is null || _step.Mode != StepMode.Fire) return;
        var values = preset switch { 1 => (70, 15, 50, 100), 2 => (90, 30, 30, 25), 3 => (50, 30, 50, 25), _ => throw new ArgumentOutOfRangeException(nameof(preset)) };
        _step.Mean = values.Item1;
        _step.StdDev = values.Item2;
        _step.Filter = values.Item3;
        _step.Refresh = values.Item4;
    }

    public void SetMode(StepMode mode)
    {
        if (_step is null) return;
        // Imported programmer files may have zero or unused values for inactive modes.
        // Initialize valid parameters before activating those fields in this editor.
        switch (mode)
        {
            case StepMode.Pulse:
            case StepMode.Flash:
                _step.Minimum = Math.Clamp(_step.Minimum, 0, 100);
                _step.Maximum = Math.Clamp(_step.Maximum, _step.Minimum, 100);
                if (_step.Cycle is < 100 or > 10000) _step.Cycle = 1000;
                if (_step.Refresh is < 1 or > 10000) _step.Refresh = 100;
                _step.Duty = Math.Clamp(_step.Duty, 0, 100);
                break;
            case StepMode.Fire:
                _step.Mean = Math.Clamp(_step.Mean, 0, 100);
                _step.StdDev = Math.Clamp(_step.StdDev, 0, 100);
                _step.Filter = Math.Clamp(_step.Filter, 0, 100);
                if (_step.Refresh is < 1 or > 10000) _step.Refresh = 100;
                break;
            case StepMode.Fixed:
                _step.Brightness = Math.Clamp(_step.Brightness, 0, 100);
                break;
        }
        _step.Mode = mode;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hints.Dispose();
        if (disposing && _step is not null) _step.PropertyChanged -= StepChanged;
        base.Dispose(disposing);
    }
    private sealed record ModeOption(StepMode Mode, string Label) { public override string ToString() => Label; }
}

