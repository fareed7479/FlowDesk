$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$env:DOTNET_ROOT_X64=Join-Path $root '.tools/dotnet'
$exe=Join-Path $root 'src/FlowDesk.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/FlowDesk.App.exe'
if(!(Test-Path -LiteralPath $exe)) { throw 'Build FlowDesk first with scripts/build.ps1.' }
Start-Process -FilePath $exe
