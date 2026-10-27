param([Parameter(Mandatory)][string]$InstallRoot,[switch]$SkipAppIntegrationCleanup)
$ErrorActionPreference='Stop'
$InstallRoot=[IO.Path]::GetFullPath($InstallRoot).TrimEnd('\')
if($InstallRoot -eq [IO.Path]::GetPathRoot($InstallRoot).TrimEnd('\')){throw 'A dedicated owned installation folder is required'}
$marker=Get-Content -Raw -LiteralPath (Join-Path $InstallRoot '.flowdesk-install-root.json')|ConvertFrom-Json
if($marker.schema -ne 1 -or $marker.owner -ne 'fareed7479.FlowDesk'){throw 'Unrecognized installation owner'}
if((Get-Item -LiteralPath $InstallRoot).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Installation root cannot be a reparse point'}
if(Get-Process FlowDesk.App,FlowDesk.FlowWebHost -ErrorAction SilentlyContinue|Where-Object {$_.Path -and $_.Path.StartsWith($InstallRoot+'\',[StringComparison]::OrdinalIgnoreCase)}){throw 'Close installed FlowDesk and browser connections before uninstalling'}
$versions=Join-Path $InstallRoot 'versions'
if(Test-Path -LiteralPath $versions){
 $resolved=(Resolve-Path -LiteralPath $versions).Path
 if(!$resolved.StartsWith($InstallRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or ((Get-Item -LiteralPath $versions).Attributes -band [IO.FileAttributes]::ReparsePoint) -or (Get-ChildItem -LiteralPath $versions -Recurse -Force|Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint})){throw 'Unsafe version directory'}
}
if(!$SkipAppIntegrationCleanup -and (Test-Path -LiteralPath $versions)){
 foreach($version in Get-ChildItem -LiteralPath $versions -Directory){
  $package=Get-Content -Raw -LiteralPath (Join-Path $version.FullName 'package.json')|ConvertFrom-Json
  foreach($file in $package.files){
 if($file.path -match '(^[\\/]|:|(^|[\\/])\.\.([\\/]|$))'){throw 'Unsafe manifest path'}
   $path=[IO.Path]::GetFullPath((Join-Path $version.FullName $file.path))
   if(!$path.StartsWith($version.FullName+'\',[StringComparison]::OrdinalIgnoreCase) -or (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.sha256){throw 'Installed file integrity check failed before integration cleanup'}
  }
  $exe=Join-Path $version.FullName 'app/FlowDesk.App.exe'
  if(Test-Path -LiteralPath $exe){$process=Start-Process -FilePath $exe -ArgumentList '--uninstall-integrations' -PassThru;if(!$process.WaitForExit(20000) -or $process.ExitCode -ne 0){throw 'App notification registration cleanup failed; files were retained'}}
 }
}
$run='HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
if(Test-Path -LiteralPath $run){$command=(Get-ItemProperty -LiteralPath $run -Name FlowDesk -ErrorAction SilentlyContinue).FlowDesk;if($command -and $command.StartsWith('"'+$versions+'\',[StringComparison]::OrdinalIgnoreCase) -and $command.EndsWith('\app\FlowDesk.App.exe"',[StringComparison]::OrdinalIgnoreCase)){Remove-ItemProperty -LiteralPath $run -Name FlowDesk}}
$shortcut=Join-Path ([Environment]::GetFolderPath('Programs')) 'FlowDesk.lnk'
if(Test-Path -LiteralPath $shortcut){$shell=New-Object -ComObject WScript.Shell;$link=$shell.CreateShortcut($shortcut);if($link.TargetPath.StartsWith($versions+'\',[StringComparison]::OrdinalIgnoreCase)){Remove-Item -LiteralPath $shortcut}}
$native=Join-Path $InstallRoot 'com.flowdesk.flowweb.json'
foreach($base in @('Software\Google\Chrome\NativeMessagingHosts','Software\Microsoft\Edge\NativeMessagingHosts')){
 $key="HKCU:\$base\com.flowdesk.flowweb"
 if(Test-Path -LiteralPath $key){$entry=Get-ItemProperty -LiteralPath $key;if($entry.FlowDeskOwner -eq $marker.owner -and $entry.'(default)' -eq $native){if(@(Get-ChildItem -LiteralPath $key).Count){throw 'Native host contains unexpected subkeys'};Remove-Item -LiteralPath $key}}
}
# Remove notification/protocol registrations only when they still point inside this installation.
$protocol='HKCU:\Software\Classes\flowdesk-control'
if(Test-Path -LiteralPath "$protocol\shell\open\command"){
 $command=(Get-ItemProperty -LiteralPath "$protocol\shell\open\command").'(default)'
 if($command -and $command.StartsWith('"'+$versions+'\',[StringComparison]::OrdinalIgnoreCase)){
  $owned=Get-ItemProperty -LiteralPath $protocol
  if($owned.FlowDeskOwner -eq $marker.owner){Remove-Item -LiteralPath $protocol -Recurse}
 }
}
if(Test-Path -LiteralPath $versions){Remove-Item -LiteralPath $versions -Recurse}
foreach($name in @('current.json','com.flowdesk.flowweb.json')){$path=Join-Path $InstallRoot $name;if(Test-Path -LiteralPath $path){Remove-Item -LiteralPath $path}}
@{Uninstalled=$true;DataPreserved=(Join-Path $InstallRoot 'data');Note='Saved data and ownership marker are retained.'}|ConvertTo-Json
