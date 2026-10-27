param(
 [Parameter(Mandatory)][string]$PackagePath,
 [Parameter(Mandatory)][ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$Sha256,
 [string]$InstallRoot,
 [string]$ImportDataPath,
 [switch]$RequireSignature,
 [switch]$NoShortcut,
 [switch]$RegisterFlowWeb,
 [switch]$Startup
)
$ErrorActionPreference='Stop'
$owner='fareed7479.FlowDesk'
if(!$InstallRoot){$InstallRoot=Join-Path ([IO.Path]::GetPathRoot($PSScriptRoot)) 'Apps/FlowDesk'}
$InstallRoot=[IO.Path]::GetFullPath($InstallRoot).TrimEnd('\')
if($InstallRoot -eq [IO.Path]::GetPathRoot($InstallRoot).TrimEnd('\')){throw 'A dedicated installation subfolder is required'}
# Refuse ancestor junctions so all generated paths stay within the named installation.
$ancestor=$InstallRoot
while($ancestor){if((Test-Path -LiteralPath $ancestor) -and ((Get-Item -LiteralPath $ancestor).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Installation ancestors cannot be reparse points'};$parent=Split-Path $ancestor;if($parent -eq $ancestor){break};$ancestor=$parent}
$PackagePath=(Resolve-Path -LiteralPath $PackagePath).Path
if((Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash -ne $Sha256){throw 'Package SHA256 mismatch'}
$marker=Join-Path $InstallRoot '.flowdesk-install-root.json'
$statePath=Join-Path $InstallRoot 'current.json'
if(Test-Path -LiteralPath $InstallRoot){
 if((Get-Item -LiteralPath $InstallRoot).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Installation root cannot be a reparse point'}
 if(Test-Path -LiteralPath $marker){$owned=Get-Content -Raw -LiteralPath $marker|ConvertFrom-Json;if($owned.owner -ne $owner -or $owned.schema -ne 1){throw 'Unrecognized installation owner'}}
 elseif(@(Get-ChildItem -LiteralPath $InstallRoot -Force).Count){throw 'Refusing to install into an unrelated nonempty folder'}
}
$running=Get-Process FlowDesk.App,FlowDesk.FlowWebHost -ErrorAction SilentlyContinue|Where-Object {$_.Path -and $_.Path.StartsWith($InstallRoot+'\',[StringComparison]::OrdinalIgnoreCase)}
if($running){throw 'Close the installed FlowDesk app and browser connection before updating'}
New-Item -ItemType Directory -Force -Path $InstallRoot|Out-Null
$stage=Join-Path $InstallRoot ('staging-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage|Out-Null
try {
$archive=[IO.Compression.ZipFile]::OpenRead($PackagePath)
try {
 $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
 [long]$total=0
 if($archive.Entries.Count -gt 6000){throw 'Package has too many entries'}
 foreach($entry in $archive.Entries){
  $name=$entry.FullName.Replace('\','/')
  if($name.Contains(':') -or $name.StartsWith('/') -or $name.Split('/') -contains '..' -or !$seen.Add($name) -or (($entry.ExternalAttributes -shr 16) -band 0xF000) -eq 0xA000){throw 'Unsafe archive entry'}
  $destination=[IO.Path]::GetFullPath((Join-Path $stage $name))
  if(!$destination.StartsWith($stage+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Archive path escapes staging'}
  $total+=$entry.Length
  if($total -gt 2GB){throw 'Package expands beyond the permitted size'}
  if($name.EndsWith('/')){[IO.Directory]::CreateDirectory($destination)|Out-Null;continue}
  [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))|Out-Null
  $input=$entry.Open();$output=[IO.File]::Create($destination);try{$input.CopyTo($output)}finally{$output.Dispose();$input.Dispose()}
 }
}finally{$archive.Dispose()}
$manifestPath=Join-Path $stage 'package.json'
if(!(Test-Path -LiteralPath $manifestPath) -or (Get-Item -LiteralPath $manifestPath).Length -gt 2MB){throw 'Missing or oversized package manifest'}
$manifest=Get-Content -Raw -LiteralPath $manifestPath|ConvertFrom-Json
if($manifest.schema -ne 1 -or $manifest.owner -ne $owner -or $manifest.architecture -ne 'win-x64' -or $manifest.version -notmatch '^\d+\.\d+\.\d+$' -or !$manifest.files -or $manifest.files.Count -gt 6000){throw 'Unsupported package manifest'}
$expected=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach($file in $manifest.files){
 if($file.path -match '(^[\\/]|:|(^|[\\/])\.\.([\\/]|$))' -or !$expected.Add($file.path.Replace('\','/')) -or $file.sha256 -notmatch '^[A-Fa-f0-9]{64}$'){throw 'Invalid package file declaration'}
 $filePath=[IO.Path]::GetFullPath((Join-Path $stage $file.path))
 if(!$filePath.StartsWith($stage+'\',[StringComparison]::OrdinalIgnoreCase) -or !(Test-Path -LiteralPath $filePath -PathType Leaf)){throw 'Package file is missing'}
 if((Get-Item -LiteralPath $filePath).Length -ne $file.size -or (Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash -ne $file.sha256){throw 'Package content integrity check failed'}
}
$actual=@(Get-ChildItem -LiteralPath $stage -Recurse -File|Where-Object {$_.FullName -ne $manifestPath})
if($actual.Count -ne $expected.Count){throw 'Package contains undeclared files'}
foreach($file in $actual){if(!$expected.Contains([IO.Path]::GetRelativePath($stage,$file.FullName).Replace('\','/'))){throw 'Package contains an undeclared path'}}
$exe=Join-Path $stage 'app/FlowDesk.App.exe'
$hostExe=Join-Path $stage 'flowweb-host/FlowDesk.FlowWebHost.exe'
if(!(Test-Path -LiteralPath $exe) -or !(Test-Path -LiteralPath $hostExe)){throw 'Package entry points are missing'}
if($RequireSignature){
 foreach($relative in @('app/FlowDesk.App.exe','app/FlowDesk.App.dll','app/FlowDesk.Core.dll','app/FlowDesk.Windows.dll','app/FlowDesk.FlowWeb.dll','flowweb-host/FlowDesk.FlowWebHost.exe','flowweb-host/FlowDesk.FlowWebHost.dll','flowweb-host/FlowDesk.FlowWeb.dll','install-flowdesk.ps1','uninstall-flowdesk.ps1','rollback-flowdesk.ps1')){if((Get-AuthenticodeSignature -LiteralPath (Join-Path $stage $relative)).Status -ne 'Valid'){throw 'Trusted package signatures are required'}}
}
$old=$null
if(Test-Path -LiteralPath $statePath){$old=Get-Content -Raw -LiteralPath $statePath|ConvertFrom-Json;if($old.owner -ne $owner){throw 'Current installation state has an unexpected owner'}}
$effectiveFlowWeb=[bool]$RegisterFlowWeb -or ($old -and $old.flowweb)
$effectiveShortcut=(!$NoShortcut) -or ($old -and $old.shortcut)
$dataRoot=Join-Path $InstallRoot 'data'
foreach($directory in @($dataRoot,(Join-Path $InstallRoot 'versions'))){if((Test-Path -LiteralPath $directory) -and ((Get-Item -LiteralPath $directory).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Installation subfolders cannot be reparse points'}}
if($ImportDataPath){
 $ImportDataPath=(Resolve-Path -LiteralPath $ImportDataPath).Path
 if(Test-Path -LiteralPath $dataRoot){throw 'Data import is allowed only before a data folder exists'}
 $sourceFiles=Get-ChildItem -LiteralPath $ImportDataPath -Recurse -Force
 if($sourceFiles|Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}){throw 'Data import cannot contain reparse points'}
 if(($sourceFiles|Where-Object {!$_.PSIsContainer}|Measure-Object -Property Length -Sum).Sum -gt 32MB){throw 'Data import is too large'}
 New-Item -ItemType Directory -Path $dataRoot|Out-Null
 foreach($file in $sourceFiles|Where-Object {!$_.PSIsContainer}){
  $relative=[IO.Path]::GetRelativePath($ImportDataPath,$file.FullName)
  if($relative -match '(?i)(^|[\\/])(mode-recovery|stage-recovery|flowweb-bridge)\.json$'){continue}
  $target=Join-Path $dataRoot $relative;[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target))|Out-Null;Copy-Item -LiteralPath $file.FullName -Destination $target
 }
}
New-Item -ItemType Directory -Force -Path $dataRoot|Out-Null
$versionRoot=Join-Path $InstallRoot "versions/$($manifest.version)"
if(Test-Path -LiteralPath $versionRoot){throw 'That version is already installed; no existing version was overwritten'}
New-Item -ItemType Directory -Force -Path (Split-Path $versionRoot)|Out-Null
@{schema=1;owner=$owner;dataDirectory=$dataRoot}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $stage 'app/flowdesk-install.json') -Encoding utf8NoBOM
@{schema=1;bridgeConfigPath=(Join-Path $dataRoot 'flowweb-bridge.json')}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $stage 'flowweb-host/flowweb-host-settings.json') -Encoding utf8NoBOM
# Validate global integration ownership before activating the new version.
$runKey='HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$oldCommand=if($old){'"'+(Join-Path $InstallRoot "versions/$($old.version)/app/FlowDesk.App.exe")+'"'}else{$null}
$currentStartup=if(Test-Path -LiteralPath $runKey){(Get-ItemProperty -LiteralPath $runKey -Name FlowDesk -ErrorAction SilentlyContinue).FlowDesk}else{$null}
if($Startup -or ($currentStartup -and $oldCommand -eq $currentStartup)){
 if($currentStartup -and $currentStartup -ne $oldCommand){throw 'Unrelated startup entry would be replaced'}
}
$shortcutPath=Join-Path ([Environment]::GetFolderPath('Programs')) 'FlowDesk.lnk'
$shell=$null
if($effectiveShortcut){
 $shell=New-Object -ComObject WScript.Shell
 if(Test-Path -LiteralPath $shortcutPath){$link=$shell.CreateShortcut($shortcutPath);if(!$old -or $link.TargetPath -ne (Join-Path $InstallRoot "versions/$($old.version)/app/FlowDesk.App.exe")){throw 'Unrelated shortcut would be replaced'}}
}
if($effectiveFlowWeb){
 foreach($base in @('Software\Google\Chrome\NativeMessagingHosts','Software\Microsoft\Edge\NativeMessagingHosts')){
  $key="HKCU:\$base\com.flowdesk.flowweb";if(Test-Path -LiteralPath $key){$entry=Get-ItemProperty -LiteralPath $key;if($entry.FlowDeskOwner -ne $owner){throw 'Unrelated native host would be replaced'}}
 }
}
Move-Item -LiteralPath $stage -Destination $versionRoot
@{schema=1;owner=$owner}|ConvertTo-Json|Set-Content -LiteralPath $marker -Encoding utf8NoBOM
$newExe=Join-Path $versionRoot 'app/FlowDesk.App.exe'
$state=@{schema=1;owner=$owner;version=$manifest.version;previousVersion=$(if($old){$old.version}else{$null});packageSha256=$Sha256;shortcut=$effectiveShortcut;flowweb=$effectiveFlowWeb}
$state|ConvertTo-Json|Set-Content -LiteralPath ($statePath+'.tmp') -Encoding utf8NoBOM
Move-Item -LiteralPath ($statePath+'.tmp') -Destination $statePath -Force
if($effectiveShortcut){$link=$shell.CreateShortcut($shortcutPath);$link.TargetPath=$newExe;$link.WorkingDirectory=Split-Path $newExe;$link.Description='FlowDesk workspace companion';$link.Save()}
if($Startup -or ($currentStartup -and $currentStartup -eq $oldCommand)){if(!(Test-Path -LiteralPath $runKey)){New-Item -Path $runKey|Out-Null};New-ItemProperty -LiteralPath $runKey -Name FlowDesk -Value ('"'+$newExe+'"') -Force|Out-Null}
if($effectiveFlowWeb){
 $native=Join-Path $InstallRoot 'com.flowdesk.flowweb.json'
 @{name='com.flowdesk.flowweb';description='FlowDesk FlowWeb local bridge';path=(Join-Path $versionRoot 'flowweb-host/FlowDesk.FlowWebHost.exe');type='stdio';allowed_origins=@('chrome-extension://dinpkkjphjbeadjlhndbgaaciimepnom/')}|ConvertTo-Json|Set-Content -LiteralPath $native -Encoding utf8NoBOM
 foreach($base in @('Software\Google\Chrome\NativeMessagingHosts','Software\Microsoft\Edge\NativeMessagingHosts')){$key="HKCU:\$base\com.flowdesk.flowweb";New-Item -Path $key -Force|Out-Null;Set-Item -LiteralPath $key -Value $native;New-ItemProperty -LiteralPath $key -Name FlowDeskOwner -Value $owner -Force|Out-Null}
}
@{Installed=$true;Version=$manifest.version;Executable=$newExe;DataDirectory=$dataRoot;Signed=[bool]$manifest.signed}|ConvertTo-Json

} finally {
 if(Test-Path -LiteralPath $stage){
  $resolved=(Resolve-Path -LiteralPath $stage).Path
  if(!$resolved.StartsWith($InstallRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($resolved) -notmatch '^staging-[a-f0-9]{32}$' -or ((Get-Item -LiteralPath $resolved).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Unsafe staging cleanup path'}
  Remove-Item -LiteralPath $resolved -Recurse
 }
}