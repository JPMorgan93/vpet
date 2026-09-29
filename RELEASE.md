# Vpet 1.7.0

## Files to distribute

- `dist/Vpet-Setup-1.7.0-Windows-x64.exe` — standalone Windows EXE installer.
- `dist/SHA256SUMS.txt` — SHA-256 checksum for that exact installer.

The installer contains the application, reference sprite sheet, Heads/Tails PNGs, icon files, and getting-started guide. The supplied Vpet Pixel icon is retained unchanged as the project's `Vpet.ico`; the installed `Vpet-Pixel.ico` copy gives shortcuts a new icon path so they do not reuse the old artwork's cache entry. Source code, tests, development settings, logs, and compiler tools are excluded. GitHub Actions attaches the installer and checksum to a public release after a successful main-branch build. See `GITHUB.md`.

## Installation behavior

- Windows 10 version 1903 or later / Windows 11; x64 application. .NET Framework 4.8 or later is checked before installation. ARM64 emulation has not been tested.
- Installs to `%LOCALAPPDATA%\Programs\Vpet` for the current Windows user without requiring administrator rights.
- Creates a Start menu shortcut and offers an optional desktop shortcut.
- Uses the supplied icon in the application EXE, tray, settings window, installer, shortcuts, and Windows uninstall entry.
- First installation offers to launch the app. Updates start without a continue prompt, show progress, preserve shortcut choices, and reopen the pet with a completion screen explaining what changed. Release notes are shown only after successful installation. Both current and older updater entry points skip the setup wizard for an existing installation. The completion history is embedded in the app and includes every release after the last version run, grouped newest first, even offline. Older apps without a last-run record fall back to the installed version.
- Blocks installation while Vpet is running. Close the pet from its menu first.
- Registers an uninstaller. Personal settings and artwork in `%LOCALAPPDATA%\VpetPrototype` are preserved. That folder name and the existing single-instance mutex remain unchanged for compatibility.
- Uses a stable installer AppId for updates. Auto-update on app startup is optional (No by default) and installs new releases automatically when enabled. Manual Check for updates shows the current/available version and waits for the Update button. Periodic checks while running continue to notify. Startup at Windows sign-in is optional under Sprite and defaults to No. Uninstall removes the startup entry for this installation.

## Rebuild

Run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build-release.ps1`.

This compiles to `bin/release`, runs the existing automated suite, builds the installer with Inno Setup, and generates its checksum. The separate release directory allows a development pet to remain running during compilation. The script finds the local compiler at `.tools/InnoSetup/ISCC.exe`, or accepts `-CompilerPath`.

The build tool used for this release is Inno Setup 6.4.3, downloaded from its official GitHub release. Its installer Authenticode signature was verified as valid, with publisher Pyrsys B.V. The tool was installed under `.tools/InnoSetup` for the current user; it is not bundled with Vpet.

For future versions, update `release.json` and the first section of `CHANGELOG.md`. The build checks that their version numbers match, embeds that section as the app's update description, and supplies the same description to GitHub Releases. The app metadata, generated manifest, settings title, and installer version derive from that single version value. Keep the installer AppId unchanged. Merge a tested candidate from `test` to `main` to publish it.

## Original 1.0.0 installer validation on 2026-09-26

- 628 existing application assertions passed using the release binary sources.
- Actual EXE installer installed successfully into a workspace test folder.
- Verified installed version 1.0.0, executable checksum, supplied icon checksum, and Start menu shortcut target/icon.
- Launched the installed application in smoke-test mode: pet/reaction rendering, all three settings tabs, layer switching, and two connected displays passed.
- Reinstalled the same release successfully.
- Uninstalled successfully; executable, Start menu shortcut, and uninstall registry entry were removed. An unrelated file was preserved.
- The running development pet was closed normally for testing and restarted afterward.

Test logs and captures remain under `bin/installer-test-55ea0e81a5d844d0becc6e0c091ef414`. The repeatable test script is `installer/Test-Installer.ps1`; it changes the current user's installer registry and Start menu, and should be run on a test account without an installed Vpet release. Tests were performed on this development PC, not a clean second PC or every supported Windows version. Optional desktop shortcut creation was configured but not exercised by the automated installation test.

## Signing status

The Vpet application and installer are **unsigned**. No publisher certificate was provided or purchased. Windows may show an unknown-publisher or SmartScreen warning. The signed build-tool download does not sign Vpet.

For publisher verification, sign and timestamp the release application with your code-signing certificate before packaging, configure Inno Setup signing for the installer/uninstaller, and regenerate the checksum after signing. Do not publish the existing checksum for a newly signed file. Signing is separate from version numbering and does not guarantee immediate SmartScreen reputation.
