param([ValidateSet('Chrome','Edge','Chromium')][string[]]$Browser=@('Chrome','Edge','Chromium'))
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
if(Get-Process FlowDesk.App,FlowDesk.FlowWebHost -ErrorAction SilentlyContinue){throw 'Close FlowDesk and disconnect the extension before removing its native host registration.'}
$manifestPath=Join-Path $root '.flowweb-native/com.flowdesk.flowweb.json'
foreach($item in $Browser) {
    $base=switch($item){Chrome{'Software\Google\Chrome\NativeMessagingHosts'}Edge{'Software\Microsoft\Edge\NativeMessagingHosts'}Chromium{'Software\Chromium\NativeMessagingHosts'}}
    $path="HKCU:\$base\com.flowdesk.flowweb"
    if(Test-Path -LiteralPath $path) {
        $values=Get-ItemProperty -LiteralPath $path
        if($values.FlowDeskOwner -ne 'fareed7479.FlowDesk' -or (Get-Item -LiteralPath $path).GetValue('') -ne $manifestPath){throw "Refusing to remove a different installation: $item"}
        if(@(Get-ChildItem -LiteralPath $path).Count -ne 0){throw 'Unexpected registration subkeys; no removal performed.'}
        Remove-Item -LiteralPath $path
    }
}
