$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$exe=Join-Path $root 'src/FlowDesk.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/FlowDesk.App.exe'
$process=Start-Process -FilePath $exe -ArgumentList '--module2-checks' -PassThru
if(!$process.WaitForExit(120000)){throw 'Module 2 WebView2 checks timed out'}
Get-Content -Raw -LiteralPath (Join-Path $root '.cache/module2-results.json')
if($process.ExitCode -ne 0){throw 'Module 2 checks failed'}
