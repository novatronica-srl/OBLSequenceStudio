using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using OBLSequenceStudio.Localization;

namespace OBLSequenceStudio.Models;

/// <summary>Effect identifiers shared by JSON persistence and the board protocol; numeric values must remain stable.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<StepMode>))]
public enum StepMode { Fixed = 0, Fire = 1, Pulse = 2, Flash = 3, Ramp = 6 }

/// <summary>Editable effect parameters with property notifications for the step editor and grid.</summary>
public sealed class SequenceStep : INotifyPropertyChanged
{
    private int _start, _end, _duration = 1000;
    private StepMode _mode = StepMode.Ramp;
    private int _brightness = 100, _minimum, _maximum = 100, _cycle = 1000, _duty = 50;
    private int _mean = 70, _stdDev = 15, _filter = 50, _refresh = 100;
    /// <summary>Effect whose parameters are active for this step.</summary>
    public StepMode Mode { get => _mode; set => Set(ref _mode, value); }
    /// <summary>Fixed-effect brightness as a percentage from 0 to 100.</summary>
    public int Brightness { get => _brightness; set => Set(ref _brightness, value); }
    /// <summary>Lower pulse or flash brightness as a percentage.</summary>
    public int Minimum { get => _minimum; set => Set(ref _minimum, value); }
    /// <summary>Upper pulse or flash brightness as a percentage.</summary>
    public int Maximum { get => _maximum; set => Set(ref _maximum, value); }
    /// <summary>Pulse or flash period in milliseconds, independently of the step duration.</summary>
    public int Cycle { get => _cycle; set => Set(ref _cycle, value); }
    /// <summary>Percentage of each flash cycle spent at maximum brightness.</summary>
    public int Duty { get => _duty; set => Set(ref _duty, value); }
    /// <summary>Mean brightness percentage for the fire effect.</summary>
    public int Mean { get => _mean; set => Set(ref _mean, value); }
    /// <summary>Standard deviation of fire brightness samples in percentage points.</summary>
    public int StdDev { get => _stdDev; set => Set(ref _stdDev, value); }
    /// <summary>Percentage weight of the previous sample in the fire smoothing filter.</summary>
    public int Filter { get => _filter; set => Set(ref _filter, value); }
    /// <summary>Fire sampling interval in milliseconds.</summary>
    public int Refresh { get => _refresh; set => Set(ref _refresh, value); }
    /// <summary>Ramp brightness percentage at the start of the step.</summary>
    public int Start { get => _start; set { if (_start == value) return; _start = value; Changed(nameof(Start)); } }
    /// <summary>Ramp brightness percentage at the end of the step.</summary>
    public int End { get => _end; set { if (_end == value) return; _end = value; Changed(nameof(End)); } }
    /// <summary>Time allocated to the step in milliseconds, validated in the range 10 to 60000.</summary>
    public int Duration { get => _duration; set { if (_duration == value) return; _duration = value; Changed(nameof(Duration)); } }
    /// <summary>Notifies bound views when an effect parameter changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    private void Set<T>(ref T field, T value, [CallerMemberName] string property = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        Changed(property);
    }
    /// <summary>Copies all effect parameters without carrying event subscriptions into the new instance.</summary>
    public SequenceStep Clone() => new()
    {
        Start = Start, End = End, Duration = Duration, Mode = Mode, Brightness = Brightness,
        Minimum = Minimum, Maximum = Maximum, Cycle = Cycle, Duty = Duty,
        Mean = Mean, StdDev = StdDev, Filter = Filter, Refresh = Refresh
    };
    /// <summary>Compares all stored effect parameters to detect edits, including parameters of inactive modes.</summary>
    public bool SameValues(SequenceStep other) => Start == other.Start && End == other.End && Duration == other.Duration &&
        Mode == other.Mode && Brightness == other.Brightness && Minimum == other.Minimum && Maximum == other.Maximum &&
        Cycle == other.Cycle && Duty == other.Duty && Mean == other.Mean && StdDev == other.StdDev && Filter == other.Filter && Refresh == other.Refresh;

    // Schematic, deterministic preview. Fire is indicative, not the firmware random generator.
    /// <summary>Evaluates a schematic brightness preview; the stateful fire simulation is implemented by LedPreviewEngine.</summary>
    /// <param name="time">Time in milliseconds relative to the start of the effect.</param>
    /// <returns>Brightness percentage for the selected effect.</returns>
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

/// <summary>Ordered cyclic steps and preview presentation settings for one output channel.</summary>
public sealed class ChannelSequence
{
    /// <summary>One-based output channel number, from 1 to 28.</summary>
    public int ChannelNumber { get; set; }
    /// <summary>Display name retained in saved configurations.</summary>
    public string Name { get; set; } = "";
    /// <summary>Preview-only RGB color in #RRGGBB form; does not affect hardware PWM.</summary>
    public string LedColor { get; set; } = "#FFB328";
    /// <summary>Legacy persistence flag normalized to true by the editor to enforce cyclic playback.</summary>
    public bool Repeat { get; set; } = true;
    /// <summary>Ordered effect steps, with at most ten entries per channel.</summary>
    public List<SequenceStep> Steps { get; set; } = new();
    /// <summary>Sum of step durations in milliseconds; computed rather than serialized.</summary>
    [JsonIgnore] public long TotalDuration => Steps.Sum(s => (long)s.Duration);

    /// <summary>Evaluates cyclic schematic playback, keeping a single non-ramp effect continuous across duration boundaries.</summary>
    /// <param name="elapsed">Elapsed playback time in milliseconds.</param>
    /// <returns>Brightness percentage, or zero for an empty or nonpositive-duration sequence.</returns>
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
    /// <summary>Deep-copies steps and preview settings to a destination channel with a localized name.</summary>
    /// <param name="destination">One-based destination channel number.</param>
    public ChannelSequence Clone(int destination) => new()
    {
        ChannelNumber = destination,
        Name = Localizer.Text("ChannelName", destination),
        Repeat = Repeat,
        LedColor = LedColor,
        Steps = Steps.Select(s => s.Clone()).ToList()
    };
}

/// <summary>Persisted configuration for all 28 channels and optional source-board identification.</summary>
public sealed class DeviceSequences
{
    /// <summary>JSON schema version; the loader currently accepts versions 1 and 2.</summary>
    public int SchemaVersion { get; set; } = 2;
    /// <summary>Optional product identifier retained from the imported or connected board.</summary>
    public string? SourceBoardPID { get; set; }
    /// <summary>Optional firmware version retained from the source board.</summary>
    public string? SourceBoardVersion { get; set; }
    /// <summary>Device channels; validation requires one unique entry for each number from 1 to 28.</summary>
    public List<ChannelSequence> Channels { get; set; } = new();
    /// <summary>Creates 28 independent channels, each with a three-second ramp-up, hold and ramp-down sequence.</summary>
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
