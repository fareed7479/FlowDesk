import fs from 'node:fs/promises';
import path from 'node:path';
import http from 'node:http';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
import {spawn} from 'node:child_process';
import {createHash} from 'node:crypto';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
process.env.PLAYWRIGHT_BROWSERS_PATH=path.join(root,'.tools/browsers');
const require=createRequire(import.meta.url);const {chromium}=require(path.join(root,'.tools/browser-tests/node_modules/playwright'));
const passed=[];const report=path.join(root,'.cache/flowweb-browser-result.json');
function check(value,label){if(!value)throw new Error(label);passed.push(label);}
async function waitFor(fn,timeout=45000){const end=Date.now()+timeout;while(Date.now()<end){const value=await fn();if(value)return value;await new Promise(r=>setTimeout(r,100));}throw new Error('Timed out waiting for browser workflow');}
let context,app;
const server=http.createServer((req,res)=>{res.writeHead(200,{'Content-Type':'text/html; charset=utf-8'});res.end(`<html><head><title>FlowWeb fixture ${req.url==='/a'?'A':'B'}</title></head><body><h1>FlowWeb isolated fixture</h1><p>${req.url}</p></body></html>`);});
try{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const base=`http://127.0.0.1:${server.address().port}`;
 const extension=path.join(root,'extensions/flowweb');const manifest=JSON.parse(await fs.readFile(path.join(extension,'manifest.json'),'utf8'));
 const hash=createHash('sha256').update(Buffer.from(manifest.key,'base64')).digest().subarray(0,16);const id=[...hash].map(v=>String.fromCharCode(97+(v>>4),97+(v&15))).join('');
 context=await chromium.launchPersistentContext(path.join(root,'.cache','flowweb-profile-'+Date.now()),{channel:'chromium',headless:true,args:[`--disable-extensions-except=${extension}`,`--load-extension=${extension}`]});
 let worker=context.serviceWorkers()[0]??await context.waitForEvent('serviceworker');check(new URL(worker.url()).host===id,'isolated Chromium loads the pinned Manifest V3 extension');
 const a=await context.newPage();await a.goto(base+'/a');const b=await context.newPage();await b.goto(base+'/b');
 const popup=await context.newPage();await popup.goto(`chrome-extension://${id}/popup.html`);await popup.locator('#connect').click();
 await waitFor(async()=>/Open FlowDesk/.test(await popup.locator('#status').textContent()));check((await popup.locator('#count').textContent())==='No tabs shared','offline native host reports unavailable without sharing tab metadata');
 const appExe=path.join(root,'src/FlowDesk.App/bin/Debug/net10.0-windows10.0.19041.0/win-x64/FlowDesk.App.exe');
 await fs.writeFile(path.join(root,'.cache/flowweb-desktop-result.json'),JSON.stringify({stage:'not-started'}));
 app=spawn(appExe,['--flowweb-checks'],{stdio:'ignore',windowsHide:true});const appExit=new Promise(resolve=>app.once('exit',code=>resolve(code)));
 await waitFor(async()=>{const s=JSON.parse(await fs.readFile(path.join(root,'.cache/flowweb-desktop-result.json'),'utf8'));return s.stage==='ready';});
 await popup.locator('#connect').click();await waitFor(async()=>/Connected/.test(await popup.locator('#status').textContent()));check((await popup.locator('#count').textContent())==='2 web tabs shared','real native messaging shares only the two isolated HTTP tabs');
 const stage=await waitFor(async()=>{const s=JSON.parse(await fs.readFile(path.join(root,'.cache/flowweb-desktop-result.json'),'utf8'));if(s.stage==='failed')throw new Error(s.error);return s.stage==='awaiting-disconnect'?s:null;},60000);
 const tabs=await worker.evaluate(async()=>await chrome.tabs.query({}));const first=tabs.find(t=>t.url===`${base}/a`);
 check(first.mutedInfo.muted && first.pinned,'desktop UI mute and pin commands changed the real browser tab');
 check(tabs.filter(t=>t.url===`${base}/a` || t.url===`${base}/b`).length===4,'confirmed desktop workspace restore created the saved browser tabs');
 check(new Set(tabs.filter(t=>t.url===`${base}/a` || t.url===`${base}/b`).map(t=>t.windowId)).size===2,'browser workspace restoration opens a separate browser window');
 await popup.locator('#disconnect').click();await waitFor(async()=>/Disconnected/.test(await popup.locator('#status').textContent()));check((await popup.locator('#count').textContent())==='No tabs shared','extension disconnect ends sharing and clears its status');
 const done=await waitFor(async()=>{const s=JSON.parse(await fs.readFile(path.join(root,'.cache/flowweb-desktop-result.json'),'utf8'));if(s.stage==='failed')throw new Error(s.error);return s.stage==='complete'?s:null;});
 check((await Promise.race([appExit,new Promise(resolve=>setTimeout(()=>resolve('timeout'),10000))]))===0,'desktop FlowWeb workflow exits cleanly');
 await fs.writeFile(report,JSON.stringify({success:true,passed,desktopPassed:done.passed},null,2));console.log(JSON.stringify({success:true,browserChecks:passed.length,desktopChecks:done.passed.length}));
}catch(error){await fs.writeFile(report,JSON.stringify({success:false,passed,error:String(error.stack)},null,2));console.error(error);process.exitCode=1;}
finally{await context?.close();if(app && app.exitCode===null)app.kill();await new Promise(r=>server.close(r));}
