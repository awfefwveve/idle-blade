const assert=require('assert/strict'),{spawnSync}=require('child_process');
(async()=>{
 const base='http://127.0.0.1:7350',id='summon-smoke-'+require('crypto').randomUUID();
 const auth=await fetch(base+'/v2/account/authenticate/device?create=true',{method:'POST',headers:{'Content-Type':'application/json',Authorization:'Basic '+Buffer.from('localdevkey:').toString('base64')},body:JSON.stringify({id})});assert.equal(auth.status,200);const {token}=await auth.json();const sub=JSON.parse(Buffer.from(token.split('.')[1],'base64url')).uid;assert.match(sub,/^[0-9a-f-]{36}$/);
 let snap;async function rpc(body){const r=await fetch(base+'/v2/rpc/game',{method:'POST',headers:{'Content-Type':'application/json',Authorization:'Bearer '+token},body:JSON.stringify(JSON.stringify(body))});const d=await r.json();assert.equal(r.status,200,JSON.stringify(d));return JSON.parse(d.payload);}
 snap=await rpc({action:'sync'});
 // Fixture only: the UUID belongs to the brand-new test account created above.
 const sql="UPDATE storage SET value=jsonb_set(jsonb_set(value,'{highestCleared}','2'),'{stage}','3') WHERE collection='progress' AND key='main' AND user_id='"+sub+"';";
 const db=spawnSync('C:/Users/USER/AppData/Local/Programs/DockerDesktop/resources/bin/docker.exe',['exec','-i','idleblade-postgres-1','psql','-U','postgres','-d','nakama','-v','ON_ERROR_STOP=1'],{input:sql,encoding:'utf8'});assert.equal(db.status,0,db.stderr);assert.match(db.stdout,/UPDATE 1/);
 snap=await rpc({action:'sync'});const q=snap.summonQuotes.find(q=>q.banner==='WEAPON'&&q.count===10);assert.equal(q.free,1);assert.equal(q.tickets,9);
 const req={...q,action:'summon',revision:snap.state.revision,requestId:require('crypto').randomUUID().replaceAll('-','')};
 const first=await rpc(req);assert.equal(first.state.lastSummon.results.length,10);const second=await rpc(req);assert.deepEqual(first.state.lastSummon,second.state.lastSummon);assert.equal(second.state.summon.WEAPON.tickets,1);
 const saved=await rpc({action:'sync'});assert.deepEqual(saved.state.lastSummon.results,first.state.lastSummon.results);
 console.log('PASS live 10-draw, UUID randomness, atomic receipt, replay and persisted inventory on isolated test account');
})().catch(e=>{console.error(e);process.exitCode=1;});
