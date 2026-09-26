param([switch]$Resume)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$version = (Get-Content (Join-Path $projectRoot 'release.json') -Raw | ConvertFrom-Json).version
$installer = Join-Path $projectRoot ('dist\Vpet-Setup-' + $version + '-Windows-x64.exe')
$testRoot = Join-Path $projectRoot ('bin\installer-test-' + [Guid]::NewGuid().ToString('N'))
$uninstallKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{25F79B0F-454A-4E2F-BE0C-C13D6F067F65}_is1'
if (Test-Path $uninstallKey) {
    $previousInstall = (Get-ItemProperty $uninstallKey).InstallLocation.TrimEnd('\')
    $allowedPrefix = (Join-Path $projectRoot 'bin\installer-test-')
    if (-not $Resume -or -not $previousInstall.StartsWith($allowedPrefix, [StringComparison]::OrdinalIgnoreCase) -or (Split-Path $previousInstall -Leaf) -ne 'app') { throw 'An installed Vpet release already exists. Test on a clean account instead.' }
    $testRoot = Split-Path -Parent $previousInstall
}
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$appDirectory = Join-Path $testRoot 'app'
$group = 'Vpet'
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) ($group + '\Vpet.lnk')
$desktopShortcut = Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) 'Vpet.lnk'
if(Test-Path -LiteralPath $desktopShortcut){throw 'An existing desktop shortcut would be overwritten. Test on a clean account instead.'}
if (-not $Resume -and (Test-Path $shortcutPath)) { throw 'An existing Vpet shortcut would be overwritten. Test on a clean account instead.' }
$developmentApp = Join-Path $projectRoot 'bin\Vpet.exe'
$runningPet = @(Get-Process Vpet -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $developmentApp })

# Send a normal close to this workspace's pet so it saves settings before testing.
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class VpetReleaseTestWindows {
    public delegate bool Callback(IntPtr handle, IntPtr state);
    [DllImport("user32.dll")] static extern bool EnumWindows(Callback callback, IntPtr state);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr handle, out uint id);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr handle, uint msg, IntPtr w, IntPtr l);
    public static void Close(uint id) {
        EnumWindows(delegate(IntPtr h, IntPtr unused) { uint owner; GetWindowThreadProcessId(h, out owner);
            if(owner==id) PostMessage(h, 0x0010, IntPtr.Zero, IntPtr.Zero); return true; }, IntPtr.Zero);
    }
}
'@

function Run-InstallerProcess([string]$File, [string]$Arguments) {
    $process = Start-Process -FilePath $File -ArgumentList $Arguments -WindowStyle Hidden -Wait -PassThru
    if ($null -ne $process.ExitCode -and $process.ExitCode -ne 0) { throw "Process failed: $File ($($process.ExitCode))" }
}

try {
    foreach ($petProcess in $runningPet) {
        [VpetReleaseTestWindows]::Close([uint32]$petProcess.Id)
        if (-not $petProcess.WaitForExit(10000)) { throw 'Vpet did not close normally; no forced termination was attempted.' }
    }
    $arguments = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /TASKS="" /DIR="' + $appDirectory + '" /GROUP="' + $group + '" /LOG="' + (Join-Path $testRoot 'install.log') + '"'
    Run-InstallerProcess $installer $arguments
    $installedApp = Join-Path $appDirectory 'Vpet.exe'
    if (-not (Test-Path -LiteralPath $installedApp)) { throw 'Installed executable missing.' }
    if ((Get-ItemProperty $uninstallKey).DisplayVersion -ne $version) { throw 'Wrong installed version.' }
    if ((Get-FileHash $installedApp).Hash -ne (Get-FileHash (Join-Path $projectRoot 'bin\release\Vpet.exe')).Hash) { throw 'Installed executable differs from release.' }
    $iconPath = Join-Path $appDirectory 'assets\reference\Vpet.ico'
    if ((Get-FileHash $iconPath).Hash -ne (Get-FileHash (Join-Path $projectRoot 'assets\reference\Vpet.ico')).Hash) { throw 'Installed icon differs from supplied icon.' }
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    if ($shortcut.TargetPath -ne $installedApp -or $shortcut.IconLocation -ne "$iconPath,0") { throw 'Shortcut target or icon is wrong.' }
    Run-InstallerProcess $installedApp '--smoke-test'
    $smokeResult = Join-Path $appDirectory 'smoke-output\smoke-result.txt'
    if (-not (Test-Path $smokeResult)) { throw 'Installed GUI smoke test did not complete.' }
    Get-Content $smokeResult
    # Reinstallation must work and leave unrelated files intact.
    $sentinel = Join-Path $appDirectory 'user-file.txt'
    [IO.File]::WriteAllText($sentinel, 'Preserve unrelated files')
    # Actual updater flags: show only progress, preserve destination and prior tasks.
    $updateArguments='/SILENT /SP- /SUPPRESSMSGBOXES /NORESTART /RESTARTEXITCODE=3010 /VPETHELPER'
    Run-InstallerProcess $installer $updateArguments
    if (-not (Test-Path $sentinel)) { throw 'Update removed an unrelated file.' }
    if(Test-Path -LiteralPath $desktopShortcut){throw 'Update created an unrequested desktop shortcut.'}
    if((Get-ItemProperty $uninstallKey).InstallLocation.TrimEnd('\') -ne $appDirectory){throw 'Update changed the install directory.'}
    Run-InstallerProcess $installer ($arguments.Replace('/TASKS=""','/TASKS="desktopicon"'))
    if(-not (Test-Path -LiteralPath $desktopShortcut)){throw 'Requested desktop shortcut missing.'}
    Run-InstallerProcess $installer $updateArguments
    if(-not (Test-Path -LiteralPath $desktopShortcut)){throw 'Update removed an existing desktop shortcut.'}
    Run-InstallerProcess (Join-Path $appDirectory 'unins000.exe') ('/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /LOG="' + (Join-Path $testRoot 'uninstall.log') + '"')
    if ((Test-Path $installedApp) -or (Test-Path $uninstallKey) -or (Test-Path $shortcutPath) -or (Test-Path $desktopShortcut)) { throw 'Uninstall left a registered application, executable, or shortcut.' }
    if (-not (Test-Path $sentinel)) { throw 'Uninstall removed an unrelated file.' }
    Write-Output "PASS: installation, version, icon, shortcut, installed GUI launch, reinstall, and uninstall. Logs: $testRoot"
}
finally {
    if ($runningPet.Count -gt 0 -and -not (Get-Process Vpet -ErrorAction SilentlyContinue)) {
        Start-Process -FilePath $developmentApp -WindowStyle Hidden
    }
}
