# Prototype validation

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
