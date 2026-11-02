using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace IdleBlade
{
    public sealed partial class IdleGame
    {
        [Serializable] class EmailCredentials { public string email, password; }
        public bool AccountBusy => busy;
        public string AccountMessage { get; private set; } = "게스트 진행을 이메일에 연결하거나 기존 계정으로 로그인하세요.";
        public void GuestLogin()
        {
            if(busy) return;
            PlayerPrefs.SetInt("idleblade.emailLogin",0); PlayerPrefs.Save();
            AccountMessage="이 기기의 게스트 계정에 접속합니다.";
            StartCoroutine(Login());
        }
        public void EmailLogin(string email,string password,bool link)
        {
            if(busy) return;
            email=email.Trim();
            if(!email.Contains("@") || password.Length<8)
            { AccountMessage="이메일과 8자 이상의 비밀번호를 입력하세요."; return; }
            if(link && !Connected) { AccountMessage="먼저 게스트 계정에 접속하세요."; return; }
            // Plain HTTP is allowed only for the existing local development server.
            if(!Uri.TryCreate(endpoint,UriKind.Absolute,out var uri) || (uri.Scheme!="https" && !uri.IsLoopback))
            { AccountMessage="외부 서버 로그인에는 HTTPS 연결이 필요합니다."; return; }
            StartCoroutine(EmailRequest(email,password,link));
        }
        IEnumerator EmailRequest(string email,string password,bool link)
        {
            busy=true; AccountMessage="계정 확인 중…";
            string auth=link?"Bearer "+token:"Basic "+Convert.ToBase64String(Encoding.UTF8.GetBytes("localdevkey:"));
            string path=link?"/v2/account/link/email":"/v2/account/authenticate/email?create=false";
            bool success=false;
            using(var req=Post(endpoint+path,JsonUtility.ToJson(new EmailCredentials{email=email,password=password}),auth))
            {
                yield return req.SendWebRequest();
                if(req.result==UnityWebRequest.Result.Success)
                {
                    if(link) success=true;
                    else
                    {
                        Session session=null;
                        try { session=JsonUtility.FromJson<Session>(req.downloadHandler.text); } catch(Exception) { }
                        if(session!=null && !string.IsNullOrEmpty(session.token))
                        { token=session.token; refreshToken=session.refresh_token; snapshot=null; AutoBoss=false; ClearRankings(); success=true; }
                    }
                }
                AccountMessage=success?(link?"연결 완료! 현재 진행이 이 이메일 계정에 유지됩니다.":"로그인 완료. 저장된 진행을 불러옵니다."):
                    req.responseCode==409?"이미 연결된 이메일입니다. 기존 계정 로그인을 이용하세요.":
                    req.responseCode==401||req.responseCode==404?"계정 또는 비밀번호를 확인하세요.":"요청에 실패했습니다. 입력 내용과 서버 연결을 확인하세요.";
            }
            if(success)
            {
                // Never persist passwords or session tokens in PlayerPrefs.
                PlayerPrefs.SetInt("idleblade.emailLogin",1); PlayerPrefs.Save();
            }
            busy=false;
            if(success) yield return Command(link?"sync":"resume");
        }
    }

    // Temporary account UI; independent of replaceable lobby artwork.
    public sealed class AccountPanel : MonoBehaviour
    {
        IdleGame game;
        bool open;
        public bool IsOpen => open;
        string email="", password="";
        void Awake() { game=GetComponent<IdleGame>(); open=false; }
        public void Open() { open=true; }
        void OnGUI()
        {
            GUI.depth=-100;
            GUI.matrix=Matrix4x4.identity;

            if(!open) return;
            float scale=Mathf.Min(1f,Mathf.Min(Screen.width/540f,Screen.height/420f));
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-520*scale)/2,(Screen.height-400*scale)/2,0),Quaternion.identity,Vector3.one*scale);
            GUI.ModalWindow(741,new Rect(0,0,520,400),Draw,"계정 연결 / 로그인");
            GUI.matrix=Matrix4x4.identity;
        }
        void Draw(int id)
        {
            GUI.Label(new Rect(20,30,480,45),"현재 진행을 보존하려면 ‘현재 계정에 연결’을 선택하세요.\n기존 계정 로그인은 해당 계정의 진행으로 전환합니다.");
            GUI.Label(new Rect(20,83,90,24),"이메일");
            email=GUI.TextField(new Rect(110,83,390,28),email,254);
            GUI.Label(new Rect(20,123,90,24),"비밀번호");
            password=GUI.PasswordField(new Rect(110,123,390,28),password,'*',128);
            GUI.enabled=!game.AccountBusy;
            if(GUI.Button(new Rect(20,170,230,36),"현재 계정에 연결")) { game.EmailLogin(email,password,true); password=""; }
            if(GUI.Button(new Rect(270,170,230,36),"기존 계정 로그인")) { game.EmailLogin(email,password,false); password=""; }
            if(GUI.Button(new Rect(20,220,480,32),"이 기기의 게스트로 접속")) game.GuestLogin();
            GUI.enabled=true;
            GUI.Label(new Rect(20,265,480,70),game.AccountMessage,new GUIStyle(GUI.skin.label){wordWrap=true});
            if(GUI.Button(new Rect(190,350,140,30),"닫기")) { open=false; password=""; }
        }
    }
}


