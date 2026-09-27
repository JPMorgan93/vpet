param([switch]$Test, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot 'bin' }
$outputDirectory = $OutputDirectory
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The Windows .NET Framework C# compiler is required.' }
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$release = Get-Content -LiteralPath (Join-Path $projectRoot 'release.json') -Raw | ConvertFrom-Json
if ($release.version -notmatch '^\d+\.\d+\.\d+$' -or $release.repository -notmatch '^[A-Za-z0-9_-]+/[A-Za-z0-9_.-]+$') { throw 'Invalid release.json version or repository.' }
$metadataPath = Join-Path $outputDirectory 'ReleaseInfo.g.cs'
$metadata = 'namespace Vpet { internal static class ReleaseInfo { public const string Version = "' + $release.version + '"; public const string Repository = "' + $release.repository + '"; } }'
[IO.File]::WriteAllText($metadataPath, $metadata)
$manifestPath = Join-Path $outputDirectory 'app.manifest'
$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'app.manifest') -Raw
$manifest = $manifest -replace 'assemblyIdentity version="[^"]+"', ('assemblyIdentity version="' + $release.version + '.0"')
[IO.File]::WriteAllText($manifestPath, $manifest)
$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
$sourceFiles += $metadataPath
$references = @('/r:System.dll', '/r:System.Core.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll', '/r:System.Runtime.Serialization.dll', '/r:System.IO.Compression.dll')
$appPath = Join-Path $outputDirectory 'Vpet.exe'
& $compiler /nologo /target:winexe /optimize+ /platform:x64 "/out:$appPath" "/win32manifest:$manifestPath" "/win32icon:$(Join-Path $projectRoot 'assets\reference\Vpet.ico')" $references $sourceFiles
if ($LASTEXITCODE -ne 0) { throw 'Vpet compilation failed.' }
$referenceDirectory = Join-Path $outputDirectory 'assets\reference'
New-Item -ItemType Directory -Path $referenceDirectory -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets\reference\Base Vpet Sprite Sheet.png') -Destination $referenceDirectory -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets\reference\Vpet.ico') -Destination $referenceDirectory -Force
Write-Output "Built $appPath"
if ($Test) {
    $testPath = Join-Path $outputDirectory 'Vpet.Tests.exe'
    & $compiler /nologo /target:exe /optimize+ /platform:x64 /main:Vpet.Tests "/out:$testPath" $references $sourceFiles (Join-Path $projectRoot 'tests\Tests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    & $testPath
    if ($LASTEXITCODE -ne 0) { throw 'Vpet tests failed.' }
}
