$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$toolDirectory = Join-Path $projectRoot '.tools\InnoSetup'
if (Test-Path (Join-Path $toolDirectory 'ISCC.exe')) { exit 0 }
$downloadDirectory = Join-Path $projectRoot '.tools'
New-Item -ItemType Directory -Path $downloadDirectory -Force | Out-Null
$download = Join-Path $downloadDirectory 'innosetup-6.4.3.exe'
Invoke-WebRequest -UseBasicParsing -Uri 'https://github.com/jrsoftware/issrc/releases/download/is-6_4_3/innosetup-6.4.3.exe' -OutFile $download
if ((Get-FileHash $download -Algorithm SHA256).Hash -ne 'F3C42116542C4CC57263C5BA6C4FEABFC49FE771F2F98A79D2F7628B8762723B') { throw 'Build tool checksum did not match.' }
if ((Get-AuthenticodeSignature $download).Status -ne 'Valid') { throw 'Build tool signature is not valid.' }
$arguments = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CURRENTUSER /NOICONS /TASKS="" /DIR="' + $toolDirectory + '"'
$process = Start-Process -FilePath $download -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if (($null -ne $process.ExitCode -and $process.ExitCode -ne 0) -or -not (Test-Path (Join-Path $toolDirectory 'ISCC.exe'))) { throw 'Compiler installation failed.' }
