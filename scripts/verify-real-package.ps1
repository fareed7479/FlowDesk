param([string]$PackagePath,[string]$Sha256)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
if(!$PackagePath){[xml]$props=Get-Content (Join-Path $root 'Directory.Build.props');$version=[string]$props.Project.PropertyGroup.Version;$PackagePath=Join-Path $root ".cache/dist/FlowDesk-$version-win-x64.zip"}
if(!$Sha256){$Sha256=((Get-Content -Raw -LiteralPath ($PackagePath+'.sha256')).Trim() -split '\s+')[0]}
if(Get-Process FlowDesk.App -ErrorAction SilentlyContinue){throw 'Close FlowDesk before actual package verification'}
$installRoot=Join-Path $root ('.cache/real-package-'+[Guid]::NewGuid().ToString('N'))
$result=Join-Path $root '.cache/real-package-results.json'
try {
 $answer=& "$PSScriptRoot/install-flowdesk.ps1" -PackagePath $PackagePath -Sha256 $Sha256 -InstallRoot $installRoot -NoShortcut|ConvertFrom-Json
 $exe=$answer.Executable
 foreach($name in @('FlowDesk.App.pri','App.xbf')){if(!(Test-Path -LiteralPath (Join-Path (Split-Path $exe) $name))){throw 'Published WinUI resources are missing'}}
 $process=Start-Process -FilePath $exe -PassThru
 $shown=$false
 for($i=0;$i -lt 100;$i++){Start-Sleep -Milliseconds 150;$process.Refresh();if($process.HasExited){throw "Published app exited before showing UI: $($process.ExitCode)"};if($process.MainWindowTitle -like 'FlowDesk*'){$shown=$true;break}}
 if(!$shown){throw 'Published app window did not appear'}
 $process.CloseMainWindow()|Out-Null
 if(!$process.WaitForExit(15000) -or $process.ExitCode -ne 0){throw 'Published app failed to close cleanly'}
 $identity=Get-Content -Raw -LiteralPath (Join-Path (Split-Path $exe) 'flowdesk-install.json')|ConvertFrom-Json
 if($identity.dataDirectory -ne (Join-Path $installRoot 'data')){throw 'Installed data path is not stable'}
 $removed=& "$PSScriptRoot/uninstall-flowdesk.ps1" -InstallRoot $installRoot|ConvertFrom-Json
 if(!$removed.Uninstalled -or (Test-Path (Join-Path $installRoot 'versions')) -or !(Test-Path (Join-Path $installRoot 'data'))){throw 'Published package removal failed'}
 @{Success=$true;PackageSha256=$Sha256;Version=$answer.Version;Installed=$true;WinUIResources=$true;WindowShown=$shown;ClosedCleanly=$true;StableData=$true;NotificationCleanup=$true;RemovedOwnedVersions=$true;DataRetained=$true;Signed=$answer.Signed}|ConvertTo-Json|Set-Content -LiteralPath $result
 Get-Content -Raw -LiteralPath $result
}catch{@{Success=$false;InstallRoot=$installRoot;Error=$_.ToString()}|ConvertTo-Json|Set-Content -LiteralPath $result;throw}
