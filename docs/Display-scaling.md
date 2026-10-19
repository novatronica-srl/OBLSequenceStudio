# Display scaling verification

Verified on 2026-10-19 using .NET SDK 10.0.402 on Windows.

## Automated results

The executable regression suite completed **1,989 assertions with zero failures**.

| Physical display viewport | 100% (96 DPI) | 125% (120 DPI) | 150% (144 DPI) | 200% (192 DPI) |
| --- | --- | --- | --- | --- |
| 1920 x 1080 | Pass | Pass | Pass, vertical workspace scrolling | Pass, workspace scrolling in both directions |
| 3840 x 2160 | Pass | Pass | Pass | Pass |

The viewport reserves physical space for the taskbar and window frame. Workspace
scrolling preserves access to the editor when the logical desktop is smaller
than its content; it does not promise that every panel fits simultaneously.

Coverage:

- Main editor layout in Italian, English, Spanish, German and French.
- Scaled timeline height, channel selection and endpoint dragging.
- Scaled action icons, seven explicit grid columns, inherited grid font and
  DPI-aware minimum column widths.
- Toolbar control heights and grid area.
- Step toolbar grows to fit wrapped controls, with no internal scrollbars or vertically clipped commands, including 4K at 150%.
- Access to the status bar and workspace through scrolling.
- Firmware wizard selection, preparation and result pages in all five languages,
  including reachable navigation and unclipped navigation buttons.
- Synthetic DPI transitions 125%, 150%, 200%, 150%, 125%, 100%.
- An application font enlarged by 50% after returning to 100% DPI.
- Rendered PNGs for every display/DPI pair, wizard preparation pages and the
  scrolled 1080p/200% workspace. Representative images were visually inspected.

## Run the checks

From the repository root, on Windows with the .NET 10 SDK:

```powershell
dotnet build OBLSequenceStudio.csproj --no-restore
dotnet run --project Tests/DisplayScalingTests -- artifacts/display-scaling
```

The test runner returns a nonzero exit code for assertion failures. It produces
`results.json` and rendered PNGs under `artifacts/display-scaling`. It creates
temporary off-screen forms; no board connection or firmware programming occurs.

## Method and remaining manual checks

These are **simulated display/DPI combinations**, not certification on eight
physical monitor configurations. The host monitor was at 144 DPI (150%). The
runner sends WM_DPICHANGED through WinForms and sets the cached child DPI via
.NET 10's internal DeviceDpiInternal property. It explicitly delivers child
DPI-change completion notifications. This reflection-based mechanism is isolated
to the test project and may need adaptation after a runtime upgrade.

The enlarged-font check changes the application's Font property. It does not
change Windows Accessibility > Text size.

Before release, verify on real 1080p and 4K monitors at each requested scale:
cold start, maximize/restore, resize, moving between monitors with different
DPI, Windows text-size changes and keyboard access to scrolled controls.
Check both forms in Visual Studio's designer as described in UI-maintenance.md;
the designer was not available to this automated run. The firmware review page
requires a validated image and bootloader session and was not exercised.
No firmware transfer or board behavior is covered by these layout checks.

The application build succeeds with four existing CS0108 warnings in
TimelineControl (inherited member names hidden by existing members).

## Implementation notes

MainForm now records its 96-DPI design baseline explicitly. Both forms host their
content in ScrollableWorkspace after their design-time guard. The timeline paints
in logical coordinates and translates mouse coordinates into the same space.
Preview headers and wizard navigation use content-driven sizing. LED drawing
reserves room for its measured text, and button icons are regenerated after DPI
changes.

Visual inspection also exposed unintended auto-generated grid columns: the
designer declared four columns while runtime formatting expected seven. The
three missing bound columns are now explicit and automatic column generation is
disabled, preserving the runtime editing contract.