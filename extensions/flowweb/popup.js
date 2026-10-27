const status=document.querySelector('#status'),count=document.querySelector('#count'),connect=document.querySelector('#connect'),disconnect=document.querySelector('#disconnect'),light=document.querySelector('#light');
function render(state){status.textContent=state.message;count.textContent=state.connected?`${state.tabCount} web tabs shared`:'No tabs shared';connect.disabled=state.connected;disconnect.disabled=!state.connected;light.classList.toggle('on',state.connected);}
async function update(){render(await chrome.runtime.sendMessage({action:'state'}));}
connect.addEventListener('click',async()=>{connect.disabled=true;try{await chrome.runtime.sendMessage({action:'connect'});}finally{await update();}});
disconnect.addEventListener('click',async()=>{await chrome.runtime.sendMessage({action:'disconnect'});await update();});
chrome.storage.onChanged.addListener((changes,area)=>{if(area==='session' && changes.flowweb)render(changes.flowweb.newValue);});
update();
