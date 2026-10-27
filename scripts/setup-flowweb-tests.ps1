$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$target=Join-Path $root '.tools/browser-tests'
New-Item -ItemType Directory -Force -Path $target|Out-Null
& npm.cmd install --prefix $target playwright@1.64.1 --no-audit --no-fund --ignore-scripts --cache (Join-Path $root '.cache/npm')
if($LASTEXITCODE -ne 0){throw 'Playwright installation failed.'}
$env:PLAYWRIGHT_BROWSERS_PATH=Join-Path $root '.tools/browsers'
& (Join-Path $target 'node_modules/.bin/playwright.cmd') install chromium
if($LASTEXITCODE -ne 0){throw 'Isolated Chromium download failed.'}
