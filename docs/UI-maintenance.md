# Maintaining the Windows Forms UI

## Layout and runtime responsibilities

`MainForm.cs` and `Controls/FirmwareUpdateWizard.cs` contain application behavior.
Their `.Designer.cs` partials and `.resx` files are managed by the Visual Studio
Windows Forms designer. Build the project before opening either form with
**View Designer** (`Shift+F7`).

Use the designer to change the layout, sizing, docking and static control
properties. Keep event orchestration, resource lookup, model binding, file access
and device operations in the behavior partial. Designer regeneration can replace
manual edits to generated files, so place maintenance comments and custom logic
in the behavior partial instead.

Both forms stop runtime initialization when `LicenseManager.UsageMode` is
`Designtime`. Preserve that boundary when adding initialization: loading user
preferences, searching for firmware and enumerating serial ports must remain
after the guard. The wizard also has an explicit parameterless constructor for
designer activation; the overload accepting a port name is used by the editor.

Runtime translations use `Localization/Strings.json`. Designer captions are
preview values and can be replaced when the form starts. Register new main-form
captions through `Translate` or `ConfigureActionButton` so subsequent language
changes also refresh their text and tooltips.

## Main editor data flow

`_device` holds the complete device configuration. Channel numbers run from 1 to
28; grid rows and step indices are zero-based. `_rows` contains editable clones
of the selected channel's steps. `CommitGrid` ends the current cell edit and
`PersistRows` clones changed rows back into the device model. Actions that depend
on edited values should respect a failed commit rather than proceeding with
stale data.

`_loading` suppresses edit and selection reactions while binding a configuration
or switching channels. `RefreshTimeline` updates the timeline, LED preview and
selected channel summary from the model. Timeline endpoint times and step
durations are in milliseconds; brightness values are percentages. Dragging a
ramp endpoint also updates the next ramp's starting brightness when applicable.

`ConfigureRuntimeUi` and `WireEvents` run once per runtime form instance.
Calling them again would attach duplicate event handlers. The step parameter
editor intentionally rebuilds its fields for the selected effect; the timeline
and LED preview retain their custom drawing behavior.

## Board operations

Live edits restart a 400 ms timer, grouping edits before an automatic write.
Configuration writes use cloned snapshots so edits made while awaiting serial
operations cannot change the data already being sent. `_writing` prevents
conflicting UI actions and causes telemetry ticks to skip writes in progress.

The telemetry timer runs every 300 ms. `_readingTelemetry` prevents overlapping
asynchronous reads. Results are ignored if the form has been disposed or the
connection has changed while awaiting the response.

`DisconnectBoard` stops both timers before releasing the normal board
connection. `OpenFirmwareWizard` retains its port name as a hint, pauses the
preview and disconnects before opening the modal wizard, allowing the bootloader
workflow to use the serial port. Closing the wizard does not automatically
reconnect the main editor.

## Firmware wizard pages

| Page | Purpose | Transition on Next |
| --- | --- | --- |
| 0 | Select a firmware binary | Validate the image, then show preparation |
| 1 | Prepare the board and select a serial port | Require preparation confirmation and open the bootloader session |
| 2 | Review image and detected board details | Start programming the reviewed image |
| 3 | Show programming progress and the result | Close with a result reflecting programming success |

`_page` follows this order and also indexes the heading resource keys in
`ShowPage`. Keep those mappings synchronized if pages are added or reordered.
The review page requires both `_image` and `_session` to have been initialized
by the preceding steps.

The design surface contains all reusable page controls. At runtime `ShowPage`
clears the choices container and reattaches the controls needed by the current
page. Removing a control from the container must not dispose it: Back navigation
needs the same instance. `Dispose` explicitly releases reusable controls that
may be detached when the form closes.

At startup, exactly one valid `.bin` file in the execution directory's
`firmware` subdirectory skips manual file selection. Missing, ambiguous or
invalid bundled images leave manual selection available. The internal
constructor accepts an execution directory to make this lookup controllable
without changing the application's base directory.

`SetBusy` disables page actions and prevents window closure during bootloader
operations. Errors before programming allow correction on the current page.
Programming errors remain on the result page. Back navigation, programming
completion and disposal release the bootloader session at their respective
workflow boundaries.

## Verification after UI changes

Run `dotnet build --no-restore` when packages are already restored. Check both
forms in Visual Studio's designer after changing generated layout or constructor
initialization. At runtime, check language changes, channel selection, pending
grid edits and minimum window sizes. For the wizard, check repeated navigation
between selection and preparation and confirm that controls remain usable.
Bootloader detection and actual programming require a compatible connected board;
layout checks alone do not validate firmware transfer.
