using UnityEngine;

namespace IdleBlade
{
    // Presentation only: all currency, upgrades and progression come from IdleGame.
    public sealed partial class BarbarianLobby : MonoBehaviour
    {
        IdleGame game;
        ArtTheme art;
        Font font;
        int lastStage;
        float stageUntil, hitUntil, nextSwing;
        double previousHp = -1;
        readonly string[] tabs = { "성장", "장비", "스킬", "유물", "던전", "상점" };
        readonly Color ink = new Color(.055f,.068f,.083f), panel = new Color(.10f,.12f,.14f), gold = new Color(.96f,.72f,.32f);
        void Awake()
        {
            game = GetComponent<IdleGame>();
            art = Resources.Load<ArtTheme>("Art/DefaultTheme");
            font = art != null && art.uiFont != null ? art.uiFont : Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"}, 24);
        }
        void Update()
        {
            UpdateLobbyCombat();
            var s = game.Current;
            if (s == null) return;
            if (lastStage != 0 && lastStage != s.state.stage) stageUntil = Time.unscaledTime + 3;
            double targetHp=s.state.bossActive?s.state.bossHp:s.state.enemyHp;
            if (previousHp >= 0 && previousHp != targetHp) hitUntil = Time.unscaledTime + .3f;
            previousHp = targetHp; lastStage = s.state.stage;
            if (game.Connected && game.AutoCombat && Time.unscaledTime > nextSwing) nextSwing = Time.unscaledTime + Mathf.Max(.05f,s.attackInterval);
        }
        void Box(float x,float y,float w,float h,Color c)
        { GUI.color=c; GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture); GUI.color=Color.white; }
        void Text(float x,float y,float w,float h,string value,int size,Color c,TextAnchor align=TextAnchor.MiddleLeft)
        { GUI.Label(new Rect(x,y,w,h),value,new GUIStyle(GUI.skin.label){font=font,fontSize=size,normal={textColor=c},alignment=align}); }
        bool Button(Rect r,string text,bool enabled,bool active=false)
        {
            Frame(r,active);
            Text(r.x,r.y,r.width,r.height,text,20,enabled?Color.white:Color.gray,TextAnchor.MiddleCenter);
            bool old=GUI.enabled; GUI.enabled=old&&enabled;
            bool clicked=GUI.Button(r,GUIContent.none,GUIStyle.none); GUI.enabled=old; return clicked;
        }
        void Strip(Texture2D texture,float footX,float footY,float scale,bool flip)
        {
            int count=Mathf.Max(1,texture.width/128);
            int frame=(int)(Time.unscaledTime*(art == null ? 8 : art.framesPerSecond))%count;
            GUI.DrawTextureWithTexCoords(new Rect(footX-64*scale,footY-112*scale,128*scale,128*scale),texture,new Rect((float)(flip?frame+1:frame)/count,0,(flip?-1f:1f)/count,1));
        }
        // Visual growth only; combat values remain server-authoritative.
        int GrowthTier => Mathf.Clamp(((game.Current == null ? 1 : game.Current.state.level)-1)/10,0,5);
        bool Raging => game.Connected && game.Current.rageActive;
        float GrowthScale => (1f + GrowthTier * .07f)*(Raging?1.15f:1f);
        string GrowthLabel => "체격 " + (GrowthTier+1) + "단계";
        string GrowthHint => GrowthTier >= 5 ? "현재 체격 최대 단계" : $"힘 Lv. {(GrowthTier+1)*10+1}에 체격 성장";
        void Fighter(float x,float ground,bool hero,bool attacking)
        {
            var saved=GUI.matrix;
            if(hero) GUI.matrix=saved*Matrix4x4.TRS(new Vector3(x,ground,0),Quaternion.identity,Vector3.one*GrowthScale)*Matrix4x4.Translate(new Vector3(-x,-ground,0));
            DrawFighter(x,ground,hero,attacking);
            GUI.matrix=saved;
        }
        void DrawFighter(float x,float ground,bool hero,bool attacking)
        {
            Texture2D sprite = art == null ? null : hero ? (attacking && art.heroAttack != null ? art.heroAttack:art.heroIdle) : (Time.unscaledTime<hitUntil && art.enemyHit!=null?art.enemyHit:art.enemyIdle);
            Box(x-48,ground-6,96,10,new Color(0,0,0,.25f));
            if(sprite!=null && sprite.height==128 && sprite.width%128==0){Strip(sprite,x,ground,1.35f,!hero);return;}
            // Deliberately code-drawn placeholder pixel silhouettes, same foot anchor as final strips.
            float bob=game.Connected?Mathf.Round(Mathf.Sin(Time.unscaledTime*6)*2):0;
            float y=ground+bob;
            if(hero)
            {
                Box(x-28,y-32,20,32,new Color(.20f,.14f,.12f)); Box(x+8,y-32,20,32,new Color(.20f,.14f,.12f));
                Box(x-37,y-92,74,64,new Color(.69f,.40f,.26f)); Box(x-45,y-91,18,30,new Color(.84f,.59f,.36f));
                Box(x-22,y-129,44,38,new Color(.88f,.65f,.41f)); Box(x-25,y-138,50,18,new Color(.34f,.20f,.13f));
                Box(x-20,y-106,40,20,new Color(.38f,.22f,.13f)); Box(x+3,y-119,9,5,Color.white);
                Box(x-37,y-42,74,12,gold); Box(x+25,y-91,20,30,new Color(.84f,.59f,.36f));
                float ax=x+52+(attacking?30:0); Box(ax,y-114,8,90,new Color(.46f,.31f,.18f));
                Box(ax-16,y-120,43,30,new Color(.68f,.76f,.76f)); Box(ax+17,y-126,17,42,new Color(.84f,.9f,.87f));
            }
            else
            {
                Color skin=Time.unscaledTime<hitUntil?new Color(1,.77f,.6f):new Color(.45f,.56f,.29f);
                Box(x-27,y-31,18,31,ink); Box(x+9,y-31,18,31,ink);
                Box(x-34,y-74,68,50,new Color(.28f,.31f,.19f)); Box(x-30,y-111,60,43,skin);
                Box(x-44,y-103,16,17,skin); Box(x+28,y-103,16,17,skin);
                Box(x-20,y-96,12,5,new Color(1,.38f,.18f)); Box(x+9,y-96,12,5,new Color(1,.38f,.18f));
                Box(x-46,y-68,12,56,new Color(.48f,.34f,.23f));
            }
        }
        void Landscape()
        {
            if(art!=null && art.battlefield!=null){GUI.DrawTexture(new Rect(0,112,1280,432),art.battlefield);return;}
            Box(0,112,1280,432,new Color(.17f,.24f,.27f));
            for(int i=0;i<18;i++)
            {
                float h=65+(i*73%135); Box(i*80,310-h,100,h,new Color(.23f,.31f,.33f));
                Box(i*80+20,310-h-25,60,30,new Color(.23f,.31f,.33f));
            }
            Box(0,325,1280,219,new Color(.20f,.26f,.23f));
            for(int i=0;i<11;i++)
            {
                float x=i*137-20; Box(x+24,250,17,153,new Color(.13f,.19f,.19f));
                Box(x,240,65,65,new Color(.12f,.21f,.20f)); Box(x+10,210,45,60,new Color(.12f,.21f,.20f));
            }
            Box(0,448,1280,96,new Color(.32f,.29f,.23f)); Box(0,445,1280,7,new Color(.45f,.42f,.28f));
            for(int i=0;i<42;i++) Box((i*167)%1280,465+(i*29)%64,12+(i%3)*9,4,new Color(.39f,.35f,.26f));
        }
        string BossResult(string result)
        {
            switch(result) {
                case "victory": return "보스 처치! 다음 구역 해금";
                case "defeat": return "패배 · 강화 후 재도전 가능";
                case "timeout": return "시간 초과 · 도전 자격 유지";
                case "disconnected": return "연결 중단 · 도전 자격 유지";
                default: return "처치 수를 채워 보스에 도전하세요";
            }
        }
    }
}




