param([ValidateSet('Chrome','Edge','Chromium')][string[]]$Browser=@('Chrome','Edge'),[string]$BridgeConfigPath)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$hostExe=Join-Path $root 'src/FlowDesk.FlowWebHost/bin/Debug/net10.0-windows/win-x64/FlowDesk.FlowWebHost.exe'
if(!(Test-Path -LiteralPath $hostExe)){throw 'Build FlowDesk before installing the native host.'}
if(!$BridgeConfigPath){$BridgeConfigPath=Join-Path $root 'src/FlowDesk.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/data/flowweb-bridge.json'}
$BridgeConfigPath=[IO.Path]::GetFullPath($BridgeConfigPath)
$extension=Get-Content -Raw (Join-Path $root 'extensions/flowweb/manifest.json') | ConvertFrom-Json
$sha=[Security.Cryptography.SHA256]::Create();$hash=$sha.ComputeHash([Convert]::FromBase64String($extension.key));$sha.Dispose()
$id=-join($hash[0..15]|ForEach-Object{[char](97+($_ -shr 4));[char](97+($_ -band 15))})
$manifestPath=Join-Path $root '.flowweb-native/com.flowdesk.flowweb.json'
New-Item -ItemType Directory -Force -Path (Split-Path $manifestPath)|Out-Null
@{name='com.flowdesk.flowweb';description='FlowDesk FlowWeb local bridge';path=$hostExe;type='stdio';allowed_origins=@("chrome-extension://$id/")} | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding utf8NoBOM
@{schema=1;bridgeConfigPath=$BridgeConfigPath} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path (Split-Path $hostExe) 'flowweb-host-settings.json') -Encoding utf8NoBOM
foreach($item in $Browser) {
    $base=switch($item){Chrome{'Software\Google\Chrome\NativeMessagingHosts'}Edge{'Software\Microsoft\Edge\NativeMessagingHosts'}Chromium{'Software\Chromium\NativeMessagingHosts'}}
    $path="HKCU:\$base\com.flowdesk.flowweb"
    if(Test-Path -LiteralPath $path){$old=Get-ItemProperty -LiteralPath $path;if($old.FlowDeskOwner -ne 'fareed7479.FlowDesk'){throw "Native host registration already belongs to another installation: $item"}}
    New-Item -Path $path -Force|Out-Null
    Set-Item -LiteralPath $path -Value $manifestPath
    New-ItemProperty -LiteralPath $path -Name FlowDeskOwner -Value 'fareed7479.FlowDesk' -PropertyType String -Force|Out-Null
}
"FlowWeb host installed for: $($Browser -join ', '). Extension ID: $id"
