using System.Text.Json;
using OBLSequenceStudio.Localization;
using OBLSequenceStudio.Models;

namespace OBLSequenceStudio.Services;

public static class SequenceService
{
    public static void EnsureCyclic(DeviceSequences device)
    {
        foreach (var channel in device.Channels) channel.Repeat = true;
    }
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static void Validate(DeviceSequences device)
    {
        if (device.SchemaVersion is < 1 or > 2) throw new InvalidDataException(Localizer.Text("InvalidJson"));
        if (device.Channels is null || device.Channels.Count != 28 ||
            device.Channels.Any(c => c is null) ||
            device.Channels.Select(c => c.ChannelNumber).Distinct().Count() != 28 ||
            device.Channels.Any(c => c.ChannelNumber < 1 || c.ChannelNumber > 28))
            throw new InvalidDataException(Localizer.Text("ChannelValidation"));
        foreach (var channel in device.Channels)
        {
            if (channel.LedColor is null || !System.Text.RegularExpressions.Regex.IsMatch(channel.LedColor, "^#[0-9A-Fa-f]{6}$"))
                throw new InvalidDataException(Localizer.Text("InvalidLedColor", channel.ChannelNumber));
            if (channel.Steps is null || channel.Steps.Count > 10)
                throw new InvalidDataException(Localizer.Text("StepValidation", channel.ChannelNumber));
            foreach (var s in channel.Steps)
            {
                if (s is null || s.Start is < 0 or > 100 || s.End is < 0 or > 100 || s.Duration is < 10 or > 60000)
                    throw new InvalidDataException(Localizer.Text("RangeValidation", channel.ChannelNumber));
                if (!ValidEffect(s)) throw new InvalidDataException(Localizer.Text("EffectValidation", channel.ChannelNumber, channel.Steps.IndexOf(s) + 1));
            }
        }
    }

    public static bool ValidEffect(SequenceStep step)
    {
        bool Percent(int value) => value is >= 0 and <= 100;
        return step.Mode switch
        {
            StepMode.Ramp => Percent(step.Start) && Percent(step.End),
            StepMode.Fixed => Percent(step.Brightness),
            StepMode.Fire => Percent(step.Mean) && Percent(step.StdDev) && Percent(step.Filter) && step.Refresh is >= 1 and <= 10000,
            StepMode.Pulse => Percent(step.Minimum) && Percent(step.Maximum) && step.Minimum <= step.Maximum && step.Cycle is >= 100 and <= 10000,
            StepMode.Flash => Percent(step.Minimum) && Percent(step.Maximum) && step.Minimum <= step.Maximum && step.Cycle is >= 100 and <= 10000 && Percent(step.Duty),
            _ => false
        };
    }

    public static string ParameterSummary(SequenceStep step) => step.Mode switch
    {
        StepMode.Fixed => $"{step.Brightness}%",
        StepMode.Fire => Localizer.Text("FireSummary", step.Mean, step.StdDev, step.Filter, step.Refresh),
        StepMode.Flash => Localizer.Text("FlashSummary", step.Minimum, step.Maximum, step.Cycle, step.Duty),
        StepMode.Pulse => Localizer.Text("CycleSummary", step.Minimum, step.Maximum, step.Cycle),
        _ => Localizer.Text("RampSummary", step.Start, step.End)
    };

    public static void ExportCsv(string filename, DeviceSequences device)
    {
        Validate(device);
        EnsureCyclic(device);
        using var writer = new StreamWriter(filename);
        writer.WriteLine("Channel;Repeat;Step;StartMs;EndMs;DurationMs;Mode;Start;End;Brightness;Minimum;Maximum;CycleMs;DutyPercent;Mean;StdDev;Filter;RefreshMs");
        foreach (var channel in device.Channels)
        {
            long time = 0;
            for (int i = 0; i < channel.Steps.Count; i++)
            {
                var s = channel.Steps[i];
                writer.WriteLine(FormattableString.Invariant($"{channel.ChannelNumber};{channel.Repeat};{i + 1};{time};{time + s.Duration};{s.Duration};{s.Mode};{s.Start};{s.End};{s.Brightness};{s.Minimum};{s.Maximum};{s.Cycle};{s.Duty};{s.Mean};{s.StdDev};{s.Filter};{s.Refresh}"));
                time += s.Duration;
            }
        }
    }

    public static void Save(string filename, DeviceSequences device)
    {
        Validate(device);
        EnsureCyclic(device);
        File.WriteAllText(filename, JsonSerializer.Serialize(device, Options));
    }

    public static DeviceSequences Load(string filename)
    {
        var device = JsonSerializer.Deserialize<DeviceSequences>(File.ReadAllText(filename), Options)
            ?? throw new InvalidDataException(Localizer.Text("InvalidJson"));
        Validate(device);
        EnsureCyclic(device);
        device.Channels = device.Channels.OrderBy(c => c.ChannelNumber).ToList();
        return device;
    }

    // Tracciato logico dei registri di un canale: 10 start, 10 end, 10 duration.
    // NON è una scrittura hardware: l'indirizzamento va configurato sulla mappa reale.
    public static ushort[] ToRegisters(ChannelSequence channel)
    {
        if (channel.Steps.Any(s => s.Mode != StepMode.Ramp)) throw new InvalidOperationException(Localizer.Text("LegacyExportError"));
        if (channel.Steps.Count > 10) throw new InvalidOperationException(Localizer.Text("TooManySteps"));
        ushort[] registers = new ushort[30];
        for (int i = 0; i < channel.Steps.Count; i++)
        {
            var s = channel.Steps[i];
            if (s.Start is < 0 or > 100 || s.End is < 0 or > 100 || s.Duration is < 10 or > 60000)
                throw new InvalidOperationException(Localizer.Text("StepOutOfRange", i + 1));
            registers[i] = (ushort)s.Start;
            registers[10 + i] = (ushort)s.End;
            registers[20 + i] = (ushort)s.Duration;
        }
        // Zero sugli step non usati: verificare la convenzione nel firmware.
        return registers;
    }
}

