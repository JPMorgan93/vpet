# Vpet 1.2.2

A native Windows desktop companion with custom sprites, personalities, names, and reactions. The pet works offline; public-release update checks use GitHub when connected.

## Install the public release

Download the installer from [the latest GitHub release](https://github.com/JPMorgan93/vpet/releases/latest). It includes the application, artwork, icon, and getting-started guide. Run the installer, then open **Vpet** from the Start menu or optional desktop shortcut. No developer tools are needed. Requires 64-bit Windows 10 version 1903 or later, or Windows 11, with .NET Framework 4.8 or later. Local builds output `dist/Vpet-Setup-1.2.2-Windows-x64.exe`.

The installer installs for the current user and provides an uninstaller in Windows Settings. Close any running Vpet before installing. Settings and custom artwork are preserved during updates and uninstall. The existing `VpetPrototype` user-data folder is retained for compatibility. The installer is currently unsigned; see `RELEASE.md` for validation, signing, and rebuild details.

## Run

**Settings > Sprite > Load Vpet on PC startup** offers **No (Default)** and **Yes**. Yes launches the pet when you sign in to your Windows account; No disables it. The choice is saved, requires no administrator access, and uninstall removes this installation's startup entry.

**Settings > Personality** starts with an optional pet name. Leave it blank to display nothing, or choose **Hide name**, **Show on hover**, or **Always display**. Names sit below the pet with white outlines around the letters and no background box. Up to 40 characters are saved; long names use an ellipsis on screen. Space is reserved below named pets to keep names clear of the taskbar. Clicking outside the right-click menu closes it, including when clicking another application.

Vpet checks stable public releases at startup and every six hours. Use **Check for updates** in its right-click menu to check immediately. Downloads and installation require your agreement and preserve your settings. Users of **1.0.0 must install 1.1.0 once** to enable future update checks.

The update screen describes what changed before you install. After an upgrade, Vpet displays the installed version's changes once when it reopens. Checking while already up to date shows the current version's description.

See [GITHUB.md](GITHUB.md) for VS Code save syncing, the `test` branch, and promoting releases to `main`.

Double-click **Launch Vpet.cmd**, or run **bin/Vpet.exe** after building. The launcher builds the app if needed. A small purple pet appears on the desktop after a short pause.

- Hover to pause and greet the pet; it switches to down-facing idle.
- Left-click without dragging for a **half-second shake** and **Love**, regardless of personality. The shake starts when the click is released and has no duration setting.
- Hold the left mouse button and drag to reposition the down-facing pet. Pickup reactions follow personality: **Sweet → Love, Sassy → Fear, Bashful → Sad**. Dragging and release do not shake; release rests for five seconds. Simple left-clicks still show Love for every personality.
- Right-click for movement, personality, artwork, and settings.
- Double-click the notification-area icon to open settings. Its menu also includes **Close Vpet**.
- Close Vpet from either menu to exit completely. Closing settings leaves the companion running.

For development builds, **Create Desktop Shortcut.ps1** creates `Vpet.lnk` on your desktop with the application icon and will not overwrite an existing shortcut. Release users can select the desktop shortcut during installation.

## Build and check

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
```

Requires 64-bit Windows 10/11 with .NET Framework 4.x and its C# compiler. The current machine has the required compiler. Outputs are in `bin/`; keep `bin/assets/` alongside the executable when copying the app.

The test executable checks movement timing, diagonal speed, hover and release behavior, restrictions, offset displays, taskbar bounds, disconnected displays, scaling, reaction intervals, persistence, source-frame extraction, mirroring, and import validation. It writes an animation contact sheet and a usable default runtime template to `bin/test-artifacts/`.

`bin/Vpet.Tests.exe --window-tests` briefly creates native test windows to verify Under All ordering, blocked promotion, reaction stacking, switching back to Dynamic or Over Everything, and showing/moving/hiding the restricted fence.

`bin/Vpet.exe --smoke-test` briefly opens the real pet and reaction windows, exercises all three settings tabs and window layers, saves screenshots of its own settings UI, writes a result to `bin/smoke-output/`, then exits after about seven seconds. It uses separate settings under `bin/smoke-data/` and does not change your normal pet settings.

## Included

- Four-frame directional idle and five-frame walking cycles with eight facings.
- Original artwork read from the unchanged reference. The renderer isolates the 45 source frames and removes only exterior white background pixels at runtime; no new AI artwork is required.
- Per-pixel alpha window transparency: empty pixels pass clicks through.
- Free Roam, a fixed restricted area with a draggable center and on-screen fence, and Static mode. Each selection shows its own description.
- A 0–100 speed slider with 50 centered on its tick marks; 0 stops autonomous movement.
- Constant normalized travel speed, arrival slowdown, and direction hysteresis.
- Multi-monitor work areas, transitions across gaps, relocation when displays disappear, and DPI-adjusted sprite size/speed.
- Over Everything, ordinary Dynamic stacking, and Under All, which locks the pet and reactions beneath other application windows.
- Three personalities, eight native Windows emoji, weighted random selection, adjustable frequency, and live custom PNG emote loading.
- Replace any of the eight default emote images with your own PNG, or restore its original symbol. Replacements work for both interactions and random reactions.
- Sprite-sheet validation, animated previews, Apply, Restore Default, and template export.
- Saved settings, position, facing, radius, and imported artwork; tray controls and single-instance protection.

## Legacy PNG artwork format

For arbitrary sheet layouts, variable frame counts, and optional diagonals, use [Sprite Maker](SPRITE-MAKER.md). The grid rules in this section apply to older PNG sheets imported directly.

The original annotated image is preserved at `assets/reference/Base Vpet Sprite Sheet.png`. It is an artwork guide, not an uploadable runtime grid.

Use **Settings → Sprite → Save default template** to export a valid transparent PNG. Default cells are **32 × 36**, in a **160 × 360** sheet. Custom sheets use **5 columns × 10 rows**, with equal cells no larger than **100 × 150** pixels. There are no margins or gutters.

Use **Download blank sprite sheet** below that button for a 500 × 1,500 PNG with labeled 100 × 150 frame boxes. Draw your sprites on a separate layer, remove the guide layer, and export with transparency. Each box identifies the activity, direction, and frame number; unused fifth idle cells must remain transparent. Right-facing poses are mirrored from the left-facing artwork.

| Row | Cycle | Active frames |
| --- | --- | --- |
| 1 | Idle up | 4 |
| 2 | Idle down | 4 |
| 3 | Idle left | 4 |
| 4 | Idle up-left | 4 |
| 5 | Idle down-left | 4 |
| 6 | Walk up | 5 |
| 7 | Walk down | 5 |
| 8 | Walk left | 5 |
| 9 | Walk up-left | 5 |
| 10 | Walk down-left | 5 |

Column five must be transparent in idle rows. Right-facing cycles are mirrored from the corresponding left-facing rows. Align artwork to a consistent bottom-center anchor. Empty active cells, invalid dimensions, corrupt files, and non-PNG imports are rejected before replacing the current pet.

Custom emotes must be PNGs with both dimensions at most **512 pixels**. Use a clean 128 × 128 source for typical display scaling; resizing a blurry source cannot recover detail. Images resize smoothly and appear on a white speech-bubble background, centered horizontally and vertically using the visible artwork (transparent padding is ignored). At 100% display scaling, the complete bubble is 68 × 62 pixels including its tail and transparent margins; the rounded body is 65 × 50 pixels. Custom artwork fits proportionally inside a 46 × 42 pixel area. These dimensions scale with Windows display scaling. Open their folder from the Personality tab; added, modified, and removed files refresh within three seconds. Each custom image has priority 2 (weight 3). Under **Your custom emotes**, each loaded image is listed by name with a **Try It Out** button to preview it on your pet. Custom replacements for default reactions appear here too, and the list refreshes as images change.

To replace a default emote, open **Personality → Replace a default emote**, select its name, then choose **Choose image…**. The replacement is copied into local storage and retains that emote's personality triggers and random-selection weight. **Restore original** reverses the replacement. The separate emote folder still adds extra random reactions.

Default reactions use the installed **Segoe UI Emoji** font with native Windows color-font rendering:

| Reaction | Emoji | Unicode |
| --- | --- | --- |
| Music | 🎵 | U+1F3B5 |
| Love | ♥️ | U+2665 |
| Question | ❓ | U+2753 |
| Anger | 💢 | U+1F4A2 |
| Sad | 💧 | U+1F4A7 |
| Fear | ❗ | U+2757 |
| Disgust | 🌀 | U+1F300 |
| Proud | 🏆 | U+1F3C6 |

Love requests emoji presentation with U+FE0F after U+2665. Appearance follows the PC's installed Windows emoji font. Custom images take precedence; Restore original returns to these emoji. The renderer uses [Direct2D's color-font option](https://blogs.windows.com/windowsdeveloper/2017/06/06/using-color-fonts-beautiful-text-icons/) and caches the eight rendered defaults.

## Restricted area

Selecting **Restricted** shows its description below the movement type, followed by **Display restricted area**, the radius slider, and a synchronized number field. Type a radius or move the slider to resize the fence. The radius ranges from **30 to 1,000 pixels**; saved larger values are clamped to 1,000. The checkbox is checked whenever you enter Restricted mode. A dashed circle shows the fence, and its center handle can be dragged without stealing keyboard focus. Both the ring and handle stay behind the sprite in every window mode. The ring and empty interior pass clicks through to applications underneath.

The fence is first created around the pet, then stays fixed until you move its center or change its radius. Dragging the pet, changing speed, hiding the fence, or switching movement modes does not recenter it. If moving or shrinking the fence leaves the pet outside, the pet returns to its center. Dropping the pet outside the fence likewise returns it to the center. Hiding the overlay leaves the restriction active. The area is saved between launches; disconnected-display recovery can relocate an otherwise unreachable center.

The right-click menu also has **Movement controls → Display restricted area**, directly below **Speed and radius**. This checked toggle is enabled in Restricted mode and shares the same saved value as the settings checkbox.

Speech bubbles use one continuous body-and-tail outline, drawn after the emoji or custom image. This keeps the full border visible for both upward- and downward-pointing bubbles.

## Local storage

Normal settings and imported assets live under `%LOCALAPPDATA%\VpetPrototype`:

- `settings.json`: preferences and position.
- `pet.vpetsprite`: active custom sprite package; legacy `pet.png`: accepted custom sprite sheet, copied independently of the original upload.
- `Emotes/`: live custom reactions.
- `DefaultEmotes/`: replacements for the eight built-in reaction images.
- `error.log` / `asset-error.txt`: diagnostics if a runtime or saved-artwork error occurs.

## Prototype boundaries

- Under All keeps the pet above the desktop wallpaper but beneath application windows, including while dragging and displaying reactions. It does not require an Explorer desktop host. Existing saved Desktop Only preferences automatically become Under All.
- Cross-display travel uses synchronized, clipped sprite fragments on both displays. At adjoining work-area edges, they form one continuously moving image; there is no position jump or missing frame. For offset or separated work areas, matching portions slide out and in at paired edges so the whole pet does not abruptly disappear and reappear. There are no physical pixels in a monitor-layout gap to draw through. Neighboring displays are used as intermediate steps when needed.
- DPI scaling follows Windows display scaling. This approximates consistent physical size and speed; monitor metadata cannot guarantee a perfect physical match.
- Artwork extraction preserves the supplied pixels, including any imperfections in the original reference. Review imported animations in the preview before applying them.
- The app and installer are unsigned. Full-screen-game integration is not provided.

Window stacking uses [SetWindowPos](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos), with a window-position guard for Under All. Rendering uses [UpdateLayeredWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-updatelayeredwindow).

The detailed behavior and source artwork mapping are in [Vpet Development Specification.md](Vpet%20Development%20Specification.md).

## Sprite Maker

Open **Settings → Sprite → Open Sprite Maker** to select frames from any transparent PNG sheet, save editing projects, preview animations, and export a custom pet. Drag the red border to move a selection or a corner to resize it. Magic Tweak aligns poses by their lowest visible pixels to a shared ground point without stretching. Each animation supports 1–5 frames, with optional diagonals. See the [Sprite Maker guide](SPRITE-MAKER.md) for the full workflow.
