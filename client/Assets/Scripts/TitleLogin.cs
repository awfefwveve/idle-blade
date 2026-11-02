using UnityEngine;
namespace IdleBlade
{
    public sealed partial class BarbarianLobby
    {
        int entryPage;
        string loginEmail="",loginPassword="",entryNotice="";
        bool entryAttempt;
        bool EntryButton(Rect r,string label,Color fill,bool enabled=true)
        {
            Box(r.x-2,r.y-2,r.width+4,r.height+4,bronze);Box(r.x,r.y,r.width,r.height,fill);
            Text(r.x+10,r.y,r.width-20,r.height,label,27,enabled?Color.white:Color.gray,TextAnchor.MiddleCenter);
            bool before=GUI.enabled;GUI.enabled=before&&enabled;
            bool clicked=GUI.Button(r,GUIContent.none,GUIStyle.none);GUI.enabled=before;return clicked;
        }
        void DrawEntry()
        {
            if(game.Connected && entryAttempt) {game.EnterLobby();entryAttempt=false;loginPassword="";return;}
            Box(0,0,1672,941,ink);
            if(fortress!=null)GUI.DrawTexture(new Rect(0,0,1672,941),fortress,ScaleMode.ScaleAndCrop);
            Box(0,0,1672,941,new Color(.025f,.015f,.01f,.68f));
            if(lobbyHero!=null)GUI.DrawTexture(new Rect(65,220,650,650),lobbyHero,ScaleMode.ScaleToFit);
            Text(750,105,780,95,"바바리안 키우기",68,gold,TextAnchor.MiddleCenter);
            Text(775,206,730,45,"부족의 불꽃을 되찾을 시간",26,ivory,TextAnchor.MiddleCenter);
            Text(32,893,450,28,"개발 중 · 로컬 테스트 서버",18,ivory);
            if(entryPage==0) {
                Text(790,390,710,75,"작은 전사에서 위대한 선조로",31,ivory,TextAnchor.MiddleCenter);
                if(EntryButton(new Rect(865,555,570,88),"게임 시작",new Color(.4f,.09f,.035f)))entryPage=1;
                Text(800,675,700,42,"자동사냥 · 성장 · 보스 돌파",23,gold,TextAnchor.MiddleCenter);
                return;
            }
            Frame(new Rect(777,280,734,554));
            Text(805,292,678,49,entryPage==2?"이메일 로그인":"로그인 방법 선택",32,ivory,TextAnchor.MiddleCenter);
            bool ready=!game.AccountBusy;
            if(entryPage==1) {
                if(EntryButton(new Rect(823,365,643,63),"G   Google로 계속하기",new Color(.23f,.25f,.28f),ready))entryNotice="Google 로그인 앱 등록 및 인증 SDK 설정이 필요합니다.";
                if(EntryButton(new Rect(823,445,643,63),"N   네이버로 계속하기",new Color(.02f,.47f,.23f),ready))entryNotice="네이버 로그인 앱 등록 및 서버 인증 설정이 필요합니다.";
                if(EntryButton(new Rect(823,525,643,63),"이메일로 로그인",panel,ready)){entryPage=2;entryNotice="";}
                if(EntryButton(new Rect(823,605,643,63),"게스트로 시작 / 이어하기",new Color(.35f,.16f,.045f),ready)) {game.GuestLogin();entryAttempt=true;entryNotice="";}
                Text(825,678,639,50,"게스트 진행은 게임 내 설정에서 이메일에 연결할 수 있어요.",20,ivory,TextAnchor.MiddleCenter);
            } else {
                var input=new GUIStyle(GUI.skin.textField){font=font,fontSize=26,padding=new RectOffset(12,12,10,10)};
                Text(825,355,620,33,"이메일",23,ivory);
                loginEmail=GUI.TextField(new Rect(823,395,643,55),loginEmail,254,input);
                Text(825,458,620,33,"비밀번호",23,ivory);
                loginPassword=GUI.PasswordField(new Rect(823,498,643,55),loginPassword,'*',128,input);
                if(EntryButton(new Rect(823,589,643,65),"로그인",new Color(.35f,.16f,.045f),ready)) {game.EmailLogin(loginEmail,loginPassword,false);loginPassword="";entryAttempt=true;entryNotice="";}
                Text(825,669,639,50,"처음이라면 게스트로 시작한 뒤 계정을 연결하세요.",20,ivory,TextAnchor.MiddleCenter);
            }
            string message=game.AccountBusy?"연결 중…":entryNotice;
            if(entryAttempt && ready && !game.Connected)message=game.AccountMessage;
            Text(812,731,665,68,message,21,gold,TextAnchor.MiddleCenter);
            if(EntryButton(new Rect(805,849,190,45),"뒤로",panel,ready)){entryPage=entryPage==2?1:0;entryNotice="";loginPassword="";entryAttempt=false;}
        }
    }
}
