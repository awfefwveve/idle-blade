using System.Linq;
using UnityEngine;
namespace IdleBlade
{
    public sealed partial class BarbarianLobby
    {
        string selectedEquipment,confirmEquipmentAction,confirmEquipmentKey,confirmEquipmentText;
        int equipmentPage;
        Vector2 equipmentConfirmScroll;
        string EquipmentKey(IdleGame.EquipmentRow row)
        {
            if(confirmEquipmentAction=="merge_all")return string.Join(";",game.Current.mergePlan.Select(x=>x.id+":"+x.count));
            return row==null?"":row.id+":"+row.duplicates+":"+row.reserved+":"+row.enhance;
        }
        void ConfirmEquipment(string action,IdleGame.EquipmentRow row,string message)
        {confirmEquipmentAction=action;confirmEquipmentKey=EquipmentKey(row);confirmEquipmentText=message;equipmentConfirmScroll=Vector2.zero;}
        void DrawEquipmentPanel()
        {
            Frame(new Rect(862,130,794,656));
            Text(886,143,540,42,"장비 · 영구 사용권 / 중복 관리",26,gold);
            if(Button(new Rect(1578,144,57,38),"닫기",true)){popup=null;confirmEquipmentAction=null;return;}
            var s=game.Current;if(s?.equipmentRows==null){Text(887,200,720,45,"장비 정보를 불러오는 중",24,ivory);return;}
            Text(886,188,750,32,$"골드 {s.state.gold:N0}   광석 {s.state.ore:N0}"+(s.state.bossActive?" · 보스전 중 변경 불가":""),20,ivory);
            var rows=s.equipmentRows.Where(r=>popup=="무기"?r.slot=="WEAPON":popup=="방어구"?r.slot!="WEAPON":true).ToArray();
            var selected=rows.FirstOrDefault(r=>r.id==selectedEquipment);
            if(selected==null){selected=rows[0];selectedEquipment=selected.id;equipmentPage=0;}
            if(confirmEquipmentAction!=null){
                bool unchanged=confirmEquipmentKey==EquipmentKey(selected);
                Text(889,234,730,40,"소모와 결과 확인",26,gold);
                equipmentConfirmScroll=GUI.BeginScrollView(new Rect(886,282,747,330),equipmentConfirmScroll,new Rect(0,0,715,Mathf.Max(330,confirmEquipmentText.Split('\n').Length*32)));
                Text(8,0,700,Mathf.Max(320,confirmEquipmentText.Split('\n').Length*32),confirmEquipmentText,21,ivory,TextAnchor.UpperLeft);GUI.EndScrollView();
                if(!unchanged)Text(891,622,735,40,"보유 수량이 변경됐어. 취소 후 새 견적을 확인해줘.",20,gold);
                if(Button(new Rect(893,688,350,57),"취소",true))confirmEquipmentAction=null;
                if(Button(new Rect(1260,688,350,57),"확정",unchanged&&game.CanGrow)){
                    game.EquipmentAction(confirmEquipmentAction,selected.id,1,true);confirmEquipmentAction=null;
                }
                return;
            }
            equipmentPage=Mathf.Clamp(equipmentPage,0,(rows.Length-1)/6);
            for(int i=0;i<6;i++){
                int index=equipmentPage*6+i;if(index>=rows.Length)break;var r=rows[index];
                string label=r.name+(r.equipped?" [장착]":"")+"\n"+(r.owned?$"중복 {r.duplicates} · 보존 {r.reserved}":"미획득");
                if(Button(new Rect(883,232+i*72,264,66),label,true,r.id==selected.id))selectedEquipment=r.id;
            }
            if(Button(new Rect(884,672,78,40),"이전",equipmentPage>0))equipmentPage--;
            Text(964,672,101,40,$"{equipmentPage+1}/{(rows.Length+5)/6}",19,ivory,TextAnchor.MiddleCenter);
            if(Button(new Rect(1067,672,78,40),"다음",(equipmentPage+1)*6<rows.Length))equipmentPage++;
            var e=selected;int available=e.duplicates-e.reserved;
            Text(1170,234,457,37,e.name+" · "+new[]{"일반","고급","희귀","영웅","전설","신화"}[e.grade-1],26,gold);
            Text(1170,275,457,55,$"공격력 {s.damage:N0} → {e.attackAfter:N0}\nHP {s.state.heroMaxHp:N0} → {e.hpAfter:N0}",19,ivory);
            Text(1170,326,457,50,e.slot=="WEAPON"?$"기본 {e.interval/1000f:0.##}초 · {e.range/1000f:0.#}m · {e.targets}명":"등급 세트: 고급 HP +5% / 영웅 +10%\n신화 +15% · 영웅/신화 피해감소 3/5%",17,ivory);
            Text(1170,381,457,40,$"슬롯 강화 +{e.enhance}/200 · 교체 후 유지",21,gold);
            bool ready=game.CanGrow&&e.owned;
            if(Button(new Rect(1170,429,214,51),e.equipped?"장착 중":"장착",ready&&!e.equipped))game.EquipmentAction("equip",e.id);
            if(Button(new Rect(1396,429,231,51),$"강화 {e.goldCost:N0} G\n광석 {e.oreCost}",ready&&e.enhance<200&&s.state.gold>=e.goldCost&&s.state.ore>=e.oreCost))game.EquipmentAction("enhance",e.id);
            Text(1170,489,457,46,e.owned?$"사용권 보유 · 중복 {e.duplicates} · 보존 {e.reserved}":"아직 획득하지 않은 장비",20,ivory);
            if(Button(new Rect(1170,546,214,49),"중복 5개 합성",ready&&e.canMerge&&available>=5))ConfirmEquipment("merge",e,$"{e.name} 중복 5개 소모\n같은 계열 다음 등급 장비 1개 획득\n사용권과 슬롯 강화는 유지돼.");
            if(Button(new Rect(1396,546,231,49),$"분해 1개 · 광석 {e.salvage}",ready&&available>0))ConfirmEquipment("salvage",e,$"{new[]{"일반","고급","희귀","영웅","전설","신화"}[e.grade-1]} {e.name}\n중복 1개 분해 → 광석 {e.salvage}개\n사용권은 유지돼.");
            if(Button(new Rect(1170,608,214,49),"보존 -1",ready&&e.reserved>0))game.EquipmentAction("reserve",e.id,e.reserved-1);
            if(Button(new Rect(1396,608,231,49),"보존 +1",ready&&e.reserved<e.duplicates))game.EquipmentAction("reserve",e.id,e.reserved+1);
            if(Button(new Rect(1170,686,457,58),"일괄 합성 견적 · 최대 200회",game.CanGrow&&s.mergePlan?.Length>0))ConfirmEquipment("merge_all",e,string.Join("\n",s.mergePlan.Select(x=>$"{x.name} 중복 {x.consume}개\n→ {x.result} {x.count}개")));
            Text(887,740,260,27,"미획득 장비는 장착할 수 없어",16,ivory);
        }
    }
}
