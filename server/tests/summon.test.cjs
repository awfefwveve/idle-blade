const {test}=require('node:test'),assert=require('node:assert/strict'),vm=require('node:vm'),fs=require('node:fs'),path=require('node:path');
function harness(){
 let now=1000000;const c=vm.createContext({Date:{now:()=>now*1000}});vm.runInContext(fs.readFileSync(path.join(__dirname,'../modules/index.js'),'utf8'),c);
 const s=c.initial(now);s.highestCleared=2;c.ensureSummon(s);let row={value:structuredClone(s),version:'1'},receipts={},conflict=false,random=0;
 const nk={uuidv4:()=>((random++*123457)%4294967296).toString(16).padStart(8,'0')+'-0000-4000-8000-000000000000',storageRead:keys=>keys[0].collection==='progress'?[structuredClone(row)]:receipts[keys[0].key]?[{value:structuredClone(receipts[keys[0].key])}]:[],storageWrite:w=>{assert.equal(w.length,2);if(conflict||w[0].version!==row.version||receipts[w[1].key])throw Error('conflict');row={value:structuredClone(w[0].value),version:String(+row.version+1)};receipts[w[1].key]=structuredClone(w[1].value);}};
 function request(banner='WEAPON',count=1,id='a'.repeat(32)){return {...JSON.parse(JSON.stringify(c.summonQuote(row.value,banner,count,now))),action:'summon',requestId:id,revision:row.value.revision};}
 return {c,nk,request,call:r=>JSON.parse(c.summonRpc({userId:'test'},nk,r)),saved:()=>structuredClone(row.value),seed:fn=>fn(row.value),advance:n=>now+=n,conflict:()=>conflict=true};
}
test('free then tickets then split gems; entire batch must be affordable',()=>{
 const h=harness();h.seed(s=>{s.summon.WEAPON.tickets=3;s.gems=200;s.paidGems=400;});
 const q=h.request('WEAPON',10);assert.deepEqual([q.free,q.tickets,q.freeGems,q.paidGems],[1,3,200,400]);
 const r=h.call(q);assert.equal(r.state.lastSummon.results.length,10);assert.equal(r.state.gems,0);assert.equal(r.state.paidGems,0);
 const old=h.saved();assert.throws(()=>h.call(h.request('WEAPON',10,'b'.repeat(32))));assert.deepEqual(h.saved(),old);
});
test('same request replays receipt without spending; changed payload rejects',()=>{
 const h=harness(),q=h.request('WEAPON',10);const a=h.call(q),b=h.call(q);assert.deepEqual(a.state.lastSummon,b.state.lastSummon);assert.equal(h.saved().summon.WEAPON.tickets,1);
 assert.throws(()=>h.call({...q,count:1}));assert.equal(h.saved().revision,1);
});
test('200th draw forces legendary while preserving natural mythic; banners independent',()=>{
 const h=harness();assert.equal(h.c.summonGrade(0,199),5);assert.equal(h.c.summonGrade(999999,199),6);
 h.seed(s=>s.summon.WEAPON.pity=199);const r=h.call(h.request());assert.equal(r.state.lastSummon.results[0].grade,5);assert.equal(r.state.summon.WEAPON.pity,0);assert.equal(r.state.summon.ARMOR.pity,0);
});
test('KST 05:00 reset and stale quote never increase charges silently',()=>{
 const h=harness();assert.equal(h.c.summonDay(Date.UTC(2026,8,27,19,59,59)/1000)+1,h.c.summonDay(Date.UTC(2026,8,27,20,0,0)/1000));
 const q=h.request();h.call(q);const old=h.saved();assert.throws(()=>h.call({...q,requestId:'b'.repeat(32),revision:old.revision}));assert.deepEqual(h.saved(),old);
});
test('storage failure cannot partially spend or grant',()=>{
 const h=harness(),q=h.request();const old=h.saved();h.conflict();assert.throws(()=>h.call(q));assert.deepEqual(h.saved(),old);
});
test('all weight boundaries and rejection-sampled uniform index',()=>{
 const h=harness();for(const [r,g] of [[0,1],[499999,1],[500000,2],[780000,3],[930000,4],[985000,5],[999000,6]])assert.equal(h.c.summonGrade(r,0),g);
 let values=['ffffffff','00000002'];assert.equal(h.c.summonRoll({uuidv4:()=>values.shift()+'-0000'},3),2);
});
