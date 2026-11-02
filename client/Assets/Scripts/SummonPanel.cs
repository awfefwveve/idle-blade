using System.Linq;
using UnityEngine;
namespace IdleBlade
{
    public sealed partial class BarbarianLobby
    {
        string summonBanner="WEAPON";
        IdleGame.SummonQuote summonConfirm;
        bool summonRates;
        Vector2 summonScroll;
        string SummonCost(IdleGame.SummonQuote q)=>$"무료 {q.free} + 티켓 {q.tickets}\n무료 보석 {q.freeGems:N0} + 유료 보석 {q.paidGems:N0}";
        void DrawSummonPanel()
        {
            Frame(new Rect(862,130,794,656));Text(886,143,600,40,"장비 소환",30,gold);
            if(Button(new Rect(1578,144,57,38),"닫기",true)){popup=null;summonConfirm=null;return;}
            var s=game.Current;if(s?.summonQuotes==null)return;
            if(summonConfirm!=null){
                var q=s.summonQuotes.First(x=>x.banner==summonConfirm.banner&&x.count==summonConfirm.count);
                bool same=SummonCost(q)==SummonCost(summonConfirm)&&q.version==summonConfirm.version;
                Text(896,240,726,200,$"{(q.banner=="WEAPON"?"무기":"방어구")} {q.count}회 소환\n"+SummonCost(summonConfirm)+"\n표시한 재화를 사용해 소환할까?",26,ivory);
                if(!same)Text(896,468,715,60,"비용이 변경됐어. 취소 후 다시 확인해줘.",21,gold);
                if(Button(new Rect(895,626,345,61),"취소",true))summonConfirm=null;
                if(Button(new Rect(1260,626,345,61),"소환 확정",same&&q.available&&game.CanSummon)){game.Summon(q);summonConfirm=null;}
                return;
            }
            if(Button(new Rect(886,195,235,46),"무기",true,summonBanner=="WEAPON")){summonBanner="WEAPON";summonScroll=Vector2.zero;}
            if(Button(new Rect(1132,195,235,46),"방어구",true,summonBanner=="ARMOR")){summonBanner="ARMOR";summonScroll=Vector2.zero;}
            if(Button(new Rect(1378,195,250,46),summonRates?"결과 보기":"확률 안내",true))summonRates=!summonRates;
            var one=s.summonQuotes.First(x=>x.banner==summonBanner&&x.count==1);
            Text(887,250,745,65,s.state.highestCleared<2?"2스테이지 클리어 후 소환 해금":"전설 이상 보장까지 "+(200-one.pity)+"회\n무료 횟수: 배너별 매일 오전 5시 초기화",23,gold);
            if(summonRates){
                var rows=s.equipmentRows.Where(x=>summonBanner=="WEAPON"?x.slot=="WEAPON":x.slot!="WEAPON").OrderBy(x=>x.grade).ToArray();
                double[] rates={50,28,15,5.5,1.4,.1};
                summonScroll=GUI.BeginScrollView(new Rect(889,325,739,283),summonScroll,new Rect(0,0,707,rows.Length*40+140));
                Text(0,0,700,100,"등급 추첨 후 계열/부위는 각 1/3.\n아래는 개별 아이템 전체 확률(반올림).\n보장 회차: 전설 99.9%, 신화 0.1%.\n무료·티켓·보석은 같은 천장을 공유해.",18,ivory);
                for(int i=0;i<rows.Length;i++)Text(0,110+i*40,700,36,$"{rows[i].name}  {rates[rows[i].grade-1]/3:0.#####}% · 해당 등급 {rates[rows[i].grade-1]}%",19,ivory);
                GUI.EndScrollView();
            } else {
                var receipt=s.state.lastSummon;
                Text(890,324,730,35,receipt==null?"소환 결과가 여기에 표시돼.":$"최근 {(receipt.banner=="WEAPON"?"무기":"방어구")} 소환 결과 · 저장 완료",22,ivory);
                if(receipt?.results!=null){
                    summonScroll=GUI.BeginScrollView(new Rect(889,366,739,242),summonScroll,new Rect(0,0,705,receipt.results.Length*42));
                    for(int i=0;i<receipt.results.Length;i++){var r=receipt.results[i];Text(0,i*42,700,39,$"{new[]{"일반","고급","희귀","영웅","전설","신화"}[r.grade-1]} · {r.name} · "+(r.fresh?"새 사용권":"중복 +1"),22,r.grade>=4?gold:ivory);}
                    GUI.EndScrollView();
                }
            }
            foreach(int n in new[]{1,10}){
                var q=s.summonQuotes.First(x=>x.banner==summonBanner&&x.count==n);float x=n==1?887:1260;
                if(Button(new Rect(x,631,360,96),$"{n}회 소환\n"+SummonCost(q),q.available&&game.CanSummon))summonConfirm=q;
            }
            Text(890,740,733,28,"사용권은 영구 보유 · 중복 장비는 장비창에서 합성/분해",18,ivory);
        }
    }
}
