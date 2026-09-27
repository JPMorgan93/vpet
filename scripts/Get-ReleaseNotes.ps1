param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$version = (Get-Content -LiteralPath (Join-Path $ProjectRoot 'release.json') -Raw | ConvertFrom-Json).version
$changelog = [IO.File]::ReadAllText((Join-Path $ProjectRoot 'CHANGELOG.md'))
$section = [regex]::Match($changelog, '(?ms)\A# Vpet ([^\r\n]+)\r?\n(.*?)(?=^## Vpet |\z)')
if (-not $section.Success -or $section.Groups[1].Value.Trim() -ne $version) { throw 'The first CHANGELOG.md section must match release.json.' }
$notes = $section.Groups[2].Value.Trim()
if (-not $notes) { throw 'Add a description of this update to CHANGELOG.md.' }
Write-Output $notes
