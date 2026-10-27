$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$exe=Join-Path $root 'src/FlowDesk.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/FlowDesk.App.exe'
if(Get-Process FlowDesk.App -ErrorAction SilentlyContinue) {throw 'Close FlowDesk before running desktop workflow checks.'}
$env:DOTNET_ROOT_X64=Join-Path $root '.tools/dotnet'
$process=Start-Process -FilePath $exe -ArgumentList '--desktop-checks' -PassThru
$process.WaitForExit()
$report=Join-Path $root '.cache/desktop-workflow-results.json'
Get-Content -Raw -LiteralPath $report
if($process.ExitCode -ne 0) {throw 'Desktop workflow checks failed.'}
