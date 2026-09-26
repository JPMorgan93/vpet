# Prototype validation

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
