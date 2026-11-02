function itemDef(id){return ITEMS.filter(function(d){return d.id===id;})[0];}
function itemSlot(d){return d.family_id?'WEAPON':d.slot;}
function grantItem(s,id){
  var own=s.equipment.owned[id];
  if(own)own.duplicates++;else s.equipment.owned[id]={duplicates:0,reserved:0};
}
function ensureEquipment(s){
  if(!s.equipment){s.equipment={owned:{},slots:{WEAPON:'W_AXE_G1',HEAD:'',BODY:'A_BODY_G1',FEET:''},enhance:{WEAPON:0,HEAD:0,BODY:0,FEET:0}};
    grantItem(s,'W_AXE_G1');grantItem(s,'A_BODY_G1');s.ore=s.ore || 0;}
  if(s.highestCleared>=1&&!s.equipment.firstBoss){grantItem(s,'W_AXE_G2');s.equipment.firstBoss=true;}
}
function weaponFamily(s){ensureEquipment(s);var d=itemDef(s.equipment.slots.WEAPON);return FAMILIES.filter(function(f){return f.id===d.family_id;})[0];}
function equipmentAttack(s){ensureEquipment(s);var e=s.equipment;return Math.floor(attack(s.level)*itemDef(e.slots.WEAPON).attack_multiplier_bp/10000*(10000+ENHANCE[e.enhance.WEAPON].weapon_bonus_bp)/10000);}
function armorStats(s){
  ensureEquipment(s);var e=s.equipment,bonus=0,min=6;
  ['HEAD','BODY','FEET'].forEach(function(slot){var d=itemDef(e.slots[slot]);min=Math.min(min,d?d.grade:0);if(d)bonus+=d.hp_bonus_bp*(10000+ENHANCE[e.enhance[slot]].armor_slot_bonus_bp)/10000;});
  return {hp:bonus,setHp:min>=6?1500:min>=4?1000:min>=2?500:0,reduction:min>=6?500:min>=4?300:0};
}
function equipmentPlan(s){
  var copy=JSON.parse(JSON.stringify(s)),lines=[],total=0;
  ITEMS.slice().sort(function(a,b){return a.grade-b.grade || (a.id<b.id?-1:1);}).forEach(function(d){
    var own=copy.equipment.owned[d.id];if(!own||!d.next_id)return;
    var n=Math.min(200-total,Math.floor((own.duplicates-own.reserved)/5));if(n<=0)return;
    own.duplicates-=n*5;for(var i=0;i<n;i++)grantItem(copy,d.next_id);
    lines.push({id:d.id,name:d.name,count:n,consume:n*5,result:itemDef(d.next_id).name});total+=n;
  });return {equipment:copy.equipment,lines:lines,count:total};
}
function equipmentView(s){
  ensureEquipment(s);var e=s.equipment;
  return ITEMS.map(function(d){
    var own=e.owned[d.id],slot=itemSlot(d),level=e.enhance[slot],curve=ENHANCE[level],family=d.family_id?FAMILIES.filter(function(f){return f.id===d.family_id;})[0]:null;
    var after=JSON.parse(JSON.stringify(s));after.equipment.slots[slot]=d.id;
    return {id:d.id,name:d.name,slot:slot,grade:d.grade,owned:!!own,duplicates:own?own.duplicates:0,reserved:own?own.reserved:0,equipped:e.slots[slot]===d.id,enhance:level,
      goldCost:Number(slot==='WEAPON'?curve.weapon_next_gold:curve.armor_next_gold),oreCost:Number(slot==='WEAPON'?curve.weapon_next_ore:curve.armor_next_ore),salvage:d.salvage_ore,canMerge:!!d.next_id,
      attackAfter:equipmentAttack(after),hpAfter:maxHealth(after),range:family?family.range_mm:0,targets:family?family.targets:0,interval:family?family.base_interval_ms:0};
  });
}
function equipmentCommand(s,req){
  ensureEquipment(s);var e=s.equipment;
  if(req.action==='merge_all'){var plan=equipmentPlan(s);if(!plan.count)throw {code:9,message:'No duplicates to merge'};s.equipment=plan.equipment;return;}
  var d=itemDef(req.item),own=d?e.owned[d.id]:null;
  if(!d||!own)throw {code:9,message:'Item not owned'};
  var slot=itemSlot(d),oldHp=maxHealth(s);
  if(req.action==='equip')e.slots[slot]=d.id;
  else if(req.action==='enhance'){
    var level=e.enhance[slot],r=ENHANCE[level];
    var gold=Number(slot==='WEAPON'?r.weapon_next_gold:r.armor_next_gold),ore=Number(slot==='WEAPON'?r.weapon_next_ore:r.armor_next_ore);
    if(level>=200 || s.gold<gold || s.ore<ore)throw {code:9,message:'Enhancement unavailable'};
    s.gold-=gold;s.ore-=ore;e.enhance[slot]++;
  } else if(req.action==='reserve'){
    if(req.count<0 || req.count>own.duplicates || req.count%1!==0)throw {code:3,message:'Invalid reserved duplicates'};
    own.reserved=req.count;
  } else if(req.action==='merge'){
    if(!d.next_id || own.duplicates-own.reserved<5)throw {code:9,message:'Merge unavailable'};
    own.duplicates-=5;grantItem(s,d.next_id);
  } else if(req.action==='salvage'){
    if(req.count<1 || req.count%1!==0 || req.count>own.duplicates-own.reserved)throw {code:9,message:'Salvage unavailable'};
    if(d.grade>=4 && req.confirm!==true)throw {code:9,message:'High grade confirmation required'};
    own.duplicates-=req.count;s.ore+=req.count*d.salvage_ore;
  }
  s.heroMaxHp=maxHealth(s);s.heroHp=Math.min(s.heroMaxHp,Math.max(0,s.heroHp)+Math.max(0,s.heroMaxHp-oldHp));
}
