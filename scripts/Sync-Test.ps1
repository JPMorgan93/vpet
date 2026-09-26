param([int]$QuietSeconds = 20, [switch]$Once)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $projectRoot
if ((git config --get vpet.autoSync) -ne 'true') { Write-Output 'Auto-sync is disabled for this checkout. Enable with: git config vpet.autoSync true'; exit 0 }
$paths = @('src', 'assets', 'tests', 'installer', 'scripts', '.github', '.vscode', '.gitignore', 'README.md', 'RELEASE.md', 'VALIDATION.md', 'CHANGELOG.md', 'GITHUB.md', 'Vpet Development Specification.md', 'build.ps1', 'build-release.ps1', 'release.json', 'app.manifest', 'Launch Vpet.cmd', 'Create Desktop Shortcut.ps1')
Write-Output 'Vpet auto-sync: saves are committed and pushed only on test, after a quiet period and passing checks. Ctrl+C stops syncing.'
$previous = ''
$stableSince = Get-Date
$lastAttempt = ''
do {
    $branch = git branch --show-current
    if ($LASTEXITCODE -ne 0 -or $branch -ne 'test') { Write-Output 'Paused: switch to test to sync saves.'; Start-Sleep -Seconds 10; continue }
    if ((Test-Path '.git\MERGE_HEAD') -or (Test-Path '.git\rebase-merge') -or (Test-Path '.git\rebase-apply')) { Write-Output 'Paused: finish the merge or rebase first.'; Start-Sleep -Seconds 10; continue }
    $staged = git diff --cached --name-only
    if ($staged) { Write-Output 'Paused: commit or unstage your manually staged changes first.'; Start-Sleep -Seconds 10; continue }
    $changes = @(git status --porcelain --untracked-files=all -- $paths)
    # Include content hashes so repeated saves restart the quiet period.
    $diff = git diff -- $paths
    $untracked = @(git ls-files --others --exclude-standard -- $paths)
    $untrackedHashes = @($untracked | ForEach-Object { if (Test-Path -LiteralPath $_ -PathType Leaf) { (Get-FileHash -LiteralPath $_).Hash } })
    $fingerprint = ($changes + $diff + $untrackedHashes) -join "`n"
    if ($fingerprint -ne $previous) { $previous = $fingerprint; $stableSince = Get-Date }
    if ($changes.Count -gt 0 -and $fingerprint -ne $lastAttempt -and ((Get-Date) - $stableSince).TotalSeconds -ge $QuietSeconds) {
        $lastAttempt = $fingerprint
        try {
            git fetch origin test
            if ($LASTEXITCODE -ne 0) { throw 'Could not fetch test. Check GitHub sign-in/network access.' }
            git merge-base --is-ancestor origin/test HEAD
            if ($LASTEXITCODE -ne 0) { throw 'Remote test has new commits. Use VS Code Sync Changes to resolve them before auto-syncing.' }
            & (Join-Path $projectRoot 'build.ps1') -Test -OutputDirectory (Join-Path $projectRoot 'bin\autosync')
            if ((git branch --show-current) -ne 'test') { throw 'Branch changed while building; no automatic commit was made.' }
            if (git diff --cached --name-only) { throw 'Changes were staged manually during the build; no automatic commit was made.' }
            $latestChanges = @(git status --porcelain --untracked-files=all -- $paths)
            $latestDiff = git diff -- $paths
            $latestUntracked = @(git ls-files --others --exclude-standard -- $paths)
            $latestHashes = @($latestUntracked | ForEach-Object { if (Test-Path -LiteralPath $_ -PathType Leaf) { (Get-FileHash -LiteralPath $_).Hash } })
            if ((($latestChanges + $latestDiff + $latestHashes) -join "`n") -ne $fingerprint) { throw 'Files changed during the build; waiting to test the next saved version.' }
            $presentPaths = @($paths | Where-Object { (Test-Path -LiteralPath $_) -or (git ls-files -- $_) })
            git add -A -- $presentPaths
            if ($LASTEXITCODE -ne 0) { throw 'Could not stage changes.' }
            git commit -m ('Save tested Vpet changes ' + (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
            if ($LASTEXITCODE -ne 0) { throw 'Could not commit. Configure your Git author identity.' }
            git push origin HEAD:test
            if ($LASTEXITCODE -ne 0) { throw 'Push failed. Your commit is local; use VS Code Sync Changes to retry.' }
            Write-Output 'Saved to GitHub test. Public users are unchanged until you merge to main with a new version.'
        } catch { Write-Warning $_.Exception.Message }
    }
    if (-not $Once) { Start-Sleep -Seconds 5 }
} while (-not $Once)
