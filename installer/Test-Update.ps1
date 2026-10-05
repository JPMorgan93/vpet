$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$toolsDirectory=Join-Path $root '.tools'
$label='Vpet Update Validation'
$guid='F56E9E32-018D-48D3-BA90-8B6073854760'
$key='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{'+$guid+'}_is1'
$folder=Join-Path $root 'bin\isolated-update-check'
$app=Join-Path $folder 'app'
$desktop=Join-Path ([Environment]::GetFolderPath('DesktopDirectory')) ($label+'.lnk')
$shortcut=Join-Path ([Environment]::GetFolderPath('Programs')) ($label+'\Vpet.lnk')
if((Test-Path $key) -or (Test-Path $desktop) -or (Test-Path $shortcut)){throw 'Isolated test already exists; inspect before resuming.'}
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$source=[IO.File]::ReadAllText((Join-Path $root 'installer\Vpet.iss'))
$source=$source.Replace('25F79B0F-454A-4E2F-BE0C-C13D6F067F65',$guid).Replace('AppName=Vpet','AppName='+$label).Replace('DefaultGroupName=Vpet','DefaultGroupName='+$label).Replace('AppMutex=Local\VpetPrototype','AppMutex=Local\VpetUpdateValidation').Replace('{autodesktop}\Vpet','{autodesktop}\'+$label).Replace('"--startup"','"--smoke-test"')
$source=$source.Replace('Getting Started.txt','..\installer\Getting Started.txt')
$iss=Join-Path $toolsDirectory 'IsolatedUpdate.iss'
[IO.File]::WriteAllText($iss,$source)
& (Join-Path $toolsDirectory 'InnoSetup\ISCC.exe') ('/O'+$folder) $iss | Select-Object -Last 3
if($LASTEXITCODE -ne 0){throw 'Isolated installer compilation failed'}
$version=(Get-Content (Join-Path $root 'release.json') -Raw|ConvertFrom-Json).version
$installer=Join-Path $folder ('Vpet-Setup-'+$version+'-Windows-x64.exe')
function Run([string]$file,[string]$arguments){
 $p=Start-Process -FilePath $file -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
 if($null -ne $p.ExitCode -and $p.ExitCode -ne 0){throw ('Process failed: '+$p.ExitCode)}
}
$base='/VERYSILENT /SP- /SUPPRESSMSGBOXES /NORESTART /DIR="'+$app+'" /GROUP="'+$label+'"'
try {
 Run $installer ($base+' /TASKS=""')
 if(-not (Test-Path (Join-Path $app 'Vpet.exe'))){throw 'Missing installed executable'}
 $iconPath=Join-Path $app 'assets\reference\Vpet-Pixel.ico'
 if((Get-FileHash -LiteralPath $iconPath).Hash -ne (Get-FileHash -LiteralPath (Join-Path $root 'assets\reference\Vpet.ico')).Hash){throw 'Installed shortcut icon differs from supplied icon'}
 foreach($asset in @('Heads.png','Tails.png','Joystick.png','Easy.mp3','Normal.mp3','Hard.mp3')){
  $relative='assets\reference\'+$asset
  if((Get-FileHash -LiteralPath (Join-Path $app $relative)).Hash -ne (Get-FileHash -LiteralPath (Join-Path $root $relative)).Hash){throw ('Installed coin artwork differs: '+$asset)}
 }
 Write-Output 'PASS: installed coin artwork and arcade songs match the supplied reference assets'
 $links=New-Object -ComObject WScript.Shell
 if($links.CreateShortcut($shortcut).IconLocation -notlike '*Vpet-Pixel.ico*'){throw 'Start menu shortcut did not switch to the new icon path'}
 if(Test-Path (Join-Path $app 'pending-update.txt')){throw 'Fresh install incorrectly requested update notes'}
 # Simulate an older installed EXE with no last-run tracking. The installer
 # must capture its version before replacing it with the current binary.
 $legacySource=Join-Path $folder 'LegacyVpet.cs'
 $legacyExe=Join-Path $folder 'LegacyVpet.exe'
 [IO.File]::WriteAllText($legacySource,'[assembly:System.Reflection.AssemblyFileVersion("1.5.1.0")] [assembly:System.Reflection.AssemblyInformationalVersion("1.5.1")] class LegacyVpet { static void Main() {} }')
 & (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /target:winexe ('/out:'+$legacyExe) $legacySource
 if($LASTEXITCODE -ne 0){throw 'Legacy version fixture compilation failed'}
 Copy-Item -LiteralPath $legacyExe -Destination (Join-Path $app 'Vpet.exe') -Force
 $sentinel=Join-Path $app 'user-data.txt';[IO.File]::WriteAllText($sentinel,'Preserve me')
 $update='/SILENT /SP- /SUPPRESSMSGBOXES /NORESTART /RESTARTEXITCODE=3010 /VPETHELPER'
 Run $installer $update
 if([IO.File]::ReadAllText((Join-Path $app 'pending-update.txt')).Trim() -ne $version){throw 'Successful update did not request release notes on relaunch'}
 $fromFile=Join-Path $app 'pending-update-from.txt'
 if([IO.File]::ReadAllText($fromFile).Trim() -ne '1.5.1.0'){throw 'Upgrade did not retain the previous binary version for skipped-release notes'}
 Write-Output 'PASS: only an upgrade records completion notes for the installed version'
 Run $installer $update
 if([IO.File]::ReadAllText($fromFile).Trim() -ne '1.5.1.0'){throw 'Another installation without running Vpet lost the unseen release history'}
 [IO.File]::WriteAllText((Join-Path $app 'last-run-version.txt'),'1.5.2')
 Run $installer $update
 if([IO.File]::ReadAllText($fromFile).Trim() -ne '1.5.2'){throw 'Upgrade did not prefer the last version actually run'}
 Write-Output 'PASS: upgrades retain skipped-version history, preserve unseen notes across installs, and prefer the last-run version'
 if(Test-Path $desktop){throw 'Update created unwanted shortcut'}
 if((Get-ItemProperty $key).InstallLocation.TrimEnd('\') -ne $app){throw 'Update changed destination'}
 Write-Output 'PASS: progress-only update preserves no-shortcut choice and existing destination'
 Run $installer ($base+' /TASKS="desktopicon"')
 Run $installer $update
 if(-not (Test-Path $desktop)){throw 'Update lost shortcut'}
 if($links.CreateShortcut($desktop).IconLocation -notlike '*Vpet-Pixel.ico*'){throw 'Desktop shortcut did not switch to the new icon path'}
 Write-Output 'PASS: installed icon matches supplied artwork and both shortcut types use the new icon path'
 if(-not (Test-Path $sentinel)){throw 'Update lost unrelated file'}
 Write-Output 'PASS: progress-only update preserves existing shortcut and unrelated files'
 # The legacy updater passes no silent flags. Observe and acknowledge only its
 # completion screen without clicking any installation or shortcut prompt.
 Add-Type 'using System; using System.Runtime.InteropServices; public static class UpdateTestClick { [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l); }'
 Add-Type -AssemblyName UIAutomationClient
 Add-Type -AssemblyName UIAutomationTypes
 $p=Start-Process -FilePath $installer -ArgumentList '/SP-' -WindowStyle Hidden -PassThru
 $deadline=(Get-Date).AddSeconds(40);$confirmed=$false
 while((Get-Date) -lt $deadline -and -not $confirmed){
  $ownedIds=@($p.Id)+@(Get-CimInstance Win32_Process -Filter ('ParentProcessId='+$p.Id) | ForEach-Object ProcessId)
  $windows=[System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)
  foreach($window in $windows){
   if($window.Current.ProcessId -notin $ownedIds){continue}
   $texts=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)
   $names=@($texts|ForEach-Object {$_.Current.Name}) -join ' '
   if($names -like '*is up to date. Your settings*'){
    $buttons=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)
    foreach($button in $buttons){if($button.Current.Name -in @('Finish','&Finish')){[UpdateTestClick]::PostMessage([IntPtr]$button.Current.NativeWindowHandle,245,[IntPtr]::Zero,[IntPtr]::Zero) | Out-Null;$confirmed=$true;break}}
   }
  }
  Start-Sleep -Milliseconds 300
 }
 if(-not $confirmed){throw 'Legacy update did not reach completion confirmation'}
 Write-Output 'PASS: legacy updater reaches completion without the setup wizard'
 if(-not $p.WaitForExit(15000)){throw 'Legacy installer did not exit'}
 Start-Sleep -Seconds 10
 if(-not (Test-Path (Join-Path $app 'smoke-output\smoke-result.txt'))){throw 'Updated pet did not relaunch successfully'}
 Write-Output 'PASS: completion relaunches updated app (isolated smoke mode)'
}
finally {
 if(Test-Path (Join-Path $app 'unins000.exe')){Run (Join-Path $app 'unins000.exe') '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART'}
}
if((Test-Path $key) -or (Test-Path $desktop) -or (Test-Path $shortcut)){throw 'Isolated test cleanup incomplete'}
Write-Output 'PASS: isolated installation removed; regular Vpet installation unchanged'
