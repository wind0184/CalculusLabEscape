using System;
using System.Linq;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CalculusLabEscape
{
    public partial class Chapter4Game : Chapter4UI
    {
        public static readonly string[] Titles={"THE PORTAL EXAMINATION","THE BROKEN BRIDGE RESCUE","THE INTEGRATION CONTAINMENT CASE"};
        public static readonly string[] Scenes={"Level01_Limits","Level02_Differentiation","Level03_Integration"};
        public Chapter4Session Session {get;private set;}
        public int Topic {get;private set;}
        public Chapter4World World {get;private set;}
        public string State {get;private set;}="Brief";
        public virtual bool PresentationBusy=>false;
        public int Stage {get;private set;}
        public int Module {get;private set;}
        public bool Ready => Topic==0?Stage==5:Topic==1?Stage==11:ModulesComplete==4;
        public int ModulesComplete => Enumerable.Range(0,4).Count(CaseModuleComplete);
        string a="",b="",c="",feedback="",supportText="",scratch="";
        bool correctFeedback, manual, accepted, settings, exitConfirm, pressurePaused;
        float probe=-1, marker=1, finishTime, saveAt, caseElapsed;
        int failures, wire=-1, wireTarget=-1, draggedWire=-1, card=-1, choice=-1;
        int[] order={-1,-1,-1,-1};
        readonly string[] cards={"Rewrite","Raise","Divide","Add C"};
        readonly string[] seqValues={"1/2","3/2","2/3","C"};
        bool exponentChip;
        string dragging="";
        public void Initialize(int topic,Chapter4World world)
        {
            Topic=topic;World=world;Session=new Chapter4Session{topic=topic,profile=Chapter4Store.Profile,required=topic==0?5:topic==1?11:10};
            if(topic==1&&DifferentiationQuestionBank.Load()!=null){Session.bridgeCurriculumVersion=1;Session.required=11+DifferentiationQuestionBank.Load().questions.Length;}
            if(LearningApi.LocalPlay&&topic==0&&Resources.Load<LimitsQuestionBank>("Production/LimitsIndependentQuestions")!=null){Session.limitsCurriculumVersion=1;Session.required=10;}
            if(LearningApi.LocalPlay&&topic==2&&IntegrationQuestionBank.Load()!=null){Session.integrationCurriculumVersion=1;Session.required=10+IntegrationQuestionBank.Load().questions.Length;}
            SetState("Brief");
        }
        bool Done(string id) => Session.completed.Contains(id);
        public void StartSession(){canvasSessionStarted=true;SetState("Explore");Save();}
        public void OpenPuzzle(){if(Topic==2)World.OpenCase();SetState("Puzzle");}
        public void SetState(string next)
        {
            string previous=State;State=next;bool move=next=="Explore";if(World!=null)World.player.ControlsEnabled=move;
            SceneSoundscape.Active?.StateChanged(previous,next);
            if(World!=null&&World.CaseAudio!=null)World.CaseAudio.SetPaused(next=="Paused"||supportModal);
            Cursor.lockState=move?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!move;
        }
        void Save(){Session.environmentElapsed=caseElapsed;Session.pressureHeld=pressurePaused;if(World!=null&&World.water!=null)Session.waterHeight=World.water.position.y;Chapter4Store.Save(Session);saveAt=Session.elapsed+10;}
        protected void Update()
        {
            if(Session==null||ScreenTransition.IsBusy||PresentationBusy)return;
            if((State=="Explore"||State=="Puzzle"||State=="Finishing") && !supportModal)
            {
                Session.elapsed+=Time.deltaTime;
                if(Topic==2&&World.CaseOpened&&State!="Finishing"){caseElapsed+=Time.deltaTime;if(caseElapsed>=600)Session.safetyMode=true;}
                if(Session.elapsed>=saveAt)Save();
                if(Topic==1&&Stage>=8&&!pressurePaused&&!Ready&&World.water!=null)
                    World.water.position=new Vector3(0,Mathf.Min(-.05f,World.water.position.y+Time.deltaTime*.003f),9.5f);
            }
            if(State=="Finishing")
            {
                float duration=World.CompletionDuration;
                finishTime+=Time.deltaTime;World.Activate(Mathf.Clamp01(finishTime/duration));
                if(finishTime>=duration){Session.status="Completed";Save();SetState("Summary");}return;
            }
            if(!ExternalPlayerInput)HandlePlayerInput();
        }
        public bool ExternalPlayerInput {get;set;}
        public void HandlePlayerInput()
        {
            if(Session==null||State=="Finishing"||ScreenTransition.IsBusy||PresentationBusy)return;
            var key=Keyboard.current;if(key==null)return;
            if(key.escapeKey.wasPressedThisFrame)
            {
                if(UsesCanvas&&ProductionCanvas.Active!=null){ProductionCanvas.Active.HandleEscape();return;}
                if(UsesCanvas&&MissionCanvas.Active!=null){MissionCanvas.Active.HandleEscape();return;}
                if(supportModal){supportModal=false;return;}if(showRetry){showRetry=false;return;}
                if(manual){manual=false;return;}if(settings){settings=false;return;}if(exitConfirm){exitConfirm=false;return;}
                if(State=="Puzzle")SetState("Explore");else if(State=="Explore")SetState("Paused");else if(State=="Paused")SetState("Explore");
            }
            if(State=="Explore"&&NearStation()&&key.eKey.wasPressedThisFrame)OpenPuzzle();
        }
        bool NearStation()=>Vector3.Distance(World.player.transform.position,World.station.position)<
            (World.player.TryGetComponent<PlayerInteraction>(out var interaction)?interaction.interactionDistance:3.8f);
        protected void OnApplicationQuit(){if(Session==null)return;if(UsesCanvas){if(canvasSessionStarted)Save();return;}if(Session.status!="Completed"){Session.status="Abandoned";Save();}}
        protected virtual void OnDestroy(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        public bool ValidateStep(string id,string response,bool correct,string error,string explanation,bool playFeedback=true)
        {
            if(Done(id))return false;
            int before=Session.attempts.Count;bool result=Session.Submit(id,response,correct,error);correctFeedback=result;
            if(Session.attempts.Count>before)Session.attempts.Last().feedback=explanation;
            bool invalid=!correct&&error=="INPUT_FORMAT";
            if(playFeedback&&Session.attempts.Count>before&&!invalid)SceneSoundscape.Active?.Play(result?"correct":"retry");
            feedback=Chapter4Session.FeedbackText(result,error,explanation);
            if(!result&&!invalid){failures++;World.Glitch();if(failures>=3){pressurePaused=true;supportText="Pressure paused. Take your time. "+BasicHint();}}
            else if(result)failures=0;
            showRetry=!result&&!invalid;
            Save();return result;
        }
        public void RequestHint(bool stronger)
        {
            if(!Session.Hint(CurrentStep(),stronger)){supportText="All three Support Hearts used. Basic hints and the manual remain free.";return;}
            supportText=stronger?StrongHint():BasicHint();if(stronger)pressurePaused=true;Save();
        }
        string CurrentStep()=>Topic==0?"L"+(Stage+1):Topic==1?"D"+Stage:"I_MODULE_"+Module;
        string BasicHint()
        {
            if(Topic==0)return new[]{"Compare outputs close to x = 3 from both sides.","Follow nearby graph points. An open point can still mark the limit.","A two-sided limit exists when both sides approach the same value.","The open point describes nearby behaviour; the solid point gives f(2).","Compare the two one-sided values before choosing a conclusion."}[Mathf.Min(Stage,4)];
            if(Topic==1)return "Power rule: multiply the coefficient by the exponent, then subtract one from the exponent. Rewrite roots and reciprocals first.";
            return "For n != -1: integral of x^n dx = x^(n+1)/(n+1) + C. For a definite integral, compute F(upper) - F(lower).";
        }
        string StrongHint()
        {
            if(Topic==0)return new[]{"For x + 2 near x = 3, both sides approach 3 + 2 = 5.","The curve approaches the open point at y = 4 from both directions. Place the marker there.","Left = 3 and right = 3, therefore the two-sided limit is 3.","Read the open point (2,4) for the limit and the filled point (2,7) for f(2).","2 is not 5, so the two-sided limit does not exist. Order: Left-hand limit, !=, Right-hand limit."}[Mathf.Min(Stage,4)];
            if(Topic==1)return "Worked example: d(2x^3)/dx = (2 x 3)x^(3-1) = 6x^2. Rewrite 1/x^2 as x^(-2). A constant has derivative 0.";
            return "Worked example: integral of 2x^2 dx = (2/3)x^3 + C. From 0 to 3 it is (2/3)(27) - 0 = 18, without +C.";
        }
        public void Activate()
        {if(!Ready||(Topic==2&&UsesCanvas&&!World.MasterCoverOpen))return;SetState("Finishing");finishTime=0;Save();}
        void Leave(){Session.status="Abandoned";Save();GameProgress.ReturnToMenu();}
        void ResetInputs(){showRetry=false;a=b=c="";choice=-1;accepted=false;exponentChip=false;feedback=supportText="";wireTarget=-1;card=-1;dragging="";}
        void Advance()
        {
            Stage++;ResetInputs();
            if(Topic==0)World.Restore(Stage);
            if(Topic==1)
            {
                int parts=Stage>=11?4:Stage>=8?3:Stage>=4?2:Stage>=1?1:0;World.Restore(parts,true);World.SetBridgeStage(Stage);
                if(Stage==1||Stage==4||Stage==8){World.station=World.devices[Stage==1?1:2];SetState("Explore");}
            }
        }
        void LegacyOnGUI()
        {
            if(Session==null)return;BeginUI();
            Box(0,0,1280,65,new Color(.012f,.026f,.043f,.97f));Label(28,19,450,32,"CALCULUS LAB  /  ESCAPE",heading);
            Label(770,23,480,25,"MISSION    /    "+(Topic+1).ToString("00"),small);
            if(State=="Brief"){Brief();return;}if(State=="Summary"){Summary();return;}
            if(State=="Puzzle")Box(0,65,1280,655,new Color(.004f,.012f,.022f,.58f));
            Hud();
            if(manual){Manual();return;}if(settings){Settings();return;}if(exitConfirm){ConfirmExit();return;}
            if(State=="Explore")
            {
                Label(630,346,30,30,"+",heading);
                if(NearStation()){Panel(420,568,440,65);Label(447,590,395,30,"E  /  "+(Ready?"ACTIVATE SYSTEM":"OPEN "+(Topic==1?"REPAIR DEVICE":"CONSOLE")),heading);}
                Label(28,680,850,24,"WASD move    /    Mouse look    /    E interact    /    Esc pause",small);
            }
            if(State=="Puzzle")Puzzle();
            if(State=="Paused")Pause();
            if(State=="Finishing"){Panel(320,540,640,115);Label(350,560,580,80,Topic==1?"BRIDGE RESTORED\nThe calf is crossing to its mother.":Topic==0?"PORTAL STABLE\nAll five calibration questions complete.":"DEVICE DEACTIVATED\nAll four modules are stable.",heading);}
            if(manual)Manual();if(settings)Settings();if(exitConfirm)ConfirmExit();
        }
        void Brief()
        {
            Panel(130,105,1020,555);Label(175,135,920,65,Titles[Topic],title);
            string[] brief={"Restore an inactive portal through five limit-calibration questions. Each completed question restores 20% charge, then unlocks the activation lever.","A broken bridge separates a calf from its mother. Restore the Power Core, Converter Box and Bridge Builder, then stabilise the final crossing.","An unstable case contains four integration modules. Complete the guided Exponent Dial, then choose the order of Wires, Sequence and Pressure."};
            Label(175,220,890,88,brief[Topic]);
            Label(175,320,890,90,Topic==0?"PREREQUISITE  /  Functions, coordinates and substitution\nSCOPE  /  Numerical and graphical limits, one-sided values, DNE and limit versus f(a).":Topic==1?"PREREQUISITE  /  Algebra and exponent notation\nSCOPE  /  Power rule, roots and reciprocals, constants and term-by-term differentiation.":"PREREQUISITE  /  Exponents and basic differentiation\nSCOPE  /  Integration power rule, +C, term-by-term integration and definite accumulation.");
            Label(175,425,890,85,"Retries preserve completed work. Basic hints are free. Stronger support costs one of three Support Hearts. Accuracy and completion are recorded separately.\n"+(Topic==2?"The 10-minute countdown enters Safety Mode at zero; play continues.":"No required countdown. Active time excludes pauses."),small);
            if(Button(175,558,380,55,"START SESSION"))StartSession();if(Button(755,558,350,55,"TOPIC SELECTION"))GameProgress.ReturnToMenu();
        }
        void Hud()
        {
            Panel(20,80,310,100);string progress=Topic==0?$"QUESTIONS {Stage}/5  |  CHARGE {Stage*20}%":Topic==1?$"COMPONENTS {(Stage>=8?3:Stage>=4?2:Stage>=1?1:0)}/3  |  STEPS {Session.completed.Count}/11":$"MODULES {ModulesComplete}/4";
            Box(35,176,280*Session.Completion/100f,3,cyan);
            Label(36,89,285,52,progress.Replace("  |  ","\n"),heading);Label(36,151,285,26,$"SUPPORT  {3-Session.heartsUsed}/3   |   {TimeSpan.FromSeconds(Session.elapsed):mm\\:ss}",small);
            if(Topic==2){Panel(935,80,325,78);Label(954,101,290,45,Session.safetyMode?"SAFETY MODE / CONTINUE":$"COUNTDOWN  {TimeSpan.FromSeconds(Mathf.Max(0,600-Session.elapsed)):mm\\:ss}",heading);}
            if(State=="Explore"){Panel(350,82,565,75);Label(369,95,530,58,Ready?"OBJECTIVE / Return to the console and activate the restored system.":Topic==1?"OBJECTIVE / Approach "+(Stage==0?"POWER CORE (left)":Stage<4?"CONVERTER BOX (centre)":"BRIDGE BUILDER (right)"):"OBJECTIVE / Approach the central console.",small);}
        }
        void Puzzle()
        {
            Panel(90,190,1100,500);
            Box(115,625,1050,1,new Color(.13f,.23f,.28f));
            if(Ready)
            {
                Label(130,222,1000,70,Topic==0?"PORTAL CHARGE 100%":Topic==1?"THREE COMPONENTS / CROSSING SECURED":"MODULES 4/4 / MASTER SWITCH UNLOCKED",title);
                Label(130,325,920,70,"All required mathematical steps are complete. Activate the restored system to finish this session.");
                if(Button(370,460,540,65,Topic==0?"PULL ACTIVATION LEVER":Topic==1?"OPEN CROSSING / RESCUE CALF":"LIFT COVER / MASTER DEACTIVATION"))Activate();return;
            }
            if(Topic==0)Limits();else if(Topic==1)Differentiation();else Integration();
            if(!string.IsNullOrEmpty(feedback)){Box(115,506,1050,66,correctFeedback?new Color(.045f,.15f,.13f):new Color(.18f,.11f,.07f));var old=GUI.color;GUI.color=correctFeedback?green:new Color(1,.71f,.42f);Label(125,512,1020,65,feedback);GUI.color=old;}
            Label(125,578,1010,53,supportText,small);
            if(Button(125,640,180,34,"BASIC HINT / FREE"))RequestHint(false);
            if(Button(320,640,255,34,"STRONGER SUPPORT / 1 HEART",Session.heartsUsed<3))RequestHint(true);
            if(Topic==2&&Button(590,640,180,34,"OPEN MANUAL"))manual=true;
            if(Button(970,640,180,34,"RETURN TO WORLD"))SetState("Explore");
        }
        static bool Number(string value,float target)
        {
            if(value==null)return false;value=value.Trim();var fraction=value.Split('/');
            if(fraction.Length==2&&float.TryParse(fraction[0],NumberStyles.Float,CultureInfo.InvariantCulture,out var n)&&float.TryParse(fraction[1],NumberStyles.Float,CultureInfo.InvariantCulture,out var d)&&d!=0)return Mathf.Abs(n/d-target)<.005f;
            return float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var v)&&!float.IsNaN(v)&&!float.IsInfinity(v)&&Mathf.Abs(v-target)<.005f;
        }
        public static bool NumericAnswer(string value,float target)=>Number(value,target);
        void Limits()
        {
            Label(125,207,1010,36,$"QUESTION {Stage+1}/5  /  "+new[]{"NUMERICAL APPROACH","GRAPHICAL LIMIT","ONE-SIDED AND TWO-SIDED LIMITS","LIMIT VERSUS FUNCTION VALUE","CONCLUSION AND REASON"}[Stage],heading);
            bool valid=false;string response="",error="",explanation="";
            if(Stage==0)
            {
                Label(125,251,850,32,"Find lim (x + 2) as x approaches 3.");
                Label(145,302,620,104,"x          2.9       2.99       3.01       3.1\nf(x)      4.9       4.99       5.01       5.1");
                Label(785,289,250,27,"Approached value");a=Field(785,328,230,a);valid=Number(a,5);response=a;error="NUMERICAL_APPROACH";explanation="Values from both sides approach 5; the limit is 5.";
                Label(140,415,120,25,"Scratchpad",small);scratch=GUI.TextArea(new Rect(260,407,720,62),scratch,300,small);
            }
            if(Stage==1)
            {
                Label(125,247,1010,30,"Move the graph probe from either side of x = 2; place the answer marker on the y-scale.");
                Graph(false);Label(740,290,320,25,"Probe position x = "+probe.ToString("0.00"),small);
                probe=Slider(new Rect(740,329,330,24),probe,0,4);
                Label(740,371,360,35,"Answer marker  y = "+marker.ToString("0"),heading);
                marker=Mathf.Round(Slider(new Rect(740,421,330,24),marker,0,8));
                valid=marker==4;response="marker="+marker;error="GRAPH_LIMIT";explanation="The nearby curve approaches y = 4. The open point does not remove the limit.";
            }
            if(Stage==2)
            {
                Label(125,251,1000,60,"x < 2: f(x) = x + 1        |        x > 2: f(x) = 5 - x\nAs x approaches 2, compare the values approached from each side.");
                Label(155,340,275,30,"Left-hand limit");Label(480,340,275,30,"Right-hand limit");Label(805,340,300,30,"Two-sided limit");
                a=Field(155,383,250,a);b=Field(480,383,250,b);c=Field(805,383,250,c);
                valid=Number(a,3)&&Number(b,3)&&Number(c,3);response=a+" | "+b+" | "+c;error="ONE_SIDED_MISMATCH";
                explanation=!Number(a,3)?"Inspect LEFT: x + 1 approaches 3 as x approaches 2.":!Number(b,3)?"Inspect RIGHT: 5 - x approaches 3 as x approaches 2.":"Both sides approach 3, so the two-sided limit is 3.";
            }
            if(Stage==3)
            {
                Graph(true);Label(720,253,400,58,"Open point (2,4), filled point (2,7).\nEnter the two different values.");
                Label(720,331,180,30,"Limit as x -> 2");a=Field(925,324,165,a);Label(720,400,180,30,"Function value f(2)");b=Field(925,393,165,b);
                valid=Number(a,4)&&Number(b,7);response=a+" | "+b;error="LIMIT_VS_FUNCTION_VALUE";explanation="Nearby values approach 4; the filled point sets f(2) = 7.";
            }
            if(Stage==4)
            {
                Label(125,252,990,35,"Left-hand limit = 2    |    Right-hand limit = 5. Choose a conclusion and order the reason.");
                string[] options={"Limit = 2","Limit = 5","Limit does not exist"};for(int i=0;i<3;i++)if(Button(135+i*330,298,310,42,(choice==i?"[SELECTED] ":"")+options[i]))choice=i;
                string[] reasons={"Left-hand limit","!=","Right-hand limit"};
                for(int i=0;i<3;i++)if(Button(135+i*330,358,310,38,reasons[i]))card=i;
                for(int i=0;i<3;i++)if(Button(135+i*330,416,310,38,order[i]<0?"Place selected card here":reasons[order[i]])){if(card>=0){for(int j=0;j<3;j++)if(order[j]==card)order[j]=-1;order[i]=card;}}
                valid=choice==2&&order[0]==0&&order[1]==1&&order[2]==2;response="conclusion="+choice+";reason="+string.Join(",",order.Take(3));error="UNEQUAL_SIDES_DNE";explanation="Left-hand limit != Right-hand limit, so the two-sided limit does not exist.";
            }
            if(accepted){if(Button(860,464,275,38,"CONTINUE"))Advance();}
            else if(Button(860,464,275,38,"SUBMIT ANSWER"))accepted=ValidateStep("L"+(Stage+1),response,valid,error,explanation);
        }
        void Graph(bool filled)
        {
            float gx=175,gy=457,w=460,h=168;
            for(int i=0;i<=8;i++){float y=gy-i*h/8;Line(new Vector2(gx,y),new Vector2(gx+w,y),new Color(.13f,.2f,.24f),1);Label(gx-28,y-9,24,20,i.ToString(),small);}
            Line(new Vector2(gx,gy),new Vector2(gx+w,gy),muted);Line(new Vector2(gx,gy),new Vector2(gx,gy-h),muted);
            for(int i=0;i<4;i++){float x=gx+i*w/4;Label(x,gy+2,60,22,i.ToString(),small);}
            Vector2 prev=Vector2.zero;for(int i=0;i<=40;i++){float x=i/10f;float y=4+.45f*(x-2)*(x-2);Vector2 point=new Vector2(gx+x*w/4,gy-y*h/8);if(i>0)Line(prev,point,cyan,3);prev=point;}
            Point(gx+w/2,gy-4*h/8,cyan,true);if(filled)Point(gx+w/2,gy-7*h/8,green);
            else{float x=Mathf.Clamp(probe,0,4);Point(gx+x*w/4,gy-(4+.45f*(x-2)*(x-2))*h/8,green);Line(new Vector2(gx,gy-marker*h/8),new Vector2(gx+w,gy-marker*h/8),new Color(1,.65f,.2f));}
        }
        readonly string[] dTitles={"POWER CORE / INSTALL POWER REGULATOR","CONVERTER / REWRITE ROOT","CONVERTER / DIFFERENTIATE ROOT","CONVERTER / DIFFERENTIATE RECIPROCAL","BRIDGE BUILDER / FIRST TERM","BRIDGE BUILDER / SECOND TERM","BRIDGE BUILDER / CONSTANT","INSTALL BRIDGE ANCHOR","RIVER CHALLENGE / REWRITE","RIVER CHALLENGE / TERM BY TERM","RIVER CHALLENGE / FINAL ASSEMBLY"};
        void Differentiation()
        {
            Label(125,208,1020,35,dTitles[Stage],heading);
            bool valid=false;string response="",error="POWER_RULE",explanation="";
            if(Stage==0)
            {
                Label(125,250,950,55,"d(x^4)/dx: drag the exponent 4 into the coefficient slot, then turn the exponent dial down by one.");
                Chip(new Rect(180,330,180,58),"4","exponent");
                var slot=new Rect(460,330,230,58);if(Button(slot.x,slot.y,slot.width,slot.height,exponentChip?"COEFFICIENT = 4":"DROP 4 HERE"))exponentChip=true;
                if(Event.current.type==EventType.MouseUp&&slot.Contains(Event.current.mousePosition)&&dragging=="exponent"){exponentChip=true;dragging="";Event.current.Use();}
                if(a=="")a="4";Dial(785,325,"Exponent",ref a,-6,8);valid=exponentChip&&Number(a,3);response="coefficient="+(exponentChip?4:0)+";exponent="+a;explanation="Bring 4 down as the coefficient, then 4 - 1 = 3. The derivative is 4x^3.";
            }
            else
            {
                string[] prompts={"","Rewrite sqrt(x) = x^n. Set n.","Differentiate x^(1/2). Set coefficient and exponent.","Differentiate 3/x^6 = 3x^(-6). Set coefficient and exponent.","For x^5 + 2x^(-3) + 7, build the derivative of x^5.","Now build the derivative of 2x^(-3).","Differentiate the constant 7. Enter its derivative.","Combine the sections. Select the complete derivative to install the Bridge Anchor.","Rewrite 1/x^2 as x^n for the final expression x^4 - 2x + 1/x^2.","Differentiate -2x. Enter its coefficient (the exponent becomes 0).","Assemble d(x^4 - 2x + x^(-2))/dx to steady the river."};
                Label(125,254,995,60,prompts[Stage]);
                if(Stage==7||Stage==10)
                {
                    var options=Stage==7?new[]{"5x^4 - 6x^(-4)","5x^4 + 2x^(-4) + 7","x^4 - 6x^(-2)"}:new[]{"4x^3 - 2 - 2x^(-3)","4x^3 - 2 + 2x^(-1)","x^3 - 2x - 2x^(-3)"};
                    for(int i=0;i<3;i++)if(Button(150+i*330,342,310,73,(choice==i?"[SELECTED] ":"")+options[i]))choice=i;
                    valid=choice==0;response=choice<0?"":options[choice];explanation=Stage==7?"Differentiate each term: 5x^4, -6x^(-4), and 0. The anchor is ready.":"Power rule gives 4x^3 - 2 - 2x^(-3). All repaired sections are stable.";
                }
                else
                {
                    Label(170,331,310,27,Stage==1||Stage==8?"Exponent (fractions allowed)":"Coefficient / result");a=Field(170,374,310,a);
                    bool pair=Stage==2||Stage==3||Stage==4||Stage==5;
                    if(pair){Label(625,331,310,27,"New exponent");b=Field(625,374,310,b);}
                    float[] ca={0,.5f,.5f,-18,5,-6,0,0,-2,-2,0};float[] cb={0,0,-.5f,-7,4,-4,0,0,0,0,0};
                    valid=Number(a,ca[Stage])&&(!pair||Number(b,cb[Stage]));response=a+" | "+b;
                    error=Stage==1||Stage==8?"EXPONENT_REWRITE":Stage==6?"CONSTANT_DERIVATIVE":"POWER_RULE";
                    explanation=Stage==1?"sqrt(x) = x^(1/2). The rewrite lock opens.":Stage==8?"1/x^2 = x^(-2). A reciprocal uses a negative exponent.":Stage==6?"A constant does not change, so d(7)/dx = 0.":"Multiply the coefficient by the old exponent, then subtract 1 from the exponent.";
                }
            }
            if(accepted){if(Button(815,459,320,42,Stage==0?"INSTALL REGULATOR":Stage==3?"INSTALL BRIDGE COMPONENT":Stage==7?"INSTALL BRIDGE ANCHOR":"PLACE / CONTINUE"))Advance();}
            else if(Button(815,459,320,42,"TEST REPAIR"))accepted=ValidateStep("D"+Stage,response,valid,error,explanation);
            if(dragging!=""&&Event.current.type==EventType.Repaint)Label(Event.current.mousePosition.x+10,Event.current.mousePosition.y,120,40,"4",heading);
            if(Event.current.type==EventType.MouseUp)dragging="";
        }
        void Chip(Rect rect,string label,string id)
        {
            GUI.Box(rect,label,button);if(Event.current.type==EventType.MouseDown&&rect.Contains(Event.current.mousePosition)){dragging=id;Event.current.Use();}
        }
        void Dial(float x,float y,string label,ref string value,int min,int max)
        {
            Label(x,y-30,250,30,label,small);if(!int.TryParse(value,out int n))n=min;
            if(Button(x,y,45,48,"-"))n=Mathf.Max(min,n-1);Label(x+68,y+8,100,40,n.ToString(),heading);if(Button(x+155,y,45,48,"+"))n=Mathf.Min(max,n+1);value=n.ToString();
        }
        void Integration()
        {
            string[] names={"EXPONENT DIAL","WIRE MATCHING","SYMBOL SEQUENCE","PRESSURE GAUGE"};
            for(int i=0;i<4;i++)if(Button(125+i*258,207,247,37,(Module==i?"> ":"")+names[i],i==0||Done("I_DIAL"))){Module=i;ResetInputs();}
            if(Module==0)IntegrationDial();if(Module==1)Wires();if(Module==2)Sequence();if(Module==3)Gauge();
            World.Restore(ModulesComplete);
        }
        void IntegrationDial()
        {
            Label(130,257,980,42,"Integral of x^4 dx. Raise the exponent, divide by the new exponent, then insert one +C chip.");
            if(a=="")a="4";if(b=="")b="1";
            Dial(175,351,"New exponent",ref a,0,8);Dial(535,351,"Divisor",ref b,1,8);
            if(Button(860,348,230,52,c=="C"?"+C INSERTED":"INSERT +C"))c=c=="C"?"":"C";
            if(Button(840,459,295,42,Done("I_DIAL")?"MODULE STABLE":"TEST DIALS",!Done("I_DIAL")))
                ValidateStep("I_DIAL",a+" | "+b+" | "+c,Number(a,5)&&Number(b,5)&&c=="C","INTEGRATION_POWER_OR_C","The antiderivative is x^5/5 + C. Other modules are now unlocked.");
        }
        void Wires()
        {
            Label(130,255,1010,30,"Integral of (3x^2 + 4x - 5) dx. Drag each wire to a socket, or select a source then a socket.");
            string[] inputs={"3x^2","4x","-5"},outputs={"x^3","2x^2","-5x","3x^3","4x^2"};
            for(int i=0;i<3;i++)
            {
                var r=new Rect(155,302+i*55,170,40);
                if(Event.current.type==EventType.MouseDown&&r.Contains(Event.current.mousePosition)&&!Done("I_W"+i)){draggedWire=wire=i;}
                if(Button(r.x,r.y,r.width,r.height,Done("I_W"+i)?inputs[i]+" / LOCKED":inputs[i],!Done("I_W"+i)))wire=i;
                if(Done("I_W"+i))Line(new Vector2(325,r.center.y),new Vector2(845,316+i*37),green,3);
            }
            for(int i=0;i<5;i++)
            {
                var r=new Rect(845,294+i*37,250,32);bool drop=Event.current.type==EventType.MouseUp&&r.Contains(Event.current.mousePosition)&&draggedWire>=0;
                if(Button(r.x,r.y,r.width,r.height,outputs[i])||drop)
                {
                    if(drop){wire=draggedWire;draggedWire=-1;}
                    if(wire>=0&&!Done("I_W"+wire))
                    {ValidateStep("I_W"+wire,inputs[wire]+" -> "+outputs[i],wire==i,"TERM_ANTIDERIVATIVE","Raise the term's exponent by 1 and divide its coefficient by that new exponent. Correct wires remain locked.");if(Done("I_W"+wire))wire=-1;}
                }
            }
            if(wire>=0)Label(365,363,440,45,"Selected source: "+inputs[wire]+"  /  choose an output",heading);
            if(draggedWire>=0)Line(new Vector2(325,322+draggedWire*55),Event.current.mousePosition,cyan,3);
            if(Button(390,459,405,40,Done("I_C")?"COMMON +C CONNECTED":"ADD ONE +C TO COMMON OUTPUT",Done("I_W0")&&Done("I_W1")&&Done("I_W2")&&!Done("I_C")))
                ValidateStep("I_C","C",true,"MISSING_C","One common +C completes x^3 + 2x^2 - 5x + C.");
            if(Event.current.type==EventType.MouseUp)draggedWire=-1;
        }
        void Sequence()
        {
            Label(130,256,980,32,"Integral of sqrt(x) dx. Select a card, place it into a slot, then enter the value for that step.");
            for(int i=0;i<4;i++)if(Button(140+i*252,302,238,35,(card==i?"> ":"")+cards[i]))card=i;
            for(int i=0;i<4;i++)
            {
                if(Button(140+i*252,354,238,40,Done("I_SEQ_"+i)?"LOCKED / "+cards[i]:order[i]<0?"SLOT "+(i+1):cards[order[i]],!Done("I_SEQ_"+i))&&card>=0)
                {bool locked=false;for(int j=0;j<4;j++)if(order[j]==card&&Done("I_SEQ_"+j))locked=true;if(!locked){for(int j=0;j<4;j++)if(order[j]==card)order[j]=-1;order[i]=card;}}
            }
            int next=0;while(next<4&&Done("I_SEQ_"+next))next++;
            if(next==4){Label(170,421,940,45,"OUTPUT / (2/3)x^(3/2) + C. Sequence module stable.",heading);return;}
            Label(145,420,590,55,"Step "+(next+1)+" value: "+new[]{"rewritten exponent","raised exponent","new coefficient","constant chip (C)"}[next]);a=Field(720,419,185,a);
            if(Button(930,419,180,42,"RUN NEXT STEP"))
            {
                bool v=next==3?a.Trim().Equals("C",StringComparison.OrdinalIgnoreCase):Number(a,next==0?.5f:next==1?1.5f:2f/3);
                if(ValidateStep("I_SEQ_"+next,"card="+order[next]+";value="+a,order[next]==next&&v,"SEQUENCE_OR_VALUE","Use Rewrite -> Raise -> Divide -> Add C. Values are 1/2, 3/2, 2/3, C."))a="";
            }
        }
        void Gauge()
        {
            Label(130,257,990,53,"Find the stored energy: integral from 0 to 2 of 2x dx. Set the release amount; no +C is needed.");
            float pressure=Done("I_GAUGE")?.15f:Session.safetyMode||pressurePaused?.65f:Mathf.Clamp(.35f+Session.elapsed/900,.35f,.95f);
            Box(160,330,455,40,new Color(.12f,.19f,.23f));Box(160,330,455*pressure,40,Done("I_GAUGE")?green:new Color(.9f,.49f,.19f));
            Label(160,387,485,50,Done("I_GAUGE")?"PRESSURE SAFE":Session.safetyMode?"SAFETY MODE / PRESSURE HELD":"LOW                  PRESSURE                  HIGH",small);
            if(a=="")a="0";Dial(815,347,"Release amount",ref a,0,10);
            if(Button(805,459,330,42,Done("I_GAUGE")?"MODULE STABLE":"PULL RELEASE LEVER",!Done("I_GAUGE")))
                ValidateStep("I_GAUGE",a,Number(a,4),"DEFINITE_ACCUMULATION",Number(a,4)?"F(x) = x^2. F(2) - F(0) = 4. Pressure is safe.":Number(a,0)||int.Parse(a)<4?"Too little: energy remains. Evaluate x^2 at 2, then subtract its value at 0.":"Too much: safety valve locked. Evaluate F(2) - F(0) and adjust the release.");
        }
        void Pause()
        {
            Panel(390,190,500,490);Label(430,225,420,60,"SESSION PAUSED",title);
            if(Button(435,318,410,48,"RESUME"))SetState("Explore");
            if(Button(435,380,410,48,"SETTINGS"))settings=true;
            if(Button(435,442,410,48,"RESTART SESSION")){Session.status="Abandoned";Save();GameManager.ChangeScene(Scenes[Topic]);}
            if(Button(435,504,410,48,"RETURN TO MAIN MENU"))exitConfirm=true;
            Label(435,582,410,60,"Your timer and environmental pressure are paused.",small);
        }
        void Settings()
        {
            Panel(300,190,680,460);Label(345,225,600,50,"SETTINGS",title);
            Label(345,306,570,30,"Master volume",heading);AudioListener.volume=Slider(new Rect(345,355,560,30),AudioListener.volume,0,1);
            if(Button(345,420,560,48,Screen.fullScreen?"SWITCH TO WINDOWED":"SWITCH TO FULLSCREEN"))Screen.fullScreen=!Screen.fullScreen;
            if(Button(345,534,560,48,"SAVE / BACK")){PlayerPrefs.SetFloat("C4Volume",AudioListener.volume);PlayerPrefs.Save();settings=false;}
        }
        void ConfirmExit()
        {
            Panel(335,260,610,280);Label(375,290,530,65,"Leave this session? Its attempts and partial completion will be saved.");
            if(Button(375,430,230,50,"SAVE AND LEAVE"))Leave();if(Button(665,430,230,50,"CANCEL"))exitConfirm=false;
        }
        void Manual()
        {
            Panel(205,170,870,505);Label(245,203,780,50,"INTEGRATION MANUAL",title);
            Label(245,280,780,270,"POWER RULE (n != -1)\nIntegral of ax^n dx = a x^(n+1)/(n+1) + C.\n\nWORKED EXAMPLE\nIntegral of 6x^2 dx = 2x^3 + C.\n\nDEFINITE ACCUMULATION\nFind F(x), then compute F(b) - F(a). The answer is a number; omit +C.\n\nROOTS\nsqrt(x) = x^(1/2). Keep fractions exact while calculating.");
            if(Button(705,599,310,44,"CLOSE MANUAL"))manual=false;
        }
        void Summary()
        {
            Box(0,65,1280,655,new Color(.004f,.012f,.022f,.7f));
            Panel(160,115,960,555);
            Label(205,147,870,25,"SESSION COMPLETE / SYSTEM RESTORED",new GUIStyle(small){normal={textColor=green}});
            Label(205,194,870,56,"Good work, trainee.",title);
            Label(205,254,870,30,Titles[Topic],heading);
            string[] values={Session.Accuracy<0?"--":Session.Accuracy.ToString("0")+"%",Session.Completion.ToString("0")+"%",Session.MathematicalIncorrectCount.ToString(),Session.heartsUsed.ToString()+" / 3"};
            string[] labels={"ACCURACY","COMPLETION","INCORRECT ATTEMPTS","SUPPORT HEARTS USED"};
            for(int i=0;i<4;i++)
            {
                float x=205+i*220;Panel(x,310,205,108);Label(x+18,326,170,23,labels[i],new GUIStyle(small){fontSize=12});
                Label(x+18,359,170,48,values[i],new GUIStyle(title){fontSize=30,normal={textColor=i==1?green:Color.white}});
            }
            Label(205,440,870,30,Session.Band,heading);
            Label(205,480,870,58,"Active time  "+System.TimeSpan.FromSeconds(Session.elapsed).ToString(@"mm\:ss")+"     /     Safety support  "+(Session.safetyMode?"Used":"Not needed")+"\nReview focus: "+(Session.CommonError=="NONE"?"No recorded errors. Keep practising to reinforce the rules.":Session.CommonError.Replace('_',' ')),small);
            if(Button(205,579,390,48,"PRACTISE AGAIN"))GameManager.ChangeScene(Scenes[Topic]);if(Button(635,579,435,48,"DASHBOARD / MAIN MENU"))GameProgress.ReturnToMenu();
            if(Chapter4Store.LastError!="")Label(205,638,870,28,Chapter4Store.LastError,small);
        }
    }
}
