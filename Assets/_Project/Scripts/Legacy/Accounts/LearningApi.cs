using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace CalculusLabEscape
{
    [Serializable] public sealed class LearningAccount { public long id; public string username,displayName,role; }
    [Serializable] public sealed class LoginReply { public LearningAccount account; public string token,serverId; }
    [Serializable] public sealed class CredentialsBody { public string username,password; }
    [Serializable] public sealed class SessionList { public List<Chapter4Session> sessions=new List<Chapter4Session>(); }
    [Serializable] public sealed class StudentList { public List<LearningAccount> students=new List<LearningAccount>(); }
    [Serializable] public sealed class SetupReply { public bool needsSetup; }
    [Serializable] public sealed class ErrorReply { public string error; }
    [Serializable] public sealed class LimitsContent { public LimitsQuestion[] questions; }

    public sealed class LearningApi : MonoBehaviour
    {
        static LearningApi instance;
        static string token="";
        static string serverId="signed-out";
        public static LearningAccount Account {get;private set;}
        public static bool Authenticated => Account!=null&&!string.IsNullOrEmpty(token);
        public static bool Student => Authenticated&&Account.role=="Student";
        public static string LastError="",SyncStatus="";
        public static string ContentJson {get;private set;}
        public static bool Busy {get;private set;}
        // Login is deferred until the Firebase integration. Normal play is entirely local.
        // Retain the prior service only for explicitly selected development regression runs.
        public static bool LocalPlay {
            get {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return !Environment.GetCommandLineArgs().Contains("--auth-qa");
#else
                return true;
#endif
            }
        }
        public static bool TestBypass {
            get {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var args=Environment.GetCommandLineArgs();return args.Contains("--canvas-qa")||args.Contains("--chapter4-qa");
#else
                return false;
#endif
            }
        }
        public static string Argument(string key,string fallback=null){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,key);return at>=0&&at+1<args.Length?args[at+1]:fallback;}
        public static string BaseUrl=>Argument("--api-url","http://127.0.0.1:5187").TrimEnd('/');
        public static string CacheRoot=>Argument("--auth-cache",Path.Combine(Application.persistentDataPath,"AccountSessions"));
        public static string CacheFolder=>Path.Combine(CacheRoot,serverId,Account==null?"signed-out":Account.id.ToString());
        public static LearningApi Instance {get{if(instance==null){var go=new GameObject("Learning service connection");DontDestroyOnLoad(go);instance=go.AddComponent<LearningApi>();}return instance;}}
        readonly Dictionary<string,string> pending=new Dictionary<string,string>();
        float nextRetry;
        public bool HasPending=>pending.Count>0||Busy;
        public IEnumerator Request(string method,string path,string body,Action<string> success,Action<string> failure=null,string setupKey=null)
        {
            using(var request=new UnityWebRequest(BaseUrl+path,method))
            {
                request.downloadHandler=new DownloadHandlerBuffer();request.timeout=8;
                if(body!=null){request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));request.SetRequestHeader("Content-Type","application/json");}
                if(!string.IsNullOrEmpty(token))request.SetRequestHeader("Authorization","Bearer "+token);
                if(setupKey!=null)request.SetRequestHeader("X-Setup-Key",setupKey);
                yield return request.SendWebRequest();
                if(request.result!=UnityWebRequest.Result.Success)
                {
                    string message=request.responseCode==0?"Cannot reach the learning service. Start it and try again.":"Request failed ("+request.responseCode+").";
                    try{var error=JsonUtility.FromJson<ErrorReply>(request.downloadHandler.text);if(!string.IsNullOrEmpty(error.error))message=error.error;}catch{}
                    if(request.responseCode==401){token="";message="Your login has expired or the credentials are invalid. Sign in again.";}
                    if(request.responseCode==429)message="Too many sign-in or registration attempts. Wait one minute and try again.";
                    LastError=message;failure?.Invoke(message);yield break;
                }
                LastError="";success?.Invoke(request.downloadHandler.text);
            }
        }
        public IEnumerator Login(string username,string password,Action<bool> done)
        {
            bool ok=false;var body=JsonUtility.ToJson(new CredentialsBody{username=username,password=password});
            yield return Request("POST","/api/login",body,json=>{var result=JsonUtility.FromJson<LoginReply>(json);if(!Guid.TryParseExact(result.serverId,"N",out _)){LastError="Incompatible learning service.";return;}serverId=result.serverId;Account=result.account;token=result.token;Chapter4Store.Profile=Account.username;ok=true;});
            if(ok)
            {
                pending.Clear();ContentJson=null;
                yield return Request("GET","/api/limits-content",null,json=>ContentJson=json);
                if(Account.role=="Student") { LoadPending();yield return Flush();yield return FetchStudentSessions(); }
            }
            done(ok&&Authenticated);
        }
        public IEnumerator FetchStudentSessions()
        {
            if(!Student)yield break;
            yield return Request("GET","/api/sessions",null,json=>{
                var rows=JsonUtility.FromJson<SessionList>(json).sessions;
                foreach(var s in rows)
                {
                    string file=Path.Combine(CacheFolder,s.id+".json");
                    if(File.Exists(file)){var local=JsonUtility.FromJson<Chapter4Session>(File.ReadAllText(file));if(local.revision>s.revision)continue;}
                    Chapter4Store.WriteCache(s);
                }
            });
        }
        public IEnumerator Logout(Action<bool> done)
        {
            yield return Flush();if(HasPending&&Authenticated){LastError="Results are still waiting to sync. Retry before signing out.";done(false);yield break;}
            bool ok=!Authenticated;
            if(Authenticated)yield return Request("POST","/api/logout","{}",_=>ok=true);
            if(ok){token="";Account=null;ContentJson=null;pending.Clear();Chapter4Store.Profile="";}
            done(ok);
        }
        public void Enqueue(Chapter4Session s)
        {
            if(Account==null||s.ownerId!=Account.id||s.topic!=0)return;
            string json=JsonUtility.ToJson(s);pending[s.id]=json;
            try{File.WriteAllText(Path.Combine(CacheFolder,s.id+".pending"),json);}catch(Exception e){LastError="Could not queue upload: "+e.Message;}
            SyncStatus="Saving to learning service...";
        }
        void LoadPending()
        {
            Directory.CreateDirectory(CacheFolder);
            foreach(var file in Directory.GetFiles(CacheFolder,"*.pending"))
                try{var json=File.ReadAllText(file);var s=JsonUtility.FromJson<Chapter4Session>(json);if(s.ownerId==Account.id)pending[s.id]=json;}catch(Exception e){Debug.LogWarning("Unreadable pending session: "+e.Message);}
            // The atomic cache also recovers a crash between saving the result and writing its queue marker.
            foreach(var s in Chapter4Store.Read())if(!pending.TryGetValue(s.id,out var queued)||JsonUtility.FromJson<Chapter4Session>(queued).revision<s.revision)pending[s.id]=JsonUtility.ToJson(s);
        }
        void Update(){if(Student&&!Busy&&pending.Count>0&&Time.unscaledTime>=nextRetry)StartCoroutine(Flush());}
        public IEnumerator Flush()
        {
            while(Busy)yield return null;
            if(!Student||pending.Count==0)yield break;Busy=true;
            try
            {
            foreach(var item in pending.ToArray())
            {
                bool saved=false;
                yield return Request("PUT","/api/sessions/"+item.Key,item.Value,_=>saved=true);
                if(!saved){SyncStatus="Saved locally; sync pending. "+LastError;nextRetry=Time.unscaledTime+10;break;}
                if(pending.TryGetValue(item.Key,out var latest)&&latest==item.Value)
                {
                    string marker=Path.Combine(CacheFolder,item.Key+".pending");
                    try{if(File.Exists(marker))File.Delete(marker);pending.Remove(item.Key);}catch(Exception e){LastError="Saved on server; local queue cleanup pending: "+e.Message;nextRetry=Time.unscaledTime+10;}
                }
            }
            if(pending.Count==0)SyncStatus="Saved to learning service.";
            }
            finally{Busy=false;}
        }
        public static string ReadSetupKey()
        {
            string file=Argument("--setup-key-file",Path.GetFullPath(Path.Combine(Application.dataPath,"../../LearningService/data/setup.key")));
            return File.Exists(file)?File.ReadAllText(file).Trim():null;
        }
    }
}
