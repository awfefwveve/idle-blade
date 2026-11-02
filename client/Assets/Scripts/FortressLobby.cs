using UnityEngine;

namespace IdleBlade
{
    public sealed partial class BarbarianLobby
    {

        string popup;
        Texture2D fortress, lobbyHero;
        readonly Color bronze=new Color(.54f,.38f,.22f), ivory=new Color(.94f,.87f,.72f);
        void Frame(Rect r, bool red=false)
        {
            if(ArtPanel(r,red))return;
            Box(r.x-3,r.y-3,r.width+6,r.height+6,new Color(.09f,.07f,.06f));
            Box(r.x-1,r.y-1,r.width+2,r.height+2,bronze);
            Box(r.x+2,r.y+2,r.width-4,r.height-4,red?new Color(.30f,.045f,.035f,.97f):new Color(.075f,.062f,.057f,.95f));
            Box(r.x+5,r.y+5,r.width-10,2,new Color(.70f,.51f,.30f));
            foreach(float x in new[]{r.x+6,r.x+r.width-10}) foreach(float y in new[]{r.y+9,r.y+r.height-13})
            {Box(x,y,5,5,new Color(.78f,.61f,.38f));}
        }
        bool HomeButton(Rect r,string name,string icon,bool active=false)
        {
            Frame(r,active);
            if(!MenuImage(name,new Rect(r.x+8,r.y+8,46,r.height-16)))Text(r.x+8,r.y+8,46,r.height-16,icon,30,gold,TextAnchor.MiddleCenter);
            Text(r.x+57,r.y+4,r.width-62,r.height-8,name,23,ivory,TextAnchor.MiddleCenter);
            return GUI.Button(r,GUIContent.none,GUIStyle.none);
        }
    }
}

