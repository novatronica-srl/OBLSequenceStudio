using OBLSequenceStudio.Models;

namespace OBLSequenceStudio.Services;

public sealed class LedPreviewEngine
{
    private readonly Dictionary<int, FireState> _fire = new();
    public void Reset() => _fire.Clear();

    public int[] Evaluate(DeviceSequences? device, long elapsed)
    {
        int[] values = new int[28];
        if (device is null) return values;
        foreach (var channel in device.Channels)
        {
            if (channel.ChannelNumber is < 1 or > 28 || channel.Steps.Count == 0) continue;
            long total = channel.TotalDuration;
            if (total <= 0) continue;
            long time = Math.Max(0, elapsed), iteration = 0;
            bool continuous = channel.Steps.Count == 1 && channel.Steps[0].Mode != StepMode.Ramp;
            if (!continuous) { iteration = time / total; time %= total; }
            int index = 0;
            while (index < channel.Steps.Count - 1 && time >= channel.Steps[index].Duration)
                time -= channel.Steps[index++].Duration;
            var step = channel.Steps[index];
            if (!continuous) time = Math.Min(time, step.Duration);
            double value;
            if (step.Mode == StepMode.Fire)
            {
                int interval = Math.Max(1, step.Refresh);
                long samples = time / interval + 1;
                if (!_fire.TryGetValue(channel.ChannelNumber, out var state) || state.Index != index || state.Iteration != iteration ||
                    !state.Step.SameValues(step) || samples < state.Samples)
                {
                    state = new FireState(step.Clone(), index, iteration, unchecked(channel.ChannelNumber * 7919 + index * 104729 + (int)iteration));
                    _fire[channel.ChannelNumber] = state;
                }
                while (state.Samples < samples)
                {
                    double gaussian = state.Gaussian() * step.StdDev + step.Mean;
                    double random = Math.Clamp(gaussian, 0, 100);
                    double filtered = random * (1 - step.Filter / 100.0) + state.Previous * (step.Filter / 100.0);
                    state.Previous = filtered;
                    state.Samples++;
                }
                value = state.Previous;
            }
            else value = step.ValueAt(time);
            values[channel.ChannelNumber - 1] = (int)Math.Clamp(Math.Floor(value + 0.5), 0, 100);
        }
        return values;
    }

    private sealed class FireState(SequenceStep step, int index, long iteration, int seed)
    {
        public SequenceStep Step { get; } = step;
        public int Index { get; } = index;
        public long Iteration { get; } = iteration;

        public long Samples { get; set; }
        public double Previous { get; set; }
        private readonly Random _random = new(seed);
        private double? _spare;
        public double Gaussian()
        {
            if (_spare is double spare) { _spare = null; return spare; }
            double u, v, s;
            do { u = _random.NextDouble() * 2 - 1; v = _random.NextDouble() * 2 - 1; s = u * u + v * v; } while (s >= 1 || s == 0);
            double factor = Math.Sqrt(-2 * Math.Log(s) / s);
            _spare = v * factor;
            return u * factor;
        }
    }
}


