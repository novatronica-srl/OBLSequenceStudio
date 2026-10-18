using System.Buffers.Binary;
using System.Xml;
using System.Xml.Linq;
using OBLSequenceStudio.Localization;
using OBLSequenceStudio.Models;

namespace OBLSequenceStudio.Services;

// Same 224 little-endian registers saved by ControllerV1Control.ReadConfig.
/// <summary>Imports legacy programmer configurations containing eight blocks of 28 little-endian registers.</summary>
public static class ProgrammerConfigurationImporter
{
    /// <summary>Reads a single XML ConfigurationFile row with a Base64 payload while disabling DTD and external resolution.</summary>
    /// <exception cref="InvalidDataException">The payload, configuration row or decoded effect parameters are invalid.</exception>
    public static DeviceSequences Load(string filename)
    {
        using var reader = XmlReader.Create(filename, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        var document = XDocument.Load(reader);
        var rows = document.Descendants().Where(e => e.Name.LocalName == "ConfigurationFile").ToArray();
        if (rows.Length != 1) throw new InvalidDataException(Localizer.Text("InvalidProgrammerConfig"));
        var row = rows[0];
        byte[] payload;
        try { payload = Convert.FromBase64String(row.Elements().Single(e => e.Name.LocalName == "Payload").Value); }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        { throw new InvalidDataException(Localizer.Text("InvalidProgrammerConfig"), ex); }
        if (payload.Length != 224 * 2) throw new InvalidDataException(Localizer.Text("InvalidProgrammerConfig"));
        return FromPayload(payload,
            row.Elements().FirstOrDefault(e => e.Name.LocalName == "BoardPID")?.Value,
            row.Elements().FirstOrDefault(e => e.Name.LocalName == "BoardVersion")?.Value);
    }

    /// <summary>Converts a legacy register payload to one cyclic effect per channel and validates the resulting configuration.</summary>
    /// <param name="payload">Exactly 448 bytes representing 224 little-endian registers.</param>
    /// <param name="product">Optional source-board product identifier.</param>
    /// <param name="version">Optional source-board firmware version.</param>
    /// <returns>A complete configuration with channels numbered 1 through 28.</returns>
    public static DeviceSequences FromPayload(byte[] payload, string? product, string? version)
    {
        if (payload.Length != 448) throw new InvalidDataException(Localizer.Text("InvalidProgrammerConfig"));
        int Register(int block, int channel) => BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan((block * 28 + channel) * 2, 2));
        var device = new DeviceSequences
        {
            SourceBoardPID = product,
            SourceBoardVersion = version
        };
        for (int channel = 0; channel < 28; channel++)
        {
            int mode = unchecked((short)Register(0, channel));
            if (mode is < -1 or > 3) throw new InvalidDataException(Localizer.Text("UnsupportedProgrammerMode", channel + 1, mode));
            int cycle = Register(6, channel), refresh = Register(5, channel);
            var step = new SequenceStep
            {
                Mode = mode == -1 ? StepMode.Fixed : (StepMode)mode,
                Brightness = mode == -1 ? 0 : Register(1, channel),
                Mean = Register(1, channel), StdDev = Register(2, channel),
                Minimum = Register(3, channel), Maximum = Register(4, channel),
                Refresh = refresh, Cycle = cycle, Filter = Register(7, channel),
                Duty = cycle == 0 ? 0 : (int)Math.Clamp(Math.Round(refresh * 100.0 / cycle), 0, 100)
            };
            device.Channels.Add(new ChannelSequence
            {
                ChannelNumber = channel + 1, Name = Localizer.Text("ChannelName", channel + 1),
                Repeat = true, Steps = new List<SequenceStep> { step }
            });
        }
        SequenceService.Validate(device);
        return device;
    }
}
