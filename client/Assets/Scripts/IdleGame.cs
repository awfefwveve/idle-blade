using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace IdleBlade
{
    // First vertical slice: IMGUI graybox, replace with production UI and sprites.
    public sealed partial class IdleGame : MonoBehaviour
    {
        [Serializable] public class Monster { public int id,sector; public float x; public double hp,maxHp; }
        [Serializable] public class CombatEvent { public int seq,id,sector; public float x; public double damage,tick; public bool dead; }
        [Serializable] public class State { public Monster[] monsters; public CombatEvent[] combatEvents; public int combatSeq; public int stage, kills, level, revision, rage, highestCleared; public float bossElapsed; public SummonReceipt lastSummon; public double gems,relicShards,ore; public double gold, onlineKills, onlineGold, enemyHp, bossHp, bossMaxHp, heroHp, heroMaxHp; public bool autoCombat=true, bossActive; public string bossResult; }
        [Serializable] public class GrowthRow { public string id,name; public int level,cap,unlockStage,tenCount,maxCount; public bool unlocked; public double value,nextValue,nextCost,tenCost,maxCost; }
        [Serializable] public class EquipmentRow { public string id,name,slot; public int grade,duplicates,reserved,enhance,oreCost,salvage,range,targets,interval; public bool owned,equipped,canMerge; public double goldCost,attackAfter,hpAfter; }
        [Serializable] public class MergeLine { public string id,name,result; public int count,consume; }
        [Serializable] public class SummonQuote { public string banner,version; public int count,free,tickets,freeGems,paidGems,pity; public bool available; }
        [Serializable] public class SummonResult { public string id,name;public int grade;public bool fresh; }
        [Serializable] public class SummonReceipt { public string requestId,banner;public int before,after;public SummonResult[] results; }
        [Serializable] public class Snapshot { public SummonQuote[] summonQuotes; public EquipmentRow[] equipmentRows; public MergeLine[] mergePlan; public int armorReduction; public GrowthRow[] growthRows; public string regionName; public float attackInterval; public State state; public double damage, upgradeCost, enemyMaxHp; public int maxLevel, requiredKills, rageRemaining; public bool rageActive; }
        [Serializable] class Session { public string token, refresh_token; }
        [Serializable] class RefreshRequest { public string token; }
        [Serializable] class Device { public string id; }
        [Serializable] class Request { public string action,stat,item,banner,version,requestId; public int free,tickets,freeGems,paidGems; public bool confirm; public int revision,count=1; }
        [Serializable] class RpcEnvelope { public string payload; }
        public string endpoint = "http://127.0.0.1:7350";
        string token, status = "Connecting to local server...";
        string refreshToken;
        Snapshot snapshot;
        bool busy;
        bool online;
        int syncFailures;
        bool paused, resumePending;
        float nextSync;
        bool tapPending;
        bool? autoPending;
        float nextTap;
        public float ManualSwingUntil { get; private set; }
        public bool AutoCombat => snapshot!=null && snapshot.state.autoCombat;
        public bool CombatToggleBusy => autoPending.HasValue || busy;
        public void ToggleCombat() { if(Connected && !CombatToggleBusy)autoPending=!AutoCombat; }
        public void TapAttack() {
            if(!Connected || !EnteredGame || Time.unscaledTime<nextTap)return;
            tapPending=true;nextTap=Time.unscaledTime+.25f;ManualSwingUntil=Time.unscaledTime+.22f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<IdleGame>() == null)
                new GameObject("IdleBlade").AddComponent<IdleGame>();
        }
        void Start()
        {
            if (Camera.main == null)
            {
                var camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.06f, .08f, .1f);
            }
            gameObject.AddComponent<BarbarianLobby>();
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            gameObject.AddComponent<AccountPanel>();
            gameObject.AddComponent<RankingPanel>();
            // Authentication starts only after a choice on the title/login screen.
        }
        IEnumerator Login()
        {
            busy = true;
            token = null; refreshToken=null; snapshot = null; online=false;
            AutoBoss=false;tapPending=false;autoPending=null;
            ClearRankings();
            var id = PlayerPrefs.GetString("idleblade.device", "");
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString("idleblade.device", id);
                PlayerPrefs.Save();
            }
            var auth = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("localdevkey:"));
            using (var req = Post(endpoint + "/v2/account/authenticate/device?create=true", JsonUtility.ToJson(new Device { id = id }), auth))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    try { var session=JsonUtility.FromJson<Session>(req.downloadHandler.text); token=session.token; refreshToken=session.refresh_token; }
                    catch (Exception) { token = null; }
                }
                status = string.IsNullOrEmpty(token) ? "서버에 연결하지 못했습니다. 다시 시도하세요." : "저장 데이터를 불러오는 중…";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if(req.result==UnityWebRequest.Result.ConnectionError &&
                    System.Uri.TryCreate(endpoint,System.UriKind.Absolute,out var serverUri) && serverUri.IsLoopback)
                    status="로컬 서버에 연결할 수 없습니다. Docker Desktop과 개발 서버 실행을 확인해 주세요.";
#endif
                AccountMessage=status;
            }
            busy = false;
            if (!string.IsNullOrEmpty(token)) yield return Command("resume");
        }
        static UnityWebRequest Post(string url, string body, string auth)
        {
            var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", auth);
            req.timeout = 10;
            return req;
        }
        IEnumerator Command(string action,string stat=null,int count=1,string item=null,bool confirm=false,SummonQuote summon=null,string requestId=null)
        {
            busy = true;
            var body = JsonUtility.ToJson(new Request { action = action, stat=stat, count=count,item=item,confirm=confirm,banner=summon?.banner,version=summon?.version,requestId=requestId,free=summon?.free ?? 0,tickets=summon?.tickets ?? 0,freeGems=summon?.freeGems ?? 0,paidGems=summon?.paidGems ?? 0, revision = snapshot == null ? 0 : snapshot.state.revision });
            // Nakama HTTP RPC body is a JSON-encoded string, response contains payload.
            var encoded = "\"" + body.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
            using (var req = Post(endpoint + "/v2/rpc/game", encoded, "Bearer " + token))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var envelope = JsonUtility.FromJson<RpcEnvelope>(req.downloadHandler.text);
                        var received = JsonUtility.FromJson<Snapshot>(envelope.payload);
                        if (received == null || received.state == null) throw new Exception();
                        if(snapshot!=null && snapshot.state.bossActive && !received.state.bossActive && received.state.bossResult!="victory")AutoBoss=false;
                        snapshot = received;
                        if(action=="dev_reset") {
                            AutoBoss=false;tapPending=false;autoPending=null;ManualSwingUntil=0;
                            ClearRankings();GetComponent<BarbarianLobby>()?.ResetCombatPresentation();
                        }
                        online = true;
                        syncFailures = 0;
                        status = "AUTO HUNT  /  server saved";
                    }
                    catch (Exception) { online=false; status = "Invalid server response; reconnecting on next sync."; }
                }
                else
                {
                    status = "Sync failed. Progress will reconcile on reconnect.";
                    online = false;
                    if (req.responseCode == 401) { token = null; ClearRankings(); AccountMessage="로그인이 만료되었습니다. 계정 메뉴에서 다시 로그인하세요."; }
                }
            }
            if(!online) syncFailures=Mathf.Min(syncFailures+1,4);
            nextSync = Time.unscaledTime + (online?2:Mathf.Min(30,2*Mathf.Pow(2,syncFailures)));
            if(string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(refreshToken))nextSync=Time.unscaledTime;
            busy = false;
        }
        public Snapshot Current => snapshot;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool CanResetProgress => !busy && Connected;
        public void ResetProgress() {
            if(!CanResetProgress)return;
            AutoBoss=false;tapPending=false;autoPending=null;
            StartCoroutine(Command("dev_reset"));
        }
#endif
        public bool EnteredGame { get; private set; }
        public void EnterLobby() { if(Connected)EnteredGame=true; }
        public bool AutoBoss { get; private set; }
        public void ToggleAutoBoss() { if(snapshot!=null && snapshot.state.highestCleared>0)AutoBoss=!AutoBoss; }
        IEnumerator RefreshSession()
        {
            busy=true;
            var auth="Basic "+Convert.ToBase64String(Encoding.UTF8.GetBytes("localdevkey:"));
            using(var req=Post(endpoint+"/v2/account/session/refresh",JsonUtility.ToJson(new RefreshRequest{token=refreshToken}),auth))
            {
                yield return req.SendWebRequest();
                if(req.result==UnityWebRequest.Result.Success)
                {
                    try { var session=JsonUtility.FromJson<Session>(req.downloadHandler.text); token=session.token; if(!string.IsNullOrEmpty(session.refresh_token))refreshToken=session.refresh_token; AccountMessage="로그인 세션을 갱신했습니다."; }
                    catch(Exception) { refreshToken=null; }
                }
                else if(req.responseCode==401 || req.responseCode==403)refreshToken=null;
            }
            nextSync=Time.unscaledTime+(string.IsNullOrEmpty(token)?5:0); busy=false;
        }
        public string ConnectionStatus => status;
        public bool CanUpgrade => !busy && Connected && !snapshot.state.bossActive && snapshot.state.gold >= snapshot.upgradeCost && snapshot.state.level < snapshot.maxLevel;
        public bool CanChallenge => !busy && Connected && !snapshot.state.bossActive && snapshot.state.stage > snapshot.state.highestCleared && snapshot.state.kills >= snapshot.requiredKills;
        public void Challenge() { if(CanChallenge) StartCoroutine(Command("boss")); }
        void OnApplicationPause(bool value) { paused=value; if(!value) resumePending=true; }
        public bool Connected => online && !string.IsNullOrEmpty(token) && snapshot != null;
        public void EquipmentAction(string action,string item=null,int count=1,bool confirm=false){if(CanGrow)StartCoroutine(Command(action,null,count,item,confirm));}
        public bool CanSummon => !busy && Connected;
        public void Summon(SummonQuote quote){if(CanSummon&&quote.available)StartCoroutine(Command("summon",null,quote.count,null,false,quote,Guid.NewGuid().ToString("N")));}
        public bool CanGrow => !busy && Connected && !snapshot.state.bossActive;
        public void UpgradeStat(string id,int count){if(CanGrow)StartCoroutine(Command("upgrade",id,count));}
        public void Upgrade() { if (CanUpgrade) StartCoroutine(Command("upgrade")); }
        public void Retry() { if (!busy && PlayerPrefs.GetInt("idleblade.emailLogin",0)==0) StartCoroutine(Login()); }
        void Update()
        {
            if(!Connected){tapPending=false;autoPending=null;}
            if(!paused && !busy && Connected && autoPending.HasValue) {
                var action=autoPending.Value?"auto_on":"auto_off";autoPending=null;StartCoroutine(Command(action));return;
            }
            if(!paused && !busy && Connected && tapPending) {tapPending=false;StartCoroutine(Command("tap"));return;}
            if(!paused && AutoBoss && CanChallenge) { Challenge(); return; }
            if(!paused && !busy && string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(refreshToken) && Time.unscaledTime>=nextSync)
            { StartCoroutine(RefreshSession()); return; }
            if (!paused && !busy && !string.IsNullOrEmpty(token) && (resumePending || Time.unscaledTime >= nextSync))
            { var action=resumePending?"resume":"sync"; resumePending=false; StartCoroutine(Command(action)); }
        }
    }
}





