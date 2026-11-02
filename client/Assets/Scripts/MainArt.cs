using System.Collections.Generic;
using UnityEngine;
namespace IdleBlade
{
    public sealed partial class BarbarianLobby
    {
        readonly Dictionary<string,Texture2D> mainTextures=new Dictionary<string,Texture2D>();
        Texture2D MainTexture(string id)
        {
            if(!mainTextures.TryGetValue(id,out var texture)) {
                texture=Resources.Load<Texture2D>("Art/Main/"+id+"-v1");
                mainTextures[id]=texture;
            }
            return texture;
        }
        bool MainImage(string id,Rect rect)
        {
            var texture=MainTexture(id);if(texture==null)return false;
            GUI.DrawTexture(rect,texture,ScaleMode.ScaleToFit,true);return true;
        }
        string MenuArt(string label)
        {
            switch(label) {
                case "전투":return "battle";case "각성":return "awakening";
                case "던전":return "dungeon";case "상점":return "shop";
                case "소환":case "장비 소환":return "summon";case "패스":return "pass";
                case "장비":return "equipment";case "무기":return "axe";
                case "방어구":return "armor";case "스킬":return "skill";
                case "분노":return "rage";case "승급":return "promotion";
                case "유물":return "relic";case "도감":return "collection";
                case "퀘스트":return "quest";case "우편함":return "mail";
                case "업적":return "achievement";case "이벤트":return "event";
                case "칭호":return "title";case "공지":return "notice";
                case "설정":return "settings";default:return null;
            }
        }
        bool MenuImage(string label,Rect rect)
        {var id=MenuArt(label);return id!=null&&MainImage(id,rect);}
        bool ArtPanel(Rect rect,bool accent)
        {
            var texture=MainTexture("panel");if(texture==null)return false;
            float edge=Mathf.Min(18,Mathf.Min(rect.width,rect.height)/3);
            float[] dx={rect.x,rect.x+edge,rect.xMax-edge,rect.xMax};
            float[] dy={rect.y,rect.y+edge,rect.yMax-edge,rect.yMax};
            float[] ux={0,.12f,.88f,1},uy={1,.82f,.18f,0};
            for(int row=0;row<3;row++)for(int col=0;col<3;col++)
                GUI.DrawTextureWithTexCoords(new Rect(dx[col],dy[row],dx[col+1]-dx[col],dy[row+1]-dy[row]),texture,
                    new Rect(ux[col],uy[row+1],ux[col+1]-ux[col],uy[row]-uy[row+1]));
            if(accent) {
                Box(rect.x+edge,rect.y+edge,rect.width-edge*2,rect.height-edge*2,new Color(.65f,.19f,.035f,.27f));
                Box(rect.x+edge,rect.yMax-6,rect.width-edge*2,3,gold);
            }
            return true;
        }
        void MainHero(float footX,float footY,bool running,bool swing)
        {
            float height=350*(1+GrowthTier*.045f)*(Raging?1.12f:1);
            if(DrawHeroRig(footX,footY,height,running,swing))return;
            float bob=running?Mathf.Sin(runClock*17)*5:Mathf.Sin(Time.unscaledTime*3)*1.5f;
            Rect body=new Rect(footX-height*.5f,footY-height*.96f+bob,height,height);
            MainImage("hero",body);
            // Base-pose socket in the generated source; animation sheets will supply per-frame sockets.
            Vector2 hand=new Vector2(body.x+height*.785f,body.y+height*.48f);
            var saved=GUI.matrix;
            GUIUtility.RotateAroundPivot(swing?45:-16,hand);
            MainImage("axe",new Rect(hand.x-height*.16f,hand.y-height*.43f,height*.32f,height*.58f));
            GUI.matrix=saved;
        }
#if UNITY_EDITOR
        // Read-only art inspection, separate from the player's scene and session.
        public void RenderArtPreview(float width,float height)
        {
            if(Application.isPlaying)return;
            game=GetComponent<IdleGame>();
            if(font==null)font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},24);
            fortress=MainTexture("bg");lobbyHero=MainTexture("hero");
            var saved=GUI.matrix;
            float scale=Mathf.Min(width/1672,height/941);
            GUI.matrix=Matrix4x4.TRS(new Vector3((width-1672*scale)/2,(height-941*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            Box(0,0,1672,941,ink);DrawTrackBackground(false);
            UpdateHordeVisuals();
            GUI.enabled=false;Header();LobbyLayout();Navigation();GUI.enabled=true;
            Box(490,110,690,38,new Color(0,0,0,.8f));
            Text(500,110,670,38,"에셋 미리보기 · 예시 데이터 · 저장/서버 연결 없음",22,gold,TextAnchor.MiddleCenter);
            GUI.matrix=saved;
        }
#endif
    }
}
