# Develop and publish Vpet

Repository: https://github.com/JPMorgan93/vpet

## Save from VS Code

Work on **test**. The workspace task **Vpet: Auto-sync saves to test** waits for 20 seconds without edits, builds and tests the app, commits project changes, and pushes them to `test`. It is configured to start when you open this trusted project folder. Reload the VS Code window after initial setup, or start the task through **Terminal > Run Task**.

Syncing is enabled for the owner's current checkout through local Git configuration. Fresh clones do nothing automatically until their developer explicitly runs `git config vpet.autoSync true`. Disable it with `git config vpet.autoSync false` and stop the task. VS Code never runs automatic tasks in an untrusted workspace.

Saving a file writes it locally first. A successful task push confirms the GitHub copy is up to date. The task requires GitHub authentication through Git Credential Manager, configured Git author information, network access, and passing tests. It pauses for manually staged changes, a merge/rebase, or any branch other than `test`. Stop it with **Terminal > Terminate Task**. It does not force-push or auto-resolve conflicts; use VS Code **Source Control > Sync Changes** if GitHub has new commits or a push fails.

Only project source, assets, scripts, settings, and documentation are included. Build output, local tooling, logs, environment files, and common certificate formats are ignored. Do not put credentials in source files. The task does not run until VS Code starts it; changes made before that can be committed and synced manually.

## Test a candidate

Each push to `test` triggers **Build, test, and release Vpet** in GitHub Actions. It builds the app, runs automated checks, and produces a downloadable **vpet-installer** artifact. Install and test that candidate before promoting it. Test builds do not become public app updates.

Local tasks **Vpet: Build and test** and **Vpet: Build installer** are also available. The installer compiler can be prepared with `scripts/Install-BuildTools.ps1`.

## Make a public update

1. Increase `version` in `release.json` (for example, `1.1.0` to `1.1.1`) and update `CHANGELOG.md`.
2. Save/push to `test`; verify the Actions run and test its installer.
3. Open a pull request from `test` to **main**, review it, and merge when ready.
4. The main workflow builds/tests that exact commit, uploads the installer and `SHA256SUMS.txt` to a draft release, then publishes it as the latest stable release.

The installer is attached to **GitHub Releases**, not committed as a binary in Git history. Download the public installer at https://github.com/JPMorgan93/vpet/releases/latest. A version already published is never overwritten automatically; increment the version for a new release. If uploading fails, a draft may remain; inspect/delete that incomplete draft and rerun the workflow after resolving the failure.

The workflow has write access only in the main-branch publishing job. PRs and test-branch jobs get read access. No repository access token is embedded in Vpet. Branch protection is a separate GitHub setting; require the `build` check on main if you want GitHub to enforce promotion through reviewed pull requests. The workflow still rebuilds/tests main before publishing even without branch protection.

## What installed apps do

Vpet 1.1.0 and later check the repository's latest public release shortly after startup and every six hours while running. Failed checks retry after 30 minutes without interrupting the pet. Right-click **Check for updates** to check immediately. Drafts, prereleases, equal versions, and older versions are ignored.

When an update exists, Vpet shows a notification and **Install Vpet [version]** in its menu. **Check for updates** opens a status window with the installed version and either an up-to-date confirmation or the available version with an **Update** button. Choosing Update downloads the installer, verifies its size and SHA-256 checksum, closes normally, and shows installation progress without setup pages or shortcut questions. **Settings > Sprite > Auto-update on app startup** defaults to No; Yes installs automatically at startup. Periodic checks while running remain notifications.

The pet reopens with a completion screen containing every release's changes after the last version run, grouped newest first through the newly installed version. Descriptions appear only after installation and are bundled for offline access. The installer captures the previous installed version for older apps without last-run tracking and preserves unseen history across consecutive installations. Previous installation directory, shortcut choices, settings, names, and custom artwork are retained. Downloads go to `%LOCALAPPDATA%\VpetPrototype\Updates`.

Write update descriptions in the first `CHANGELOG.md` section, headed `# Vpet X.Y.Z`, matching `release.json`. Use `## Vpet ...` for older releases and retain those sections. The build extracts the latest section for GitHub Releases and embeds the full history for skipped-version completion notes. An up-to-date check shows status without repeating old descriptions.

The repository and release assets must remain public for unauthenticated app checks. Checksums detect damaged or mismatched downloads; they do not replace publisher signing. This installer is unsigned. Users on **1.0.0 must manually install 1.1.0 once** because the original app contains no updater.
