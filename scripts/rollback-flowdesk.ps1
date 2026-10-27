param([Parameter(Mandatory)][string]$InstallRoot)
$ErrorActionPreference='Stop'
$InstallRoot=[IO.Path]::GetFullPath($InstallRoot).TrimEnd('\')
$statePath=Join-Path $InstallRoot 'current.json'
$marker=Get-Content -Raw -LiteralPath (Join-Path $InstallRoot '.flowdesk-install-root.json')|ConvertFrom-Json
$state=Get-Content -Raw -LiteralPath $statePath|ConvertFrom-Json
if($marker.owner -ne 'fareed7479.FlowDesk' -or $state.owner -ne $marker.owner -or $state.version -notmatch '^\d+\.\d+\.\d+$' -or $state.previousVersion -notmatch '^\d+\.\d+\.\d+$'){throw 'A valid owned installation with a previous version is required'}
if(Get-Process FlowDesk.App,FlowDesk.FlowWebHost -ErrorAction SilentlyContinue|Where-Object {$_.Path -and $_.Path.StartsWith($InstallRoot+'\',[StringComparison]::OrdinalIgnoreCase)}){throw 'Close installed FlowDesk and browser connections before rollback'}
$previous=Join-Path $InstallRoot "versions/$($state.previousVersion)"
$manifest=Get-Content -Raw -LiteralPath (Join-Path $previous 'package.json')|ConvertFrom-Json
foreach($file in $manifest.files){
 if($file.path -match '(^[\\/]|:|(^|[\\/])\.\.([\\/]|$))'){throw 'Unsafe manifest path'}
 $path=[IO.Path]::GetFullPath((Join-Path $previous $file.path))
 if(!$path.StartsWith($previous+'\',[StringComparison]::OrdinalIgnoreCase) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.sha256){throw 'Previous version integrity check failed'}
}
$oldExe=Join-Path $InstallRoot "versions/$($state.version)/app/FlowDesk.App.exe"
$newExe=Join-Path $previous 'app/FlowDesk.App.exe'
if(!(Test-Path -LiteralPath $newExe)){throw 'Previous executable is missing'}
if($state.shortcut){$shell=New-Object -ComObject WScript.Shell;$shortcut=Join-Path ([Environment]::GetFolderPath('Programs')) 'FlowDesk.lnk';$link=$shell.CreateShortcut($shortcut);if($link.TargetPath -ne $oldExe){throw 'Shortcut no longer belongs to this installation'};$link.TargetPath=$newExe;$link.WorkingDirectory=Split-Path $newExe;$link.Save()}
$run='HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
if(Test-Path -LiteralPath $run){$command=(Get-ItemProperty -LiteralPath $run -Name FlowDesk -ErrorAction SilentlyContinue).FlowDesk;if($command -eq ('"'+$oldExe+'"')){Set-ItemProperty -LiteralPath $run -Name FlowDesk -Value ('"'+$newExe+'"')}}
if($state.flowweb){$native=Join-Path $InstallRoot 'com.flowdesk.flowweb.json';$hostManifest=Get-Content -Raw -LiteralPath $native|ConvertFrom-Json;if($hostManifest.path -ne (Join-Path (Split-Path (Split-Path $oldExe)) 'flowweb-host/FlowDesk.FlowWebHost.exe')){throw 'Native host manifest changed'};$hostManifest.path=Join-Path $previous 'flowweb-host/FlowDesk.FlowWebHost.exe';$hostManifest|ConvertTo-Json|Set-Content -LiteralPath $native -Encoding utf8NoBOM}
$old=$state.version;$state.version=$state.previousVersion;$state.previousVersion=$old
$state|ConvertTo-Json|Set-Content -LiteralPath ($statePath+'.tmp') -Encoding utf8NoBOM
Move-Item -LiteralPath ($statePath+'.tmp') -Destination $statePath -Force
@{RolledBack=$true;Version=$state.version;DataPreserved=$true;Executable=$newExe}|ConvertTo-Json
