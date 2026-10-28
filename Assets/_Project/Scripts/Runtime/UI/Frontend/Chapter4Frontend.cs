using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CalculusLabEscape
{
    public sealed class Chapter4Frontend : Chapter4UI
    {
        public static string ReturnPage="Login";
        string page,profile,password="demo",loginFeedback="",studentFilter="",exportStatus="";
        bool teacher;
        int filter=-1;
        Vector2 scroll;
        Chapter4Session selected;
        List<Chapter4Session> sessions=new List<Chapter4Session>();
        void Awake(){page=ReturnPage;ReturnPage="Home";profile=PlayerPrefs.GetString("C4Profile","Trainee");Chapter4Store.Profile=profile;AudioListener.volume=PlayerPrefs.GetFloat("C4Volume",.75f);sessions=Chapter4Store.Read();Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        void OnGUI()
        {
            BeginUI();ConsoleFrame(page=="Login"?"LOGIN":page=="Home"?"MAIN MENU":page=="Topics"?"CHOOSE A MISSION":page=="Teacher"?"TEACHER DASHBOARD":page=="Dashboard"?"STUDENT DASHBOARD":page.ToUpperInvariant());
            if(page=="Login")Login();else if(page=="Home")Home();else if(page=="Topics")Topics();else if(page=="Dashboard"||page=="Teacher")Dashboard();else if(page=="Settings")Settings();else if(page=="Instructions")Instructions();else if(page=="Exit")Exit();
        }
        void Login()
        {
            Panel(320,179,535,403);Label(360,211,420,27,"Username",small);profile=Field(360,248,445,profile);
            Label(360,306,420,27,"Access code",small);password=GUI.PasswordField(new Rect(360,343,445,42),password,'*',32,field);
            if(Button(390,430,390,48,"LOG IN"))
            {
                if(string.IsNullOrWhiteSpace(profile)||password!="demo")loginFeedback="Enter a username and a valid access code.";
                else{Chapter4Store.Profile=profile.Trim();PlayerPrefs.SetString("C4Profile",Chapter4Store.Profile);PlayerPrefs.Save();sessions=Chapter4Store.Read(teacher);page=teacher?"Teacher":"Home";}
            }
            Label(360,500,450,62,loginFeedback,small);
            if(Button(901,234,236,65,teacher?"STUDENT ACCESS":"[STUDENT ACCESS]"))teacher=false;
            Label(906,307,221,74,"Explore missions, solve puzzles and restore the laboratory.",small);
            if(Button(901,398,236,65,teacher?"[LOCAL RECORDS]":"LOCAL RECORDS"))teacher=true;
            Label(906,473,221,85,"View learning records saved on this computer.",small);
        }
        void Home()
        {
            // Figure 4.5: the laboratory entrance fills the left; navigation is on the right.
            Panel(132,167,699,423);Vector2 vp=new Vector2(478,319);
            foreach(Vector2 corner in new[]{new Vector2(150,185),new Vector2(810,185),new Vector2(150,570),new Vector2(810,570)})Line(corner,vp,muted);
            for(int i=0;i<5;i++)Line(new Vector2(168,473+i*22),new Vector2(798,473+i*22),muted,1);
            Panel(350,237,248,284);Panel(368,288,105,232);Panel(477,288,105,232);
            Center(355,247,240,31,"CALCULUS LAB",heading);Panel(387,313,67,101);Panel(496,313,67,101);Box(449,441,7,31,ink);Box(495,441,7,31,ink);
            Panel(173,251,125,145);Center(182,270,107,36,"d / dx",title);Line(new Vector2(190,366),new Vector2(273,316),ink);Panel(659,251,124,145);Center(668,271,107,36,"F(x)",title);Line(new Vector2(680,370),new Vector2(746,320),ink);
            Panel(877,179,270,402);string[] labels={"START","STUDENT DASHBOARD","INSTRUCTIONS","SETTINGS","EXIT"};
            for(int i=0;i<5;i++)if(Button(898,204+i*71,228,50,labels[i])){page=new[]{"Topics","Dashboard","Instructions","Settings","Exit"}[i];if(i==1)sessions=Chapter4Store.Read();}
            if(Button(932,601,194,30,"LOG OUT")){teacher=false;page="Login";}
        }
        void Topics()
        {
            string[] names={"LIMITS","DIFFERENTIATION","INTEGRATION"};string[] sub={"The Portal Examination","The Broken Bridge Rescue","The Containment Case"};
            for(int i=0;i<3;i++)
            {
                float x=137+i*341;Panel(x,177,320,393);
                if(i==0)PortalSketch(x+160,289,66,0);else if(i==1)BridgeSketch(x+40,252,240,0);else CaseSketch(x+30,232,260,0);
                Center(x+10,377,300,35,names[i],heading);Center(x+12,418,296,30,sub[i],small);Center(x+12,456,296,30,new[]{"Solve / Submit / Restore","Repair / Build / Rescue","Configure / Connect / Deactivate"}[i],small);
                if(Button(x+37,507,246,43,"VIEW MISSION "+(i+1)))GameManager.ChangeScene(Chapter4Game.Scenes[i]);
            }
            Center(290,587,700,29,"Wrong attempts allow retry and preserve your completed progress.",small);if(Button(140,602,125,32,"BACK"))page="Home";
        }
        void Dashboard()
        {
            bool all=page=="Teacher";var rows=sessions.Where(s=>(filter<0||s.topic==filter)&&(!all||s.profile.IndexOf(studentFilter,StringComparison.OrdinalIgnoreCase)>=0)).ToList();
            if(selected!=null){Detail();return;}
            if(all)
            {
                Label(133,151,125,22,"Student",small);studentFilter=Field(133,177,190,studentFilter);TopicFilters(338,177,190);
                Label(133,233,993,25,"LOCAL LEARNING RECORDS",small);
            }
            else
            {
                int attempts=rows.Sum(s=>s.MathematicalAttemptCount);float accuracy=attempts==0?-1:100f*rows.Sum(s=>s.MathematicalCorrectCount)/attempts;
                Panel(138,168,476,78);Panel(633,168,507,78);Center(158,183,435,47,"Overall accuracy  "+(accuracy<0?"--":accuracy.ToString("0")+"%"),title);Center(648,183,475,47,"Completed levels  "+rows.Where(s=>s.status=="Completed").Select(s=>s.topic).Distinct().Count()+" / 3",title);
                TopicFilters(140,262,245);
            }
            float top=all?271:317;scroll=GUI.BeginScrollView(new Rect(136,top,1007,all?163:138),scroll,new Rect(0,0,980,Mathf.Max(220,rows.Count*66)));
            if(rows.Count==0)Label(25,40,900,90,"No records yet. Complete a mission to populate this dashboard.");
            for(int i=0;i<rows.Count;i++){var s=rows[i];Panel(2,i*66,972,58);Label(14,i*66+7,660,44,(all?s.profile+" / ":"")+new[]{"LIMITS","DIFFERENTIATION","INTEGRATION"}[s.topic]+" / "+s.status+"\nAccuracy "+(s.Accuracy<0?"--":s.Accuracy.ToString("0")+"%")+"   Completion "+s.Completion.ToString("0")+"%   Errors "+s.MathematicalIncorrectCount,small);if(Button(792,i*66+10,163,37,"VIEW ATTEMPTS"))selected=s;}GUI.EndScrollView();
            Panel(139,all?451:472,486,103);Panel(647,all?451:472,490,103);
            float chartY=all?458:479;
            Label(155,chartY,457,27,all?"OVERALL COMPLETION OVERVIEW":"RECENT ATTEMPTS",small);
            Label(663,chartY,456,27,"MOST COMMON ERRORS",small);
            if(all){int done=rows.Count(s=>s.status=="Completed");Ring(188,chartY+57,23,ink);Label(228,chartY+36,358,54,"Completed "+done+" / "+rows.Count+" sessions\n"+(rows.Count==0?"No submissions yet.":(100f*done/rows.Count).ToString("0")+"% of filtered sessions"),small);}
            else{var recent=rows.SelectMany(s=>s.attempts).OrderByDescending(a=>a.timestamp).Take(2).ToArray();Label(157,chartY+32,453,58,recent.Length==0?"No attempts recorded yet.":string.Join("\n",recent.Select(a=>a.step+" / "+(a.correct?"Correct":"Retry"))),small);}
            var errors=rows.SelectMany(s=>s.attempts).Where(a=>!a.correct&&!Chapter4Session.IsInvalidInput(a)).GroupBy(a=>a.error).OrderByDescending(g=>g.Count()).Take(2).ToArray();
            Label(664,chartY+32,445,58,errors.Length==0?"No recorded errors.":string.Join("\n",errors.Select(g=>g.Key.Replace('_',' ')+" ("+g.Count()+")")),small);
            if(Button(137,591,221,39,all?"LOG OUT":"MAIN MENU"))page=all?"Login":"Home";
            if(all&&Button(874,591,263,39,"EXPORT LOCAL CSV")){Export(rows);}
            Label(387,590,455,36,exportStatus,small);
        }
        void TopicFilters(float x,float y,float width){for(int i=-1;i<3;i++)if(Button(x+(i+1)*(width+5),y,width,37,(filter==i?"> ":"")+(i<0?"ALL TOPICS":new[]{"LIMITS","DIFFERENTIATION","INTEGRATION"}[i]))){filter=i;scroll=Vector2.zero;}}
        void Detail()
        {
            Panel(139,163,999,161);Label(166,180,950,137,selected.Summary,small);scroll=GUI.BeginScrollView(new Rect(142,345,994,223),scroll,new Rect(0,0,960,selected.attempts.Count*46+25));
            for(int i=0;i<selected.attempts.Count;i++){var a=selected.attempts[i];Label(10,i*46,939,44,a.number+" / "+a.step+" / "+selected.AttemptLabel(a)+" / "+a.response+"\n"+a.error,small);}GUI.EndScrollView();if(Button(139,596,250,38,"BACK TO DASHBOARD")){selected=null;scroll=Vector2.zero;}
        }
        void Export(List<Chapter4Session> rows)
        {
            try{string path=Path.Combine(Chapter4Store.Folder,"teacher-preview.csv");Directory.CreateDirectory(Chapter4Store.Folder);var csv=new System.Text.StringBuilder("Profile,Topic,Status,Accuracy,Completion,Incorrect,Hearts,Seconds\n");foreach(var s in rows){string name=s.profile.Replace("\"","\"\"");if(name.Length>0&&"=+-@".Contains(name[0].ToString()))name="'"+name;csv.AppendLine("\""+name+"\","+s.topic+","+s.status+","+s.Accuracy.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+","+s.Completion.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+","+s.MathematicalIncorrectCount+","+s.heartsUsed+","+(int)s.elapsed);}File.WriteAllText(path,csv.ToString());exportStatus="Saved teacher-preview.csv in the session folder.";}catch(Exception e){exportStatus="Export failed: "+e.Message;}
        }
        void Settings(){Panel(322,178,639,378);Label(361,213,520,35,"MASTER VOLUME",heading);AudioListener.volume=Slider(new Rect(361,277,550,25),AudioListener.volume,0,1);if(Button(361,344,550,43,Screen.fullScreen?"SWITCH TO WINDOWED":"SWITCH TO FULLSCREEN"))Screen.fullScreen=!Screen.fullScreen;Label(361,411,548,58,"",small);if(Button(361,489,550,43,"SAVE / BACK")){PlayerPrefs.SetFloat("C4Volume",AudioListener.volume);PlayerPrefs.Save();page="Home";}}
        void Instructions(){Panel(190,171,900,389);Label(229,215,824,269,"WASD / Arrow keys   Move through the level\nMouse                         Look and operate controls\nE                                  Open a nearby device\nEsc                              Close a puzzle or pause\n\nUse the chapter's device controls to solve each puzzle.\nBasic hints are free. Stronger support uses one of three hearts.\nAll completed repairs survive an incorrect attempt.");if(Button(785,584,305,42,"BACK"))page="Home";}
        void Exit(){Panel(351,223,577,269);Center(388,265,500,84,"Exit Calculus Lab Escape?",title);if(Button(391,402,222,43,"CONFIRM EXIT"))Application.Quit();if(Button(657,402,222,43,"CANCEL"))page="Home";}
    }
}
