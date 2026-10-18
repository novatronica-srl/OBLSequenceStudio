using System.Buffers.Binary;
using System.Xml;
using System.Xml.Linq;
using OBLSequenceStudio.Localization;
using OBLSequenceStudio.Models;

namespace OBLSequenceStudio.Services;

// Same 224 little-endian registers saved by ControllerV1Control.ReadConfig.
public static class ProgrammerConfigurationImporter
{
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
