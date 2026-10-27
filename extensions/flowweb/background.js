import { HOST, webUrl, packet, validPacket, validCommand } from './protocol.js';
let port = null, connected = false, timer = null, commands = Promise.resolve();
const browser = navigator.userAgent.includes('Edg/') ? 'Edge' : 'Chrome';
async function state(message, tabCount = 0) { await chrome.storage.session.set({ flowweb: { connected, message, tabCount } }); }
async function snapshot() {
  if (!connected || !port) return;
  const current=port;
  const all = (await chrome.tabs.query({})).filter(t => !t.incognito && webUrl(t.url)).sort((a,b) => a.id-b.id);
  const tabs = all.slice(0, 50).map(t => ({ id:t.id, windowId:t.windowId, title:(t.title || 'Untitled').slice(0,200), url:t.url, active:!!t.active, audible:!!t.audible, muted:!!t.mutedInfo?.muted, pinned:!!t.pinned }));
  if(!connected || port!==current)return;
  current.postMessage(packet('snapshot', { browser, tabs }));
  await state(all.length > 50 ? 'Connected · sharing the first 50 web tabs' : 'Connected to FlowDesk', tabs.length);
}
function schedule() { clearTimeout(timer); timer = setTimeout(() => snapshot().catch(() => disconnect('Connection interrupted. Connect again.')), 250); }
async function execute(c) {
  if (!validCommand(c)) throw new Error('Unsupported browser command.');
  if (c.action === 'openWorkspace') { await chrome.windows.create({ url:c.urls, focused:true, incognito:false }); return; }
  const tab = await chrome.tabs.get(c.tabId);
  if (tab.incognito || !webUrl(tab.url)) throw new Error('This tab is outside the shared web-tab scope.');
  if (c.action === 'activate') { await chrome.tabs.update(c.tabId, { active:true }); await chrome.windows.update(tab.windowId, { focused:true }); }
  if (c.action === 'mute') await chrome.tabs.update(c.tabId, { muted:c.value });
  if (c.action === 'pin') await chrome.tabs.update(c.tabId, { pinned:c.value });
}
async function disconnect(message = 'Disconnected · tab sharing is off') {
  const previous=port;port=null;connected=false;clearTimeout(timer);previous?.disconnect();await state(message);
}
async function connect() {
  if (port) return;
  await state('Connecting…');
  const next=chrome.runtime.connectNative(HOST);port=next;
  next.onMessage.addListener(p => {
    if (port !== next) return;
    if (!validPacket(p)) { disconnect('Invalid native message. Connect again.'); return; }
    if (p.type === 'ready') { connected=true;snapshot().catch(() => disconnect('Connection interrupted. Connect again.')); }
    else if (p.type === 'error') disconnect('Open FlowDesk, enable its FlowWeb bridge, then connect again.');
    else if (p.type === 'command') {
      commands=commands.then(async () => {
        let result;
        try { await execute(p.data); result={ok:true}; } catch(e) { result={ok:false,error:String(e.message || 'Browser command failed.').slice(0,200)}; }
        if (port === next) { next.postMessage(packet('result',result,p.id)); await snapshot(); }
      }).catch(() => disconnect('Connection interrupted. Connect again.'));
    } else disconnect('Unsupported native message.');
  });
  next.onDisconnect.addListener(() => { void chrome.runtime.lastError; if(port===next){port=null;connected=false;state('Open FlowDesk, enable its FlowWeb bridge, then connect again.');} });
  next.postMessage(packet('hello',{browser}));
}
chrome.runtime.onMessage.addListener((message,sender,reply) => {
  if(sender.id !== chrome.runtime.id || !sender.url?.startsWith(chrome.runtime.getURL(''))) return false;
  if(message?.action === 'connect') { connect().then(() => reply({ok:true})).catch(() => {disconnect('Connection unavailable.');reply({ok:false});});return true; }
  if(message?.action === 'disconnect') {disconnect().then(() => reply({ok:true}));return true;}
  if(message?.action === 'state') {chrome.storage.session.get('flowweb').then(s => reply(s.flowweb || {connected:false,message:'Disconnected · tab sharing is off',tabCount:0}));return true;}
  return false;
});
for(const event of [chrome.tabs.onCreated,chrome.tabs.onRemoved,chrome.tabs.onUpdated,chrome.tabs.onActivated,chrome.tabs.onMoved,chrome.tabs.onAttached,chrome.tabs.onDetached]) event.addListener(schedule);
state('Disconnected · tab sharing is off');
