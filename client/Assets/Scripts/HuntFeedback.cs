using System.Collections.Generic;
using UnityEngine;
namespace IdleBlade
{
    public sealed partial class BarbarianLobby
    {
        struct CoinFlight { public Vector2 start; public float born; }
        readonly List<CoinFlight> coins=new List<CoinFlight>();
        double seenKills=-1,seenGold;
        float deathUntil,deathX,goldPulse,rewardUntil,clearUntil;
        double rewardAmount;
        int seenClear,seenRevision=-1;
        void UpdateHuntFeedback()
        {
            var s=game.Current;
            if(s==null) {seenKills=-1;seenRevision=-1;coins.Clear();return;}
            if(seenKills<0 || s.state.revision<seenRevision) {
                seenKills=s.state.onlineKills;seenGold=s.state.onlineGold;seenClear=s.state.highestCleared;
            }
            if(s.state.onlineKills>seenKills) {
                deathX=Mathf.Min(enemyX,1000);deathUntil=Time.unscaledTime+.45f;
                enemyX=1500;
                double gain=s.state.onlineGold-seenGold;
                if(gain>0) {
                    rewardAmount=gain;rewardUntil=Time.unscaledTime+1.6f;
                    int count=Mathf.Min(12,(int)System.Math.Min(12,s.state.onlineKills-seenKills)*3);
                    for(int i=0;i<count && coins.Count<48;i++)coins.Add(new CoinFlight{start=new Vector2(deathX+(i%3-1)*28,605-(i%4)*13),born=Time.unscaledTime+i*.045f});
                }
            }
            if(s.state.highestCleared>seenClear)clearUntil=Time.unscaledTime+2.8f;
            seenKills=s.state.onlineKills;seenGold=s.state.onlineGold;seenClear=s.state.highestCleared;seenRevision=s.state.revision;
            for(int i=coins.Count-1;i>=0;i--)if(Time.unscaledTime-coins[i].born>1.15f){coins.RemoveAt(i);goldPulse=Time.unscaledTime+.25f;}
        }
        void DrawHuntFeedback()
        {

            float now=Time.unscaledTime;
            if(now<deathUntil && (game.Current==null || game.Current.state.combatEvents==null)) {
                float t=1-(deathUntil-now)/.45f;
                var saved=GUI.matrix;
                GUI.matrix=saved*Matrix4x4.TRS(new Vector3(deathX+60*t,644+25*t,0),Quaternion.identity,Vector3.one*(1.5f*(1-t*.7f)));
                if(!MainImage("mob",new Rect(-64,-112,128,128)))Fighter(0,0,false,false);GUI.matrix=saved;
                Text(deathX-55,570-40*t,110,40,"처치!",24,gold,TextAnchor.MiddleCenter);
            }
            foreach(var c in coins) {
                float age=now-c.born;if(age<0)continue;
                Vector2 position;
                if(age<.3f)position=c.start+new Vector2(0,-Mathf.Sin(age/.3f*Mathf.PI)*60);
                else {
                    float t=Mathf.Clamp01((age-.3f)/.85f);t=t*t;
                    position=Vector2.Lerp(c.start,new Vector2(600,35),t)+new Vector2(110*Mathf.Sin(t*Mathf.PI),-130*Mathf.Sin(t*Mathf.PI));
                }
                if(!MainImage("gold",new Rect(position.x-13,position.y-13,26,26))) {
                    Disc(new Rect(position.x-9,position.y-9,18,18),gold);
                    Disc(new Rect(position.x-5,position.y-5,10,10),new Color(1,.9f,.4f));
                }
            }
            if(now<rewardUntil)Text(deathX-120,520-(1.6f-(rewardUntil-now))*35,240,45,$"+{rewardAmount:N0} 골드",27,gold,TextAnchor.MiddleCenter);
            if(now<goldPulse) {Box(579,57,204,4,gold);}
            if(now<clearUntil) {
                Frame(new Rect(530,185,550,65),true);
                Text(540,190,530,55,$"STAGE {seenClear} CLEAR · 다음 구역 진입",27,ivory,TextAnchor.MiddleCenter);
            }
        }
    }
}

