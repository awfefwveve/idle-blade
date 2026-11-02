using UnityEngine;
namespace IdleBlade
{
    public sealed partial class BarbarianLobby
    {
        // Reference image coordinates: both screens share a 1672 x 941 canvas.
        void Disc(Rect r, Color color)
        { GUI.DrawTexture(r,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,color,0,r.width/2); }
        bool RoundControl(Rect r,string icon,string caption,bool active=false)
        {
            Disc(r,bronze);Disc(new Rect(r.x+4,r.y+4,r.width-8,r.height-8),active?new Color(.35f,.15f,.035f):ink);
            string artId=icon=="!"?"notice":icon=="✉"?"mail":icon=="⚙"?"settings":null;
            if(artId==null||!MainImage(artId,new Rect(r.x+10,r.y+10,r.width-20,r.height-20)))Text(r.x,r.y+6,r.width,r.height*.52f,icon,32,gold,TextAnchor.MiddleCenter);
            Text(r.x,r.y+r.height*.52f,r.width,r.height*.37f,caption,18,ivory,TextAnchor.MiddleCenter);
            return GUI.Button(r,GUIContent.none,GUIStyle.none);
        }
        void ProgressPath(float x,float y,float width,float progress)
        {
            Box(x,y,width,5,new Color(.32f,.08f,.06f));
            Box(x,y,width*Mathf.Clamp01(progress),5,new Color(.9f,.18f,.08f));
            for(int i=0;i<7;i++)Text(x-10+width*i/6,y-13,24,30,"◆",20,i/6f<=progress?gold:bronze,TextAnchor.MiddleCenter);
        }
        bool Tile(Rect r,string title,string icon,bool active=false)
        {
            Frame(r,active);
            if(!MenuImage(title,new Rect(r.x+(r.width-48)/2,r.y+6,48,r.height*.52f)))Text(r.x,r.y+5,r.width,r.height*.55f,icon,32,gold,TextAnchor.MiddleCenter);
            Text(r.x,r.y+r.height*.55f,r.width,r.height*.4f,title,22,ivory,TextAnchor.MiddleCenter);
            return GUI.Button(r,GUIContent.none,GUIStyle.none);
        }
        void Header()
        {
            var s=game.Current;
            Frame(new Rect(12,8,350,118));
            Frame(new Rect(20,17,98,98));
            if(MainTexture("hero")!=null)GUI.DrawTextureWithTexCoords(new Rect(22,19,94,94),MainTexture("hero"),new Rect(.32f,.57f,.39f,.41f));
            else if(lobbyHero!=null) GUI.DrawTexture(new Rect(22,19,94,94),lobbyHero,ScaleMode.ScaleToFit);
            Text(134,16,225,34,"바바리안",27,ivory);
            Text(134,52,210,25,$"Lv. {(s==null?1:s.state.level)}",22,gold);
            Box(135,82,206,7,new Color(.22f,.08f,.06f));
            Text(134,94,220,26,"공격력 "+(s==null?"—":s.damage.ToString("N0")),21,new Color(1,.4f,.3f));
            if(GUI.Button(new Rect(12,8,350,118),GUIContent.none,GUIStyle.none))popup="성장";
            string[] icons={"●","◆","◈","♨"};
            string[] values={s==null?"—":s.state.gold.ToString("N0"),s==null?"—":s.state.gems.ToString("N0"),"—","—"};
            for(int i=0;i<4;i++) {
                float x=575+i*220;
                Frame(new Rect(x,15,208,40));
                if(!MainImage(new[]{"gold","gem","meat","rage"}[i],new Rect(x+7,18,34,34)))Text(x+7,15,36,40,icons[i],26,gold);
                Text(x+42,15,130,40,values[i],23,ivory,TextAnchor.MiddleRight);
                if(GUI.Button(new Rect(x+176,18,27,32),"+"))popup=i==0?"성장":"재화";
            }
            if(Tile(new Rect(385,12,104,88),"칭호","♜"))popup="칭호";
            if(RoundControl(new Rect(1442,10,54,54),"!",""))popup="공지";
            if(RoundControl(new Rect(1517,9,58,58),"✉",""))popup="우편함";
            if(RoundControl(new Rect(1596,9,58,58),"⚙",""))popup="설정";
        }
        void Mission(Rect r)
        {
            Frame(r);Text(r.x+18,r.y+5,r.width-36,32,"오늘의 임무",25,gold);
            Text(r.x+18,r.y+37,r.width-105,27,"임무 준비 중",19,ivory);
            Box(r.x+20,r.y+r.height-28,r.width-110,12,new Color(.1f,.18f,.22f));
            if(!MainImage("gem",new Rect(r.x+r.width-82,r.y+39,60,45)))Text(r.x+r.width-82,r.y+39,60,45,"◆",30,new Color(.3f,.7f,1));
            if(GUI.Button(r,GUIContent.none,GUIStyle.none))popup="퀘스트";
        }
        void Navigation()
        {
            string[] labels={"전투","각성","던전","상점","소환","패스"};
            string[] icons={"⚔","★","▣","◇","✦","♜"};
            int[] xs={12,214,416,1062,1264,1466};
            for(int i=0;i<6;i++) if(Tile(new Rect(xs[i],800,194,132),labels[i],icons[i],i==0))
            { if(i==0)popup=null;else popup=labels[i]; }
            StageDock();
        }
        void LobbyLayout()
        {
            DrawLobbyCombat();
            bool auto=game.AutoCombat;
            var toggle=new Rect(16,143,272,76);
            Frame(toggle,auto);
            MainImage("battle",new Rect(23,151,60,60));
            Text(87,148,192,34,auto?"자동 전투 중":"수동 전투",26,gold);
            Text(87,184,192,25,auto?"터치로 추가 공격":"전장을 터치해 공격",15,ivory);
            bool enabled=GUI.enabled;GUI.enabled=enabled&&game.Connected&&!game.CombatToggleBusy;
            if(GUI.Button(toggle,GUIContent.none,GUIStyle.none))game.ToggleCombat();
            GUI.enabled=enabled;
            string[] left={"퀘스트","업적","이벤트"};
            for(int i=0;i<3;i++)if(Tile(new Rect(16,229+i*90,89,84),left[i],new[]{"▤","★","◇"}[i]))popup=left[i];
            GrowthDrawer();
            Mission(new Rect(14,504,350,119));
            CombatVitals();
            OptionalCards();
        }

        void HandleBattleTouch()
        {
            if(!GUI.enabled || !game.Connected || !game.EnteredGame || popup!=null)return;
            var account=GetComponent<AccountPanel>();var ranking=GetComponent<RankingPanel>();
            if((account!=null&&account.IsOpen)||(ranking!=null&&ranking.IsOpen))return;
            var e=Event.current;
            if(e.type!=EventType.MouseDown || e.button!=0)return;
            var p=e.mousePosition;
            // Exclude all overlay panels, including their non-button areas.
            if(p.y<135 || p.y>=800 || p.x<0 || p.x>=1672)return;
            if(new Rect(0,135,300,88).Contains(p)||new Rect(0,223,112,277).Contains(p))return;
            if(new Rect(0,500,369,130).Contains(p)||new Rect(0,636,496,164).Contains(p))return;
            if(new Rect(618,760,436,40).Contains(p))return;
            float drawerX=1520+drawerOffset;
            if(new Rect(drawerX,86,145,610).Contains(p)||new Rect(Mathf.Min(1628,drawerX-45),370,40,85).Contains(p))return;
            if(!hideRewards&&new Rect(956,648,293,143).Contains(p))return;
            if(!hideSummon&&new Rect(1270,649,240,139).Contains(p))return;
            game.TapAttack();e.Use();
        }
        void OnGUI()
        {
            if(game==null)return;
            GUI.depth=0;
            if(fortress==null)fortress=MainTexture("bg")??Resources.Load<Texture2D>("Art/Lobby/fortress-v1");
            if(lobbyHero==null)lobbyHero=MainTexture("hero")??Resources.Load<Texture2D>("Art/Lobby/chieftain-v1");
            var safe=Screen.safeArea;
            float k=Mathf.Min(safe.width/1672f,safe.height/941f);
            GUI.matrix=Matrix4x4.TRS(new Vector3(safe.x+(safe.width-1672*k)/2,Screen.height-safe.yMax+(safe.height-941*k)/2,0),Quaternion.identity,Vector3.one*k);
            if(!game.EnteredGame) { DrawEntry(); GUI.matrix=Matrix4x4.identity;return; }

            Box(0,0,1672,941,ink);
            DrawTrackBackground();
            GUI.enabled=popup==null;
            Header();LobbyLayout();Navigation();DrawHuntFeedback();
            HandleBattleTouch();
            GUI.enabled=true;
            if(popup=="소환") {DrawSummonPanel();GUI.matrix=Matrix4x4.identity;return;}
            if(popup=="장비" || popup=="무기" || popup=="방어구") { DrawEquipmentPanel();GUI.matrix=Matrix4x4.identity;return; }
            if(popup=="성장") { DrawGrowthPanel(); GUI.matrix=Matrix4x4.identity;return; }
            if(popup!=null) {
                Box(0,0,1672,941,new Color(0,0,0,.7f));Frame(new Rect(536,275,600,385));
                Text(560,292,552,45,popup,30,gold);
                if(popup=="설정") {
                    if(HomeButton(new Rect(575,354,240,56),"계정 / 로그인","♙")){popup=null;GetComponent<AccountPanel>().Open();}
                    if(HomeButton(new Rect(847,354,240,56),"랭킹","★")){popup=null;GetComponent<RankingPanel>().Open();}
                    if(HomeButton(new Rect(575,435,512,52),"서버 다시 연결","↻"))game.Retry();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    GUI.enabled=game.CanResetProgress;
                    if(HomeButton(new Rect(575,503,512,52),"[개발] 계정 진행 초기화 · Lv.1","↺")){game.ResetProgress();popup=null;}
                    GUI.enabled=true;
#endif
                } else if(popup=="성장") {
                    Text(560,352,552,45,GrowthLabel+" · "+GrowthHint,22,ivory);
                    if(HomeButton(new Rect(575,427,512,57),game.Current==null?"연결 대기":$"힘 강화 · {game.Current.upgradeCost:N0} G","↑")){if(game.CanUpgrade)game.Upgrade();}
                } else Text(564,359,544,135,popup=="방치 보상"?"접속 시 서버에서 자동 정산됩니다.\n상세 보상 내역은 준비 중입니다.":"아직 구현되지 않은 기능입니다.",23,ivory,TextAnchor.MiddleCenter);
                if(HomeButton(new Rect(729,574,213,55),"닫기","×"))popup=null;
            }
            GUI.matrix=Matrix4x4.identity;
        }
    }
}


