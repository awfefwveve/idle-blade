const {test} = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');
function harness(seed,devReset=false) {
  let now = 1000000, row = seed ? {value:structuredClone(seed),version:"1"} : undefined, version = seed ? 1 : 0, conflict = false;
  const c = vm.createContext({Date: {now: () => now * 1000}});
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../modules/index.js'), 'utf8'), c);
  let writes;
  const nk = {
    leaderboardRecordDelete:()=>{},leaderboardRecordWrite:()=>{},
    storageRead: () => row ? [structuredClone(row)] : [],
    storageWrite: (batch) => {
      const [w]=batch; writes=structuredClone(batch);
      assert.equal(w.permissionWrite, 0); assert.equal(w.permissionRead, 0);
      if (conflict || w.version !== (row ? row.version : '*')) throw Error('version conflict');
      row = {value: structuredClone(w.value), version: String(++version)};
    }
  };
  return {c, call: (r, uid = 'player') => JSON.parse(c.gameRpc({userId: uid,env:devReset?{IDLEBLADE_DEV_RESET:'true'}:{}}, {}, nk, JSON.stringify(r))),
    writes: () => writes, advance: n => now += n, conflict: () => conflict = true, saved: () => structuredClone(row)};
}
test('offline rewards do not advance stages or boss qualification', () => {
  const h = harness(); h.call({action:'sync'}); h.advance(30);
  const r = h.call({action:'sync'}); assert.equal(r.state.stage, 1); assert.equal(r.state.kills, 0); assert.equal(r.state.gold, 135);
});
test('upgrade spends gold and raises attack; replay cannot spend twice', () => {
  const h = harness(); h.call({action:'sync'}); for(let i=0;i<5;i++){h.advance(3);h.call({action:"sync"});}
  const r = h.call({action:'sync'});
  const upgraded = h.call({action:'upgrade', revision:r.state.revision});
  assert.equal(upgraded.state.gold, r.state.gold-r.upgradeCost); assert.equal(upgraded.damage, 27);
  assert.throws(() => h.call({action:'upgrade', revision:r.state.revision}));
  assert.equal(h.saved().value.level, 2);
});
test('same-time sync cannot duplicate reward', () => {
  const h = harness(); h.call({action:'sync'}); h.advance(300);
  const a = h.call({action:'sync'}), b = h.call({action:'sync'});
  assert.equal(a.state.gold, b.state.gold); assert.equal(a.state.enemyHp, b.state.enemyHp);
});
test('offline cap consumes excess time once', () => {
  const h = harness(), j = harness(); h.call({action:'sync'}); j.call({action:'sync'});
  h.advance(86400); j.advance(43200);
  assert.equal(h.call({action:'sync'}).state.gold, j.call({action:'sync'}).state.gold);
  const a = h.saved().value.gold; assert.equal(h.call({action:'sync'}).state.gold, a);
});
test('client gold, clock, level, and stage claims are ignored', () => {
  const h = harness(); const r = h.call({action:'sync', gold:999999, level:100, stage:100, now:9999999999});
  assert.equal(r.state.gold, 100); assert.equal(r.state.level, 1); assert.equal(r.state.stage, 1);
});
test('unauthenticated and invalid requests fail without writes', () => {
  const h = harness(); assert.throws(() => h.call({action:'sync'}, ''));
  assert.throws(() => h.call({action:'cheat'})); assert.equal(h.saved(), undefined);
});
test('insufficient funds do not mutate save', () => {
  const base=harness();const seed=base.c.initial(1000000);seed.gold=0;
  const h = harness(seed); const r = h.call({action:'sync'}); const before = h.saved();
  assert.throws(() => h.call({action:'upgrade', revision:r.state.revision})); assert.deepEqual(h.saved(), before);
});
test('storage conflict does not overwrite committed state', () => {
  const h = harness(); h.call({action:'sync'}); const before = h.saved(); h.advance(60); h.conflict();
  assert.throws(() => h.call({action:'sync'})); assert.deepEqual(h.saved(), before);
});
test('backward clock grants nothing and does not rewind cursor', () => {
  const h = harness(); const a = h.call({action:'sync'}); h.advance(-10);
  const b = h.call({action:'sync'}); assert.equal(a.state.lastTick, b.state.lastTick); assert.equal(b.state.gold, 100);
});

function ready(h) {
  let r=h.call({action:'sync'});
  for(let i=0;i<15;i++){h.advance(3);r=h.call({action:'sync'});}
  assert.equal(r.state.kills,15);assert.equal(r.state.stage,1);
  return r;
}
test('horde attacks only in range, three targets, deterministic distance/id order',()=>{
 const h=harness(),s=h.c.initial(0);
 s.monsters=[5,3,2,1].map(id=>({id,x:1500,hp:30,maxHp:30}));s.nextMonsterId=6;
 h.c.hordeTick(s,1);
 assert.equal(s.monsters.filter(m=>m.hp<30).length,3);
 assert.equal(s.monsters.find(m=>m.id===5).hp,30);assert.equal(s.rage,8);
 const far=h.c.initial(0);far.monsters=[{id:1,x:6000,hp:30,maxHp:30}];
 h.c.hordeTick(far,1);assert.equal(far.monsters[0].hp,30);assert.equal(far.rage||0,0);
});
test('rage expands range and targets; spawn cap cannot build a backlog',()=>{
 const h=harness(),s=h.c.initial(0);s.rageUntil=10;s.nextMonsterId=41;
 s.monsters=Array.from({length:40},(_,i)=>({id:i+1,x:i<8?2200:6000,hp:100,maxHp:100}));
 h.c.hordeTick(s,10);assert.equal(s.monsters.length,40);assert.equal(s.nextMonsterId,41);
 assert.equal(s.monsters.filter(m=>m.hp<100).length,7);
 s.monsters=[];h.c.hordeTick(s,20);assert.equal(s.monsters.length,4);
});
test('rage triggers on 13 attacks, buffs damage and speed, then locks gain',()=>{
 const h=harness(),s=h.c.initial(0);h.c.resetRage(s);
 for(let t=1;t<=12;t++)h.c.strikes(s,t,100);
 assert.equal(s.rage,96);h.c.strikes(s,13,100);assert.equal(s.rageUntil,19);
 let hits=[];for(let t=14;t<19;t++)hits.push(...h.c.strikes(s,t,100));
 assert.equal(hits.length,6);assert.ok(hits.every(x=>x===125));
 h.c.strikes(s,19,100);assert.equal(s.rage,0);
 h.c.strikes(s,20,100);assert.equal(s.rage,0);
 h.c.strikes(s,21,100);assert.equal(s.rage,8);
});
test('rage state survives sync partitioning and offline resets it',()=>{
 const h=harness(),a=h.c.initial(0),b=h.c.initial(0);
 for(let t=1;t<=24;t++)h.c.settle(a,t,false);
 for(let t=3;t<=24;t+=3)h.c.settle(b,t,false);
 assert.equal(JSON.stringify(a),JSON.stringify(b));
 h.c.settle(a,100,true);assert.equal(a.rage,0);assert.equal(a.rageUntil,0);
});
test('online kill reward counters are authoritative and not replayed or increased offline',()=>{
 const h=harness();h.call({action:'sync'});h.advance(3);
 const a=h.call({action:'sync',onlineGold:99999,onlineKills:999});
 assert.equal(a.state.onlineKills,1);assert.equal(a.state.onlineGold,5);
 const b=h.call({action:'sync'});assert.equal(b.state.onlineGold,5);assert.equal(b.state.onlineKills,1);
 h.advance(60);const c=h.call({action:'resume'});
 assert.ok(c.state.gold>b.state.gold);assert.equal(c.state.onlineGold,5);assert.equal(c.state.onlineKills,1);
 const d=h.call({action:'upgrade',revision:c.state.revision});assert.equal(d.state.onlineGold,5);
});
test('qualified boss win advances once and replay cannot reward twice',()=>{
 const base=harness();const seed=base.c.initial(1000000);seed.growth.ATK=100;seed.growth.HP=100;seed.level=101;
 const h=harness(seed); const r=ready(h);
 const start=h.call({action:'boss',revision:r.state.revision});assert.equal(start.state.bossActive,true);
 assert.throws(()=>h.call({action:'upgrade',revision:start.state.revision}));
 let end;for(let i=0;i<5;i++){h.advance(3);end=h.call({action:'sync'});if(!end.state.bossActive)break;}
 assert.equal(end.state.bossResult,'victory');assert.equal(end.state.stage,2);assert.equal(end.state.highestCleared,1);assert.equal(end.state.kills,0);
 const gold=end.state.gold;assert.throws(()=>h.call({action:'boss',revision:r.state.revision}));
 assert.equal(h.call({action:'sync'}).state.gold,gold);
});
test('unqualified challenge rejected and resume never grants qualification',()=>{
 const h=harness();let r=h.call({action:'sync'});
 assert.throws(()=>h.call({action:'boss',revision:r.state.revision}));
 h.advance(3);r=h.call({action:'resume'});assert.equal(r.state.kills,0);
});
test('disconnect during boss preserves eligibility and consumes elapsed time',()=>{
 const h=harness(),r=ready(h);h.call({action:'boss',revision:r.state.revision});h.advance(60);
 const end=h.call({action:'resume'});assert.equal(end.state.bossResult,'disconnected');assert.equal(end.state.kills,15);assert.equal(end.state.stage,1);
 assert.equal(h.call({action:'sync'}).state.gold,end.state.gold);
});
test('migration preserves progression and resources without offline unlocks',()=>{
 const old={schema:1,revision:7,stage:8,kills:6,gold:1234,level:12,enemyHp:30,lastTick:999990};
 const h=harness(old);const r=h.call({action:'sync'});
 assert.equal(h.writes()[1].collection,"progress_backup");assert.deepEqual(h.writes()[1].value,old);assert.equal(r.state.schema,2);assert.equal(r.state.stage,8);assert.equal(r.state.highestCleared,7);assert.equal(r.state.level,12);assert.ok(r.state.gold>=1234);assert.equal(r.state.kills,6);
});
test('simultaneous death loses; last second victory beats timeout',()=>{
 const h=harness();
 function boss(){const s=h.c.initial(1000000);Object.assign(s,{bossActive:true,bossElapsed:29.95,bossTicks:599,bossCredit:20000,bossHp:25,bossDamage:25,heroHp:8,bossAttack:8,kills:15});return s;}
 let s=boss();h.c.settle(s,1000001,false);assert.equal(s.bossResult,'defeat');assert.equal(s.kills,15);
 s=boss();s.heroHp=9;h.c.settle(s,1000001,false);assert.equal(s.bossResult,'victory');
 s=boss();s.bossHp=100;s.heroHp=9;h.c.settle(s,1000001,false);assert.equal(s.bossResult,'timeout');assert.equal(s.kills,15);
});



test('radial attacks reach every approach sector and event replay cannot duplicate kills',()=>{
 const h=harness(),s=h.c.initial(0);s.rageUntil=100;s.nextMonsterId=9;
 s.monsters=Array.from({length:8},(_,sector)=>({id:sector+1,sector,x:1500,hp:10,maxHp:10}));
 h.c.hordeTick(s,1);assert.equal(s.onlineKills,7);assert.equal(s.gold,135);
 assert.equal(s.combatEvents.length,7);assert.ok(s.combatEvents.every(e=>e.dead&&e.damage>=31));
 h.c.hordeTick(s,2);assert.equal(s.onlineKills,7);assert.equal(s.combatSeq,7);
 h.c.hordeTick(s,13);assert.equal(s.onlineKills,8);assert.equal(s.combatEvents[7].sector,7);
 assert.equal(new Set(s.combatEvents.map(e=>e.seq)).size,8);
});
test('burst spawn rotates all directions, respects remaining capacity and migrates existing health',()=>{
 const h=harness(),s=h.c.initial(0);h.c.initHorde(s);s.monsters[0].hp=17;
 delete s.monsters[0].sector;h.c.initHorde(s);assert.equal(s.monsters[0].hp,17);assert.equal(s.monsters[0].sector,1);
 h.c.hordeTick(s,20);h.c.hordeTick(s,40);
 assert.equal(new Set(s.monsters.map(m=>m.sector)).size,8);
 s.monsters=Array.from({length:39},(_,i)=>({id:i+1,sector:i%8,x:6000,hp:100,maxHp:100}));
 s.nextMonsterId=100;h.c.hordeTick(s,60);assert.equal(s.monsters.length,40);assert.equal(s.nextMonsterId,101);
 for(let n=0;n<150;n++)h.c.hordeEvent(s,{id:n,sector:n%8,x:900},10,true,n);
 assert.equal(s.combatEvents.length,96);assert.ok(s.combatEvents[0].seq<s.combatEvents[95].seq);
});

test('manual mode stops automatic damage but monsters still approach; tap attacks and rewards',()=>{
 const h=harness();let r=h.call({action:'sync'});r=h.call({action:'auto_off',revision:r.state.revision});
 h.advance(2);r=h.call({action:'sync'});assert.equal(r.state.autoCombat,false);assert.equal(r.state.combatSeq||0,0);assert.equal(r.state.monsters[0].x,900);
 for(let i=0;i<3;i++){r=h.call({action:'tap',revision:r.state.revision});h.advance(.25);}
 assert.equal(r.state.onlineKills,1);assert.equal(r.state.gold,105);assert.equal(r.state.kills,1);
 const old=r.state.combatSeq;h.advance(2);r=h.call({action:'sync'});assert.equal(r.state.combatSeq,old);
 r=h.call({action:'auto_on',revision:r.state.revision});h.advance(2);r=h.call({action:'sync'});assert.ok(r.state.combatSeq>old);
});
test('tap is additional to auto, has a server cooldown, and cannot replay',()=>{
 const base=harness();const seed=base.c.initial(1000000);seed.enemyHp=1000;
 const h=harness(seed);let r=h.call({action:'sync'});h.advance(1);r=h.call({action:'sync'});
 const seq=r.state.combatSeq, revision=r.state.revision;
 r=h.call({action:'tap',revision});assert.ok(r.state.combatSeq>seq);
 assert.throws(()=>h.call({action:'tap',revision}));
 const after=r.state.combatSeq;r=h.call({action:'tap',revision:r.state.revision});assert.equal(r.state.combatSeq,after);
 h.advance(.25);r=h.call({action:'tap',revision:r.state.revision});assert.ok(r.state.combatSeq>after);
});
test('manual boss kill advances and rewards only once; auto-off still takes enemy damage',()=>{
 const h=harness();let s=h.c.initial(1000000);s.kills=15;s.growth.ATK=1000;s.level=1001;s.autoCombat=false;
 const j=harness(s);let r=j.call({action:'boss',revision:0});j.advance(2);r=j.call({action:'sync'});
 assert.equal(r.state.bossHp,r.state.bossMaxHp);assert.ok(r.state.heroHp<r.state.heroMaxHp);
 const rev=r.state.revision;
 r=j.call({action:'tap',revision:rev});assert.equal(r.state.stage,2);assert.equal(r.state.highestCleared,1);assert.equal(r.state.gold,350);
 assert.throws(()=>j.call({action:'tap',revision:rev}));assert.equal(j.saved().value.gold,350);
});
test('auto mode change rejects stale revision without reversing the current choice',()=>{
 const h=harness();let r=h.call({action:'sync'});let rev=r.state.revision;
 r=h.call({action:'auto_off',revision:rev});assert.throws(()=>h.call({action:'auto_on',revision:rev}));assert.equal(h.saved().value.autoCombat,false);
});

test('six growth definitions use design caps, unlocks and server quotes',()=>{
 const h=harness();const r=h.call({action:'sync'});assert.equal(r.damage,25);assert.equal(r.state.heroMaxHp,275);
 assert.equal(r.growthRows.length,6);assert.deepEqual(r.growthRows.map(x=>x.cap),[3000,3000,100,150,200,100]);
 assert.deepEqual(r.growthRows.map(x=>x.unlockStage),[0,0,3,5,5,10]);
 assert.equal(r.growthRows[0].tenCost,160);assert.equal(r.growthRows[0].maxCount,6);
 assert.throws(()=>h.call({action:'upgrade',stat:'ASPD',count:1,revision:r.state.revision}));
});
test('bulk and maximum upgrades spend exact sums and reject replay and injected costs',()=>{
 const base=harness(),seed=base.c.initial(1000000);seed.gold=200;const h=harness(seed);
 let r=h.call({action:'sync'});const rev=r.state.revision;
 r=h.call({action:'upgrade',stat:'ATK',count:10,cost:0,revision:rev});assert.equal(r.state.gold,40);assert.equal(r.damage,45);assert.equal(r.state.growth.ATK,10);
 assert.throws(()=>h.call({action:'upgrade',stat:'ATK',count:10,revision:rev}));
 r=h.call({action:'upgrade',stat:'HP',count:-1,revision:r.state.revision});assert.equal(r.state.growth.HP,3);assert.equal(r.state.gold,8);assert.equal(r.state.heroMaxHp,341);
 assert.throws(()=>h.call({action:'upgrade',stat:'HP',count:0,revision:r.state.revision}));
});
test('growth cap handles partial ten and locked boss build cannot change',()=>{
 const base=harness(),seed=base.c.initial(1000000);seed.growth.ATK=2998;seed.gold=900000000000;seed.kills=15;
 const h=harness(seed);let r=h.call({action:'sync'});const q=r.growthRows[0];assert.equal(q.tenCount,2);
 r=h.call({action:'upgrade',stat:'ATK',count:10,revision:r.state.revision});assert.equal(r.state.growth.ATK,3000);assert.equal(r.growthRows[0].maxCount,0);
 r=h.call({action:'boss',revision:r.state.revision});assert.throws(()=>h.call({action:'upgrade',stat:'HP',count:1,revision:r.state.revision}));
});
test('speed, critical damage and recovery change combat outputs',()=>{
 const h=harness(),s=h.c.initial(0);assert.equal(h.c.aps(s,false),1250);s.growth.ASPD=100;assert.equal(h.c.aps(s,false),1875);
 s.growth.REGEN=100;s.heroHp=100;for(let i=0;i<200;i++)h.c.regenerate(s);assert.equal(s.heroHp,127);
 let normal=0,boosted=0;const a=h.c.initial(0),b=h.c.initial(0);b.growth.CRIT=150;b.growth.CRIT_DMG=200;
 for(let i=0;i<1000;i++){normal+=h.c.rollDamage(a,false,false);boosted+=h.c.rollDamage(b,false,false);}assert.ok(boosted>normal*1.5);
});
test('balance migration preserves wallet and progression, snapshots original once',()=>{
 const old={schema:2,revision:8,stage:26,highestCleared:25,kills:40,gold:19693,level:10,lastTick:999990,enemyHp:40};
 const h=harness(old);let r=h.call({action:'sync'});assert.equal(r.state.gold,19693);assert.equal(r.state.stage,26);assert.equal(r.state.kills,40);assert.equal(r.state.growth.ATK,9);
 assert.equal(h.writes()[1].key,'pre-growth-v1');assert.deepEqual(h.writes()[1].value,old);
 h.call({action:'sync'});assert.equal(h.writes().length,1);
});
test('stage 500 is terminal and milestone rewards match design',()=>{
 const h=harness(),s=h.c.initial(0);s.stage=500;s.highestCleared=499;s.bossActive=true;s.bossHp=0;s.heroHp=1;
 h.c.finishBoss(s);assert.equal(s.stage,500);assert.equal(s.highestCleared,500);assert.equal(s.gems,100);assert.equal(s.relicShards,25);
 assert.equal(h.c.stageData(1).bossHp,2475);assert.equal(h.c.stageData(10).bossHp,h.c.stageData(10).hp*60);
});

test('starter licenses and first boss reward are granted once without phantom duplicates',()=>{
 const h=harness();let r=h.call({action:'sync'});assert.equal(r.equipmentRows.length,36);assert.equal(r.state.equipment.owned.W_AXE_G1.duplicates,0);assert.equal(r.state.heroHp,275);
 const s=h.c.initial(0);s.highestCleared=1;h.c.ensureEquipment(s);h.c.ensureEquipment(s);assert.equal(s.equipment.owned.W_AXE_G2.duplicates,0);
});
test('merge requires five unlocked duplicates, retains license and follows same family',()=>{
 const h=harness(),s=h.c.initial(0);h.c.ensureEquipment(s);for(let i=0;i<5;i++)h.c.grantItem(s,'W_AXE_G1');
 h.c.equipmentCommand(s,{action:'reserve',item:'W_AXE_G1',count:1});assert.throws(()=>h.c.equipmentCommand(s,{action:'merge',item:'W_AXE_G1'}));
 h.c.equipmentCommand(s,{action:'reserve',item:'W_AXE_G1',count:0});h.c.equipmentCommand(s,{action:'merge',item:'W_AXE_G1'});
 assert.equal(s.equipment.owned.W_AXE_G1.duplicates,0);assert.ok(s.equipment.owned.W_AXE_G2);assert.equal(s.equipment.slots.WEAPON,'W_AXE_G1');
});
test('salvage cannot consume license or reserved copies and requires high-grade confirmation',()=>{
 const h=harness(),s=h.c.initial(0);h.c.ensureEquipment(s);h.c.grantItem(s,'W_AXE_G4');h.c.grantItem(s,'W_AXE_G4');
 assert.throws(()=>h.c.equipmentCommand(s,{action:'salvage',item:'W_AXE_G4',count:1}));
 h.c.equipmentCommand(s,{action:'salvage',item:'W_AXE_G4',count:1,confirm:true});assert.equal(s.ore,30);assert.equal(s.equipment.owned.W_AXE_G4.duplicates,0);
 assert.throws(()=>h.c.equipmentCommand(s,{action:'salvage',item:'W_AXE_G4',count:1,confirm:true}));
});
test('slot enhancement persists through equip and grants exact attack multiplier',()=>{
 const h=harness(),s=h.c.initial(0);h.c.ensureEquipment(s);s.ore=5;s.gold=100;h.c.grantItem(s,'W_AXE_G2');
 h.c.equipmentCommand(s,{action:'enhance',item:'W_AXE_G1'});assert.equal(s.gold,0);assert.equal(s.ore,0);assert.equal(s.equipment.enhance.WEAPON,1);
 h.c.equipmentCommand(s,{action:'equip',item:'W_AXE_G2'});assert.equal(s.equipment.enhance.WEAPON,1);assert.equal(h.c.equipmentAttack(s),41);
 assert.throws(()=>h.c.equipmentCommand(s,{action:'equip',item:'W_SWORD_G6'}));
});
test('bulk merge cascades low to high, respects locks and stops at 200 operations',()=>{
 const h=harness(),s=h.c.initial(0);h.c.ensureEquipment(s);s.equipment.owned.W_AXE_G1.duplicates=6000;s.equipment.owned.W_AXE_G1.reserved=5;
 const plan=h.c.equipmentPlan(s);assert.equal(plan.count,200);assert.equal(s.equipment.owned.W_AXE_G1.duplicates,6000);
 h.c.equipmentCommand(s,{action:'merge_all'});assert.equal(s.equipment.owned.W_AXE_G1.duplicates,5000);assert.equal(s.equipment.owned.W_AXE_G1.reserved,5);assert.equal(s.equipment.owned.W_AXE_G2.duplicates,199);
});
test('armor highest set only and weapon family modify combat parameters',()=>{
 const h=harness(),s=h.c.initial(0);h.c.ensureEquipment(s);
 for(const slot of ['HEAD','BODY','FEET']){h.c.grantItem(s,'A_'+slot+'_G6');h.c.equipmentCommand(s,{action:'equip',item:'A_'+slot+'_G6'});}
 assert.equal(h.c.armorStats(s).setHp,1500);assert.equal(h.c.armorStats(s).reduction,500);
 h.c.grantItem(s,'W_SWORD_G1');h.c.equipmentCommand(s,{action:'equip',item:'W_SWORD_G1'});assert.equal(h.c.weaponFamily(s).targets,5);assert.equal(h.c.aps(s,false),909);
});
test('equipment RPC rejects stale replay and all mutations during boss battle',()=>{
 const base=harness(),seed=base.c.initial(1000000);base.c.ensureEquipment(seed);seed.ore=10;seed.kills=15;
 const h=harness(seed);let r=h.call({action:'sync'}),rev=r.state.revision;
 r=h.call({action:'enhance',item:'W_AXE_G1',revision:rev});assert.throws(()=>h.call({action:'enhance',item:'W_AXE_G1',revision:rev}));
 r=h.call({action:'boss',revision:r.state.revision});for(const action of ['equip','enhance','reserve','merge','merge_all','salvage'])assert.throws(()=>h.call({action,item:'W_AXE_G1',revision:r.state.revision}));
});

test('development reset clears the whole economy and keeps revision monotonic',()=>{
 const base=harness();const seed=base.c.initial(1);Object.assign(seed,{revision:999,stage:80,highestCleared:79,level:200,gold:999999,gems:900,paidGems:400,ore:700,bossActive:true,onlineKills:5000,combatSeq:800,lastSummon:{results:['old']},rageUntil:9999999});base.c.ensureSummon(seed);seed.growth.ATK=199;seed.equipment.enhance.WEAPON=150;seed.summon.WEAPON.pity=199;
 const h=harness(seed,true),r=h.call({action:'dev_reset',revision:999}).state;
 assert.equal(r.level,1);assert.equal(r.stage,1);assert.equal(r.highestCleared,0);assert.equal(r.gold,100);assert.equal(r.gems,0);assert.equal(r.ore,0);assert.equal(r.revision,1000);assert.equal(r.resetGeneration,1);assert.equal(r.lastTick,1000000);assert.equal(r.bossActive,false);assert.equal(r.summon.WEAPON.pity,0);assert.equal(r.equipment.enhance.WEAPON,0);assert.equal(r.growth.ATK,0);assert.equal(r.lastSummon,undefined);assert.equal(r.onlineKills,undefined);
 assert.throws(()=>h.call({action:'upgrade',revision:999}));assert.throws(()=>h.call({action:'dev_reset',revision:999}));
 const again=h.call({action:'dev_reset',revision:1000}).state;assert.equal(again.resetGeneration,2);
});
test('reset requires development flag, authentication and matching save version',()=>{
 const base=harness(),seed=base.c.initial(1000000);const h=harness(seed);
 assert.throws(()=>h.call({action:'dev_reset',revision:0}));assert.equal(h.saved().value.revision,0);
 const dev=harness(seed,true);assert.throws(()=>dev.call({action:'dev_reset',revision:0},''));dev.conflict();assert.throws(()=>dev.call({action:'dev_reset',revision:0}));assert.equal(dev.saved().value.revision,0);
});
