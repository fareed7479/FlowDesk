$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
if(Get-Process FlowDesk.App -ErrorAction SilentlyContinue){throw 'Close FlowDesk before advanced UI verification'}
$exe=Join-Path $root 'src/FlowDesk.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/FlowDesk.App.exe'
$process=Start-Process -FilePath $exe -ArgumentList '--advanced-checks' -PassThru
if(!$process.WaitForExit(120000)){throw 'Advanced UI verification timed out'}
Get-Content -Raw -LiteralPath (Join-Path $root '.cache/advanced-workflow-results.json')
if($process.ExitCode -ne 0){throw 'Advanced UI verification failed'}
