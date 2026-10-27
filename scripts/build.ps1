param([switch]$Run)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$env:DOTNET_CLI_HOME=Join-Path $root '.cache/dotnet-home'
$env:NUGET_PACKAGES=Join-Path $root '.cache/nuget'
$env:DOTNET_ROOT_X64=Join-Path $root '.tools/dotnet'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$dotnet=Join-Path $root '.tools/dotnet/dotnet.exe'
Push-Location $root
try {
 & $dotnet build FlowDesk.slnx
 if($LASTEXITCODE -ne 0) { throw 'Build failed' }
 & $dotnet run --project tests/FlowDesk.Checks -- --native --movement
 if($LASTEXITCODE -ne 0) { throw 'Checks failed' }
 if($Run) { & $dotnet run --project src/FlowDesk.App }
} finally { Pop-Location }
