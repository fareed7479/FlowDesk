$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$workspace=Join-Path $root ('.cache/distribution-tests-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $workspace|Out-Null
$passed=[Collections.Generic.List[string]]::new()
function Check([bool]$condition,[string]$message){if(!$condition){throw $message};$passed.Add($message)}
function Package([string]$version,[string]$name,[switch]$BadHash,[switch]$Traversal,[switch]$Undeclared){
 $directory=Join-Path $workspace $name
 New-Item -ItemType Directory -Path "$directory/app","$directory/flowweb-host"|Out-Null
 'fixture desktop payload'|Set-Content "$directory/app/FlowDesk.App.exe"
 'fixture host payload'|Set-Content "$directory/flowweb-host/FlowDesk.FlowWebHost.exe"
 $files=@(Get-ChildItem $directory -Recurse -File|ForEach-Object{@{path=[IO.Path]::GetRelativePath($directory,$_.FullName).Replace('\','/');size=$_.Length;sha256=$(if($BadHash){'0'*64}else{(Get-FileHash -LiteralPath $_.FullName).Hash})}})
 @{schema=1;owner='fareed7479.FlowDesk';architecture='win-x64';version=$version;signed=$false;files=$files}|ConvertTo-Json -Depth 6|Set-Content "$directory/package.json"
 if($Undeclared){'unexpected'|Set-Content "$directory/unlisted.txt"}
 $zip=Join-Path $workspace "$name.zip"
 [IO.Compression.ZipFile]::CreateFromDirectory($directory,$zip)
 if($Traversal){$archive=[IO.Compression.ZipFile]::Open($zip,[IO.Compression.ZipArchiveMode]::Update);try{$entry=$archive.CreateEntry('../escaped.txt');$writer=[IO.StreamWriter]::new($entry.Open());try{$writer.Write('escaped')}finally{$writer.Dispose()}}finally{$archive.Dispose()}}
 return @{Path=$zip;Hash=(Get-FileHash -LiteralPath $zip).Hash}
}
$installScript=Join-Path $PSScriptRoot 'install-flowdesk.ps1'
$uninstallScript=Join-Path $PSScriptRoot 'uninstall-flowdesk.ps1'
$rollbackScript=Join-Path $PSScriptRoot 'rollback-flowdesk.ps1'
$install=Join-Path $workspace 'installed'
try {
 $first=Package '0.5.0' 'first';$next=Package '0.5.1' 'next'
 $import=Join-Path $workspace 'old-data';New-Item -ItemType Directory -Path $import|Out-Null
 '{"saved":"keep"}'|Set-Content (Join-Path $import 'workspace.json')
 '{"token":"do not migrate"}'|Set-Content (Join-Path $import 'flowweb-bridge.json')
 $answer=& $installScript -PackagePath $first.Path -Sha256 $first.Hash -InstallRoot $install -ImportDataPath $import -NoShortcut|ConvertFrom-Json
 Check ($answer.Installed -and (Test-Path $answer.Executable)) 'per-user package installation creates a versioned app entry point'
 Check ((Get-Content -Raw "$install/data/workspace.json") -match 'keep') 'explicit data migration preserves saved workspace contents'
 Check (!(Test-Path "$install/data/flowweb-bridge.json")) 'data migration excludes bridge tokens and transient recovery state'
 $marker=Get-Content -Raw "$install/versions/0.5.0/app/flowdesk-install.json"|ConvertFrom-Json
 Check ($marker.dataDirectory -eq "$install\data") 'installed app marker points at a stable data directory'
 $answer=& $installScript -PackagePath $next.Path -Sha256 $next.Hash -InstallRoot $install -NoShortcut|ConvertFrom-Json
 Check ($answer.Version -eq '0.5.1' -and (Test-Path "$install/versions/0.5.0/app/FlowDesk.App.exe")) 'update activates the new version and retains the prior version'
 $answer=& $rollbackScript -InstallRoot $install|ConvertFrom-Json
 Check ($answer.RolledBack -and $answer.Version -eq '0.5.0' -and (Get-Content -Raw "$install/data/workspace.json") -match 'keep') 'rollback verifies previous files and preserves data'
 $before=Get-Content -Raw "$install/current.json"
 $failed=$false;try{& $installScript -PackagePath $next.Path -Sha256 ('0'*64) -InstallRoot $install -NoShortcut|Out-Null}catch{$failed=$true}
 Check ($failed -and (Get-Content -Raw "$install/current.json") -eq $before) 'wrong archive checksum does not alter the active installation'
 $failed=$false;try{& $installScript -PackagePath $next.Path -Sha256 $next.Hash -InstallRoot "$workspace/unsigned" -RequireSignature -NoShortcut|Out-Null}catch{$failed=$true}
 Check $failed 'trusted-signature mode rejects an unsigned package'
 foreach($kind in @('bad-hash','traversal','undeclared')){
  $parameters=@{version='0.6.0';name=$kind};switch($kind){bad-hash{$parameters.BadHash=$true}traversal{$parameters.Traversal=$true}undeclared{$parameters.Undeclared=$true}}
  $bad=Package @parameters;$failed=$false
  try{& $installScript -PackagePath $bad.Path -Sha256 $bad.Hash -InstallRoot "$workspace/reject-$kind" -NoShortcut|Out-Null}catch{$failed=$true}
  Check ($failed -and !(Test-Path "$workspace/escaped.txt")) "installer rejects $kind package contents without escaping staging"
 }
 $unrelated=Join-Path $workspace 'unrelated';New-Item -ItemType Directory -Path $unrelated|Out-Null;'keep'|Set-Content "$unrelated/keep.txt"
 $failed=$false;try{& $installScript -PackagePath $first.Path -Sha256 $first.Hash -InstallRoot $unrelated -NoShortcut|Out-Null}catch{$failed=$true}
 Check ($failed -and (Get-Content "$unrelated/keep.txt") -eq 'keep') 'installer refuses unrelated nonempty directories'
 $answer=& $uninstallScript -InstallRoot $install -SkipAppIntegrationCleanup|ConvertFrom-Json
 Check ($answer.Uninstalled -and !(Test-Path "$install/versions") -and (Get-Content -Raw "$install/data/workspace.json") -match 'keep') 'uninstall removes owned app versions while preserving saved data'
 $failed=$false;try{& $uninstallScript -InstallRoot $unrelated|Out-Null}catch{$failed=$true}
 Check ($failed -and (Test-Path "$unrelated/keep.txt")) 'uninstall refuses unrelated directories'
 @{Success=$true;Passed=$passed;FixturePayloads=$true;RealPackageTest='Run install with an actual published package separately'}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $root '.cache/distribution-results.json')
 Get-Content -Raw (Join-Path $root '.cache/distribution-results.json')
}catch{@{Success=$false;Passed=$passed;Error=$_.ToString()}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $root '.cache/distribution-results.json');throw}
