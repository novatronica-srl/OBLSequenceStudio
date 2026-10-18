using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using OBLSequenceStudio.Localization;

namespace OBLSequenceStudio.Models;

[JsonConverter(typeof(JsonStringEnumConverter<StepMode>))]
public enum StepMode { Fixed = 0, Fire = 1, Pulse = 2, Flash = 3, Ramp = 6 }

public sealed class SequenceStep : INotifyPropertyChanged
{
    private int _start, _end, _duration = 1000;
    private StepMode _mode = StepMode.Ramp;
    private int _brightness = 100, _minimum, _maximum = 100, _cycle = 1000, _duty = 50;
    private int _mean = 70, _stdDev = 15, _filter = 50, _refresh = 100;
    public StepMode Mode { get => _mode; set => Set(ref _mode, value); }
    public int Brightness { get => _brightness; set => Set(ref _brightness, value); }
    public int Minimum { get => _minimum; set => Set(ref _minimum, value); }
    public int Maximum { get => _maximum; set => Set(ref _maximum, value); }
    public int Cycle { get => _cycle; set => Set(ref _cycle, value); }
    public int Duty { get => _duty; set => Set(ref _duty, value); }
    public int Mean { get => _mean; set => Set(ref _mean, value); }
    public int StdDev { get => _stdDev; set => Set(ref _stdDev, value); }
    public int Filter { get => _filter; set => Set(ref _filter, value); }
    public int Refresh { get => _refresh; set => Set(ref _refresh, value); }
    public int Start { get => _start; set { if (_start == value) return; _start = value; Changed(nameof(Start)); } }
    public int End { get => _end; set { if (_end == value) return; _end = value; Changed(nameof(End)); } }
    public int Duration { get => _duration; set { if (_duration == value) return; _duration = value; Changed(nameof(Duration)); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    private void Set<T>(ref T field, T value, [CallerMemberName] string property = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        Changed(property);
    }
    public SequenceStep Clone() => new()
    {
        Start = Start, End = End, Duration = Duration, Mode = Mode, Brightness = Brightness,
        Minimum = Minimum, Maximum = Maximum, Cycle = Cycle, Duty = Duty,
        Mean = Mean, StdDev = StdDev, Filter = Filter, Refresh = Refresh
    };
    public bool SameValues(SequenceStep other) => Start == other.Start && End == other.End && Duration == other.Duration &&
        Mode == other.Mode && Brightness == other.Brightness && Minimum == other.Minimum && Maximum == other.Maximum &&
        Cycle == other.Cycle && Duty == other.Duty && Mean == other.Mean && StdDev == other.StdDev && Filter == other.Filter && Refresh == other.Refresh;

    // Schematic, deterministic preview. Fire is indicative, not the firmware random generator.
    public double ValueAt(double time)
    {
        double phase = Math.Max(0, time) % Math.Max(1, Cycle) / Math.Max(1, Cycle);
        return Mode switch
        {
            StepMode.Fixed => Brightness,
            StepMode.Flash => phase < Duty / 100.0 ? Maximum : Minimum,
            StepMode.Pulse => Minimum + (Maximum - Minimum) * (1 - Math.Abs(2 * phase - 1)),
            StepMode.Fire => Math.Clamp(Mean + StdDev * (1 - Filter / 100.0) * Math.Sin(Math.Floor(time / Math.Max(1, Refresh)) * 2.399), 0, 100),
            _ => Start + (End - Start) * Math.Clamp(time / Math.Max(1, Duration), 0, 1)
        };
    }
}

public sealed class ChannelSequence
{
    public int ChannelNumber { get; set; }
    public string Name { get; set; } = "";
    public string LedColor { get; set; } = "#FFB328";
    public bool Repeat { get; set; } = true;
    public List<SequenceStep> Steps { get; set; } = new();
    [JsonIgnore] public long TotalDuration => Steps.Sum(s => (long)s.Duration);
    public double ValueAt(long elapsed)
    {
        if (Steps.Count == 0) return 0;
        elapsed = Math.Max(0, elapsed);
        // A single repeating effect runs continuously, even when its period exceeds Duration.
        if (Steps.Count == 1 && Steps[0].Mode != StepMode.Ramp) return Steps[0].ValueAt(elapsed);
        if (TotalDuration <= 0) return 0;
        elapsed %= TotalDuration;
        foreach (var step in Steps)
        {
            if (elapsed < step.Duration) return step.ValueAt(elapsed);
            elapsed -= step.Duration;
        }
        return Steps[^1].ValueAt(Steps[^1].Duration);
    }
    public ChannelSequence Clone(int destination) => new()
    {
        ChannelNumber = destination,
        Name = Localizer.Text("ChannelName", destination),
        Repeat = Repeat,
        LedColor = LedColor,
        Steps = Steps.Select(s => s.Clone()).ToList()
    };
}

public sealed class DeviceSequences
{
    public int SchemaVersion { get; set; } = 2;
    public string? SourceBoardPID { get; set; }
    public string? SourceBoardVersion { get; set; }
    public List<ChannelSequence> Channels { get; set; } = new();
    public static DeviceSequences CreateDefault() => new()
    {
        Channels = Enumerable.Range(1, 28).Select(n => new ChannelSequence
        {
            ChannelNumber = n,
            Name = Localizer.Text("ChannelName", n),
            Steps = new List<SequenceStep>
            {
                new() { Start = 0, End = 100, Duration = 1000 },
                new() { Start = 100, End = 100, Duration = 1000 },
                new() { Start = 100, End = 0, Duration = 1000 }
            }
        }).ToList()
    };
}
