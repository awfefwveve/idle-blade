var SUMMON_VERSION='gear-seed-1';
function summonDay(now){return Math.floor((now+4*3600)/86400);}
function ensureSummon(s){
  ensureEquipment(s);
  if(!s.summon)s.summon={WEAPON:{pity:0,tickets:0,freeDay:-1},ARMOR:{pity:0,tickets:0,freeDay:-1}};
  if(s.highestCleared>=2&&!s.summon.intro){s.summon.WEAPON.tickets+=10;s.summon.intro=true;}
}
function summonQuote(s,banner,count,now){
  ensureSummon(s);var b=s.summon[banner];
  if(!b || [1,10].indexOf(count)<0)throw {code:3,message:'Invalid banner or count'};
  var free=b.freeDay===summonDay(now)?0:1,tickets=Math.min(b.tickets,count-free),gems=(count-free-tickets)*100;
  var freeGems=Math.min(s.gems || 0,gems),paidGems=gems-freeGems;
  return {banner:banner,count:count,free:free,tickets:tickets,freeGems:freeGems,paidGems:paidGems,available:s.highestCleared>=2&&paidGems<=(s.paidGems || 0),pity:b.pity,version:SUMMON_VERSION};
}
function summonViews(s){var out=[];['WEAPON','ARMOR'].forEach(function(b){[1,10].forEach(function(n){out.push(summonQuote(s,b,n,s.lastTick));});});return out;}
function summonRoll(nk,max){
  // UUID v4 source is independent from exposed combat RNG. Reject modulo bias.
  var limit=Math.floor(4294967296/max)*max;
  for(var i=0;i<128;i++){var n=parseInt(nk.uuidv4().slice(0,8),16);if(n<limit)return n%max;}
  throw {code:13,message:'Random source unavailable'};
}
function summonGrade(roll,pity){var acc=0,grade=6;for(var i=0;i<SUMMON_WEIGHTS.length;i++){acc+=SUMMON_WEIGHTS[i].weight;if(roll<acc){grade=SUMMON_WEIGHTS[i].grade;break;}}return pity===199&&grade<5?5:grade;}
function summonRpc(ctx,nk,req){
  if(!/^[0-9a-f]{32}$/.test(req.requestId || ''))throw {code:3,message:'Invalid request ID'};
  var signature=JSON.stringify([req.banner,req.count,req.version,req.free,req.tickets,req.freeGems,req.paidGems]);
  var key={collection:'progress',key:'main',userId:ctx.userId},rows=nk.storageRead([key]);
  if(!rows.length)throw {code:9,message:'Sync first'};
  var s=rows[0].value;
  var receiptKey={collection:'summon_receipts',key:(s.resetGeneration?s.resetGeneration+'_':'')+req.requestId,userId:ctx.userId};
  var receipts=nk.storageRead([receiptKey]);
  if(receipts.length){if(receipts[0].value.signature!==signature)throw {code:3,message:'Request ID reused'};s.lastSummon=receipts[0].value;return respond(s);}
  if(req.revision!==s.revision || s.balanceVersion!==1)throw {code:9,message:'Sync and review quote'};
  var now=Math.floor(Date.now()/1000);settle(s,now,false);ensureSummon(s);
  var q=summonQuote(s,req.banner,req.count,now);
  if(!q.available || req.version!==q.version || ['free','tickets','freeGems','paidGems'].some(function(k){return req[k]!==q[k];}))throw {code:9,message:'Quote changed or insufficient currency'};
  var b=s.summon[req.banner],before=b.pity,results=[];
  if(q.free)b.freeDay=summonDay(now);b.tickets-=q.tickets;s.gems-=q.freeGems;s.paidGems=(s.paidGems || 0)-q.paidGems;
  for(var i=0;i<req.count;i++){
    var grade=summonGrade(summonRoll(nk,1000000),b.pity);
    var options=ITEMS.filter(function(d){return d.grade===grade&&(req.banner==='WEAPON'?!!d.family_id:!d.family_id);});
    var d=options[summonRoll(nk,3)],fresh=!s.equipment.owned[d.id];grantItem(s,d.id);
    results.push({id:d.id,name:d.name,grade:grade,fresh:fresh});b.pity=grade>=5?0:b.pity+1;
  }
  s.revision++;
  var receipt={requestId:req.requestId,signature:signature,banner:req.banner,version:SUMMON_VERSION,at:now,before:before,after:b.pity,free:q.free,tickets:q.tickets,freeGems:q.freeGems,paidGems:q.paidGems,results:results};
  s.lastSummon=receipt;
  nk.storageWrite([{collection:key.collection,key:key.key,userId:ctx.userId,value:s,version:rows[0].version,permissionRead:0,permissionWrite:0},{collection:receiptKey.collection,key:receiptKey.key,userId:ctx.userId,value:receipt,version:'*',permissionRead:0,permissionWrite:0}]);
  return respond(s);
}
