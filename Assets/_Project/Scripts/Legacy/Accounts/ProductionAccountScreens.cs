using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    public sealed partial class ProductionCanvas
    {
        bool teacherRecords,accountBusy;
        void SetupAccountControls()
        {
            if(LearningApi.LocalPlay)
            {
                Show("HomeLogout",false);
                Text("ProfileLabel","LOCAL PLAY");
                Text("LocalNotice","Progress is saved on this computer.");
                Control<TMP_InputField>("ProfileInput").interactable=false;
                Control<Button>("Mission1").interactable=true;Control<Button>("Mission2").interactable=true;
                Text("Mission1Label","Open mission");Text("Mission2Label","Open mission");
                return;
            }
            
            Control<TMP_InputField>("LoginPassword").contentType=TMP_InputField.ContentType.Password;
            Control<TMP_InputField>("LoginPassword").ForceLabelUpdate();
            if(!LearningApi.LocalPlay)
            {
                Control<TMP_InputField>("ProfileInput").interactable=false;
                Text("ProfileLabel","SIGNED-IN STUDENT");Text("LocalNotice","Personal learning records are linked to your account.");
                // These remain accessible in the explicitly selected legacy development test only.
                Control<Button>("Mission1").interactable=false;Control<Button>("Mission2").interactable=false;
                Text("Mission1Label","Account migration pending");Text("Mission2Label","Account migration pending");
            }
        }
        IEnumerator RestoreAccountMenu()
        {
            if(!LearningApi.Authenticated){OpenPage("Login");yield return RefreshSetup();yield break;}
            bool valid=false;yield return LearningApi.Instance.Request("GET","/api/me",null,_=>valid=true);
            if(!valid&&!LearningApi.Authenticated){OpenPage("Login");Text("LoginStatus",LearningApi.LastError);yield break;}
            // A temporary service outage must not strand a locally saved session at the login page.
            // A real 401 clears the token in Request and still takes the login path above.
            yield return RouteAccount();
            if(!valid&&LearningApi.Student)Text("HomeStatus","Service temporarily unavailable. Saved work can be continued; results will sync when it returns.");
        }
        IEnumerator RefreshSetup()
        {
            Show("SetupTeacher",false);Text("LoginStatus","Connecting to the learning service...");
            yield return LearningApi.Instance.Request("GET","/api/setup-status",null,json=>{
                bool needs=JsonUtility.FromJson<SetupReply>(json).needsSetup;Show("SetupTeacher",needs);
                Text("LoginStatus",needs?"First run: create the teacher account with the fields above. Students can register afterwards.":"Enter your account details. New students can register below.");
            },error=>Text("LoginStatus",error));
        }
        void SetAccountBusy(bool busy)
        {
            accountBusy=busy;foreach(string name in new[]{"LoginSubmit","RegisterStudent","SetupTeacher","HomeLogout","TeacherLogout","TeacherSearch","TeacherExport","TeacherStudents","TeacherView"})Control<Button>(name).interactable=!busy;
        }
        IEnumerator SignIn()
        {
            if(accountBusy)yield break;SetAccountBusy(true);Text("LoginStatus","Signing in...");
            string username=Control<TMP_InputField>("LoginUsername").text.Trim(),password=Control<TMP_InputField>("LoginPassword").text;
            bool ok=false;yield return LearningApi.Instance.Login(username,password,result=>ok=result);
            Control<TMP_InputField>("LoginPassword").text="";SetAccountBusy(false);
            if(ok)yield return RouteAccount();else Text("LoginStatus",LearningApi.LastError);
        }
        IEnumerator Register(bool teacher)
        {
            if(accountBusy)yield break;
            string key=teacher?LearningApi.ReadSetupKey():null;
            if(teacher&&key==null){Text("LoginStatus","Use Start-LearningLab.ps1 on the service computer for initial teacher setup.");yield break;}
            SetAccountBusy(true);Text("LoginStatus","Creating account...");
            var body=JsonUtility.ToJson(new CredentialsBody{username=Control<TMP_InputField>("LoginUsername").text.Trim(),password=Control<TMP_InputField>("LoginPassword").text});bool ok=false;
            yield return LearningApi.Instance.Request("POST",teacher?"/api/setup":"/api/register",body,_=>ok=true,null,key);
            Control<TMP_InputField>("LoginPassword").text="";SetAccountBusy(false);
            if(ok){if(teacher)Show("SetupTeacher",false);Text("LoginStatus","Account created. Enter the password again and sign in.");}else Text("LoginStatus",LearningApi.LastError);
        }
        IEnumerator RouteAccount()
        {
            teacherRecords=LearningApi.Account.role=="Teacher";
            if(teacherRecords){Chapter4Frontend.ReturnPage="Home";OpenPage("Teacher");yield return TeacherSearch();}
            else
            {
                UpdateAccountHome();bool dashboard=Chapter4Frontend.ReturnPage=="Dashboard";Chapter4Frontend.ReturnPage="Home";
                if(dashboard){RefreshRecords();OpenPage("Records");}else OpenPage("Home");
            }
        }
        void UpdateAccountHome()
        {
            if(!LearningApi.Student)return;Control<TMP_InputField>("ProfileInput").SetTextWithoutNotify(LearningApi.Account.username);
            var pending=Chapter4Store.Read().FirstOrDefault(s=>s.topic==0&&s.status=="InProgress");
            Control<Button>("HomeContinue").interactable=pending!=null;
            Text("HomeContinueLabel",pending==null?"No Limits session to continue":"Continue Limits");
            Text("HomeStatus",pending==null?"Signed in. Your learning records belong to this account.":"Continue Limits: "+pending.completed.Count+"/5 questions accepted.");
        }
        IEnumerator SignOut()
        {
            if(accountBusy)yield break;SetAccountBusy(true);bool ok=false;
            yield return LearningApi.Instance.Logout(value=>ok=value);SetAccountBusy(false);
            if(ok){teacherRecords=false;records.Clear();ResumeId=null;OpenPage("Login");Text("LoginStatus","Signed out. Enter an account to continue.");}
            else Text(teacherRecords?"TeacherStatus":"HomeStatus",LearningApi.LastError);
        }
        string TeacherQuery()
        {
            int topic=Control<TMP_Dropdown>("FilterTopic").value,step=Control<TMP_Dropdown>("FilterStep").value,error=Control<TMP_Dropdown>("FilterError").value,status=Control<TMP_Dropdown>("FilterStatus").value;
            string[] errors={"","NUMERICAL_APPROACH","GRAPH_LIMIT","ONE_SIDED_MISMATCH","LIMIT_VS_FUNCTION_VALUE","UNEQUAL_SIDES_DNE"};
            return "?student="+Uri.EscapeDataString(Control<TMP_InputField>("FilterStudent").text.Trim())+"&topic="+(topic==0?"":(topic-1).ToString())+"&step="+(step==0?"":"L"+step)+"&error="+errors[error]+"&status="+new[]{"","InProgress","Completed","Abandoned"}[status];
        }
        IEnumerator TeacherSearch()
        {
            if(accountBusy)yield break;SetAccountBusy(true);Text("TeacherStatus","Loading matching sessions...");
            yield return LearningApi.Instance.Request("GET","/api/teacher/sessions"+TeacherQuery(),null,json=>{
                records=JsonUtility.FromJson<SessionList>(json).sessions;teacherRecords=true;
                Text("TeacherSummary",records.Count+" matching sessions\n"+records.Count(s=>s.status=="Completed")+" completed   /   "+records.Sum(s=>s.attempts.Count(a=>!a.correct))+" incorrect attempts\n"+records.Select(s=>s.ownerId).Distinct().Count()+" students in these results");
                Text("TeacherStatus",records.Count==0?"No sessions match these filters.":"Results ready. Open sessions to review the submitted evidence.");
            },message=>Text("TeacherStatus",message));
            SetAccountBusy(false);
        }
        IEnumerator ShowStudents()
        {
            if(accountBusy)yield break;SetAccountBusy(true);
            yield return LearningApi.Instance.Request("GET","/api/teacher/students",null,json=>{
                var students=JsonUtility.FromJson<StudentList>(json).students;Text("StudentsList",students.Count==0?"No student accounts yet.":string.Join("\n",students.Select(s=>s.id+"   /   "+s.username)));OpenPage("Students");
            },error=>Text("TeacherStatus",error));SetAccountBusy(false);
        }
        IEnumerator ExportTeacher()
        {
            if(accountBusy)yield break;SetAccountBusy(true);
            yield return LearningApi.Instance.Request("GET","/api/teacher/export"+TeacherQuery(),null,csv=>{
                try{string folder=Path.Combine(LearningApi.CacheRoot,"Exports");Directory.CreateDirectory(folder);string file=Path.Combine(folder,"learning-records-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".csv");File.WriteAllText(file,csv,new System.Text.UTF8Encoding(true));Text("TeacherStatus","CSV exported to: "+file);}
                catch(Exception e){Text("TeacherStatus","Could not export: "+e.Message);}
            },error=>Text("TeacherStatus",error));SetAccountBusy(false);
        }
    }
}
