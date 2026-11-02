const assert=require('node:assert/strict');
async function main(){
 const base='http://127.0.0.1:7350';
 const a=await fetch(base+'/v2/account/authenticate/device?create=true',{method:'POST',headers:{'Content-Type':'application/json',Authorization:'Basic '+Buffer.from('localdevkey:').toString('base64')},body:JSON.stringify({id:'boss-test-'+require('node:crypto').randomUUID()})});
 assert.equal(a.status,200);let {token,refresh_token}=await a.json();
 async function rpc(action,revision){const r=await fetch(base+'/v2/rpc/game',{method:'POST',headers:{'Content-Type':'application/json',Authorization:'Bearer '+token},body:JSON.stringify(JSON.stringify({action,revision}))});const d=await r.json();assert.equal(r.status,200,JSON.stringify(d));return JSON.parse(d.payload);}
 let r=await rpc('resume');
 for(let i=0;i<25 && r.state.kills<15;i++){await new Promise(x=>setTimeout(x,2000));r=await rpc('sync');}
 assert.equal(r.state.stage,1);assert.equal(r.state.kills,15);console.log('PASS live hunt qualifies without advancing');
 const renew=await fetch(base+'/v2/account/session/refresh',{method:'POST',headers:{'Content-Type':'application/json',Authorization:'Basic '+Buffer.from('localdevkey:').toString('base64')},body:JSON.stringify({token:refresh_token})});
 assert.equal(renew.status,200);token=(await renew.json()).token;console.log('PASS session refresh');
 r=await rpc('boss',r.state.revision);assert.equal(r.state.bossActive,true);
 for(let i=0;i<16 && r.state.bossActive;i++){await new Promise(x=>setTimeout(x,2000));r=await rpc('sync');}
 assert.equal(r.state.bossResult,'victory');assert.equal(r.state.stage,2);assert.equal(r.state.highestCleared,1);
 console.log('PASS live boss victory unlocks stage 2');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});
