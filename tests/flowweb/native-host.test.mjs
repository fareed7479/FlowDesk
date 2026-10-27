import assert from 'node:assert/strict';
import test from 'node:test';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
import {createHash} from 'node:crypto';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const exe=path.join(root,'src/FlowDesk.FlowWebHost/bin/Debug/net10.0-windows/win-x64/FlowDesk.FlowWebHost.exe');
const manifest=JSON.parse(fs.readFileSync(path.join(root,'extensions/flowweb/manifest.json')));
const bytes=createHash('sha256').update(Buffer.from(manifest.key,'base64')).digest().subarray(0,16);
const id=[...bytes].map(v=>String.fromCharCode(97+(v>>4),97+(v&15))).join('');const origin=`chrome-extension://${id}/`;
function frame(value){const data=Buffer.from(JSON.stringify(value));const size=Buffer.alloc(4);size.writeUInt32LE(data.length);return Buffer.concat([size,data]);}
function run(args,input=Buffer.alloc(0)){return spawnSync(exe,args,{input,windowsHide:true,timeout:5000});}
test('real native host rejects a foreign extension before reading data',()=>{const r=run(['chrome-extension://aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/']);assert.equal(r.status,2);assert.equal(r.stdout.length,0);});
test('real native host rejects unsupported versions',()=>{const r=run([origin],frame({v:2,id:'a'.repeat(32),type:'hello',data:{browser:'Chrome'}}));assert.equal(r.status,1);assert.equal(r.stdout.length,0);});
test('real native host rejects oversized framing',()=>{const size=Buffer.alloc(4);size.writeUInt32LE(0x7fffffff);const r=run([origin],size);assert.equal(r.status,1);assert.equal(r.stdout.length,0);});
test('real native host rejects unexpected command-line switches',()=>{const r=run([origin,'--execute=bad']);assert.equal(r.status,2);assert.equal(r.stdout.length,0);});
