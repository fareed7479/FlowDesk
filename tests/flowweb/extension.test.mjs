import assert from 'node:assert/strict';
import test from 'node:test';
import fs from 'node:fs';
import {webUrl,validCommand,validPacket} from '../../extensions/flowweb/protocol.js';
test('extension permissions stay within native messaging, tabs and session state',()=>{const m=JSON.parse(fs.readFileSync(new URL('../../extensions/flowweb/manifest.json',import.meta.url)));assert.deepEqual([...m.permissions].sort(),['nativeMessaging','storage','tabs']);assert.equal(m.incognito,'not_allowed');assert.equal(m.host_permissions,undefined);assert.equal(m.content_scripts,undefined);assert.equal(m.externally_connectable,undefined);});
test('extension rejects unsafe and credential-bearing URLs',()=>{for(const u of ['javascript:alert(1)','file:///C:/secret','data:text/html,x','https://user:pass@example.test/'])assert.equal(webUrl(u),false);assert.equal(webUrl('https://example.test/?q=1'),true);});
test('extension accepts only explicit supported tab commands',()=>{assert.equal(validCommand({action:'mute',tabId:1,value:true}),true);assert.equal(validCommand({action:'mute',tabId:1}),false);assert.equal(validCommand({action:'execute',script:'x'}),false);assert.equal(validCommand({action:'activate',tabId:1,extra:'x'}),false);});
test('extension validates restore limits and message versions',()=>{assert.equal(validCommand({action:'openWorkspace',urls:['https://example.test/']}),true);assert.equal(validCommand({action:'openWorkspace',urls:Array(26).fill('https://example.test/')}),false);assert.equal(validPacket({v:2,id:'a'.repeat(32),type:'command',data:{}}),false);});
