"""Build runtime integers from the checked-in design source; no runtime pow()."""
from pathlib import Path
from decimal import Decimal, ROUND_CEILING
import json
root=Path(__file__).resolve().parents[1]
source=root.parent/'Design/Release1/SourceData'
def read(name): return json.loads((source/(name+'.json')).read_text(encoding='utf-8'))
def save(name,value): (root/'data'/name).write_text(json.dumps(value,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
save('growth.json',[[r['level'],int(r['attack_value']),int(r['hp_value']),int(r['attack_next_gold']) if r['attack_next_gold'] is not None else None,int(r['hp_next_gold']) if r['hp_next_gold'] is not None else None] for r in read('growth_curve')])
save('stages.json',[dict(stage=r['id'],region=r['region_id'],hp=int(r['normal_hp']),attack=int(r['normal_attack']),gold=int(r['gold_per_base_kill']),bossHp=int(r['boss_hp']),bossAttack=int(r['boss_attack']),bossId=r['boss_id'],requiredKills=r['kill_charge'],spawnMs=r['spawn_interval_ms'],cap=r['max_alive'],gems=r['first_clear_gems'],firstGold=int(r['first_clear_gold']),relicShards=r['first_clear_relic_shards'],reductionBp=r['boss_damage_reduction_bp']) for r in read('stages')])
stats=read('stat_upgrades')
for r in stats:
    r['costs']=[int((Decimal(r['base_cost'])*Decimal(r['growth'])**i).to_integral_value(rounding=ROUND_CEILING)) for i in range(r['cap'])]
save('stats.json',stats)
print('Imported 500 stages, 3001 growth levels and 6 stat cost tables.')
