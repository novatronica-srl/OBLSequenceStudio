# Source code map

This guide describes the handwritten C# sources. XML comments document types,
domain properties and operational contracts in English. Designer-generated
partials keep the layout produced by Visual Studio; their behavior is explained
in the handwritten form partials and [UI maintenance](UI-maintenance.md).

## Application and models

| Source | Responsibility |
| --- | --- |
| `Program.cs` | STA entry point, Windows Forms initialization and application message loop |
| `MainForm.cs` | File editing, model binding, preview coordination and board operations |
| `Models/SequenceModels.cs` | Serializable device/channel/step models, cloning and schematic effect evaluation |

Channel numbers are one-based (1–28); collection and step indices are zero-based.
Effect durations, periods, refresh intervals and playback positions use
milliseconds. Brightness and duty use percentages. Preview RGB colors are
presentation settings and do not change hardware PWM. Effect enum numbers are
protocol values and must not be renumbered when changing their display order.

## Services

| Source | Responsibility and boundary |
| --- | --- |
| `SequenceService.cs` | Schema and effect validation, JSON persistence, CSV export and ramp-only legacy register conversion |
| `ProgrammerConfigurationImporter.cs` | Legacy XML/Base64 import of 224 little-endian register words; creates one effect per channel |
| `LedPreviewEngine.cs` | Stateful fire simulation and channel playback evaluation with deterministic replay |
| `IRegisterClient.cs` | Register operations used by board workflows independently of the transport |
| `ModbusRtuMaster.cs` | RTU framing, big-endian register bytes, CRC checking and response validation |
| `BoardConnection.cs` | Discovery, protocol compatibility checks, staged configuration, command acknowledgements and telemetry |
| `FirmwareUpdater.cs` | Binary validation, bootloader adapter, target validation and single-attempt programming with readback verification |
| `ConnectionDiagnostics.cs` | Best-effort synchronized connection log and 4 MiB rotation |

Services in this table are under `Services/`. Do not confuse the three register
representations: the imported programmer payload is 224 little-endian words,
the current board staging record is 142 words per channel, and `ToRegisters`
returns a logical 30-word ramp-only legacy layout without hardware addresses.

The current staging record contains a step count, a cyclic flag and ten slots
of 14 words. Each slot stores mode, duration, start, end, brightness, minimum,
maximum, cycle, duty, mean, standard deviation, filter, refresh and a reserved
zero word, in that order. `Encode` and `Decode` must evolve together if the
firmware contract changes.

Board operations share a semaphore so a staged write and its acknowledgement
cannot interleave with another operation. Command arguments are written first;
the sequence-number trigger is a separate final transaction. Acknowledgement
waits last up to five seconds. Board save completion can take a further
20 seconds. Telemetry is split into blocks below 125 registers because deployed
firmware rejects the boundary count even though the RTU client supports it.

`ModbusRtuMaster` leaves its caller-owned transport open on disposal.
`BoardConnection` owns the normal application's serial connection.
`FirmwareUpdateSession` owns the separate bootloader connection through its
adapter. Release the normal connection before entering the firmware workflow.

Firmware validation requires a binary of 8 bytes to 128 KiB, a supported SRAM
stack vector and a Thumb reset vector inside the image. The programming buffer
is padded with `0xFF` to four-byte alignment, while the displayed length and
SHA-256 identify the original file. Target probing requires product `0x0410`
and 128 KiB flash. An attempt consumes its session before erasing flash: a
failure requires a fresh session rather than reusing potentially partial state.

## Controls and localization

| Source | Responsibility |
| --- | --- |
| `Controls/TimelineControl.cs` | Timeline drawing, pointer hit testing and endpoint/selection events |
| `Controls/StepEditorControl.cs` | Dynamic parameter fields and valid defaults when switching effect modes |
| `Controls/LedPreviewControl.cs` | Local clock, simulated or hardware PWM rendering, channel selection and preview color editing |
| `Controls/FirmwareUpdateWizard.cs` | File selection, board preparation, review and programming pages |
| `Controls/UiHints.cs` | Localized tooltips, disabled-control hover support and subscription cleanup |
| `Controls/ButtonVisuals.cs` | Shared button interaction styling and owned generated icon images |
| `Controls/DarkTitleBar.cs` | Supported native Windows caption attributes |
| `Controls/DarkCheckBox.cs` | Readable disabled checkbox text on the dark preview panel |
| `Localization/Localizer.cs` | Embedded translations, formatting culture and per-user language preference |
| `Localization/Dialogs.cs` | Application-language OK and Yes/No dialogs |

Fire history is intentionally deterministic for repeatable local previews. It
does not reproduce the board's random stream. A single non-ramp effect runs
continuously; multi-step sequences restart each effect at its step boundary.
Seeking backwards or changing fire parameters rebuilds the relevant history.

Translation arrays in `Strings.json` follow `Localizer.Languages` order. Adding
a language requires updating both. Calls to `SetLanguage` update the current
thread cultures; views refresh captions through their own language-update
methods. Tooltip and button-image owners must be disposed with their controls.

## Embedded STM32 bootloader library

| Source | Responsibility |
| --- | --- |
| `STBootLib/STBoot.cs` | Serial command implementation, supported-command discovery, chunked memory transfers and acknowledgement handling |
| `STBootLib/STBootCmds.cs` | Command bytes and classic/extended erase selectors |
| `STBootLib/STBootResps.cs` | ACK and NACK response bytes |
| `STBootLib/STBootProgress.cs` | Processed and total byte counters for writes |
| `STBootLib/STBootException.cs` | Command rejection and unsupported-command failures |
| `STBootLib/STBootTimeout.cs` | Timeout for waiting on a task; does not cancel the underlying operation |

The embedded library retains its original public names and numeric protocol
constants. The application adapter opens the bootloader at 115200 baud, 8E1.
Memory transfers use chunks of at most 256 bytes, with cancellation checked
between chunks. Erase itself is not cancellable and may wait for a longer
acknowledgement. The updater does not use unprotection or jump commands. See
[the embedded library notes](../STBootLib/README.md) for its integration details.

## Documentation verification

With packages already restored, run:

```powershell
dotnet build --no-restore -p:GenerateDocumentationFile=true -p:NoWarn=1591
git diff --check
```

This also checks XML documentation syntax and parameter references. CS1591 is
suppressed for intentionally undocumented generated and incidental members;
other XML-documentation warnings should be investigated. Hardware behavior
still requires an appropriate board and cannot be established by a successful
documentation build.
