using UnityEngine;
namespace IdleBlade
{
    public sealed partial class BarbarianLobby
    {
        bool hideRewards,hideSummon,drawerClosed,layoutLoaded;
        float drawerOffset,trackOffset,enemyX=1450,runClock;
        double observedHp=-1;
        int observedStage;
        bool observedBoss;
        void SaveLayout()
        {
            PlayerPrefs.SetInt("ui.hideRewards",hideRewards?1:0);
            PlayerPrefs.SetInt("ui.hideSummon",hideSummon?1:0);
            PlayerPrefs.SetInt("ui.drawerClosed",drawerClosed?1:0);PlayerPrefs.Save();
        }
        void UpdateLobbyCombat()
        {
            UpdateHuntFeedback();
            UpdateHordeVisuals();
            if(!layoutLoaded) {
                hideRewards=PlayerPrefs.GetInt("ui.hideRewards",0)==1;
                hideSummon=PlayerPrefs.GetInt("ui.hideSummon",0)==1;
                drawerClosed=PlayerPrefs.GetInt("ui.drawerClosed",0)==1;
                drawerOffset=drawerClosed?160:0;layoutLoaded=true;
            }
            drawerOffset=Mathf.MoveTowards(drawerOffset,drawerClosed?160:0,Time.unscaledDeltaTime*700);
            var s=game.Current;if(s==null || !game.Connected)return;
            bool boss=s.state.bossActive;
            double health=boss?s.state.bossHp:s.state.enemyHp;
            if(observedStage!=s.state.stage || observedBoss!=boss)enemyX=1450;
            observedHp=health;observedStage=s.state.stage;observedBoss=boss;
            if(!boss && s.state.monsters!=null && s.state.monsters.Length>0)enemyX=655+s.state.monsters[0].x*.14f;
            bool running=!hordeHolding&&enemyX>965;
            if(boss)enemyX=Mathf.MoveTowards(enemyX,910,Time.unscaledDeltaTime*850);
            if(!boss)trackOffset=(trackOffset+Time.unscaledDeltaTime*(hordeHolding?0:running?95:22))%3344;
            runClock+=Time.unscaledDeltaTime;
        }
        void DrawTrackBackground(bool clip=true)
        {
            if(fortress==null)return;

            // Alternate mirrored panels: both joins share the same edge pixels.
            if(clip)GUI.BeginGroup(new Rect(0,0,1672,941));
            for(int i=-1;i<3;i++) {
                float x=i*1672-trackOffset;
                bool flip=(i&1)!=0;
                GUI.DrawTextureWithTexCoords(new Rect(x,0,1672,941),fortress,new Rect(flip?1:0,0,flip?-1:1,1));
            }
            if(clip)GUI.EndGroup();
        }
        void DrawLobbyCombat()
        {
            var s=game.Current;bool boss=s!=null&&s.state.bossActive;
            bool running=game.Connected&&!hordeHolding&&enemyX>965;
            bool swing=game.Connected && (Time.unscaledTime<game.ManualSwingUntil || (game.AutoCombat&&!running&&nextSwing-Time.unscaledTime>Mathf.Max(0,(s==null?.8f:s.attackInterval)-.22f)));
            float bob=running?Mathf.Sin(runClock*17)*7:0;
            if(!boss){DrawHorde(false,true);DrawDeathFragments(false,true);}
            CombatCover(false);
            if(!boss){DrawHorde(false);DrawDeathFragments(false);}
            if(MainTexture("hero")!=null)MainHero(655,644,running,swing);
            else if(lobbyHero!=null) {
                float size=(440+GrowthTier*8)*(Raging?1.12f:1);
                GUI.DrawTexture(new Rect(655-size/2,644-size*.9f+bob,size,size),lobbyHero,ScaleMode.ScaleToFit);
            }
            else {var m=GUI.matrix;GUI.matrix=m*Matrix4x4.TRS(new Vector3(655,644+bob,0),Quaternion.identity,Vector3.one*2);Fighter(0,0,true,swing);GUI.matrix=m;}
            if(!boss){DrawHorde(true);DrawDeathFragments(true);DrawCombatImpacts();}
            else {
                if(!MainImage("boss",new Rect(enemyX-180,284,360,360))) {
                    var saved=GUI.matrix;
                    GUI.matrix=saved*Matrix4x4.TRS(new Vector3(enemyX,644,0),Quaternion.identity,Vector3.one*2.3f);
                    Fighter(0,0,false,false);GUI.matrix=saved;
                }
                Box(enemyX-110,348,220,9,ink);
                Box(enemyX-110,348,220*Mathf.Clamp01((float)(s.state.bossHp/s.state.bossMaxHp)),9,Color.red);
                Text(enemyX-140,313,280,30,"BOSS · 약탈자 우두머리",22,ivory,TextAnchor.MiddleCenter);
            }
            CombatCover(true);
            if(swing)Text(820,450,160,110,"╱",85,Raging?Color.red:gold,TextAnchor.MiddleCenter);
            if(s!=null && Time.unscaledTime<hitUntil)Text(enemyX-90,390,180,45,s.damage.ToString("N0"),29,gold,TextAnchor.MiddleCenter);
        }
        void CombatVitals()
        {
            var s=game.Current;
            float rage=Raging?100:s==null?0:Mathf.Clamp(s.state.rage,0,100);
            Frame(new Rect(14,641,475,98),Raging);
            MainImage("rage",new Rect(17,645,88,88));
            Text(109,645,367,41,Raging?$"광폭화 · {s.rageRemaining}초":"광폭화",32,gold);
            Box(108,692,366,31,new Color(.3f,.08f,.025f));
            Box(112,696,358*rage/100,23,new Color(1,.27f,.035f));
            Text(112,693,358,30,$"{rage:N0}/100",24,ivory,TextAnchor.MiddleCenter);
            Frame(new Rect(14,743,475,51));
            bool known=s!=null&&s.state.heroMaxHp>0;
            float hp=known?Mathf.Clamp01((float)(s.state.heroHp/s.state.heroMaxHp)):0;
            Box(78,753,396,31,new Color(.24f,.025f,.04f));
            Box(82,757,388*hp,23,new Color(.8f,.07f,.08f));
            Text(25,749,50,35,"HP",26,ivory);
            Text(82,752,388,32,known?$"{s.state.heroHp:N0} / {s.state.heroMaxHp:N0}":"—",24,ivory,TextAnchor.MiddleCenter);
        }
        void StageDock()
        {
            var s=game.Current;bool boss=s!=null&&s.state.bossActive;
            if(game.Current!=null && game.Current.state.highestCleared>0 && Button(new Rect(618,760,436,34),game.AutoBoss?"보스 자동 도전 ON":"보스 자동 도전 OFF",true))game.ToggleAutoBoss();
            Frame(new Rect(618,800,436,132),true);
            Text(630,810,412,36,$"스테이지 {(s==null?1:s.state.stage)} · {s?.regionName ?? "핏빛초원"}",26,ivory,TextAnchor.MiddleCenter);
            int need=s==null?15:Mathf.Max(1,s.requiredKills);
            ProgressPath(642,871,180,s==null?0:s.state.kills/(float)need);
            Text(635,894,199,28,$"보스 자격 {(s==null?0:s.state.kills)}/{need}",17,gold);
            string label=boss?$"보스전 {Mathf.CeilToInt(30-s.state.bossElapsed)}초":game.CanChallenge?"보스 도전":"자격 충전 중";
            if(Button(new Rect(850,860,189,60),label,game.CanChallenge,true))game.Challenge();
        }
        void GrowthDrawer()
        {
            float x=1520+drawerOffset;
            string[] labels={"장비","무기","방어구","스킬","분노","승급","유물","도감"};
            for(int i=0;i<8;i++)if(x<1672 && HomeButton(new Rect(x,86+i*77,145,65),labels[i],new[]{"◇","⚔","▣","✦","♨","★","◆","▤"}[i]))popup=labels[i];
            if(Button(new Rect(Mathf.Min(1628,x-45),370,40,85),drawerClosed?"◀":"▶",true)) {drawerClosed=!drawerClosed;SaveLayout();}
        }
        void OptionalCards()
        {
            if(!hideRewards) {
                Frame(new Rect(956,648,293,143));Text(970,657,224,30,"방치 보상",25,gold);
                Text(970,696,265,37,"최대 12시간 · 자동 정산",18,ivory);
                if(HomeButton(new Rect(978,741,251,37),"보상 안내","▣"))popup="방치 보상";
                if(Button(new Rect(1209,654,32,32),"×",true)){hideRewards=true;SaveLayout();}
            }
            if(!hideSummon) {
                Frame(new Rect(1270,649,240,139)); if(!MainImage("axe",new Rect(1340,664,100,65)))Text(1270,668,240,55,"✦",32,gold,TextAnchor.MiddleCenter); Text(1270,730,240,35,"장비 소환",23,ivory,TextAnchor.MiddleCenter);
                if(GUI.Button(new Rect(1270,692,240,96),GUIContent.none,GUIStyle.none))popup="소환";

                if(GUI.Button(new Rect(1469,654,32,32),"×")){hideSummon=true;popup=null;SaveLayout();}
            }
        }
    }
}

