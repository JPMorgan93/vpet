# Vpet validation

## Pending release: Find My Vpet and interaction adjustments

6,068 automated assertions passed. New coverage checks default-off and saved locator settings, key normalization, modifier double taps, normal chords, held-key repeat rejection, invalid/cleared mappings, one-second fade timing, moving spotlight geometry, transparency and continuity across display boundaries, the eighth-second eating pause before food disappears, and the three distinct notes in the reminder WAV.

258 native UI checks passed: 25 locator/settings/D20-hover, 18 plate/Hunger, 139 expanded settings/game, and 76 reminder/adjustment checks. The locator checks inject only synthetic test shortcuts into isolated windows, verify capture feedback and persistence, activation while another app is focused, no focus theft, click-through/topmost overlays on both connected displays, fade dismissal, and hook cleanup. D20 checks cover stopping/facing/value display and resuming walking or feeding after hover. The Advanced layout was visually inspected at the minimum test width. Startup and auto-update controls are first, with vertical scrolling for the remaining settings.

Live desktop tests depend on cursor position. An initial expanded-game run and one die-hover run failed; unchanged reruns passed. The reminder menu test now moves the pointer away from the die after its drag checks, because D20 hover is intentionally a separate reason to pause. The updater-window harness stalled before invoking its fake Update callback in both the candidate and the unchanged September 30 1.9.1 binary; it is not counted as passing. Temporary diagnostic changes were removed. The relocated auto-update setting's on/off persistence is covered by the passing locator suite.

The candidate EXE installer built successfully. The isolated installer/update test passed artwork/icon verification, fresh installation, progress-only updates, preserved destination and shortcut choices, skipped-version history, the legacy updater entry point, completion/relaunch, and cleanup. The normal Vpet installation and personal settings were unchanged. Version selection and public promotion are pending the owner's minor-versus-patch decision.

## Version 1.9.1

6,033 automated assertions passed, including the smaller artwork, all three bites, feeding position, and screen-edge recovery. The 72 × 63 pixel render states at 100% display scaling were visually inspected. 94 native UI checks passed: 18 plate/Hunger checks for serving, dragging, and window placement, plus 76 reminder/adjustment checks including the separators above and below Reminders. Tests use isolated preferences.

## Version 1.9.0

6,033 automated assertions passed. New coverage exercises serving food in all movement modes with shared and independent fences, three one-eighth-second bites, portion rendering, restriction return, interruption, plate relocation, display crossings/disconnection, persistence, and coin flips while another toy is dragged even over the pet. Hunger tests cover the system emoji, replacement/restore, optional animation export, and migration of version 3/4 projects without losing their existing reaction mappings, sizes, offsets, or speeds. The pudding's four render states were visually inspected.

425 native UI checks passed: 18 plate/Hunger, 121 Sprite Maker, 79 toy controls, 131 expanded settings/games, and 76 reminder/adjustment checks. These include native plate clicks and dragging, the new menus, optional Hunger controls, transparency, all three window layers, food above the pet, and Under All below application windows. Tests use isolated preferences.

The candidate EXE installer built successfully. The isolated update test passed fresh installation, both updater entry points, preservation of shortcut choices and destination, supplied artwork/icon checks, skipped-version notes, completion/relaunch, and uninstall. It used a separate application ID and left the normal Vpet installation unchanged. The owner explicitly chose the 1.9.0 minor release for the new plate, feeding, and Hunger features.

## Version 1.8.1

5,850 automated assertions passed. The new native regression reproduced the original failure: the rendered pet was on a negative-origin display while the reminder used the unchanged WinForms bounds at (0, 0), 300 x 300. Layered windows now record the bounds successfully passed to UpdateLayeredWindow, and reminders use those coordinates in the pet's rendering pass.

588 reminder-follow UI assertions and 76 existing reminder/adjustment UI checks passed. They compare reminder placement with native GetWindowRect results during real mouse drags, walking, all three window layers, named pets with emotes, screen corners, crossings in both directions across this PC's two connected displays, and release settling. Dismiss remains effective after subsequent renders. Tests use isolated preferences and reminders.

## Version 1.8.0

5,850 automated assertions passed. Reminder checks use explicit calendar fixtures for every weekday/frequency; midnight/noon conversion; monthly and leap-year boundaries; skipped and repeated clock hours; inactive and completed reminders; missed-occurrence catch-up; restart persistence; queued and coalesced notifications; deletion; 200-character limits; atomic backups; unreadable-file preservation; and HTTP/HTTPS link validation. Movement checks cover continued visits while the ball, triangle, coin, or card is being moved.

75 new reminder/adjustment UI checks, 119 existing Sprite Maker checks, 79 toy-window checks, and 131 expanded settings/game checks passed. Native tests cover Add/Edit/Save/Delete/Cancel, Sunday-first multi-day selection and independent frequencies, read-only saved messages, explicit AM/PM, responsive layouts, non-activating reminder bubbles, Dismiss, full-length messages at 100%, 150%, and 200% scale, cleanup availability for every toy, pet movement during toy/chest drags and both menus and instrument settings, both Sprite Maker dividers, and visible footer controls. Reminder and editor screenshots were reviewed, including an actual on-screen capture to verify rich-text links. Tests use separate data directories and do not alter the personal installation or reminders. The owner explicitly selected the 1.8.0 minor release for the new Reminders feature.

The 1.8.0 installer passed the isolated installation/update test. It preserved destination, shortcut choices, unrelated files, and skipped-release history; installed artwork and icons matched; legacy updater entry points skipped setup questions; and completion relaunched the installed app through its smoke test. Cleanup removed the test installation and shortcuts while leaving the personal installation and preferences intact.

## Version 1.7.0

4,550 automated assertions passed. New coverage checks manual face-up card turnover without starting a game; autonomous approach before turning the card down; the 30-second choice timeout, paused timers, and departure in every movement mode at speed zero; left-drag containment and cancellation for ball and die; upward coin flips beyond fence and display bounds; clearing all toys while retaining chest and fence; and red/blue fence artwork.

79 native toy-window checks, 131 expanded settings/game checks, and 65 updater-window checks passed. Actual mouse messages verify left-drag positioning without an arrow, right-drag aiming and launching, the D20 hover speech bubble and removal on pointer exit, face-up versus face-down card clicks, toy cleanup and unchecked menu items, and both menu orders. Existing shared/independent fence controls, sound volume, animation speed, responsive settings, and updater dialogs remain covered. The expanded suite now drains the initial Shown event before stopping its timer and centers test fences independently of the user's cursor position. Fence labels and colors were visually inspected.

The 1.7.0 EXE installer passed the isolated installation/update test: supplied artwork and shortcut icons matched, destination and shortcut choices were preserved, skipped-release notes survived consecutive upgrades, legacy update entry points skipped the setup wizard, and completion relaunched the installed app through its smoke test. Cleanup removed the test installation and its shortcuts. The personal installation and preferences were unchanged.

## Version 1.6.3

4,474 automated assertions passed. New checks cover Ace labels and unchanged low-rank comparisons in all four suits; autonomous D20 visits in every movement mode with walking speed zero; no launch before arrival or while settings pause the visit; a clear adjacent launch position; stationary watching and result announcement; return to saved restrictions; rerouting after moving the play zone or disconnecting its display; and cancellation or interruption by user launches, pet dragging, and toy removal.

110 native settings/game UI checks passed, including the existing card and D20 mouse controls. The four Ace faces and their matching speech-bubble emotes were visually inspected.

## Version 1.6.2

4,429 automated assertions passed. Coin and card spacing checks cover screen centers and corners with small, default, and maximum-size sprite frames at 100%, 150%, and 200% scaling. Original-size D20 containment and rolling remain covered. Pixel comparisons verify every die value from 1–20 is visible and does not paint over face lines at multiple scales and rotation angles; contact sheets were inspected at normal and enlarged scale.

110 native settings/game UI checks passed, including the rendered pet standing clear of the coin, both card halves remaining clickable, the restored 46 × 46 D20 window at 100% scale, and pulling/launching the die.

## Version 1.6.1

4,333 automated assertions passed. New checks cover clear card-playing positions at the center and all four screen corners for 16 × 16, 32 × 36, and 100 × 150 source frames at 100%, 150%, and 200% scaling. They also verify that the enlarged D20 fits the minimum fence and remains fully contained through ricochets after moving from a 200% display to a 100% display. Existing coin outcomes and timing, card relocation/interruption, and die-watching behavior remain covered.

108 native settings/game UI checks passed, including the doubled D20 window, pulling and launching it, and actual Windows hit testing of both card choices while the rendered pet stands beside the card. The blank coin and its X-axis flip contact sheet, plus the enlarged die artwork, were visually inspected.

## Version 1.6.0

4,241 automated assertions passed. New coverage checks default/migrated sound volume and exact PCM scaling/mute; independent animation speeds through project saving and runtime exports; rectangular fence migration, containment, resizing, synchronization, and monitor disconnection; every High/Low rank comparison, equal-rank draws, and all 51 permitted second cards; coin pause/shake/landing timing; persistent called cards and safe relocation/interruption; D20 containment, all-edge ricochets, watching without walking, and display recovery; and spontaneous play of every new toy with return to normal movement rules.

119 native Sprite Maker checks, 72 toy chest checks, 106 expanded settings/game checks, and 65 updater-window checks passed. New native checks exercise actual Coin/Card/D20 clicks and pull gestures, independent/shared fence movement and resizing, the hidden-chest shared fence, volume Test/Save/Cancel, immediate per-animation speed preview, and all four settings tabs at narrow and wide widths without horizontal scrolling or clipped descriptions. Settings text measurements use the production text renderer. New windows and game artwork were visually inspected. Audio generation and playback dispatch were checked; loudspeaker quality was not assessed.

The 1.6.0 EXE installer built successfully. The isolated installation/update test verified that both Heads and Tails PNGs exactly match the supplied reference artwork, shortcuts retain the supplied icon, updates preserve shortcut choices and destination, skipped-release notes survive consecutive installations, legacy updater entry points skip the setup wizard, and completion relaunches the installed app. The installed app rendered and captured all four settings tabs and exercised window layers on two connected displays. Cleanup removed the separate test installation and its shortcuts; the personal Vpet installation and preferences were unchanged.

## Version 1.5.4

3,075 automated assertions passed. New coverage verifies skipped-release notes, numeric version ordering, previous/future release exclusion, four-part installed version normalization, missing-version fallbacks, embedded offline history, acknowledgement cleanup, last-run persistence, and a nearly transparent clickable triangle interior.

119 native Sprite Maker/settings checks, 72 toy checks, and 65 update-window checks passed. They verify the bottom-right editor footer, removal of the Back button, actual Windows input routing through the triangle's hollow center, right-click menu opening, transparent outside corners, and simplified sound labels. Manual update checks show current/available versions, wait for Update, remain safe when closed during a check, display network errors, and join an in-flight startup check without installing automatically. Enabled startup auto-updates and quiet periodic checks remain covered. Dialog text wrapping and button visibility were verified; the changed windows were visually inspected.

The EXE installer built successfully. The isolated installer test simulated an older 1.5.1 binary and verified capture before replacement, preservation of unseen notes across consecutive installs, preference for the last version actually run, progress-only updates, unchanged shortcuts/destination, completion relaunch, and cleanup. Tests used a separate installation identity and preferences; the personal Vpet installation was unchanged.

## Version 1.5.3

3,063 automated assertions passed. New checks cover saved sound choices and default migration, distinct valid PCM waves with headroom and smooth endpoints, honk/chime pitch separation and snare rattle, and both user/pet tap playback. Sheet checks cover exact preservation of all mappings and options, revised pixels through preview/save/reload/export, DPI-independent image replacement, smaller sheets with repairable mapping errors, and rejected corrupt/opaque/oversized files leaving work intact.

117 native Sprite Maker/settings checks and 69 native toy checks passed. They cover the Update Sprite Sheet button, retained selection and zoom, unsaved/save behavior, error reporting, triangle menu attachment, mutually exclusive sound choices, immediate persistence, outside-click dismissal, closing the sound menu with the chest, and absence of a pause emote for toy menus. The updated editor layout was visually inspected. Audio generation and dispatch were verified; loudspeaker quality was not assessed. The EXE installer built successfully; tests used isolated files and preferences.

## Version 1.5.2

3,032 automated assertions and 109 native Sprite Maker/settings checks passed. Last-project checks cover older/null preference migration, initially disabled state, immediate persistence after opening, restoring dimensions and tweaks after a preferences reload, switching the remembered project, preserving history/current work after missing or corrupt file errors, and reopening through the new button. The button layout and missing-file guidance were visually inspected. The EXE installer built successfully; tests used isolated files and preferences.

## Version 1.5.1

3,032 automated assertions and 95 native Sprite Maker/settings checks passed. The settings checks verify that movement pauses and either right-click menu do not display the pause emote, opening settings does, explicit reaction previews still work, and closing settings removes the symbol immediately.

59 native toy checks passed, including Help Messages directly above Close Toy Chest below all toys, working menu toggles, and ball aiming without the pause emote. The first toy run reported a one-pixel difference in an existing mouse-driven resize assertion; an unchanged rerun passed. The EXE installer built successfully. No personal installation or preferences were changed.

## Version 1.5.0

3,032 automated assertions passed. New checks cover left/right source normalization, optional reaction frame counts and fallback, version 1/2 project migration, preserved selections when optional rows are disabled, clipping and runtime package round-trips. Toy checks cover exact triangle tap counts and rhythm in all movement modes, rapid taps, settings pauses, interruption, returning to the restricted circle, instrument containment and display recovery, preserved crossings on additional taps, remaining-note playback after relocation, randomized visits, and generated PCM audio bounds.

92 native Sprite Maker checks and 59 native toy checks passed, including the guides, optional controls, reaction-row Undo, live reaction animation while hovering/picking up and normal fallback, pause indicator/preview priority, triangle clicks versus dragging, Close Toy Chest, and toy layering in every window mode. The existing 35 updater/settings UI checks passed. Both Sprite Maker windows and the pause/triangle artwork were inspected. Audio generation and dispatch were verified; loudspeaker quality was not assessed.

The EXE installer built successfully. The isolated update test passed fresh installation, progress-only upgrades, preserved shortcuts/destination, icon checks, completion markers, compatibility with older update entry points, installed-app smoke testing, and cleanup. The personal installed Vpet and its settings were unchanged.

The separate legacy `--window-tests` harness completed all 42 assertions but its process then exited with Windows native callback error 0xC000041D (inner exception 0xC0020001). Event logs contain the same shutdown failure from earlier runs before this update. This is recorded as a failed process-level check, not a clean pass. The feature-specific native suites and normal installed-app smoke run exited successfully.

## Version 1.4.0

2,955 automated assertions passed. New checks cover independent animation sizes, shared sizes within a type, odd/even bottom-center padding, exact cropping on both axes, fully clipped blank frames through project save/export/runtime import, and version 1 project/sprite migration. Toy checks verify exactly three bounce peaks with successive half heights, varied pet return strengths, persisted help preferences, a red-only arrow, and a plus-shaped center icon.

78 native Sprite Maker checks passed, including restoring each animation's size fields and clipping in the preview. 49 native toy checks passed, including the Ball toggle, removal during fetch, hover-only help for border/center/ball, transparent-interior and covered-window exclusions, the actual help window above the chest, and hiding hints on pointer exit or toggle. 35 updater/settings UI checks passed using fake releases: saved auto-update settings take effect at startup, manual checks install directly regardless of the option, periodic checks remain notifications, offline automatic checks stay quiet, and release descriptions have completion controls only. No live update was installed by these UI tests. Settings, preview, arrow and hover-help artwork were visually inspected.

The 1.4.0 installer built successfully. The isolated installer test passed fresh installation, progress-only upgrades, preserved shortcut choices and destination, unchanged icon artwork, post-install completion marker, compatibility with the older helper's entry point, app relaunch with the full smoke test, and cleanup. The personal installed Vpet and its settings were not changed.

## Version 1.3.0

2,858 automated assertions passed. Toy checks cover saved preferences and older-settings migration; containment when dragging, moving or resizing; monitor/taskbar bounds and disconnection recovery; hidden fences; click bounce and reverse pull direction; and 72 varied ricochet trajectories at 100%, 125% and 200% scale. Predicted resting points match simulated endpoints within 0.04 pixels. Fetch checks cover all movement modes, zero speed, resting interruption, exact quarter-second pause and half-second shake within frame precision, returning to the unchanged restricted circle, launch ownership, relaunching, hover, cancellation, and crossing separated displays with different scaling.

37 isolated native toy UI checks passed for the pet/chest menus, center and corner dragging, ball bounce/aim/release, capture loss, outside-menu dismissal, hiding the complete toy group, and Over Everything / Dynamic / Under All ordering. The existing 42 native window-layer and input checks also passed. The toy artwork was visually inspected. The full app smoke test rendered chest, fence and ball and launched the ball using the live timer, exercised all three settings tabs and window modes, and completed on this PC's two displays. Tests used isolated preferences; the personal installed app was not changed. The 1.3.0 EXE installer built successfully.

The rectangular play zone occupies one monitor's working area at a time; its center can move it to another connected monitor. Screen-edge fetch destinations account for the full pet sprite and name, so the pet approaches as closely as it can remain visible. Additional physical monitor layouts were covered by model tests rather than hardware testing.

## Version 1.2.2

2,663 automated assertions and 73 native Sprite Maker checks passed. Zoom checks cover typed percentages, zoom buttons, 100%, Fit (including a 4096-pixel sheet), source-coordinate preservation around a chosen zoom anchor, scroll retention after Set, zoom limits, unchanged project dimensions/offsets, correct source-pixel nudging at 400%, and stable playback geometry. Both preview windows were captured and visually inspected.

The supplied `Vpet Pixel.ico` was copied unchanged to `assets/reference/Vpet.ico`; hashes match the uploaded file and packaged icon. The isolated installer test verified the installed icon's hash, the new icon path in both Start menu and desktop shortcuts, preserved shortcut choices, update completion, and app relaunch. The actual personal Vpet installation was left unchanged. The new icon is used as supplied, including its opaque square background; no generated artwork is used.

## Version 1.2.1

2,663 automated assertions passed. New checks cover asymmetric poses with different lowest pixels, faint alpha, nonstandard PNG DPI, exact pixel preservation through upload/save/export/runtime playback, repeatable ground alignment, and all-or-nothing rejection of poses that cannot fit a shared ground point. Update checks cover GitHub descriptions, old full-changelog bodies, missing descriptions, bundled offline notes, and completion markers that only match the installed version and are acknowledged once.

47 native Sprite Maker checks passed, including scroll preservation while focusing, placing, setting, and restoring frames on a large sheet; border dragging at every zoom; sheet-edge bounds; lost mouse capture; unchanged frame dimensions; and a stable preview baseline when scrubbing or switching between editing and playback. Six native update-dialog checks passed for scrolling descriptions, Later, and the completion screen. Both windows were captured and visually inspected. The installer built successfully.

The isolated installer update test passed: fresh installs omit the completion marker, upgrades record the installed version, shortcut choices and unrelated files remain intact, the older interactive updater skips optional setup screens, and completion relaunches the app in isolated smoke mode. The completion dialog and its acknowledgement were checked separately; the live personal Vpet installation was not updated by these tests.

## Version 1.2.0

2,624 assertions passed. Sprite Maker checks cover 1–5 frame counts (including five idle frames), nonconsecutive slots, project source/offset persistence, optional diagonals, cardinal fallback with hysteresis, bottom-center alignment without stretching, source-pixel preservation, invalid selections/clipping, unsupported versions, duplicate package entries, and opaque sheets. Eighteen native editor checks passed for Set/Clear, advancing slots, zoomed corner resizing, DPI-stable selection coordinates, shared dimensions, frame sliders, Magic Tweak/Undo, and applying/restarting a custom sprite package. Both editor windows were captured and visually inspected. The EXE installer built successfully.

## Version 1.1.6

2,339 assertions passed, including continuous drag positions at shared edges, progressive fragments in both directions across monitor gaps, mixed scaling, stacked and offset displays, release settling while hovered/in Static mode, outer-edge bounds, and display disconnection. An isolated native mouse-input test dragged the pet both ways across this PC's two monitors and verified retained capture, split fragments, arrival, and release. Other physical display layouts were covered by model tests rather than additional hardware.

## Version 1.1.5

819 assertions passed, including 512-pixel PNG persistence, rejection above the new limit, and smooth sampled edges. The compact emote list and high-resolution rendering were visually inspected. An isolated installer identity verified progress-only updates with desktop shortcuts both enabled and disabled, destination and unrelated-file preservation, compatibility with an older updater's interactive launch, completion confirmation, app relaunch in smoke mode, and cleanup. The existing personal Vpet installation was not changed. The repeatable isolated test is installer/Test-Update.ps1.

## Version 1.1.4

816 assertions passed. Pixel-level checks confirm horizontal and vertical centering of visible custom artwork with uneven transparent padding, portrait/landscape/square proportions, both tail orientations, and 100%, 125%, 150%, and 200% scaling. Bubble dimensions and complete outlines remain unchanged, and a fully transparent emote leaves the white body intact. Generated artwork was visually inspected, and the EXE installer compiled successfully.

## Version 1.1.3

695 automated assertions passed. An isolated settings-window check verified the empty state, uploaded names, default replacements, live refresh after insertion/removal, readable preview buttons, and correct preview selection after filename sorting changes. The custom-emote list was visually inspected. The EXE installer compiled successfully.

## Version 1.1.2

695 automated assertions passed, including startup default/migration/persistence and safely quoted executable paths. Seven isolated registry tests verified enable, disable, repeated disable, relocation, and preservation of unrelated values without changing the real startup key. The three-tab GUI smoke test passed and the new Sprite startup setting was visually inspected. The installer compiled with startup cleanup on uninstall. A Windows sign-out/reboot was not performed.

## Version 1.1.1

688 automated assertions passed, including bottom-edge name placement, transparent caption backgrounds, white letter outlines, and uninterrupted vertical display crossings. All 42 native window/input assertions passed, including an actual outside mouse click dismissing the menu and reaching the underlying test button, preserving submenu interaction, and dismissal after reopening. The caption artwork was visually inspected at 200% scaling.

## Version 1.1.0

The release build passes **666 assertions**, including persisted optional names, hide/hover/always modes, caption headroom at 100%, 125%, 150%, and 200% display scaling, and update policy checks. Update checks reject drafts, prereleases, downgrades, missing/duplicate assets, foreign download URLs, oversized installers, and missing/duplicate checksums. Name-and-bubble images are generated under `bin/release/test-artifacts/`.

The 1.1.0 EXE installer also passed installation, version/icon/Start menu shortcut checks, installed GUI smoke testing, reinstall, and uninstall. The Personality settings capture and name/bubble artwork were visually inspected. Native combo-box text is omitted by Windows Forms `DrawToBitmap`, as with prior captures. Logs are under `bin/installer-test-5353a4dd687246c2a2a32ff4a38dc871`.

The 1.0.0 release was also built and tested with the EXE installer. See `RELEASE.md` for installation, icon, shortcut, installed-app smoke test, reinstall, uninstall, and signing status. The release build passed the same 628 assertions.

Built and tested in the development workspace on 2026-09-26.

## Automated checks

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test`

Result: **628 assertions passed**. Includes half-second click shaking and Love across all personalities, no shake on drag release, down-facing interaction idle, the 1,000-pixel radius cap and saved-value migration, and complete bubble borders at multiple scales with both tail directions. Also covers native color emoji, fixed fence position, image replacements, artwork, and display travel. Crossing checks verify continuous positions and exact pixel coverage in all four directions across adjoining work areas, plus intermediate-display routing and progressive fragments for separated displays.

The visual contact sheet at `bin/test-artifacts/animation-preview.png` was inspected after the Unicode update. The character is visible against a transparent background, directions are correctly mirrored, and all eight specified Windows emoji render in color inside their bubbles. This uses the installed Windows emoji font rather than replacement image assets.

## Windows GUI smoke test

`bin/Vpet.exe --smoke-test`

Result: **application exited successfully** after rendering the native layered pet and reaction windows, opening the three settings tabs, and switching window layers. Two connected displays were enumerated. UI captures are in `bin/smoke-output/`.

The latest smoke test displays Restricted mode and its on-screen fence, opens all three settings tabs, and exercises the current layer modes. The Movement capture was inspected to confirm the removal of the shake-duration control and the new click guidance. The emoji contact sheet was inspected for complete bubble outlines. Settings captures use Windows Forms `DrawToBitmap`, which does not reliably capture native combo-box text; that text was not visually verified by these captures.

## Under All native window tests

`bin/Vpet.Tests.exe --window-tests`

Result: **37 assertions passed**, with native Windows windows created for the pet, a reaction, another application, and the restricted fence. Verified:

- Under All places both pet windows below the application and removes topmost status.
- Neither pet nor bubble can promote itself above the application, including an explicit topmost request.
- Movement and newly shown reactions preserve the ordering.
- The lock restores the ordering after another application sends itself to the bottom.
- Dynamic releases the lock; Over Everything can become topmost again.
- No desktop parent is used.
- The fence handle remains at the same native window position while the pet moves, follows explicit center changes, relocates the excluded pet, and shows/hides with the checkbox and movement mode.
- Fence rings and center handles remain below the sprite in every layer mode and resist promotion on interaction without breaking Under All.
- Native mouse-down/up messages delivered to the real pet window produce down idle, Love for every personality, and a half-second shake.

## Remaining hands-on checks

- Mouse hover, dragging, and transparent-pixel click-through over other applications.
- Real monitor unplug/replug, nonaligned-monitor crossing, and mixed display scaling.
- Under All on the user's desktop with third-party windows and wallpaper utilities.
- Normal Dynamic stacking against other applications, full-screen programs, and multiple Windows virtual desktops.

These are integration checks for the user's desktop environment. They are not covered by the pure movement-model tests or the brief GUI smoke test.
