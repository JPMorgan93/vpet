# Vpet 1.11.5

A native Windows desktop companion with custom sprites, personalities, names, and reactions. The pet works offline; public-release update checks use GitHub when connected.

## Install the public release

Download the installer from [the latest GitHub release](https://github.com/JPMorgan93/vpet/releases/latest). It includes the application, artwork, icon, arcade songs, and getting-started guide. Run the installer, then open **Vpet** from the Start menu or optional desktop shortcut. No developer tools are needed. Requires 64-bit Windows 10 version 1903 or later, or Windows 11, with .NET Framework 4.8 or later. Local builds output `dist/Vpet-Setup-1.11.5-Windows-x64.exe`.

The installer installs for the current user and provides an uninstaller in Windows Settings. Close any running Vpet before installing. Settings and custom artwork are preserved during updates and uninstall. The existing `VpetPrototype` user-data folder is retained for compatibility. The installer is currently unsigned; see `RELEASE.md` for validation, signing, and rebuild details.

## Run

**Settings > Advanced > Load Vpet on PC startup** offers **No (Default)** and **Yes**. Yes launches the pet when you sign in to your Windows account; No disables it. The choice is saved, requires no administrator access, and uninstall removes this installation's startup entry.

**Settings > Personality** starts with an optional pet name. Leave it blank to display nothing, or choose **Hide name**, **Show on hover**, or **Always display**. Names sit below the pet with white outlines around the letters and no background box. Up to 40 characters are saved; long names use an ellipsis on screen. Space is reserved below named pets to keep names clear of the taskbar. Clicking outside the right-click menu closes it, including when clicking another application.

**Settings > Advanced > Auto-update on app startup**, directly below the Windows startup option, offers **No (Default)** and **Yes**. Yes checks and automatically installs the newest public release when Vpet starts, with download/installation progress and no continue prompt. No keeps startup checks as notifications. Checks every six hours while running remain notifications, so automatic installation happens at startup. Offline checks leave the pet running normally.

**Check for Updates** sits below the final separator, directly above **Close Vpet** in the right-click menu. A window shows your installed version and either confirms that it is current or shows the available version with an **Update** button. Choose Update to download and install it. Settings, custom art and shortcut choices are preserved. Release notes appear once after successful installation and relaunch, including every skipped release since the last version you ran, grouped newest first. The history is bundled for offline viewing. Older installations without a recorded last-run version use their installed version as the starting point. Up-to-date checks show status without repeating old notes. Users of **1.0.0 must install 1.1.0 once** to enable future update checks.

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

`bin/Vpet.exe --smoke-test` briefly opens the real pet and reaction windows, exercises all four settings tabs and window layers, saves screenshots of its own settings UI, writes a result to `bin/smoke-output/`, then exits after about eight seconds. It uses separate settings under `bin/smoke-data/` and does not change your normal pet settings. `bin/Vpet.Tests.exe --expanded-window-tests` checks the new game controls, independent/shared fences, sound volume, animation speed, and responsive settings with isolated preferences.

## Find My Vpet

**Find My Vpet** is under **Settings > Advanced**, below the two startup options. It defaults off. Enable it, choose **ALT** or **CTRL**, then click the key box and press a key to map it. Clicking clears the previous key; the message confirms the new mapping or explains a failure. With the default ALT + ALT binding, double-tap Alt within half a second. CTRL + CTRL likewise uses two taps. Other bindings use the modifier and mapped key together.

The shortcut circles the pet and dims all connected displays, following the pet as it moves and fading away over one second. It also reveals a pet underneath application windows without changing Window Location. The spotlight passes mouse input through and keeps keyboard focus in your current application. `bin/Vpet.Tests.exe --finder-window-tests` checks key capture, global activation, screen overlays, and D20 hover with isolated preferences.

## Included

- Four-frame directional idle and five-frame walking cycles with eight facings.
- Original artwork read from the unchanged reference. The renderer isolates the 45 source frames and removes only exterior white background pixels at runtime; no new AI artwork is required.
- Per-pixel alpha window transparency: empty pixels pass clicks through.
- Free Roam, a fixed restricted area with a draggable center and on-screen fence, and Static mode. Each selection shows its own description.
- A 0–100 speed slider with 50 centered on its tick marks; 0 stops autonomous movement.
- Constant normalized travel speed, arrival slowdown, and direction hysteresis.
- Multi-monitor work areas, transitions across gaps, relocation when displays disappear, and DPI-adjusted sprite size/speed.
- Over Everything, ordinary Dynamic stacking, and Under All, which locks the pet and reactions beneath other application windows.
- Three personalities, nine native Windows emoji, weighted random selection, adjustable frequency, and live custom PNG emote loading.
- Replace any default emote image with your own PNG, or restore its original symbol. Replacements work for both interactions and random reactions.
- Sprite-sheet validation, animated previews, Apply, Restore Default, and template export.
- Saved settings, position, facing, fence geometry, and imported artwork; tray controls and single-instance protection.

## Sprite sheets

Use [Sprite Maker](SPRITE-MAKER.md) to turn a PNG sheet into a `.vpetsprite` package, with frame selections, optional diagonals and reactions, alignment, and independent animation speeds. Upload Custom Sprite accepts these packages. Previously installed legacy PNG pets still load.

The original annotated image is preserved at `assets/reference/Base Vpet Sprite Sheet.png`. It is an artwork guide, not an uploadable runtime grid.

Use **Settings → Sprite → Save default template** to export a transparent PNG. Default cells are **32 × 36**, in a **160 × 360** sheet with the rows below. Custom sheets can have any layout; Sprite Maker frames are at most **100 × 150** pixels.

Use **Download current sprite sheet**, below the default template button, to save the active pet's transparent PNG. Edit that image and upload it to Sprite Maker to select frames and export a .vpetsprite. Directional rows in downloaded sheets face left; optional emote rows follow the ten movement rows. The PNG alone does not contain frame-count metadata.

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
| Hunger | 🍽️ | U+1F37D |

Love and Hunger request emoji presentation with U+FE0F. Appearance follows the PC's installed Windows emoji font. Custom images take precedence; Restore original returns to these emoji. Hunger has normal random weight for every personality and appears when food is served. The renderer uses [Direct2D's color-font option](https://blogs.windows.com/windowsdeveloper/2017/06/06/using-color-fonts-beautiful-text-icons/) and caches the rendered defaults.

## Restricted area

Selecting **Restricted** shows its description and **Display restricted area** below the movement type. The checkbox starts checked on entering Restricted mode. The fence is rectangular: drag the plus control to move it, an edge to resize one dimension, or a corner to resize both. There is no radius slider. The independent fence is red with a **Restricted Area** label above its top-left corner. Fence controls and labels remain below the sprite and do not take keyboard focus; labels and the empty interior pass clicks through.

The fence stays fixed until you move or resize it. Dragging the pet, changing speed, hiding the fence, or switching movement modes does not recenter it. If moving or shrinking the fence excludes the pet, the pet returns to its center. Dropping the pet outside likewise returns it to the center. The full sprite fits within the fence and the monitor work area. Hiding the overlay leaves the restriction active. Position and dimensions are saved; disconnected-display recovery moves unreachable fences onto a connected screen. Older saved radii initialize an independent rectangle with the same diameter.

**Advanced > Sync Play Zone with Restricted Area** defaults on. In Restricted mode, the pet uses the toy play-zone fence instead of the separate restricted fence. **Display restricted area** and **Display Play Zone** control the same overlay; it works even with the chest closed. Turn sync off to restore the independent saved restricted fence. Toy boundaries remain active in either case.

The right-click menu also has **Movement Controls → Display restricted area**, directly below **Movement settings…**. This toggle is enabled in Restricted mode and shares the settings checkbox's value.

Speech bubbles use one continuous body-and-tail outline, drawn after the emoji or custom image. This keeps the full border visible for both upward- and downward-pointing bubbles.

## Toy chest and fetch

Right-click your pet and select **Display Items > Toy Chest**. Display Items is the first menu entry. A chest and a blue dashed rectangular play zone appear, labeled **Play Zone** above its top-left corner. Labels stay on screen when a fence touches the top edge. Drag the center control to move the zone; drag any edge to change one dimension, or a corner to change both. Drag the chest anywhere inside the fence. If moving or shrinking the fence leaves the chest outside, it moves to the new center. The center control stays accessible above the chest when they overlap.

The zone fits inside one monitor's usable area, avoiding taskbars and gaps between screens. Move its center onto another monitor to move the play space there. It defaults to 480 × 320 pixels at 100% scaling; minimum dimensions expand to fit the full pet, and its maximum is the monitor's working area. Toy size follows the app's launch display scale. The chest, fence, and toys follow **Window Location**, stay below the pet, and never take keyboard focus. Empty transparent space passes clicks through.

Right-click the chest for **Display Play Zone**, **Clean Up Toys**, toy toggles for **Ball**, **Triangle**, **Coin**, **Card**, and **D20**, followed by **Help Messages** and **Close Toy Chest**. Clean Up Toys removes all toys and cancels active play while leaving the chest and fence available. It is greyed out when no toys are present and becomes available as soon as any toy is added. Toggle a toy off to remove it and cancel its active interaction. Hiding the fence keeps its boundaries active. Help Messages stays directly above Close Toy Chest at the bottom of the menu. It defaults on and displays hints above the chest only while hovering over the fence border/center or a toy; disable it to hide those hints. The fence uses a plus-shaped center control.

Left-click the ball for three bounces, each reaching half the previous height. **Left-drag to reposition** it inside the fence; **right-drag to aim** in the opposite direction. Release the right button to launch along the solid red arrow; a longer pull increases power up to a limit. The ball reflects off the fence and slows to a stop. Pet return shots vary randomly in both direction and strength.

User launches interrupt wandering or resting. The pet approaches the predicted resting point, waits for the ball to stop, pauses for ¼ second, shakes for ½ second, and launches it in a random direction. A pet launch does not immediately trigger another fetch; later spontaneous toy play is independent. Fetching temporarily overrides Static mode and the restricted fence; speed 0 uses speed 50 during fetching. Saved settings are unchanged. Restricted pets walk back inside their fence afterwards. At screen edges the pet approaches as closely as its full sprite can fit. Hovering pauses the pet; dragging it cancels fetching. Moving or resizing the play zone updates the fetch destination.

Chest visibility, fence visibility, play-zone geometry and chest position are saved. Closing the chest removes all toys and ends active play; a synced restricted fence can remain visible and active. Restarting does not restore an in-progress game. A disconnected monitor relocates the play zone and toys to a connected work area.

`bin/Vpet.Tests.exe --toy-window-tests` exercises real desktop controls with isolated preferences, including dragging, resizing, aiming, outside-menu dismissal and all three stacking modes.

The **Triangle** toggle adds or removes a small instrument. Right-click the triangle, including its hollow center, to choose **Chime** (the default), **Honk**, or **Drum**. The choice is saved and used for both your clicks and the pet's replies. Selecting a sound does not count as a tap. Tap the instrument repeatedly to play the selected sound on each press. After a brief pause (0.75 seconds), the pet walks to the instrument and repeats the same number and rhythm of taps. Very fast taps are spaced at least 0.1 seconds apart during playback so each is audible. Drag the triangle to move it inside the fence. A new tap during playback starts a new phrase; launching the ball or picking up the pet interrupts the phrase.

**Sound Setting**, at the top of the triangle menu, opens a **0–100% volume** slider. **Test sound** previews the selected instrument at that level without changing saved volume or adding a remembered tap. Choose **Save** to apply it to both user and pet notes, or Cancel to retain the previous level. At 0%, the instrument is muted.

Click the **Coin** to summon the pet to a clear spot beside it, using the same spacing as the card. On arrival it pauses for ¼ second, shakes for ½ second, then flips the coin upward around the horizontal X axis. The animation may rise outside the fence or beyond the display's visible edge before returning to its resting position. The coin itself is plain gold, with no face values. The supplied Heads or Tails artwork appears in the speech bubble only after landing. Drag the coin to reposition it inside the zone.

Click the face-down **Card** to start High/Low. The pet walks to a clear spot beside the card and announces a card. Its position accounts for the full sprite and screen edges so both card halves stay accessible. Within **30 seconds**, click the top half (up arrow) for **High**, or the bottom half (down arrow) for **Low**. Without a choice, the pet clears its announcement and moves away before resuming normal movement. Settings and other movement pauses suspend the timer. The flip excludes the exact announced card. A correct guess shows **Love**, an incorrect guess shows **Sad**, and equal ranks show **Question**. Suits do not change rank; Ace (A) is lowest, followed by 2–10, J, Q, K. **Every round resets all 52 cards**. Clicking a revealed card only flips it face down; click again to start another round. Drag it to reposition. Autonomous play approaches first, turns a face-up card down, then starts a game and chooses High or Low randomly.

**Left-drag the D20 to reposition** it inside the fence. **Right-drag and release** along its red arrow to launch it, or left-click for a random roll. Hover over the D20 to pause the pet's current action, face the die, and show its current value in the speech bubble. Moving away resumes the paused action and restores the usual reaction or game announcement. It uses its original 46 × 46 pixel size at 100% scaling, including transparent margins. Small numbers fit inside the central face without overlapping its lines, including double digits. It spins, ricochets within the fence, and slows to a stop. The pet stops and faces the die without chasing, then announces its final **1–20** result. Card, coin, and die results are special announcements, excluded from random/customizable reactions just like the pause indicator.

Available toys attract spontaneous play every **60–120 seconds**, chosen randomly after the previous action. The pet may return a ball, play a triangle phrase, flip a coin, play High/Low, or roll the D20. Before an autonomous D20 roll, the pet walks to a clear position beside it, then launches and watches the die. User clicks and pulls still launch it immediately. Toy visits temporarily override Static, speed 0, and restricted roaming just like user-triggered fetching; saved movement rules resume afterwards. Moving toys or the chest, opening the chest or triangle menus, and changing instrument sound settings keep the pet walking. Moving an approached toy updates the visit; interaction at arrival waits until it is released. The pet's own menu, Settings, hovering over or dragging the pet, and moving a fence still pause movement.

Random reaction frequency is separate: **Often: 15–30 seconds**, **Sometimes: 30–60 seconds**, and **Rarely: 90–120 seconds**.

A **pause symbol** appears in the speech bubble only while the Vpet settings window is open. Toy interactions and right-click menus do not trigger it. It is a built-in status indicator, excluded from replaceable reactions. Explicit **Try It Out** previews temporarily replace it, then the pause symbol returns.

## Plate and food

Choose **Display Items > Plate** to show or hide a white plate. Left-drag it around the desktop; it is independent of both fences and uses the pet's window location setting. Closing the chest or cleaning up toys leaves the plate available. Its visibility, position, and default food are saved; it starts empty after restarting Vpet.

The plate and pudding are 75% of their original size. At 100% Windows scaling their shared transparent canvas is 72 × 63 pixels; both artwork and interaction placement follow display scaling.

Right-click the plate and select **Pudding** to make it the default food and serve it. Left-click an empty plate to serve the default again. Pudding has caramel, whipped cream, and a strawberry, with whole, two-thirds, and one-third portions. Clicking a nonempty plate leaves its food intact; selecting Pudding from the menu serves a fresh portion.

Serving food interrupts the pet's current action and temporarily overrides Static, zero speed, and movement fences. The pet shows Hunger and walks to a position just above the plate, behind the food. For each bite, it shakes for one eighth of a second, pauses for another eighth of a second, then removes one third of the food. A brief rest separates the bites. After three bites the plate is empty and normal movement resumes; restricted pets walk back inside their fence. Moving the plate redirects the pet and delays eating until release. Picking up the pet or starting another toy interaction interrupts the visit. Choose **Remove Plate** in the plate menu to hide it, clear the food, and end feeding. The pet menu can display it again.

## Joystick and arcade

Choose **Display Items > Joystick** to show or hide the supplied Joystick.png artwork. Left-drag it anywhere on a connected display, independently of movement fences. It follows Window Location. Click it to send the pet behind it, temporarily overriding Static, zero speed, and restricted movement. On arrival, **Arcade Window** opens and the desktop pet hides. The arcade always uses normal window stacking, allowing other applications above it, while keeping all other Vpet assets beneath it regardless of Window Location. Closing the window restores the pet behind the joystick and resumes its normal rules and asset stacking. Right-click the joystick for **Remove Joystick**.

The arcade lobby has five cabinets and your current pet walking in front of them. **Dance Time** and **Simon Says** are playable; the three grey cabinets are reserved. Both games offer **Easy**, **Normal**, and **Hard**, defaulting to Easy, a three-second countdown, and WASD controls. Use the circular-arrow button beside the upper square to switch to arrow keys. **Close Game** returns to the lobby. The key choice, music volume, and each game's separate high scores for all three difficulties are saved in your settings. Click the score box to view the selected difficulty's high score.

**Dance Time** plays the supplied Easy, Normal, or Hard MP3. The arcade prepares all songs in the background, decoding them to memory before playback. Start becomes available only when the selected song is completely prepared; the countdown starts an already queued audio buffer. Playback keeps the native sample rate and uses the audio sample clock for target timing. Replay and difficulty changes reuse prepared songs. Targets travel inward toward matching squares at 1x, 1.5x, or 2x speed, with three, two, or one allowed misses. Press the matching key while a target overlaps its square: at least 1% earns **Good / 10**, 50% **Great / 20**, and 90% **Excellent / 30**. Scored targets pulse before disappearing. An early/late press or a target reaching the pet costs a miss. Three consecutive Excellents start a streak at **1.0x**; each additional Excellent adds **0.1**. A bold, flashing **Streak Combo** indicator shows its multiplier at the top right. Starting with the third Excellent, points accumulate in a separate **Streak** total. When Good, Great, a miss, or song completion ends the streak, only those points receive the multiplier and join **Calculated** points, rounded to the nearest whole point. Earlier points and completed streak bonuses are never multiplied again. A miss or successful song completion banks the calculated total. The final miss ends the game immediately and discards its entire score. Completing the song records a new high score when greater.

**Practice mode**, to the right of Difficulty, plays the full song without scoring, streak bonuses, or a miss limit. A numeric miss total at the top left replaces the miss-limit circles. Hits retain their visual feedback, and misses never end the song. Practice rounds leave high scores unchanged. The choice is saved, defaults to off, and can be changed between rounds.

The vertical music volume slider stays available before, during, and after a round, with its title and percentage in one padded neutral column. At **100%**, music plays at the normal level controlled by your Windows/device volume. Lower settings attenuate the music progressively, with a curve that provides useful quiet levels near the bottom; **0% mutes it**. Saved slider choices are retained, and new preferences default to **25%**. This controls the music stream without changing the device's master volume. Song elapsed/total time appears in the header to the left of Score Card, including in Practice mode when scoring is hidden.

**Background**, below Difficulty, offers **Dynamic** (default), **Static**, and **Off**. Dynamic displays the supplied Dance Floor PNG and instantly rotates it clockwise 90 degrees every one second, cycling through four orientations. Static displays the original floor without rotating; Off uses the same dark neutral color as the game canvas and volume column. The misses section and four fixed directional squares have padded neutral backings, while moving targets use bright pink fills and borders. The choice is saved and can be changed during a round. Both games' play spaces and backgrounds remain square when the window is resized; their names appear only in the window title.

**Simon Says** uses red/up, blue/down, green/left, and yellow/right squares without music. **Background**, below Difficulty, offers **On** (default) to display the supplied Simon Floor PNG, or **Off** for the dark neutral game background. This choice is saved separately from Dance Time. Easy starts with **one button**, Normal with **three**, and Hard with **five**; completed rounds add one button. Each highlighted direction plays its own tone, and your inputs play the same matching tones. The tones are prepared before playing, have smooth attack/release fades, and finish their fade-outs even when presses overlap. Watch the pet show a sequence, then repeat the entire sequence within **five seconds**. Each correct round adds 50 pending points. The last input keeps its normal highlight, then the pet faces down for a full one-second pause before beginning a sequence with one more square. Easy shows each step for 0.9 seconds, Normal 0.6, and Hard 0.4. A wrong key or timeout ends the game, banks your points, and records a new high score when greater.

In both games, **Start** becomes **Stop** during countdowns and active rounds. Stop cancels the round immediately, stopping playback and discarding its score without changing your high score. Start returns after stopping or finishing a round.

`bin/Vpet.Tests.exe --arcade-window-tests` verifies joystick interactions, both games, saved scores, window resizing, desktop return, and muted playback of all three bundled songs using isolated preferences.

## Reminders

Choose **Reminders**, between **Settings** and **Check for Updates** with a separator on each side, to open the **Reminder Window**. **Add Reminder** opens an **Edit Reminder** box above the saved list. Saved entries are read-only; use the **Edit** button on the left to load one into the editor.

- **One-Time:** choose a date and time.
- **Recurring:** select one or more days from **Sun–Sat**, then set each day's frequency: **Every**, **Every other**, or the **First**, **Second**, **Third**, or **Fourth** occurrence of that weekday in the month. Each day starts at Every. Every other uses the Sunday–Saturday week when that day was selected as its first week; editing an existing selected day retains its original week.
- All times have explicit **AM/PM** controls and use the PC's local time. Every selected recurring day uses the reminder's common time.
- Enter a message of up to **200 characters**. HTTP and HTTPS links are clickable in saved messages and reminder bubbles.
- **Active** starts checked. **Save** adds or updates the locked entry. **Delete** erases the selected reminder; deleting an unsaved draft just closes it. **Cancel** discards unsaved edits.

When due, Vpet plays a simple three-tone chime and shows a separate speech bubble beside the pet with a **Dismiss** button. It follows the pet while walking, dragging, or moving between displays, updating alongside the emote bubble. It keeps the normal emote bubble available and does not steal keyboard focus. Hovering over the reminder holds the pet still for reading and clicking; it does not use the Settings pause emote. The bubble follows Window Location and remains inside the active monitor's working area. Dismiss acknowledges that occurrence; recurring reminders can fire again at their next scheduled time. One-time reminders become Completed in the list and do not repeat.

Vpet must be running to notify you. Reminders, undismissed messages, and firing history are saved across restarts. After sleep or downtime, Vpet shows the latest missed occurrence of each active reminder. Multiple reminders queue in due-time order, one bubble at a time. Repeated occurrences of an undismissed recurring reminder are combined to avoid a backlog. Saving edits restarts that reminder's schedule from the save time and clears its old pending message. Turning it inactive or deleting it also cancels its pending message. For reminders after signing in, enable **Load Vpet on PC startup** in Advanced settings.

`bin/Vpet.Tests.exe --reminder-window-tests` checks reminder editing and notifications, toy motion during dragging/menus, cleanup availability, and Sprite Maker resizing using isolated data.

## Local storage

Normal settings and imported assets live under `%LOCALAPPDATA%\VpetPrototype`:

- `settings.json`: preferences and position.
- `reminders.json`: reminder schedules, messages, firing history, and pending notifications. Successful saves keep the previous file as `reminders.json.bak`.
- `pet.vpetsprite`: active custom sprite package; legacy `pet.png`: accepted custom sprite sheet, copied independently of the original upload.
- `Emotes/`: live custom reactions.
- `DefaultEmotes/`: replacements for the nine built-in reaction images.
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

**Load Last Project**, next to Load Project in Sprite Maker, reopens the most recently opened or saved .vpetproject. The path is remembered across Vpet restarts. Open or save a project once to enable the button; save edits before closing to resume them later. If the file moves, use Load Project to locate it again. Missing or unreadable projects leave current work intact.

**Update Sprite Sheet** replaces the PNG in an open project while keeping all frame mappings, sizes, tweak offsets, and animation options. Keep the revised artwork in the same sheet positions for the mappings to line up. Save Project to keep the new image, then use Tweak and Complete. Empty or out-of-bounds selections are flagged for repair; invalid replacement files leave existing work intact.
