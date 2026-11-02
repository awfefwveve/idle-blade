const assert=require('node:assert/strict');
async function main(){
 const base='http://127.0.0.1:7350';
 async function post(path,body,auth){const r=await fetch(base+path,{method:'POST',headers:{'Content-Type':'application/json',Authorization:auth},body:JSON.stringify(body)});const t=await r.text();return {status:r.status,data:t?JSON.parse(t):{}};}
 const a=await post('/v2/account/authenticate/device?create=true',{id:'ranking-test-'+require('node:crypto').randomUUID()},'Basic '+Buffer.from('localdevkey:').toString('base64'));assert.equal(a.status,200);
 const auth='Bearer '+a.data.token;
 const sync=await post('/v2/rpc/game',JSON.stringify({action:'sync'}),auth);assert.equal(sync.status,200);
 const rank=await post('/v2/rpc/ranking',JSON.stringify({score:999999,stage:999999}),auth);assert.equal(rank.status,200,JSON.stringify(rank.data));
 const page=JSON.parse(rank.data.payload);assert.ok(page.records.length>0 && page.records.length<=20);assert.equal(page.mine.length,1);assert.equal(page.mine[0].stage,1);assert.ok(page.mine[0].rank>0);
 for(let i=1;i<page.records.length;i++)assert.ok(page.records[i-1].stage>=page.records[i].stage);
 console.log('PASS ranked list, own rank, sorting and forged RPC score ignored');
 const forged=await post('/v2/leaderboard/stage_all_time_v2',{record:{score:'999999'}},auth);assert.notEqual(forged.status,200);
 const check=await post('/v2/rpc/ranking','{}',auth);assert.equal(JSON.parse(check.data.payload).mine[0].stage,1);
 console.log('PASS direct client score write rejected');
}
main().catch(e=>{console.error(e.message);process.exitCode=1;});

