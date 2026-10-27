$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
if(Get-Process FlowDesk.App -ErrorAction SilentlyContinue){throw 'Close FlowDesk before isolated FlowWeb tests.'}
$config=Join-Path $root '.cache/flowweb-desktop-data/flowweb-bridge.json'
& "$PSScriptRoot/install-flowweb.ps1" -Browser Chrome,Edge,Chromium -BridgeConfigPath $config
try {
    & node --test "$root/tests/flowweb/extension.test.mjs" "$root/tests/flowweb/native-host.test.mjs"
    if($LASTEXITCODE -ne 0){throw 'Extension unit checks failed.'}
    & node "$root/tests/flowweb/browser.e2e.mjs"
    if($LASTEXITCODE -ne 0){throw 'Browser/native FlowWeb checks failed.'}
} finally {
    & "$PSScriptRoot/install-flowweb.ps1" -Browser Chrome,Edge
}
