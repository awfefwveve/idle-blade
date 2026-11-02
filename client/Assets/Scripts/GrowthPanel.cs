using UnityEngine;
namespace IdleBlade
{
    public sealed partial class BarbarianLobby
    {
        string GrowthValue(IdleGame.GrowthRow r,double value)
        {
            if(r.id=="ATK" || r.id=="HP")return value.ToString("N0");
            return (r.id=="ASPD"?"+":"")+(value/100).ToString("0.##")+"%"+(r.id=="REGEN"?" / 초":"");
        }
        void DrawGrowthPanel()
        {
            // Content panel only: the user's main HUD and navigation positions stay intact.
            Frame(new Rect(862,130,794,656));
            Text(886,143,590,40,"전사의 성장",32,gold);
            if(Button(new Rect(1578,144,57,38),"닫기",true)){popup=null;return;}
            var s=game.Current;
            Text(886,185,730,30,s==null?"연결 대기":$"보유 골드 {s.state.gold:N0}"+(s.state.bossActive?" · 보스전 중 강화 불가":""),22,ivory);
            if(s?.growthRows==null)return;
            for(int i=0;i<s.growthRows.Length;i++) {
                var r=s.growthRows[i];float y=225+i*90;
                Box(878,y,762,84,new Color(.07f,.065f,.06f,.96f));
                Text(888,y+2,355,27,$"{r.name}  Lv.{r.level}/{r.cap}",21,gold);
                Text(888,y+29,355,26,GrowthValue(r,r.value)+" → "+GrowthValue(r,r.nextValue),18,ivory);
                string note=!r.unlocked?$"{r.unlockStage}스테이지 클리어 시 해금":r.level==r.cap?"최대 레벨":$"다음 강화 {r.nextCost:N0} G";
                Text(888,y+55,355,23,note,16,ivory);
                int[] counts={1,10,-1};
                for(int j=0;j<3;j++) {
                    double cost=j==0?r.nextCost:j==1?r.tenCost:r.maxCost;
                    int amount=j==0?1:j==1?r.tenCount:r.maxCount;
                    bool can=game.CanGrow&&r.unlocked&&r.level<r.cap&&amount>0&&cost<=s.state.gold;
                    string label=(j==2?$"최대 +{amount}":$"+{amount}")+"\n"+cost.ToString("N0")+" G";
                    if(Button(new Rect(1250+j*128,y+12,122,62),label,can))game.UpgradeStat(r.id,counts[j]);
                }
            }
        }
    }
}
