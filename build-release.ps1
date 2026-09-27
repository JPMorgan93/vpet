param([string]$CompilerPath)
$ErrorActionPreference = 'Stop'
$release = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'release.json') -Raw | ConvertFrom-Json
$releaseDirectory = Join-Path $PSScriptRoot 'bin\release'
& (Join-Path $PSScriptRoot 'build.ps1') -Test -OutputDirectory $releaseDirectory
if (-not $CompilerPath) {
    $candidates = @(
        (Join-Path $PSScriptRoot '.tools\InnoSetup\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )
    $CompilerPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $CompilerPath) { throw 'Install Inno Setup 6.4.3 or later, or pass -CompilerPath pointing to ISCC.exe.' }
$appPath = Join-Path $releaseDirectory 'Vpet.exe'
if ([Diagnostics.FileVersionInfo]::GetVersionInfo($appPath).ProductVersion -ne $release.version) { throw 'Unexpected Vpet release version.' }
& $CompilerPath (Join-Path $PSScriptRoot 'installer\Vpet.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$installerPath = Join-Path $PSScriptRoot ('dist\Vpet-Setup-' + $release.version + '-Windows-x64.exe')
$hash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
$checksumPath = Join-Path $PSScriptRoot 'dist\SHA256SUMS.txt'
[IO.File]::WriteAllText($checksumPath, "$hash  $([IO.Path]::GetFileName($installerPath))`r`n", [Text.Encoding]::ASCII)
Copy-Item -LiteralPath (Join-Path $releaseDirectory 'ReleaseNotes.txt') -Destination (Join-Path $PSScriptRoot 'dist\ReleaseNotes.txt') -Force
Write-Output "Release installer: $installerPath"
Write-Output "SHA256: $hash"
Write-Output 'This build is unsigned. Public publisher verification requires signing with your code-signing certificate.'
