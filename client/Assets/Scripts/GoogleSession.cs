using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
namespace IdleBlade
{
    public sealed partial class IdleGame
    {
        [Serializable] class GoogleProof { public string token; }
        // Called by the platform sign-in adapter after Google returns a valid token.
        // No platform SDK is installed yet. Never pass a user ID as proof.
        public void CompleteGoogleSignIn(string providerToken,bool linkCurrentAccount)
        {
            if(busy || string.IsNullOrEmpty(providerToken))return;
            if(linkCurrentAccount && !Connected){AccountMessage="먼저 현재 계정에 로그인하세요.";return;}
            if(!Uri.TryCreate(endpoint,UriKind.Absolute,out var uri) || (uri.Scheme!="https"&&!uri.IsLoopback))
            {AccountMessage="소셜 로그인에는 HTTPS 서버가 필요합니다.";return;}
            StartCoroutine(GoogleSessionRequest(providerToken,linkCurrentAccount));
        }
        IEnumerator GoogleSessionRequest(string proof,bool link)
        {
            busy=true;AccountMessage="Google 계정 확인 중…";
            string path=link?"/v2/account/link/google":"/v2/account/authenticate/google?create=true";
            string auth=link?"Bearer "+token:"Basic "+Convert.ToBase64String(Encoding.UTF8.GetBytes("localdevkey:"));
            bool success=false;
            using(var req=Post(endpoint+path,JsonUtility.ToJson(new GoogleProof{token=proof}),auth))
            {
                yield return req.SendWebRequest();
                if(req.result==UnityWebRequest.Result.Success)
                {
                    if(link)success=true;
                    else {
                        try {
                            var session=JsonUtility.FromJson<Session>(req.downloadHandler.text);
                            if(session!=null&&!string.IsNullOrEmpty(session.token)) {
                                token=session.token;refreshToken=session.refresh_token;snapshot=null;online=false;
                                AutoBoss=false;ClearRankings();success=true;
                            }
                        }catch(Exception) { }
                    }
                }
            }
            AccountMessage=success?"Google 계정 확인 완료":"Google 인증에 실패했습니다. 다시 로그인하세요.";
            busy=false;
            if(success)yield return Command(link?"sync":"resume");
        }
    }
}
