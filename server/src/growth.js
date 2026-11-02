function ensureGrowth(s) {
  if(!s.growth)s.growth={ATK:Math.max(0,Math.min(3000,(s.level || 1)-1)),HP:0,ASPD:0,CRIT:0,CRIT_DMG:0,REGEN:0};
  s.level=s.growth.ATK+1;
}
function maxHealth(s){ensureGrowth(s);var a=armorStats(s);return Math.floor(GROWTH[s.growth.HP][2]*(1+a.hp/10000)*(1+a.setHp/10000));}
function aps(s,active){ensureGrowth(s);return Math.min(active?5000:3500,Math.floor((1000000/weaponFamily(s).base_interval_ms)*(10000+s.growth.ASPD*50+(active?3500:0))/10000));}
function rollDamage(s,active,boss){
  ensureGrowth(s);
  s.rng=((s.rng || 1234567)*1664525+1013904223)>>>0;
  var crit=s.rng%10000<Math.min(8000,500+s.growth.CRIT*20);
  return Math.max(1,Math.floor(equipmentAttack(s)*weaponFamily(s).attack_coefficient_bp/10000*(active?1.25:1)*(crit?Math.min(50000,15000+s.growth.CRIT_DMG*100)/10000:1)*(boss?1-stageData(s.stage).reductionBp/10000:1)));
}
function regenerate(s){
  ensureGrowth(s);
  s.regenCredit=(s.regenCredit || 0)+maxHealth(s)*s.growth.REGEN;
  var heal=Math.floor(s.regenCredit/200000);s.regenCredit%=200000;
  s.heroHp=Math.min(maxHealth(s),s.heroHp+heal);
}
function growthQuote(s,def,count){
  ensureGrowth(s);var level=s.growth[def.id],total=0,n=0;
  var limit=count===-1?def.cap-level:Math.min(count,def.cap-level);
  for(;n<limit;n++){var next=def.costs[level+n];if(count===-1&&total+next>s.gold)break;total+=next;}
  return {count:n,cost:total};
}
function growthView(s){
  ensureGrowth(s);
  return STAT_DEFS.map(function(d){
    var l=s.growth[d.id],q=growthQuote(s,d,-1),ten=growthQuote(s,d,10);
    var value=d.id==='ATK'?GROWTH[l][1]:d.id==='HP'?GROWTH[l][2]:d.id==='ASPD'?l*50:d.id==='CRIT'?500+l*20:d.id==='CRIT_DMG'?15000+l*100:l;
    var next=l===d.cap?value:d.id==='ATK'?GROWTH[l+1][1]:d.id==='HP'?GROWTH[l+1][2]:value+(d.id==='ASPD'?50:d.id==='CRIT'?20:d.id==='CRIT_DMG'?100:1);
    return {id:d.id,name:d.name,level:l,cap:d.cap,unlockStage:d.unlock_stage,unlocked:s.highestCleared>=d.unlock_stage,value:value,nextValue:next,nextCost:l<d.cap?d.costs[l]:0,tenCount:ten.count,tenCost:ten.cost,maxCount:q.count,maxCost:q.cost};
  });
}
function upgradeStat(s,req){
  var id=req.stat || 'ATK',count=req.count===undefined?1:req.count;
  var def=STAT_DEFS.filter(function(d){return d.id===id;})[0];
  if(!def || [1,10,-1].indexOf(count)<0)throw {code:3,message:'Invalid growth request'};
  if(s.highestCleared<def.unlock_stage)throw {code:9,message:'Stat locked'};
  var q=growthQuote(s,def,count);
  if(!q.count || q.cost>s.gold)throw {code:9,message:'Upgrade unavailable'};
  var oldHp=maxHealth(s);s.gold-=q.cost;s.growth[id]+=q.count;s.level=s.growth.ATK+1;
  s.heroMaxHp=maxHealth(s);s.heroHp=Math.min(s.heroMaxHp,(s.heroHp || oldHp)+s.heroMaxHp-oldHp);
}
