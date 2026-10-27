param([string]$CertificateThumbprint,[string]$TimestampServer)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$env:DOTNET_CLI_HOME=Join-Path $root '.cache/dotnet-home'
$env:NUGET_PACKAGES=Join-Path $root '.cache/nuget'
$env:DOTNET_ROOT_X64=Join-Path $root '.tools/dotnet'
$dotnet=Join-Path $root '.tools/dotnet/dotnet.exe'
[xml]$props=Get-Content (Join-Path $root 'Directory.Build.props')
$version=[string]$props.Project.PropertyGroup.Version
$stage=Join-Path $root ('.cache/package-'+[Guid]::NewGuid().ToString('N'))
$out=Join-Path $root '.cache/dist'
New-Item -ItemType Directory -Force -Path $stage,$out|Out-Null
Push-Location $root
try {
 & $dotnet publish src/FlowDesk.App/FlowDesk.App.csproj -c Release -o (Join-Path $stage 'app') --self-contained true
 if($LASTEXITCODE -ne 0){throw 'Desktop publish failed'}
 & $dotnet publish src/FlowDesk.FlowWebHost/FlowDesk.FlowWebHost.csproj -c Release -o (Join-Path $stage 'flowweb-host') --self-contained true
 if($LASTEXITCODE -ne 0){throw 'Native host publish failed'}
 Copy-Item -LiteralPath (Join-Path $root 'extensions/flowweb') -Destination (Join-Path $stage 'extension') -Recurse
 foreach($script in @('install-flowdesk.ps1','uninstall-flowdesk.ps1','rollback-flowdesk.ps1')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $script) -Destination $stage}
 Copy-Item -LiteralPath (Join-Path $root 'docs/distribution.md') -Destination (Join-Path $stage 'README.md')
 foreach($directory in @('app','flowweb-host')){
  $data=Join-Path $stage "$directory/data"
  if(Test-Path -LiteralPath $data){throw 'Runtime data unexpectedly entered publish output'}
 }
 foreach($required in @('app/FlowDesk.App.exe','app/FlowDesk.App.pri','app/App.xbf','flowweb-host/FlowDesk.FlowWebHost.exe')){if(!(Test-Path -LiteralPath (Join-Path $stage $required))){throw "Required published artifact is missing: $required"}}
 $signed=$false
 if($CertificateThumbprint) {
  $cert=Get-Item -LiteralPath "Cert:\CurrentUser\My\$CertificateThumbprint"
  if(!$cert.HasPrivateKey -or $cert.NotAfter -lt (Get-Date) -or !($cert.EnhancedKeyUsageList.ObjectId -contains '1.3.6.1.5.5.7.3.3')){throw 'An unexpired code-signing certificate with private key is required'}
  foreach($relative in @('app/FlowDesk.App.exe','app/FlowDesk.App.dll','app/FlowDesk.Core.dll','app/FlowDesk.Windows.dll','app/FlowDesk.FlowWeb.dll','flowweb-host/FlowDesk.FlowWebHost.exe','flowweb-host/FlowDesk.FlowWebHost.dll','flowweb-host/FlowDesk.FlowWeb.dll','install-flowdesk.ps1','uninstall-flowdesk.ps1','rollback-flowdesk.ps1')){
   $parameters=@{FilePath=(Join-Path $stage $relative);Certificate=$cert;HashAlgorithm='SHA256'}
   if($TimestampServer){$parameters.TimestampServer=$TimestampServer}
   $signature=Set-AuthenticodeSignature @parameters
   if($signature.Status -ne 'Valid'){throw "Signature verification failed: $relative"}
  }
  $signed=$true
 }
 $entries=@(Get-ChildItem -LiteralPath $stage -File -Recurse|ForEach-Object{@{path=[IO.Path]::GetRelativePath($stage,$_.FullName).Replace('\','/');sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash;size=$_.Length}})
 @{schema=1;owner='fareed7479.FlowDesk';version=$version;architecture='win-x64';signed=$signed;files=$entries}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $stage 'package.json') -Encoding utf8NoBOM
 $zip=Join-Path $out "FlowDesk-$version-win-x64.zip"
 if(Test-Path -LiteralPath $zip){throw 'Package output already exists; move it aside before creating another build of the same version'}
 [IO.Compression.ZipFile]::CreateFromDirectory($stage,$zip,[IO.Compression.CompressionLevel]::Optimal,$false)
 $hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
 "$hash  $([IO.Path]::GetFileName($zip))"|Set-Content -LiteralPath ($zip+'.sha256') -Encoding ascii
 @{Package=$zip;Sha256=$hash;Signed=$signed;Files=$entries.Count}|ConvertTo-Json
}finally{Pop-Location}
