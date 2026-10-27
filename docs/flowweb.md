# FlowWeb setup and usage

FlowWeb is an optional Windows Chrome/Edge extension for FlowDesk v0.4.0. It shares web-tab metadata only after you connect, and exposes activate, mute and pin controls plus named browser tab workspaces.

## Setup

Use PowerShell 7 (`pwsh`) from the FlowDesk repository:

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\build.ps1
pwsh -ExecutionPolicy Bypass -File .\scripts\install-flowweb.ps1
pwsh -ExecutionPolicy Bypass -File .\scripts\run.ps1
```

The installer registers `com.flowdesk.flowweb` under the current user's Chrome and Edge native messaging host keys. The executable and generated manifest stay on D: in the project. It refuses to replace a registration owned by another application. The host is self-contained; it does not need a system-wide .NET installation.

Load the extension manually in each browser you want to connect:

1. Open `chrome://extensions` or `edge://extensions`.
2. Enable the browser's developer extension-loading option yourself and choose Load unpacked.
3. Select this repository's `extensions\flowweb` folder.
4. Check the displayed extension ID: `dinpkkjphjbeadjlhndbgaaciimepnom`. The development manifest includes a public key to keep this ID stable across folders/browsers.
5. In FlowDesk, open FlowWeb and enable the bridge.
6. Open the FlowWeb browser popup, review its sharing description, then choose Connect & share tabs.

No personal browser profiles are changed by the build or native host installer. The extension is not published in a store yet. A future store release needs its store identity explicitly reviewed and included in the native origin configuration.

## Use

Select a connected browser in FlowDesk, then one tab to activate, mute/unmute, or pin/unpin. Browser profiles/windows may reuse numeric tab IDs; FlowDesk scopes actions to the selected live connection. Selection is cleared when changing browser sessions.

For a browser workspace, select up to 25 tabs (or leave selection empty to use all shared tabs, up to that limit), enter a unique name, then Save selected tabs. Select a saved workspace and Open workspace; review and confirm the new-window action. Remove workspace uses a confirmation dialog. Original browser tabs remain open.

At most 50 normal HTTP/HTTPS tabs are shared per connection and four simultaneous browser connections are accepted. Snapshot updates are automatic while connected. A disconnected browser's metadata is cleared from FlowDesk. Reconnect explicitly after the desktop app closes or the connection fails. Desktop bridge enablement is session-based and off on startup.

Active FlowDesk focus/fullscreen sessions block FlowWeb actions that change browser focus or open a browser workspace. Mute and pin remain available.

## Data and permissions

- `nativeMessaging`: the local native bridge.
- `tabs`: web-tab titles, URLs and state for the shared tab list and controls.
- `storage`: ephemeral popup/connection state.

There are no page content scripts or website host permissions. The extension does not run in incognito. Native communication accepts the pinned origin and a local pairing token over a current-user named pipe. Frames have a 256 KiB limit, protocol version 1, correlation IDs and a command whitelist. Messages cannot invoke arbitrary OS commands. Browser restore accepts HTTP/HTTPS URLs without embedded credentials.

Live snapshots stay in memory. Browser workspace URLs are saved explicitly in `data/flowweb-workspaces.json` beside FlowDesk's executable. The local bridge token is in `data/flowweb-bridge.json`; the native host's generated settings reference that file. These files, native host manifests and test browser profiles are excluded from Git. Disabling the bridge closes connections, clears snapshots and rotates its token. Invalid or oversized workspace data preserves the existing library.

## Disconnect and remove

Disconnect in the extension, disable the desktop bridge and close FlowDesk. Then run:

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\uninstall-flowweb.ps1
```

This removes only matching FlowWeb native host registrations. Remove the unpacked extension manually through the browser's extension page if desired. Saved workspaces are retained.

## Repeatable tests

Node.js and PowerShell 7 are required. Test dependencies and browser downloads stay under `.tools` and `.cache` on D:.

```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\setup-flowweb-tests.ps1
pwsh -ExecutionPolicy Bypass -File .\scripts\verify-flowweb.ps1
```

The suite uses Playwright 1.64.1 with its isolated Chromium persistent profile. It serves synthetic loopback pages and drives the real extension, native host and WinUI controls. It verifies offline behavior, sharing, activate/mute/pin, saved workspace confirmation/new-window restore and disconnect cleanup. Additional tests cover permission scope, framing/origin/version rejection, URL restrictions and local client authentication. Native host settings are restored to the normal desktop configuration afterward.

The test suite does not load extensions into personal Chrome/Edge profiles. Actual installed Chrome/Edge profile validation is manual; current branded browsers do not support the test sideload flags used by isolated Chromium. Broader browser/store policies and managed enterprise installations require separate validation.

## Troubleshooting

If the popup cannot connect, build first, rerun the installer, open FlowDesk and enable its bridge, then connect again. Check the extension ID against the value above. A protocol version/origin mismatch requires matching extension and host builds. A blocked enterprise native-messaging policy must be handled by the browser/user administrator; the app does not change browser policies.

If saved browser data is unreadable, back up `flowweb-workspaces.json` for inspection before repairing it. Browser commands can fail if a tab closes between snapshot and action; reconnect or choose a current tab. Browser session cookies, history, page state, forms and scroll position are not part of tab-workspace restoration.

## Official references

- [Chrome native messaging](https://developer.chrome.com/docs/extensions/develop/concepts/native-messaging)
- [Microsoft Edge native messaging](https://learn.microsoft.com/en-us/microsoft-edge/extensions-chromium/developer-guide/native-messaging)
- [Chrome Tabs API and permissions](https://developer.chrome.com/docs/extensions/reference/api/tabs)
- [Incognito manifest behavior](https://developer.chrome.com/docs/extensions/reference/manifest/incognito)
- [Playwright extension testing](https://playwright.dev/docs/chrome-extensions)
