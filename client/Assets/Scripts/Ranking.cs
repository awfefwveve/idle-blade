using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace IdleBlade
{
    public sealed partial class IdleGame
    {
        [Serializable] public class RankEntry { public int rank, stage; public string name; }
        [Serializable] public class RankPage { public RankEntry[] records, mine; }
        public RankPage Rankings { get; private set; }
        public string RankingMessage { get; private set; } = "랭킹을 불러오세요.";
        bool rankingBusy;
        public bool RankingBusy => rankingBusy;
        public void LoadRankings() { if(!rankingBusy) StartCoroutine(ReadRankings()); }
        public void ClearRankings() { Rankings=null; RankingMessage="계정이 변경되었습니다. 새로고침하세요."; }
        IEnumerator ReadRankings()
        {
            if(string.IsNullOrEmpty(token)) { RankingMessage="먼저 로그인하세요."; yield break; }
            rankingBusy=true; RankingMessage="불러오는 중…";
            string requestedToken=token;
            using(var req=Post(endpoint+"/v2/rpc/ranking","\"{}\"","Bearer "+requestedToken))
            {
                yield return req.SendWebRequest();
                if(requestedToken==token)
                {
                    Rankings=null;
                    if(req.result==UnityWebRequest.Result.Success)
                    {
                        try { Rankings=JsonUtility.FromJson<RankPage>(JsonUtility.FromJson<RpcEnvelope>(req.downloadHandler.text).payload); }
                        catch(Exception) { }
                    }
                    RankingMessage=Rankings==null?"불러오기 실패. 다시 시도하세요.":"최고 도달 스테이지 · 상위 20명 (동점은 서버 순서)";
                }
            }
            rankingBusy=false;
        }
    }
    public sealed class RankingPanel : MonoBehaviour
    {
        IdleGame game;
        bool open;
        public bool IsOpen => open;
        Vector2 scroll;
        void Awake() { game=GetComponent<IdleGame>(); }
        public void Open() { open=true; game.LoadRankings(); }
        void OnGUI()
        {
            GUI.depth=-90; GUI.matrix=Matrix4x4.identity;

            if(!open) return;
            float k=Mathf.Min(1f,Mathf.Min(Screen.width/560f,Screen.height/510f));
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-540*k)/2,(Screen.height-490*k)/2,0),Quaternion.identity,Vector3.one*k);
            GUI.ModalWindow(742,new Rect(0,0,540,490),Draw,"스테이지 랭킹");
            GUI.matrix=Matrix4x4.identity;
        }
        void Draw(int id)
        {
            GUI.Label(new Rect(20,30,500,40),game.RankingMessage);
            var page=game.Rankings;
            var mine=page!=null && page.mine!=null && page.mine.Length>0?page.mine[0]:null;
            GUI.Label(new Rect(20,70,500,30),mine==null?"내 순위: 아직 기록 없음":$"내 순위: {mine.rank}위  /  스테이지 {mine.stage}");
            int count=page==null||page.records==null?0:page.records.Length;
            scroll=GUI.BeginScrollView(new Rect(20,110,500,305),scroll,new Rect(0,0,475,Mathf.Max(305,count*30)));
            for(int i=0;i<count;i++) { var r=page.records[i]; GUI.Label(new Rect(5,i*30,465,28),$"{r.rank}위     {r.name}     스테이지 {r.stage}"); }
            GUI.EndScrollView();
            GUI.enabled=!game.RankingBusy;
            if(GUI.Button(new Rect(70,440,180,32),"새로고침"))game.LoadRankings();
            GUI.enabled=true;
            if(GUI.Button(new Rect(290,440,180,32),"닫기"))open=false;
        }
    }
}

