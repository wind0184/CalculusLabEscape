#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    // One-time authoring of existing controls. Never called by normal Play/startup.
    public static partial class ReportFusionLayout
    {
        const string Prefabs="Assets/_Project/Prefabs/Resources/Production/";
        const string Art="Assets/_Project/Art/UI/ReportFusion/";
        static readonly string[] Names={"MainMenuUI","LimitsUI","DifferentiationUI","IntegrationUI"};
        static readonly Color Background=new Color(.035f,.08f,.105f),Panel=new Color(.07f,.14f,.18f),Raised=new Color(.10f,.20f,.25f),Ink=new Color(.91f,.95f,.96f),Muted=new Color(.68f,.77f,.81f),Cyan=new Color(.48f,.85f,.90f),Line=new Color(.18f,.29f,.34f);
        static string Evidence=>string.IsNullOrEmpty(LearningApi.Argument("--report-ui-evidence"))?Path.GetFullPath("../../qa/report-ui-2026-10-23"):LearningApi.Argument("--report-ui-evidence");
        static Dictionary<string,Transform> nodes;
        static TMP_FontAsset font;
        static readonly RenderTexture[] previews=new RenderTexture[3];
        static readonly List<string> report=new List<string>();
        static bool refining;
        static Transform Find(string name)=>nodes.TryGetValue(name,out var value)?value:null;
        static RectTransform Rect(string name)=>Find(name) as RectTransform;
        static void Index(GameObject root){nodes=root.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());font=root.GetComponentsInChildren<TMP_Text>(true).First().font;}
        static RectTransform Create(Transform parent,string name,float x,float y,float w,float h)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);nodes[name]=rect;Place(rect,x,y,w,h);return rect;
        }
        static void Place(RectTransform rect,float x,float y,float w,float h)
        {
            if(rect==null)return;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);
        }
        static void At(string name,float x,float y,float w,float h,Transform parent=null)
        {
            var rect=Rect(name);if(rect==null)return;if(parent!=null)rect.SetParent(parent,false);Place(rect,x,y,w,h);
        }
        static Image Surface(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var rect=Find(name) as RectTransform;if(rect==null)rect=Create(parent,name,x,y,w,h);else{rect.SetParent(parent,false);Place(rect,x,y,w,h);}
            var image=rect.GetComponent<Image>()??rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
        }
        static TMP_Text Label(Transform parent,string name,string text,float x,float y,float w,float h,float size,Color color)
        {
            var rect=Find(name) as RectTransform;if(rect==null)rect=Create(parent,name,x,y,w,h);else{rect.SetParent(parent,false);Place(rect,x,y,w,h);}
            var label=rect.GetComponent<TMP_Text>()??rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.text=text;label.fontSize=size;label.color=color;label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.Normal;label.alignment=TextAlignmentOptions.TopLeft;label.enableAutoSizing=true;label.fontSizeMin=Mathf.Max(13,size-5);label.fontSizeMax=size;return label;
        }
        static void Type(string name,float size,Color? color=null)
        {
            var label=Find(name)?.GetComponent<TMP_Text>();if(label==null)return;label.fontSize=size;label.fontSizeMax=size;label.fontSizeMin=Mathf.Max(13,size-5);label.enableAutoSizing=true;if(color.HasValue)label.color=color.Value;
        }
        static void Text(string name,string text){var label=Find(name)?.GetComponent<TMP_Text>();if(label!=null)MathText.Set(label,text);}
        static void StretchChildren(string name)
        {
            var rect=Rect(name);if(rect==null)return;
            foreach(var label in rect.GetComponentsInChildren<TMP_Text>(true))
            {
                if(label.GetComponentInParent<TMP_InputField>()!=null)continue;
                var r=label.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=new Vector2(10,3);r.offsetMax=new Vector2(-10,-3);
                label.enableAutoSizing=true;label.fontSizeMax=Mathf.Min(label.fontSize,20);label.fontSizeMin=13;
            }
        }
        static void Button(string name,float x,float y,float w,float h,Transform parent=null,bool primary=false)
        {
            At(name,x,y,w,h,parent);var button=Find(name)?.GetComponent<Button>();if(button==null)return;
            var image=button.GetComponent<Image>();if(image!=null){image.color=primary?Cyan:Raised;image.raycastTarget=true;}
            var outline=button.GetComponent<Outline>();if(outline!=null){outline.effectColor=primary?Cyan:Line;outline.effectDistance=new Vector2(1,-1);}
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.85f,.97f,1);colors.pressedColor=new Color(.68f,.85f,.9f);colors.disabledColor=new Color(.45f,.53f,.57f,.65f);button.colors=colors;
            StretchChildren(name);foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))label.color=primary?Background:Ink;
        }
        static void Input(string name,float x,float y,float w,float h)
        {
            At(name,x,y,w,h);var input=Find(name)?.GetComponent<TMP_InputField>();if(input==null)return;var image=input.GetComponent<Image>();if(image!=null)image.color=Raised;
            var view=input.textViewport;view.anchorMin=Vector2.zero;view.anchorMax=Vector2.one;view.offsetMin=new Vector2(11,5);view.offsetMax=new Vector2(-11,-5);
            foreach(var label in view.GetComponentsInChildren<TMP_Text>(true)){label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;label.fontSize=19;label.enableAutoSizing=false;label.color=label==input.textComponent?Ink:Muted;}
        }
        static void Common(GameObject root)
        {
            var scaler=root.GetComponent<CanvasScaler>();scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            foreach(var page in Find("Pages").Cast<Transform>()){var image=page.GetComponent<Image>();if(image!=null&&page.name!="World"&&page.name!="Finishing")image.color=Background;}
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if(text.GetComponentInParent<TMP_InputField>()!=null)continue;
                // Preserve meaningful warning/accepted colours; replace pale legacy surfaces separately.
                if(text.color.r>.8f&&text.color.g>.8f&&text.color.b>.8f)text.color=Ink;
            }
            if(Find("SimulationAnswerArt")!=null)Find("SimulationAnswerArt").gameObject.SetActive(false);
            At("Header",0,0,1280,56);var header=Find("Header").GetComponent<Image>();if(header!=null)header.color=Panel;
            At("Brand",24,15,470,27);Type("Brand",19,Ink);At("Section",675,18,455,22);Type("Section",13,Muted);
            At("HeaderRule",0,55,1280,1);At("MissionEmblem",620,14,32,28);
            At("LiveStatus",24,692,760,22);Type("LiveStatus",14,Muted);At("SaveStatus",700,58,550,24);Type("SaveStatus",14);
            if(Find("LiveStatus")!=null)Surface(Find("DesignFrame"),"ReportFooter",0,680,1280,40,Panel).transform.SetAsFirstSibling();
        }
        static void Workbench(GameObject root,int topic)
        {
            var puzzle=Find("Puzzle");var mission=root.GetComponent<MissionCanvas>();
            Surface(puzzle,"ReportWorkbench",0,250,1280,430,Panel).transform.SetAsFirstSibling();
            Surface(puzzle,"ReportStepSurface",0,250,238,430,Panel).transform.SetSiblingIndex(1);
            Surface(puzzle,"ReportResultSurface",980,250,300,430,Panel).transform.SetSiblingIndex(2);
            Surface(puzzle,"ReportStepDivider",237,250,1,430,Line).transform.SetSiblingIndex(3);
            Surface(puzzle,"ReportResultDivider",979,250,1,430,Line).transform.SetSiblingIndex(4);
            Label(puzzle,"ReportStepHeading","YOUR CURRENT STEP",24,275,194,23,14,Cyan);
            Label(puzzle,"ReportResultHeading","RESULT / FEEDBACK",1002,278,254,23,14,Cyan);
            var state=Label(puzzle,"ReportResultState","Preview your work, then check it.\nYou can revise an unsuccessful attempt.",1002,404,254,47,16,Muted);
            Label(puzzle,"ReportAssistantName","LABORATORY AI",350,608,420,22,13,Ink);
            Label(puzzle,"ReportAssistantNote","A little support, whenever you need it.",350,630,540,20,14,Muted);
            var portrait=Surface(puzzle,"ReportAssistantPortrait",266,599,68,78,Color.white);portrait.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Think.png");portrait.preserveAspect=true;
            Button(topic==0?"PuzzleSupport":"SupportOpen",350,651,220,26,null,false);
            var view=root.GetComponent<ReportFusionView>()??root.AddComponent<ReportFusionView>();view.resultState=state;
            RectTransform device;
            if(topic>0){At("DevicePanel",0,56,1280,194);device=Rect("DevicePanel");device.GetComponent<Image>().color=Color.clear;At("ScenePreview",0,0,1280,194,device);view.scenePreview=Find("ScenePreview").GetComponent<RawImage>();}
            else{device=Rect("ReportSceneStrip")??Create(puzzle,"ReportSceneStrip",0,56,1280,194);device.SetSiblingIndex(5);var rawRect=Rect("ReportScenePreview")??Create(device,"ReportScenePreview",0,0,1280,194);var raw=rawRect.GetComponent<RawImage>()??rawRect.gameObject.AddComponent<RawImage>();raw.raycastTarget=false;view.scenePreview=raw;}
            view.scenePreview.texture=previews[topic];view.scenePreview.raycastTarget=false;
            Surface(device,"ReportSceneShade",18,124,670,57,new Color(.025f,.08f,.11f,.88f));
            Surface(device,"ReportSceneTitleShade",18,12,670,44,new Color(.025f,.08f,.11f,.88f)).transform.SetSiblingIndex(1);
            if(topic>0)Surface(device,"ReportSceneInfoShade",710,12,546,166,new Color(.025f,.08f,.11f,.88f)).transform.SetSiblingIndex(2);
            Label(device,"ReportSceneTitle",new[]{"THE PORTAL EXAMINATION","THE BROKEN BRIDGE RESCUE","THE INTEGRATION CONTAINMENT CASE"}[topic],24,18,780,27,17,Ink);
            view.progressSegments=new Image[topic==0?5:topic==1?3:4];
            for(int i=0;i<view.progressSegments.Length;i++)view.progressSegments[i]=Surface(device,"ReportProgressSegment"+i,25+i*50,172,44,4,Line);
            Button("PuzzlePause",1160,12,96,32,Find("Header"));
            Button("PuzzleWorld",1052,686,204,28);Type("PuzzleWorldLabel",15,Muted);
            At("Feedback",1002,461,254,124);Type("Feedback",18);
            if(topic>0)
            {
                At("MissionProgress",24,193,620,29,puzzle);Type("MissionProgress",23,Ink);
                At("SecondaryProgress",730,79,500,24,puzzle);Type("SecondaryProgress",16,Muted);
                for(int i=0;i<3;i++){At("Component"+i,938,112+i*27,306,23,puzzle);Type("Component"+i,16);}
                At("EnvironmentStatus",730,217,500,25,puzzle);Type("EnvironmentStatus",16,Muted);
                At("TaskTitle",24,312,194,87);Type("TaskTitle",24,Ink);
                At("TaskPrompt",266,278,686,70);Type("TaskPrompt",22,Ink);
                At("NextAction",24,422,194,176);Type("NextAction",18,Cyan);
                Button("ControlsOpen",24,626,194,35);Button("ManualOpen",24,585,194,33);
                Button("CaseBack",1060,213,196,30);
                if(Find("CaseGuidance")!=null)Find("CaseGuidance").gameObject.SetActive(false);
                At("JourneyHeading",266,253,686,22);Type("JourneyHeading",14,Cyan);
                At("JourneyDetail",1002,334,254,60);Type("JourneyDetail",16,Muted);
                for(int i=0;i<3;i++){At("JourneyPhase"+i,266+i*231,353,224,25);At("JourneyPhaseLabel"+i,7,3,210,19);Type("JourneyPhaseLabel"+i,13);}
                MissionControls(topic);
            }
            else LimitsControls();
            AssistantDock(topic);
        }
        static void AssistantDock(int topic)
        {
            // The sidebar keeps support separate from answer controls and notes.
            At("ReportAssistantPortrait",22,576,76,88);
            Find("ReportAssistantPortrait").GetComponent<Image>().raycastTarget=false;
            At("ReportAssistantName",104,581,116,32);Type("ReportAssistantName",12,Ink);
            foreach(string name in new[]{"ReportAssistantNote","ReportAssistantPrompt"})if(Find(name)!=null)Find(name).gameObject.SetActive(false);
            Button(topic==0?"PuzzleSupport":"SupportOpen",104,619,116,36);
            if(topic>0){Button("ManualOpen",266,633,320,34);Button("ControlsOpen",620,633,332,34);}
            else PortalContrast();
        }
        static void PortalContrast()
        {
            roundSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Rounded.png");
            var card=Surface(Find("Puzzle"),"PortalCard",1012,68,244,176,new Color(.035f,.10f,.14f,.97f));
            Rounded("PortalCard");card.raycastTarget=false;
            card.transform.SetSiblingIndex(Find("ReportSceneStrip").GetSiblingIndex()+1);
            At("PuzzlePortal",1054,76,160,136);At("ChargeLabel",1066,118,136,46);Type("ChargeLabel",34,Cyan);
            At("PortalCaption",1030,215,208,23);Type("PortalCaption",14,Ink);
        }
        static void Submit(string name)=>Button(name,1002,613,254,44,null,true);
        static void Dial(string prefix,float x,float y,float width)
        {
            At(prefix+"Caption",x,y-30,width,25);Type(prefix+"Caption",16,Muted);
            Button(prefix+"Minus",x,y,48,45);At(prefix+"Value",x+57,y+2,width-114,41);Type(prefix+"Value",29,Ink);Button(prefix+"Plus",x+width-48,y,48,45);
        }
        static void MissionControls(int topic)
        {
            At("PowerExpression",266,359,686,58);Type("PowerExpression",37,Ink);Text("PowerExpression","f(x) = x^4");
            Button("BridgeChip",288,453,74,56,null,true);Button("CoefficientSlot",392,453,242,56);
            Dial("Exponent",698,453,254);
            At("PowerPreview",1002,328,254,65);Type("PowerPreview",39,Cyan);
            At("PowerInstructions",266,529,686,41);Type("PowerInstructions",18,Muted);
            At("BridgeALabel",266,406,320,29);Type("BridgeALabel",18,Muted);Input("BridgeA",266,444,320,50);
            At("BridgeBLabel",620,406,332,29);Type("BridgeBLabel",18,Muted);Input("BridgeB",620,444,332,50);
            At("AnswerKeyboardHelp",266,521,686,64);Type("AnswerKeyboardHelp",17,Muted);
            for(int i=0;i<3;i++)Button("BridgeChoice"+i,266,390+i*65,686,55);
            Submit("RepairSubmit");Submit("RepairContinue");
            At("BridgeReady",0,0,1280,720);At("ReadyTitle",266,312,686,70);At("ReadyBody",266,401,686,153);Button("Rescue",1002,592,254,64,null,true);Type("ReadyTitle",32);Type("ReadyBody",22);
            At("CaseTitle",266,280,686,40);Type("CaseTitle",28,Ink);
            for(int i=0;i<4;i++){At("ModuleCard"+i,266+i%2*350,337+i/2*126,336,110);Button("Module"+i,12,12,312,47);At("ModuleState"+i,14,68,308,31);Type("ModuleState"+i,15,Muted);}
            At("MasterStatus",1002,333,254,225);Type("MasterStatus",20,Muted);Submit("MasterCover");Submit("MasterActivate");
            Dial("DialExponent",266,438,216);Dial("Divisor",506,438,216);Button("InsertC",754,438,198,45);
            At("DialPreview",1002,328,254,69);Type("DialPreview",31,Cyan);Submit("DialSubmit");
            for(int i=0;i<3;i++)Button("WireSource"+i,266,393+i*67,180,45);
            for(int i=0;i<5;i++)Button("WireTarget"+i,746,365+i*45,206,39);
            Submit("CommonC");
            for(int i=0;i<4;i++){Button("SequenceCard"+i,266+i*174,389,164,44);Button("SequenceSlot"+i,266+i*174,451,164,49);}
            At("SequenceValueLabel",266,519,686,29);Type("SequenceValueLabel",19,Muted);Input("SequenceValue",266,554,686,39);Submit("SequenceSubmit");
            At("GaugeFormula",266,380,686,55);Type("GaugeFormula",34,Cyan);At("Pressure",266,465,376,28);Dial("Release",700,465,252);At("GaugeState",266,521,686,55);Type("GaugeState",20,Muted);Submit("GaugeSubmit");
            At("CalibrationPrompt",266,282,686,70);Type("CalibrationPrompt",22);
            for(int i=0;i<3;i++)Button("CalibrationOption"+i,266,390+i*65,686,55);
            Submit("CalibrationSubmit");Submit("CalibrationNext");
            // The overview is a dashboard for the physical device, not an answer panel.
            if(topic==2)for(int i=0;i<3;i++)Find("Component"+i).gameObject.SetActive(false);
        }
        static void LimitsControls()
        {
            Find("PortalCard").GetComponent<Image>().color=Color.clear;
            At("PuzzlePortal",1052,66,170,148);At("ChargeLabel",1070,115,136,52);Type("ChargeLabel",36,Cyan);
            At("PortalCaption",1004,218,246,24);Type("PortalCaption",16,Ink);
            At("PortalDescription",24,429,194,136);Type("PortalDescription",18,Muted);
            At("QuestionNumber",24,199,900,26);Type("QuestionNumber",19,Ink);
            At("QuestionTitle",24,314,194,100);Type("QuestionTitle",24,Ink);At("QuestionPrompt",266,280,686,70);Type("QuestionPrompt",22,Ink);
            At("NumberTable",266,376,686,123);At("TableValues",18,22,650,85);Type("TableValues",24,Ink);
            At("Piecewise",266,376,686,123);At("PiecewiseText",18,22,650,85);Type("PiecewiseText",25,Ink);
            At("IndependentGiven",266,381,686,123);Type("IndependentGiven",28);
            At("Graph",279,370,400,150);
            for(int i=0;i<=4;i++)At("XTick"+i,300+i*89,502,24,20);
            for(int i=0;i<=8;i+=2)At("YTick"+i,279,488-i*14.2f,24,20);
            At("GraphAxes",680,502,22,20);At("GraphYAxis",262,370,22,20);
            At("ProbeLabel",730,381,222,28);Type("ProbeLabel",16);At("Probe",730,420,222,28);At("ProbeTrack",0,12,222,6);
            At("MarkerLabel",730,462,222,28);Type("MarkerLabel",16);At("Marker",730,501,222,28);At("MarkerTrack",0,12,222,6);
            for(int i=0;i<3;i++){At("AnswerGroup"+i,266+i*231,535,224,75);At("AnswerLabel"+i,0,0,224,24);Type("AnswerLabel"+i,15,Muted);Input("Answer"+i,0,28,222,43);}
            At("ConclusionHint",266,379,686,24);Type("ConclusionHint",16,Muted);
            for(int i=0;i<3;i++){Button("Conclusion"+i,266+i*231,411,224,43);Button("ReasonCard"+i,266+i*231,493,224,39);Button("ReasonSlot"+i,266+i*231,550,224,43);}
            At("ReasonHint",266,457,686,24);Type("ReasonHint",16,Muted);At("ReasonPrompt",266,457,686,24);Type("ReasonPrompt",16,Muted);
            Input("Scratchpad",595,629,357,44);At("ScratchLabel",595,605,357,22);Type("ScratchLabel",14,Muted);
            At("ReportAssistantNote",350,630,232,20);Type("ReportAssistantNote",14,Muted);Text("ReportAssistantNote","Basic hints are always free.");Submit("Submit");Submit("Continue");
            At("JourneyHeading",24,436,194,23);Type("JourneyHeading",15,Cyan);
            for(int i=0;i<3;i++){At("JourneyPhase"+i,24,471+i*43,194,38);At("JourneyPhaseLabel"+i,7,3,180,32);Type("JourneyPhaseLabel"+i,13);var label=Find("JourneyPhaseLabel"+i).GetComponent<TMP_Text>();label.textWrappingMode=TextWrappingModes.Normal;}
            At("JourneyDetail",24,606,194,64);Type("JourneyDetail",15,Muted);
            At("ReadyBody",266,280,686,340);At("ReadyTitle",0,0,686,70);At("ReadyDescription",0,94,686,153);Button("ActivatePortal",736,333,254,44,Find("ReadyBody"),true);
        }
        static void Menu(GameObject root)
        {
            var home=Find("Home");Find("HomePortal").gameObject.SetActive(false);
            var avatar=Surface(home,"ReportWelcomePortrait",306,382,245,247,Color.white);avatar.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Welcome.png");avatar.preserveAspect=true;
            Text("HomeKicker","WELCOME TO THE LABORATORY");Type("HomeTitle",46,Ink);Type("HomeIntro",21,Muted);
            At("HomeCard",770,108,452,550);Find("HomeCard").GetComponent<Image>().color=Panel;
            foreach(var name in new[]{"HomeContinue","HomeStart","HomeRecords","HomeInstructions","HomeSettings","HomeExit"}){var rect=Rect(name);Button(name,rect.anchoredPosition.x,-rect.anchoredPosition.y,rect.sizeDelta.x,rect.sizeDelta.y,null,name=="HomeStart");}
            var topics=Find("Topics");At("TopicsTitle",48,105,1120,59);Type("TopicsTitle",37,Ink);
            Label(topics,"ReportTopicsSubtitle","Three missions. Apply what you learn to restore the world.",50,166,1120,28,19,Muted);
            for(int i=0;i<3;i++)
            {
                At("TopicCard"+i,48+i*401,218,382,399);Find("TopicCard"+i).GetComponent<Image>().color=Panel;
                var image=Surface(Find("TopicCard"+i),"ReportMissionImage"+i,18,18,346,116,Color.white);image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Mission"+i+".png");image.preserveAspect=false;
                At("TopicIndex"+i,24,25,80,33);Type("TopicIndex"+i,23,Cyan);
                At("TopicName"+i,18,154,346,63);Type("TopicName"+i,25,Ink);
                At("TopicDescription"+i,18,228,346,88);Type("TopicDescription"+i,17,Muted);
                Button("Mission"+i,18,337,346,43,null,true);
            }
        }
        static void ImportSprite(string path)
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)return;importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
        }
        static void PrepareArt()
        {
            foreach(string path in new[]{Art+"Think.png",Art+"Welcome.png"})ImportSprite(path);
            for(int topic=0;topic<3;topic++)
            {
                EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity");var world=Object.FindFirstObjectByType<Chapter4World>();previews[topic]=world.Preview;
                var camera=world.transform.Find("Storyboard view").GetComponent<Camera>();var old=camera.targetTexture;var target=RenderTexture.GetTemporary(640,240,24);var active=RenderTexture.active;Texture2D pixels=null;
                try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels=new Texture2D(640,240,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,640,240),0,0);pixels.Apply();File.WriteAllBytes(Art+"Mission"+topic+".png",pixels.EncodeToPNG());}
                finally{camera.targetTexture=old;RenderTexture.active=active;if(pixels!=null)Object.DestroyImmediate(pixels);RenderTexture.ReleaseTemporary(target);}
                world.Preview.Release();world.Preview.width=1536;world.Preview.height=232;EditorUtility.SetDirty(world.Preview);
            }
            AssetDatabase.Refresh();for(int i=0;i<3;i++)ImportSprite(Art+"Mission"+i+".png");AssetDatabase.SaveAssets();
        }
        [MenuItem("Calculus Lab/Manual editing/Apply report fusion UI layout")]
        public static void Apply()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Author the UI before Play.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene edits before applying the layout.");
            Directory.CreateDirectory(Evidence);Directory.CreateDirectory(Art);report.Clear();var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                PrepareArt();
                if(fidelity)PrepareConceptCamera();
                for(int i=0;i<Names.Length;i++)
                {
                    string path=Prefabs+Names[i]+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                    try{Index(root);if(!refining&&(Find("ReportWorkbench")!=null||Find("ReportWelcomePortrait")!=null))throw new InvalidOperationException("Layout already applied; edit the saved prefab directly.");Common(root);if(i==0)Menu(root);else Workbench(root,i-1);if(fidelity)Fidelity(root,i-1);ManualEditingMigration.ConfigureCanvas(root);PrefabUtility.SaveAsPrefabAsset(root,path);report.Add("Saved editable UI: "+path);}
                    finally{PrefabUtility.UnloadPrefabContents(root);}
                }
                foreach(string name in new[]{"MainMenu"}.Concat(Chapter4Game.Scenes))
                {
                    var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+name+".unity");var manager=Object.FindFirstObjectByType<GameManager>();var ui=manager.missionUI!=null?manager.missionUI.gameObject:manager.menuOrLimitsUI.gameObject;
                    // Retain the saved scene UI object and all references; apply geometry to this
                    // instance too because earlier editor previews saved layout overrides.
                    Index(ui);Common(ui);if(manager.world==null)Menu(ui);else{Workbench(ui,manager.world.topic);ui.GetComponent<ReportFusionView>().gameManager=manager;}
                    if(fidelity)Fidelity(ui,manager.world==null?-1:manager.world.topic);
                    ManualEditingMigration.ConfigureCanvas(ui);
                    // Direct assignments on a prefab instance need explicit overrides.
                    // Otherwise the editor preview works but a reopened scene loses the manager.
                    foreach(var component in ui.GetComponentsInChildren<Component>(true))
                        if(component!=null){EditorUtility.SetDirty(component);if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);}
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.Add("Saved scene UI and manager reference: "+scene.path);
                }
                AssetDatabase.SaveAssets();File.WriteAllLines(Path.Combine(Evidence,"applied.txt"),report);
            }
            finally{if(setup.Length>0&&!string.IsNullOrEmpty(setup[0].path))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");}
            ManualEditingMigration.Validate();Debug.Log("REPORT_FUSION_LAYOUT_APPLIED");
        }
        public static void ApplyCaptureAndBuild(){Apply();Capture();ManualEditingMigration.BuildQA();}
        public static void RefineCaptureAndBuild(){refining=true;try{ApplyCaptureAndBuild();}finally{refining=false;}}
        public static void Capture()
        {
            Directory.CreateDirectory(Evidence);var setup=EditorSceneManager.GetSceneManagerSetup();var checks=new List<string>();
            try
            {
                for(int topic=0;topic<3;topic++)
                {
                    var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity");var world=Object.FindFirstObjectByType<Chapter4World>();var canvas=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.GetComponent<ReportFusionView>()!=null);
                    string[] views=topic==0?LimitsEditorPreview.Views:topic==1?new[]{"Power Core","Number inputs","Expression choices","Independent check","Check accepted","Support","Controls help","Rescue","Brief","Summary"}:new[]{"Case overview","Exponent dial","Wire matching","Symbol sequence","Pressure gauge","Independent check","Check accepted","Support","Controls help","Brief","Summary"};
                    foreach(string view in views)
                    {
                        SceneAuthoringWindow.ApplyView(world,canvas,view);var presentation=canvas.GetComponent<ReportFusionView>();if(presentation!=null)presentation.Present();Canvas.ForceUpdateCanvases();
                        foreach(var label in canvas.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();if(label.isTextOverflowing)checks.Add("OVERFLOW: "+topic+" / "+view+" / "+label.name);}
                        string safe=view.Replace(" ","-");SceneAuthoring.Capture(world,canvas,Path.Combine(Evidence,topic+"-"+safe+".png"),false);
                    }
                    EditorSceneManager.OpenScene(scene.path);
                }
                File.WriteAllLines(Path.Combine(Evidence,"layout-checks.txt"),checks.Count==0?new[]{"PASS: all authored previews fit without text overflow."}:checks);
                if(checks.Count>0)throw new Exception("UI preview overflow; see layout-checks.txt");
            }
            finally{if(setup.Length>0&&!string.IsNullOrEmpty(setup[0].path))EditorSceneManager.RestoreSceneManagerSetup(setup);else EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");}
        }
    }
}
#endif
