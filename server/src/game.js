// ES5-compatible Nakama runtime. Economy mutations use one versioned document.
var MAX_STAGE = 500;
var MAX_LEVEL = 3001;
var OFFLINE_CAP = 12 * 60 * 60;
function stageData(stage){return STAGES[Math.max(0,Math.min(499,stage-1))];}
function requiredKills(stage){return stageData(stage).requiredKills;}
function hp(stage){return stageData(stage).hp;}
function attack(level){return GROWTH[Math.max(0,Math.min(3000,level-1))][1];}
function cost(level){return GROWTH[Math.max(0,Math.min(3000,level-1))][3] || 0;}
function initial(now) {
  return {schema: 2, revision: 0, stage: 1, highestCleared: 0, kills: 0, gold: 100, gems:0, relicShards:0, level: 1, balanceVersion:1, growth:{ATK:0,HP:0,ASPD:0,CRIT:0,CRIT_DMG:0,REGEN:0},heroHp:250,heroMaxHp:250,
    autoCombat: true, bossActive: false, bossResult: '', rewardRemainder: 0, enemyHp: hp(1), lastTick: now};
}
function resetRage(s) { s.rage=0; s.rageUntil=0; s.rageLockUntil=0; s.attackCredit=0; }
function strikes(s, at, base) {
  if (s.rageUntil && at >= s.rageUntil) {
    s.rageLockUntil=s.rageUntil+2; s.rageUntil=0; s.rage=0;
  }
  var active=s.rageUntil>at;
  s.attackCredit=(s.attackCredit || 0)+(active?135:100);
  var count=Math.floor(s.attackCredit/100); s.attackCredit%=100;
  var hits=[];
  for(var n=0;n<count;n++) {
    hits.push(Math.floor(base*(s.rageUntil>at?1.25:1)));
    if(!s.rageUntil && at >= (s.rageLockUntil || 0)) {
      s.rage=Math.min(100,(s.rage || 0)+8);
      if(s.rage===100) {s.rageUntil=at+6; s.rage=0;}
    }
  }
  return hits;
}
function initHorde(s) {
  if (!s.monsters || s.hordeStage !== s.stage) {
    s.monsters=[{id:(s.nextMonsterId || 1),x:1500,hp:s.enemyHp,maxHp:hp(s.stage)}];
    s.nextMonsterId=(s.nextMonsterId || 1)+1; s.hordeStage=s.stage;
    s.attackReadyTick=0;
  }
  // x is now radial distance to the hero, not a right-only screen coordinate.
  for(var i=0;i<s.monsters.length;i++)if(typeof s.monsters[i].sector!=='number')s.monsters[i].sector=s.monsters[i].id%8;
}
function hordeEvent(s,m,damage,dead,tick) {
  s.combatSeq=(s.combatSeq || 0)+1;
  if(!s.combatEvents)s.combatEvents=[];
  s.combatEvents.push({seq:s.combatSeq,id:m.id,sector:m.sector || 0,x:m.x,damage:damage,dead:dead,tick:tick});
  if(s.combatEvents.length>96)s.combatEvents.shift();
}
function hordeTick(s, tick) {
  var at=tick/20;
  if(s.rageUntil && at>=s.rageUntil){s.rageLockUntil=s.rageUntil+2;s.rageUntil=0;s.rage=0;}
  // Missed spawn slots are discarded at the cap; no backlog burst.
  if(tick%20===0 && s.monsters.length<40) {
    for(var spawn=0;spawn<4 && s.monsters.length<40;spawn++) {
      var id=s.nextMonsterId++;
      s.monsters.push({id:id,sector:id%8,x:5600+(id%4)*240,hp:hp(s.stage),maxHp:hp(s.stage)});
    }
  }
  s.monsters.sort(function(a,b){return a.x-b.x || a.id-b.id;});
  var active=(s.rageUntil || 0)>at;
  var family=weaponFamily(s);var range=family.range_mm*(active?1.5:1);
  var advance=s.monsters.length && s.monsters[0].x>range?40:0;
  for(var i=0;i<s.monsters.length;i++)if(tick>=(s.monsters[i].stunUntil || 0))s.monsters[i].x=Math.max(900,s.monsters[i].x-60-advance);
  if(tick<(s.attackReadyTick || 0))return;
  if(s.autoCombat===false)return;
  if(!hordeStrike(s,tick))return;
  s.attackReadyTick=tick+Math.ceil(20000/aps(s,(s.rageUntil || 0)>at));
}
function hordeStrike(s,tick) {
  var at=tick/20, active=(s.rageUntil || 0)>at;
  var family=weaponFamily(s);var range=family.range_mm*(active?1.5:1);
  s.monsters.sort(function(a,b){return a.x-b.x || a.id-b.id;});
  var targets=s.monsters.filter(function(m){return m.x<=range;}).slice(0,family.targets+(active?4:0));
  if(!targets.length)return false;
  s.basicActions=(s.basicActions || 0)+1;
  var damage;
  for(var j=0;j<targets.length;j++) {
    damage=rollDamage(s,active,false);
    targets[j].hp-=damage;
    if(family.id==='HAMMER' && s.basicActions%3===0)targets[j].stunUntil=tick+4;
    hordeEvent(s,targets[j],damage,targets[j].hp<=0,tick);
  }
  // Rage is charged once per valid attack, not once per target.
  if(!active && at>=(s.rageLockUntil || 0)) {
    s.rage=Math.min(100,(s.rage || 0)+8);
    if(s.rage===100){s.rage=0;s.rageUntil=at+6;}
  }
  var alive=[];
  for(var k=0;k<s.monsters.length;k++) {
    var m=s.monsters[k];
    if(m.hp>0){alive.push(m);continue;}
    var reward=Math.min(stageData(s.stage).gold,1000000000000-s.gold);
    s.gold+=reward;s.onlineGold=(s.onlineGold || 0)+reward;s.onlineKills=(s.onlineKills || 0)+1;
    if(s.stage>s.highestCleared)s.kills=Math.min(requiredKills(s.stage),s.kills+1);
  }
  s.monsters=alive;
  return true;
}
function finishBoss(s) {
        // Both attacks resolve before checking victory: simultaneous death loses.
        if (s.heroHp <= 0) { s.bossActive = false; s.bossResult = 'defeat'; }
        else if (s.bossHp <= 0) {
          s.bossActive = false; s.bossResult = 'victory';
          if (s.stage > s.highestCleared) {
            s.highestCleared = s.stage;
            var bossReward = Math.min(stageData(s.stage).firstGold,1000000000000-s.gold);
            s.gold += bossReward;
            s.gems=(s.gems || 0)+stageData(s.stage).gems;
            s.relicShards=(s.relicShards || 0)+stageData(s.stage).relicShards;
            s.onlineGold = (s.onlineGold || 0) + bossReward;
            s.onlineKills = (s.onlineKills || 0) + 1;
          }
          ensureEquipment(s);
          s.stage = Math.min(MAX_STAGE, s.stage + 1); s.kills = 0; s.enemyHp = hp(s.stage);
        } else if (s.bossElapsed >= 30) { s.bossActive = false; s.bossResult = 'timeout'; }
}
function settle(s, now, reconnect) {
  var elapsed = Math.max(0, now - s.lastTick);
  var seconds = Math.min(OFFLINE_CAP, elapsed);
  var offline = reconnect || elapsed > 5;
  if (offline) resetRage(s);
  if (s.bossActive) {
    if (offline) { s.bossActive = false; s.bossResult = 'disconnected'; }
    else {
      for (var tick = 1; tick <= seconds*20 && s.bossActive; tick++) {
        var at=s.lastTick+tick/20;
        if(s.rageUntil && at>=s.rageUntil){s.rageLockUntil=s.rageUntil+2;s.rageUntil=0;s.rage=0;}
        s.bossTicks=(s.bossTicks || 0)+1;s.bossElapsed=s.bossTicks/20;
        var active=(s.rageUntil || 0)>at;
        s.bossCredit=Math.min(20000,(s.bossCredit || 0)+aps(s,active));
        if(s.autoCombat!==false && s.bossCredit>=20000){
          s.bossCredit-=20000;s.bossHp=Math.max(0,s.bossHp-rollDamage(s,active,true));
          if(!active && at>=(s.rageLockUntil || 0)){s.rage=Math.min(100,(s.rage || 0)+8);if(s.rage===100){s.rage=0;s.rageUntil=at+6;}}
        }
        if(s.bossTicks%40===0)s.heroHp=Math.max(0,s.heroHp-Math.floor(s.bossAttack*(1-armorStats(s).reduction/10000)));
        if(s.heroHp>0)regenerate(s);
        finishBoss(s);
      }
    }
    if (!s.bossActive) resetRage(s);
    s.lastTick = Math.max(s.lastTick, now);
    return;
  }
  if(!offline) {
    initHorde(s);
    for(var t=1;t<=seconds*20;t++)hordeTick(s,s.lastTick*20+t);
    s.enemyHp=s.monsters.length?s.monsters[0].hp:hp(s.stage);
    s.lastTick=Math.max(s.lastTick,now);return;
  }
  s.monsters=null;
  for (var i = 0; i < seconds; i++) {
    var hits=offline?[Math.floor(equipmentAttack(s)*weaponFamily(s).attack_coefficient_bp/10000)]:strikes(s,s.lastTick+i+1,attack(s.level));
    for(var h=0;h<hits.length;h++) {
    s.enemyHp -= hits[h];
    if (s.enemyHp <= 0) {
      var reward = stageData(s.stage).gold;
      var tenths = reward * (offline ? 7 : 10) + (s.rewardRemainder || 0);
      var granted = Math.min(Math.floor(tenths / 10),1000000000000-s.gold);
      s.gold += granted;
      if (!offline) {
        s.onlineKills = (s.onlineKills || 0) + 1;
        s.onlineGold = (s.onlineGold || 0) + granted;
      }
      s.rewardRemainder = tenths % 10;
      if (!offline && s.stage > s.highestCleared) s.kills = Math.min(requiredKills(s.stage), s.kills + 1);
      s.enemyHp = hp(s.stage);
    }
  }
  }
  // Consume the entire absence, including time beyond the cap. Never rewind.
  s.lastTick = Math.max(s.lastTick, now);
}
function respond(s) {
  ensureEquipment(s);
  return JSON.stringify({summonQuotes:summonViews(s),equipmentRows:equipmentView(s),mergePlan:equipmentPlan(s).lines,armorReduction:armorStats(s).reduction,growthRows:growthView(s),attackInterval:20000/aps(s,(s.rageUntil || 0)>s.lastTick)/20,regionName:["핏빛초원","검은숲","잿불광산","얼음무덤","선조의폐허"][Math.floor((s.stage-1)/100)],state: s, damage: equipmentAttack(s), upgradeCost: cost(s.level),
    enemyMaxHp: s.bossActive ? s.bossMaxHp : hp(s.stage), requiredKills: requiredKills(s.stage), maxLevel: MAX_LEVEL, rageActive: (s.rageUntil || 0)>s.lastTick, rageRemaining: Math.ceil(Math.max(0,(s.rageUntil || 0)-s.lastTick))});
}
function gameRpc(ctx, logger, nk, payload) {
  if (!ctx.userId) throw {code: 16, message: 'Authentication required'};
  var req;
  try { req = JSON.parse(payload || '{}'); } catch (e) { throw {code: 3, message: 'Invalid JSON'}; }
  if(req && req.action==='dev_reset') {
    if(!ctx.env || ctx.env.IDLEBLADE_DEV_RESET!=='true')throw {code:7,message:'Development reset disabled'};
    var resetKey={collection:'progress',key:'main',userId:ctx.userId};
    var resetRows=nk.storageRead([resetKey]);
    if(!resetRows.length || req.revision!==resetRows[0].value.revision)throw {code:9,message:'Sync before reset'};
    var fresh=initial(Math.floor(Date.now()/1000));
    fresh.revision=resetRows[0].value.revision+1;
    fresh.resetGeneration=(resetRows[0].value.resetGeneration || 0)+1;
    ensureEquipment(fresh);ensureSummon(fresh);
    fresh.heroMaxHp=maxHealth(fresh);fresh.heroHp=fresh.heroMaxHp;
    nk.storageWrite([{collection:'progress',key:'main',userId:ctx.userId,value:fresh,version:resetRows[0].version,permissionRead:0,permissionWrite:0}]);
    nk.leaderboardRecordDelete(STAGE_BOARD,ctx.userId);
    publishRank(ctx,nk,fresh);
    return respond(fresh);
  }
  if(req && req.action==='summon')return summonRpc(ctx,nk,req);
  if (!req || ['sync', 'resume', 'upgrade', 'boss', 'auto_on', 'auto_off', 'tap','equip','enhance','reserve','merge','merge_all','salvage'].indexOf(req.action) < 0)
    throw {code: 3, message: 'Unknown action'};
  var key = {collection: 'progress', key: 'main', userId: ctx.userId};
  var rows = nk.storageRead([key]);
  var now = Math.floor(Date.now() / 1000);
  var s = rows.length ? rows[0].value : initial(now);
  var legacy = null;
  if (s.schema === 1) {
    legacy = JSON.parse(JSON.stringify(s));
    s.schema = 2; s.highestCleared = Math.max(0, s.stage - 1);
    s.bossActive = false; s.bossResult = ''; s.rewardRemainder = 0;
    s.kills = Math.min(s.kills, requiredKills(s.stage));
  }
  if (s.schema !== 2) throw {code: 9, message: 'Unsupported save schema'};
  var balanceBackup=null;
  if(s.balanceVersion!==1){
    balanceBackup=JSON.parse(JSON.stringify(s));ensureGrowth(s);s.balanceVersion=1;
    s.bossActive=false;s.bossResult='';s.monsters=null;s.enemyHp=hp(s.stage);resetRage(s);
    s.heroMaxHp=maxHealth(s);s.heroHp=s.heroMaxHp;
    s.gems=s.gems || 0;s.relicShards=s.relicShards || 0;
    // Old absence cannot be revalued retroactively at a new content balance.
    s.lastTick=Math.max(s.lastTick,now);
  }
  // A retry of an already committed purchase cannot charge again.
  if ((['upgrade','boss','auto_on','auto_off','tap','equip','enhance','reserve','merge','merge_all','salvage'].indexOf(req.action)>=0) && req.revision !== s.revision)
    throw {code: 9, message: 'State changed; sync before retrying'};
  if (s.bossActive && ['upgrade','boss','equip','enhance','reserve','merge','merge_all','salvage'].indexOf(req.action)>=0)
    throw {code: 9, message: 'Challenge in progress'};
  var priorMax=s.heroMaxHp || maxHealth(s);ensureEquipment(s);s.heroMaxHp=maxHealth(s);s.heroHp=Math.min(s.heroMaxHp,s.heroHp+Math.max(0,s.heroMaxHp-priorMax));
  if(typeof s.autoCombat!=='boolean')s.autoCombat=true;
  settle(s, now, req.action === 'resume' || legacy !== null);
  if(req.action==='auto_on' || req.action==='auto_off')s.autoCombat=req.action==='auto_on';
  if(req.action==='tap') {
    var ms=Date.now();
    // Server time and versioned storage prevent replay and concurrent extra hits.
    if(!s.lastManualMs || ms-s.lastManualMs>=250) {
      s.lastManualMs=ms;
      if(s.bossActive) {
        var damage=rollDamage(s,(s.rageUntil || 0)>now,true);strikes(s,now,0);
        s.bossHp=Math.max(0,s.bossHp-damage);finishBoss(s);
        if(!s.bossActive)resetRage(s);
      } else {initHorde(s);hordeStrike(s,now*20);s.enemyHp=s.monsters.length?s.monsters[0].hp:hp(s.stage);}
    }
  }
  if (req.action === 'boss') {
    if (s.kills < requiredKills(s.stage) || s.stage <= s.highestCleared)
      throw {code: 9, message: 'Boss not unlocked'};
    s.monsters=null; s.bossActive = true; s.bossResult = ''; s.bossElapsed = 0; resetRage(s);
    // Temporary slice balance; final content tables replace these coefficients.
    s.bossMaxHp = stageData(s.stage).bossHp; s.bossHp = s.bossMaxHp;
    s.heroMaxHp = maxHealth(s); s.heroHp = s.heroMaxHp;
    s.bossDamage = attack(s.level); s.bossAttack = stageData(s.stage).bossAttack; s.bossTicks=0;s.bossCredit=20000;
  }
  if (req.action === 'upgrade') upgradeStat(s,req);
  if(['equip','enhance','reserve','merge','merge_all','salvage'].indexOf(req.action)>=0)equipmentCommand(s,req);
  ensureSummon(s);
  s.revision++;
  var writes = [{collection: key.collection, key: key.key, userId: ctx.userId,
    value: s, version: rows.length ? rows[0].version : '*',
    permissionRead: 0, permissionWrite: 0}];
  if (legacy) writes.push({collection:'progress_backup', key:'schema1', userId:ctx.userId,
    value:legacy, version:'*', permissionRead:0, permissionWrite:0});
  if(balanceBackup)writes.push({collection:'progress_backup',key:'pre-growth-v1',userId:ctx.userId,value:balanceBackup,version:'*',permissionRead:0,permissionWrite:0});
  nk.storageWrite(writes);
  try { publishRank(ctx, nk, s); } catch (e) { if (logger.warn) logger.warn('Ranking update deferred: %s', String(e)); }
  return respond(s);
}
var STAGE_BOARD = 'stage_all_time_v2';
function publishRank(ctx, nk, s) {
  nk.leaderboardRecordWrite(STAGE_BOARD, ctx.userId, ctx.username || '', s.stage, 0, {});
}
function rankingRpc(ctx, logger, nk, payload) {
  if (!ctx.userId) throw {code:16, message:'Authentication required'};
  var rows = nk.storageRead([{collection:'progress', key:'main', userId:ctx.userId}]);
  if (rows.length) publishRank(ctx, nk, rows[0].value);
  var list = nk.leaderboardRecordsList(STAGE_BOARD, [ctx.userId], 20);
  function entry(r) { return {rank:Number(r.rank), stage:Number(r.score), name:'전사 ' + r.ownerId.slice(0,8)}; }
  return JSON.stringify({records:(list.records || []).map(entry), mine:(list.ownerRecords || []).map(entry)});
}
function InitModule(ctx, logger, nk, initializer) {
  nk.leaderboardCreate(STAGE_BOARD, true, 'desc', 'best', null, {}, true);
  initializer.registerRpc('ranking', rankingRpc);
  initializer.registerRpc('game', gameRpc);
  logger.info('IdleBlade economy schema 2 / boss progression ready');
}

