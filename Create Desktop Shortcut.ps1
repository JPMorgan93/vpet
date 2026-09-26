$ErrorActionPreference = 'Stop'
$launcher = Join-Path $PSScriptRoot 'bin\Vpet.exe'
if (-not (Test-Path -LiteralPath $launcher)) { & (Join-Path $PSScriptRoot 'build.ps1') }
$desktop = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktop 'Vpet.lnk'
if (Test-Path -LiteralPath $shortcutPath) { throw "A shortcut already exists at $shortcutPath. It was left unchanged." }
$shellObject = New-Object -ComObject WScript.Shell
$shortcut = $shellObject.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $launcher
$shortcut.WorkingDirectory = Split-Path -Parent $launcher
$shortcut.Description = 'Your virtual desktop companion'
$shortcut.IconLocation = "$launcher,0"
$shortcut.Save()
Write-Output "Created $shortcutPath"
