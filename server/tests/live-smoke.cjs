// Creates a separate local test account; never edits player saves directly.
const assert = require('node:assert/strict');
async function main() {
  const base = 'http://127.0.0.1:7350';
  const id = 'smoke-' + require('node:crypto').randomUUID();
  const auth = await fetch(base + '/v2/account/authenticate/device?create=true', {
    method:'POST', headers:{'Content-Type':'application/json', Authorization:'Basic '+Buffer.from('localdevkey:').toString('base64')}, body:JSON.stringify({id})
  });
  assert.equal(auth.status,200); const {token} = await auth.json();
  async function rpc(action,revision) {
    const r = await fetch(base+'/v2/rpc/game',{method:'POST',headers:{'Content-Type':'application/json',Authorization:'Bearer '+token},body:JSON.stringify(JSON.stringify({action,revision}))});
    const data = await r.json();
    return {status:r.status, data:data.payload ? JSON.parse(data.payload) : data};
  }
  let r = await rpc('sync'); assert.equal(r.status,200,JSON.stringify(r.data)); assert.equal(r.data.state.gold,0);
  console.log('PASS guest authentication and initial save');
  for(let i=0;i<8;i++) { await new Promise(resolve=>setTimeout(resolve,2000)); await rpc('sync'); }
  r = await rpc('sync'); assert.equal(r.status,200); assert.ok(r.data.state.gold>=25);
  console.log('PASS real-time auto hunt rewards');
  assert.ok(r.data.state.monsters.length>1 && r.data.state.monsters.length<=40);
  assert.equal(new Set(r.data.state.monsters.map(m=>m.id)).size,r.data.state.monsters.length);
  assert.ok(r.data.state.monsters.every(m=>m.x>=900 && m.hp>0));
  assert.ok(r.data.state.onlineKills>0);
  console.log('PASS server horde entities, positions, living HP and kills');
  const revision=r.data.state.revision;
  r=await rpc('upgrade',revision); assert.equal(r.status,200); assert.equal(r.data.state.level,2);
  console.log('PASS paid attack upgrade');
  r=await rpc('upgrade',revision); assert.notEqual(r.status,200);
  r=await rpc('sync'); assert.equal(r.data.state.level,2);
  console.log('PASS replay rejection and persisted level');
}
main().catch(e=>{console.error(e);process.exitCode=1;});
