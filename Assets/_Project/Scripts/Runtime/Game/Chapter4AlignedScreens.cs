using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CalculusLabEscape
{
    public partial class Chapter4Game
    {
        bool supportModal,showRetry,caseOverview=true,supportApplied;
        int supportChoice;
        protected void OnGUI()
        {
            if(UsesCanvas)return;
            if(Session==null)return;BeginUI();
            if(supportModal){SupportSheet();return;}if(showRetry){RetrySheet();return;}
            if(manual){Box(0,0,1280,720,paper);Manual();return;}if(settings){Box(0,0,1280,720,paper);Settings();return;}if(exitConfirm){Box(0,0,1280,720,paper);ConfirmExit();return;}
            if(State=="Brief"){AlignedBrief();return;}
            if(State=="Summary"){AlignedSummary();return;}
            if(State=="Explore"||State=="Finishing")
            {
                Panel(20,16,1240,73);Label(39,27,980,30,Titles[Topic],heading);
                Label(40,58,1150,26,Topic==2?"Objective: complete all four modules and deactivate the device.":Ready?"Objective: activate the restored system.":Topic==0?"Objective: complete five portal-calibration questions.":"Objective: restore the bridge and reunite the calf with its mother.",small);
                Panel(20,608,1240,92);HeartRow(42,629,3-Session.heartsUsed);
                string progress=Topic==2?$"Modules: {ModulesComplete}/4":Topic==1?$"Bridge repair: {(Stage>=8?3:Stage>=4?2:Stage>=1?1:0)}/3":$"Completed steps: {Session.completed.Count}/{Session.required}";
                Label(150,627,830,32,$"Support Hearts: {3-Session.heartsUsed}   |   {progress}   |   E interact / Esc pause",small);
                if(State=="Explore"&&NearStation())Center(240,661,800,28,"PRESS E / "+(Ready?"ACTIVATE SYSTEM":"OPEN DEVICE"),heading);
                else if(State=="Explore")Center(230,661,830,28,"Follow the outlined device: "+(Topic==1?(Stage==0?"POWER CORE":Stage<4?"CONVERTER BOX":"BRIDGE BUILDER"):Topic==0?"PORTAL CONSOLE":"CONTAINMENT CASE"),small);
                if(State=="Finishing")Center(200,405,880,80,Topic==1?"BRIDGE RESTORED / THE CALF IS CROSSING":Topic==0?"PORTAL ACTIVATED":"DEVICE DEACTIVATED",heading);
            }
            else if(State=="Paused"){Box(0,0,1280,720,paper);Pause();}
            else if(State=="Puzzle")
            {
                Box(0,0,1280,720,paper);
                if(Topic==0)AlignedLimits();else if(Topic==1)AlignedBridge();else AlignedCase();
            }
            if(manual)Manual();if(settings)Settings();if(exitConfirm)ConfirmExit();
            if(showRetry)RetrySheet();if(supportModal)SupportSheet();
        }
        void AlignedBrief()
        {
            Box(0,0,1280,720,paper);Panel(25,20,1230,680);Center(80,45,1120,47,"SESSION BRIEF",title);Center(80,103,1120,37,Titles[Topic],heading);
            Panel(67,168,414,397);
            if(Topic==0)PortalSketch(274,338,113,0);else if(Topic==1)BridgeSketch(102,290,341,0);else CaseSketch(100,264,348,0);
            Center(85,503,376,43,new[]{"Five questions restore the portal","Three components repair the crossing","Four modules deactivate the case"}[Topic],small);
            string[] keys={"OBJECTIVE","LEARNING FOCUS","PREREQUISITE","SUPPORT"};
            string[] values={new[]{"Complete five limit-calibration questions.","Restore the bridge and rescue the calf.","Stabilise four modules, then use the master switch."}[Topic],new[]{"Approach, one-sided limits, DNE and limit versus f(a).","Power rule, exponent transformations and constants.","Integration power rule, +C and definite accumulation."}[Topic],new[]{"Read numerical tables and graphs.","Algebra and exponent notation.","Basic exponent notation and differentiation."}[Topic],"Support Hearts: 3. Basic hints are free. Retries preserve progress."};
            for(int i=0;i<4;i++){Panel(523,168+i*101,679,87);Label(545,177+i*101,630,26,keys[i],heading);Label(545,210+i*101,630,43,values[i],small);}
            if(Button(101,599,318,47,"BACK"))GameProgress.ReturnToMenu();
            if(Button(811,599,354,47,Topic==0?"START EXAMINATION":Topic==1?"START RESCUE":"INSPECT CASE"))StartSession();
            Center(275,657,730,28,"Wrong submissions allow retry and do not reduce completed progress.",small);
        }
        void MiniHud(string text)
        {
            Panel(24,16,1231,82);Label(40,29,810,29,Titles[Topic],heading);Label(40,63,990,25,text,small);
            if(Button(1170,32,61,44,"II"))SetState("Paused");
        }
        void AlignedLimits()
        {
            MiniHud($"Question {Mathf.Min(Stage+1,5)} / 5    |    Portal Charge {Stage*20}%    |    Complete five calibration questions");
            PortalSketch(210,300,118,Stage);Center(65,451,290,53,Stage==0?"Portal inactive":Stage==5?"All five sections restored":Stage+" section(s) restored",heading);
            Panel(42,535,343,110);HeartRow(65,555,3-Session.heartsUsed);Label(157,555,210,30,"Support Hearts: "+(3-Session.heartsUsed),small);
            if(Button(65,599,295,31,"HINT / SUPPORT"))supportModal=true;
            Panel(421,127,813,520);
            if(Ready){Center(461,231,733,96,"PORTAL CHARGE 100%\nActivation lever unlocked",title);if(Button(606,461,445,55,"PULL ACTIVATION LEVER"))Activate();return;}
            Label(445,145,753,35,$"Q{Stage+1}  "+new[]{"Numerical approach","Read the graphical limit","Compare both sides","Limit and function value","Conclusion and reason"}[Stage],heading);
            bool ok=false;string response="",error="",explanation="";
            if(Stage==0)
            {
                Label(445,197,755,36,"Find lim (x + 2) as x approaches 3.");
                Panel(452,257,431,178);Label(471,278,399,140,"x          2.9       2.99      3.01      3.1\n\nf(x)      4.9       4.99      5.01      5.1");
                Label(912,257,260,32,"Approached value");a=Field(912,301,266,a);ok=Number(a,5);response=a;error="NUMERICAL_APPROACH";explanation="Compare both sides: nearby outputs approach 5.";
            }
            else if(Stage==1||Stage==3)
            {
                Label(445,196,755,44,Stage==1?"Move the probe towards x = 2, then place the answer marker.":"Open point (2,4), solid point (2,7): distinguish the limit from f(2).");AlignedGraph(Stage==3);
                if(Stage==1){Label(900,257,293,31,"Probe x = "+probe.ToString("0.00"),small);probe=Slider(new Rect(900,298,287,22),probe,0,4);Label(900,337,287,31,"Marker position y = "+marker.ToString("0"),small);marker=Mathf.Round(Slider(new Rect(900,379,287,22),marker,0,8));ok=marker==4;response="marker="+marker;error="GRAPH_LIMIT";explanation="The curve approaches the open point at y = 4.";}
                else{Label(900,255,283,32,"Limit as x -> 2",small);a=Field(900,290,285,a);Label(900,350,283,32,"Function value f(2)",small);b=Field(900,385,285,b);ok=Number(a,4)&&Number(b,7);response=a+" | "+b;error="LIMIT_VS_FUNCTION_VALUE";explanation="Nearby values approach 4; the filled point gives f(2) = 7.";}
            }
            else if(Stage==2)
            {
                Label(445,196,755,59,"For x < 2, f(x) = x + 1. For x > 2, f(x) = 5 - x.\nAs x approaches 2, compare the two approached values.");
                string[] sides={"Left-hand limit","Right-hand limit","Two-sided limit"};for(int i=0;i<3;i++){Panel(451+i*253,281,238,151);Center(459+i*253,297,222,35,sides[i],small);}
                a=Field(469,352,204,a);b=Field(722,352,204,b);c=Field(975,352,204,c);ok=Number(a,3)&&Number(b,3)&&Number(c,3);response=a+" | "+b+" | "+c;error="ONE_SIDED_MISMATCH";explanation="Both sides approach 3; therefore the two-sided limit is also 3.";
            }
            else
            {
                Label(445,197,754,40,"Left-hand limit = 2. Right-hand limit = 5. Select and justify the conclusion.");
                string[] values={"Limit = 2","Limit = 5","Limit does not exist"},reason={"Left-hand limit","!=","Right-hand limit"};
                for(int i=0;i<3;i++)if(Button(451+i*250,264,236,41,(choice==i?"> ":"")+values[i]))choice=i;
                for(int i=0;i<3;i++)if(Button(451+i*250,327,236,39,(card==i?"> ":"")+reason[i]))card=i;
                for(int i=0;i<3;i++)if(Button(451+i*250,392,236,44,order[i]<0?"REASON SLOT "+(i+1):reason[order[i]])){if(card>=0){for(int j=0;j<3;j++)if(order[j]==card)order[j]=-1;order[i]=card;}}
                ok=choice==2&&order[0]==0&&order[1]==1&&order[2]==2;response=choice+";"+string.Join(",",order.Take(3));error="UNEQUAL_SIDES_DNE";explanation="Left-hand limit != Right-hand limit, so the two-sided limit does not exist.";
            }
            Label(455,456,151,26,"SCRATCHPAD",small);scratch=GUI.TextField(new Rect(598,450,583,43),scratch,200,field);
            if(accepted){Label(448,513,737,43,feedback,small);if(Button(978,576,214,43,"CONTINUE"))Advance();}
            else{if(Button(978,576,214,43,"SUBMIT ANSWER"))accepted=ValidateStep("L"+(Stage+1),response,ok,error,explanation);if(Button(720,576,236,43,"BASIC HINT / FREE"))RequestHint(false);}
            if(supportText!="")Label(448,504,746,61,supportText,small);
            if(Button(43,669,265,33,"RETURN TO WORLD"))SetState("Explore");
        }
        void AlignedGraph(bool filled)
        {
            float gx=484,gy=423,w=340,h=151;
            for(int i=0;i<=8;i++){float y=gy-i*h/8;Line(new Vector2(gx,y),new Vector2(gx+w,y),new Color(.81f,.82f,.8f),1);Label(gx-26,y-9,24,20,i.ToString(),small);}
            Line(new Vector2(gx,gy),new Vector2(gx+w,gy),ink);Line(new Vector2(gx+w/2,gy+10),new Vector2(gx+w/2,gy-h),ink);
            Vector2 previous=Vector2.zero;for(int i=0;i<=40;i++){float x=i/10f;var p=new Vector2(gx+x*w/4,gy-(4+.8f*(x-2)*(x-2))*h/8);if(i>0)Line(previous,p,ink,2);previous=p;}
            Ring(gx+w/2,gy-4*h/8,5,ink,16);Box(gx+w/2-2,gy-4*h/8-2,4,4,paper);Label(gx+w/2-10,gy+3,50,21,"2",small);
            if(filled)Point(gx+w/2,gy-7*h/8,ink);else{float x=Mathf.Clamp(probe,0,4);Point(gx+x*w/4,gy-(4+.8f*(x-2)*(x-2))*h/8,green);Line(new Vector2(gx,gy-marker*h/8),new Vector2(gx+w,gy-marker*h/8),muted,1);}
        }
        void AlignedBridge()
        {
            MiniHud("Objective: apply the Power Rule    |    Bridge repair: "+(Stage>=8?3:Stage>=4?2:Stage>=1?1:0)+"/3");
            if(World.Preview!=null)GUI.DrawTexture(new Rect(41,113,1199,177),World.Preview,ScaleMode.StretchToFill);
            Panel(40,304,1200,322);
            if(Ready){Center(96,351,1087,69,"BRIDGE SECURED / THREE COMPONENTS INSTALLED",title);if(Button(406,481,470,56,"OPEN CROSSING / RESCUE CALF"))Activate();}
            else if(Stage==0)AlignedPowerCore();
            else
            {
                GUI.BeginGroup(new Rect(0,103,1280,522));Differentiation();GUI.EndGroup();
                if(accepted)Label(60,603,1130,24,"Accepted. Install or place this repair to continue.",small);
            }
            Panel(40,642,1200,63);HeartRow(59,661,3-Session.heartsUsed);Label(158,665,587,30,$"Support Hearts: {3-Session.heartsUsed}    |    Water: {(pressurePaused?"HELD":Stage>=8?"RISING":"LOW")}",small);
            if(Button(775,654,212,38,"HINT / SUPPORT"))supportModal=true;if(Button(1005,654,217,38,"RETURN TO WORLD"))SetState("Explore");
        }
        void AlignedPowerCore()
        {
            Panel(57,321,300,286);Panel(375,321,499,286);Panel(892,321,330,286);
            Label(76,337,266,65,"1. Drag the 4 to the coefficient slot",small);Chip(new Rect(159,414,95,65),"4","exponent");Label(76,511,260,66,"2. Turn the exponent dial down by one",small);
            Center(400,331,448,34,"CURRENT EXPRESSION",small);Center(463,369,328,64,"x^4",title);
            var slot=new Rect(405,469,199,60);if(Button(slot.x,slot.y,slot.width,slot.height,exponentChip?"COEFFICIENT: 4":"COEFFICIENT SLOT"))exponentChip=true;
            if(Event.current.type==EventType.MouseUp&&slot.Contains(Event.current.mousePosition)&&dragging=="exponent"){exponentChip=true;dragging="";Event.current.Use();}
            if(!int.TryParse(a,out int exponent))exponent=4;
            Center(640,434,200,30,"Exponent dial",small);Ring(740,498,32,ink);
            if(Button(640,474,45,48,"-"))exponent=Mathf.Max(-6,exponent-1);
            Center(704,478,72,40,exponent.ToString(),heading);
            if(Button(795,474,45,48,"+"))exponent=Mathf.Min(8,exponent+1);
            a=exponent.ToString();
            Center(910,340,290,34,"RESULT PREVIEW",small);Center(913,407,290,73,(exponentChip?"4":"?")+"x^"+a,title);
            if(accepted){if(Button(914,545,284,43,"INSTALL REGULATOR"))Advance();}
            else if(Button(914,545,284,43,"TEST REPAIR"))accepted=ValidateStep("D0","coefficient="+(exponentChip?4:0)+";exponent="+a,exponentChip&&Number(a,3),"POWER_RULE","Multiply by 4, then reduce the exponent from 4 to 3: 4x^3.");
            if(dragging!="")Label(Event.current.mousePosition.x+8,Event.current.mousePosition.y,80,40,"4",heading);if(Event.current.type==EventType.MouseUp)dragging="";
        }
        void AlignedCase()
        {
            MiniHud($"Objective: complete all four modules and deactivate the device    |    Modules: {ModulesComplete}/4    |    "+(Session.safetyMode?"SAFETY MODE":TimeSpan.FromSeconds(Mathf.Max(0,600-caseElapsed)).ToString(@"mm\:ss")));
            if(Button(30,112,175,39,"OPEN MANUAL"))manual=true;
            if(caseOverview||Ready)
            {
                Panel(88,168,1110,458);Panel(113,179,1060,49);Center(130,190,1030,28,"OPEN CONTAINMENT CASE / INTEGRATION MANUAL IN LID",small);
                string[] names={"EXPONENT DIAL","WIRE MATCHING","SYMBOL SEQUENCE","PRESSURE GAUGE"};
                bool[] solved={Done("I_DIAL"),Done("I_C"),Done("I_SEQ_3"),Done("I_GAUGE")};
                for(int i=0;i<4;i++)
                {
                    float x=119+i*263;Panel(x,242,248,225);Center(x+8,253,232,39,(i+1)+"  "+names[i],small);
                    if(i==0){Ring(x+123,350,42,ink);Center(x+70,322,108,51,"n+1",heading);}else if(i==1){for(int j=0;j<3;j++){Ring(x+44,323+j*24,5,ink);Ring(x+204,323+j*24,5,ink);Line(new Vector2(x+50,323+j*24),new Vector2(x+198,323+(2-j)*24),ink);}}
                    else if(i==2){for(int j=0;j<4;j++){Panel(x+17+j*57,325,48,46);Center(x+17+j*57,329,48,36,(j+1).ToString(),small);}}else{Ring(x+124,349,45,ink);Line(new Vector2(x+124,349),new Vector2(x+149,325),ink,3);}
                    if(Button(x+19,416,210,35,solved[i]?"STABLE":names[i],!solved[i]&&(i==0||Done("I_DIAL")))){Module=i;caseOverview=false;ResetInputs();}
                }
                Panel(356,487,575,112);Center(378,497,530,34,"MASTER DEACTIVATION",heading);
                if(Button(378,543,530,40,"LIFT COVER / MASTER DEACTIVATION",Ready))Activate();
            }
            else
            {
                Panel(86,183,1110,427);Integration();if(Button(1003,113,205,39,"BACK TO CASE"))caseOverview=true;
                if(feedback!=""&&!showRetry)Label(122,521,1033,71,feedback,small);
            }
            Panel(39,644,1201,61);HeartRow(58,661,3-Session.heartsUsed);Label(158,663,562,30,"Support Hearts: "+(3-Session.heartsUsed)+"   |   Safety Mode activates at 0:00",small);
            if(Button(778,655,212,37,"HINT / SUPPORT"))supportModal=true;if(Button(1006,655,214,37,"RETURN TO WORLD"))SetState("Explore");
        }
        void SupportSheet()
        {
            Box(0,0,1280,720,new Color(.8f,.8f,.78f,.94f));Panel(180,91,920,546);Center(220,114,840,41,"SUPPORT",title);Center(220,164,840,31,"Support Hearts: "+(3-Session.heartsUsed),heading);
            if(supportApplied)
            {
                Panel(230,237,820,240);Label(264,267,752,185,supportText,body);
                if(Button(263,548,347,45,"CHOOSE SUPPORT"))supportApplied=false;
                if(Button(663,548,347,45,"RETURN TO PUZZLE")){supportModal=false;supportApplied=false;}return;
            }
            string[] names={"BASIC HINT","RULE REMINDER","GUIDED DEMONSTRATION"};
            string[] descriptions={"A short prompt to help you inspect the current step.","A rule reminder or a related worked example.","Read a related worked example, explained in steps."};
            for(int i=0;i<3;i++){float x=211+i*288;Panel(x,225,269,270);if(Button(x+12,243,245,47,(supportChoice==i?"> ":"")+names[i],i==0||Session.heartsUsed<3))supportChoice=i;Center(x+12,314,245,35,i==0?"FREE":"USES 1 HEART",heading);Label(x+24,379,222,98,descriptions[i],small);}
            if(Button(273,548,339,45,"USE SUPPORT")){RequestHint(supportChoice>0);if(supportChoice==2&&Session.support.Count>0){Session.support.Last().type="GuidedDemonstration";supportText=Topic==0?StrongHint():Topic==1?"1. Multiply the coefficient by the exponent.\n2. Subtract one from the exponent.\n3. Keep the new coefficient and power together.\nExample: 2x^3 -> (2*3)x^(3-1) -> 6x^2.":"1. Raise the exponent by one.\n2. Divide the coefficient by the new exponent.\n3. Add C for an indefinite integral.\nExample: integral of 6x^2 dx = (6/3)x^3 + C = 2x^3 + C.";Save();}supportApplied=true;}
            if(Button(663,548,339,45,"CANCEL"))supportModal=false;
        }
        void RetrySheet()
        {
            Box(0,0,1280,720,new Color(.88f,.88f,.86f,.97f));Panel(245,86,790,553);Center(280,110,720,47,"INCORRECT ACTION",title);
            var attempt=Session.attempts.LastOrDefault();Center(282,171,717,47,attempt==null?"Review the active step":attempt.error.Replace('_',' '),heading);
            Panel(282,241,344,132);Panel(650,241,345,132);Label(303,259,301,24,"YOUR STEP",small);Label(303,307,301,56,attempt?.response??"",body);Label(672,259,300,24,"EXPECTED RELATIONSHIP",small);Label(672,298,300,66,feedback.Replace("TRY AGAIN  /  ",""),small);
            Panel(283,393,711,103);Center(298,403,680,31,"ENVIRONMENTAL CONSEQUENCE",heading);Center(300,444,675,42,Topic==0?"The portal flickers. Completed charge is preserved.":Topic==1?"The affected bridge section flashes. Completed repairs stay in place.":"The module warns and remains unlocked for another attempt.",small);
            if(Button(285,548,211,43,"RETRY"))showRetry=false;
            if(Button(524,548,220,43,"BASIC HINT / FREE")){showRetry=false;RequestHint(false);}
            if(Button(770,548,221,43,"STRONGER SUPPORT")){showRetry=false;supportModal=true;supportChoice=1;}
        }
        void AlignedSummary()
        {
            Box(0,0,1280,720,paper);Panel(138,31,1005,661);Center(160,52,961,42,"CALCULUS LAB ESCAPE",heading);Center(160,106,961,48,"TOPIC SUMMARY",title);
            Center(185,166,914,36,Titles[Topic],heading);Center(185,207,914,28,"COMPLETED",small);
            string[] keys={"Accuracy","Incorrect attempts","Completion rate","Support Hearts used","Completion time","Most common error"};
            string[] vals={Session.Accuracy<0?"Not assessed":Session.Accuracy.ToString("0")+"%",Session.MathematicalIncorrectCount.ToString(),Session.Completion.ToString("0")+"%",Session.heartsUsed.ToString(),TimeSpan.FromSeconds(Session.elapsed).ToString(@"mm\:ss"),Session.CommonError.Replace('_',' ')};
            for(int i=0;i<6;i++){float x=183+(i%2)*474,y=258+(i/2)*76;Panel(x,y,440,61);Label(x+15,y+16,208,30,keys[i],small);Center(x+226,y+8,204,47,vals[i],i==5?small:heading);}
            Panel(183,501,914,85);Center(200,510,878,33,Session.Band,heading);Center(200,548,878,25,Topic==2?"Safety Mode triggered: "+(Session.safetyMode?"Yes":"No"):"Review the rule before the next challenge.",small);
            if(Button(183,615,288,43,"PRACTISE AGAIN"))GameManager.ChangeScene(Scenes[Topic]);
            if(Button(495,615,288,43,"VIEW DASHBOARD")){Chapter4Frontend.ReturnPage="Dashboard";GameProgress.ReturnToMenu();}
            if(Button(807,615,288,43,"MAIN MENU")){Chapter4Frontend.ReturnPage="Home";GameProgress.ReturnToMenu();}
        }
    }
}
