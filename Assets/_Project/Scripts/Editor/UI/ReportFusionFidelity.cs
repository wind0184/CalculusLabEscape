#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static partial class ReportFusionLayout
    {
        static bool fidelity;
        static Sprite roundSprite,circleSprite;
        static TMP_FontAsset mathFont;
        static void ShapeAssets()
        {
            foreach(string kind in new[]{"Rounded","Circle","SceneGradient"})
            {
                string path=Art+kind+".png";
                if(!File.Exists(path))
                {
                    var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);
                    for(int y=0;y<64;y++)for(int x=0;x<64;x++)
                    {
                        float dx=Mathf.Abs(x-31.5f),dy=Mathf.Abs(y-31.5f);
                        float distance=kind=="Circle"?Mathf.Sqrt(dx*dx+dy*dy)-31:Mathf.Sqrt(Mathf.Pow(Mathf.Max(dx-21,0),2)+Mathf.Pow(Mathf.Max(dy-21,0),2))-10;
                        texture.SetPixel(x,y,kind=="SceneGradient"?new Color(.015f,.045f,.07f,Mathf.Pow(1-y/63f,1.7f)*.7f):new Color(1,1,1,Mathf.Clamp01(.5f-distance)));
                    }
                    texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
                }
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.spriteBorder=kind=="Rounded"?new Vector4(12,12,12,12):Vector4.zero;importer.SaveAndReimport();
            }
            roundSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Rounded.png");circleSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Circle.png");
            string fontPath=Art+"Fonts/SourceSerif4 SDF.asset";mathFont=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if(mathFont==null)
            {
                var source=AssetDatabase.LoadAssetAtPath<Font>(Art+"Fonts/SourceSerif4-Regular.ttf");if(source==null)throw new Exception("Import the Source Serif font before authoring the concept layout.");
                mathFont=TMP_FontAsset.CreateFontAsset(source,90,9,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);mathFont.name="Source Serif 4 Math";
                AssetDatabase.CreateAsset(mathFont,fontPath);AssetDatabase.AddObjectToAsset(mathFont.material,mathFont);foreach(var atlas in mathFont.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,mathFont);
                mathFont.TryAddCharacters("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ ()=+-/.,?□√∫∞",out string missing);EditorUtility.SetDirty(mathFont);AssetDatabase.SaveAssets();
            }
        }
        static void PrepareConceptCamera()
        {
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");var world=Object.FindFirstObjectByType<Chapter4World>();
            var camera=world.transform.Find("Storyboard view").GetComponent<Camera>();camera.transform.position=new Vector3(13,12,-1);camera.transform.LookAt(new Vector3(0,0,7));camera.fieldOfView=65;
            world.Preview.Release();world.Preview.width=1536;world.Preview.height=864;EditorUtility.SetDirty(world.Preview);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static void Regular(string name,float size,Color color,TextAlignmentOptions alignment=TextAlignmentOptions.TopLeft)
        {
            Type(name,size,color);var label=Find(name)?.GetComponent<TMP_Text>();if(label==null)return;label.fontStyle=FontStyles.Normal;label.alignment=alignment;label.characterSpacing=0;
        }
        static void MathLabel(string name,float size)
        {
            Regular(name,size,Ink,TextAlignmentOptions.Center);var label=Find(name)?.GetComponent<TMP_Text>();if(label==null)return;label.font=mathFont;label.fontStyle=FontStyles.Italic;label.margin=Vector4.zero;label.textWrappingMode=TextWrappingModes.NoWrap;
        }
        static void Rounded(string name,bool circle=false)
        {
            var image=Find(name)?.GetComponent<Image>();if(image==null)return;image.sprite=circle?circleSprite:roundSprite;image.type=circle?Image.Type.Simple:Image.Type.Sliced;image.pixelsPerUnitMultiplier=1;
        }
        static void Quiet(string name,float x,float y,float width,float height,Transform parent=null)
        {
            Button(name,x,y,width,height,parent);var node=Find(name);if(node==null)return;node.GetComponent<Image>().color=Color.clear;var outline=node.GetComponent<Outline>();if(outline!=null)outline.effectColor=Color.clear;Regular(name+"Label",14,Muted,TextAlignmentOptions.MidlineLeft);
        }
        static void Notes(GameObject root)
        {
            if(root.GetComponent<MissionCanvas>()==null)return;var view=root.GetComponent<ReportFusionView>();
            if(Find("ReportNotesOpen")==null)
            {
                var button=Surface(Find("Puzzle"),"ReportNotesOpen",998,640,254,30,Color.clear).gameObject.AddComponent<Button>();button.targetGraphic=button.GetComponent<Image>();
                Label(button.transform,"ReportNotesOpenLabel","Scratchpad",0,0,254,30,14,Muted);button.GetComponent<Image>().raycastTarget=true;UnityEventTools.AddPersistentListener(button.onClick,view.OpenScratchpad);
                var modal=Surface(Find("DesignFrame"),"ReportScratchpadModal",0,0,1280,720,new Color(.01f,.03f,.05f,.92f));modal.raycastTarget=true;
                var card=Surface(modal.transform,"ReportNotesCard",282,160,716,400,Panel);Rounded("ReportNotesCard");
                Label(card.transform,"ReportNotesTitle","Think it through.",30,26,650,50,29,Ink);
                Label(card.transform,"ReportNotesHint","Working notes · These notes do not submit an answer.",30,86,650,27,16,Muted);
                var box=Surface(card.transform,"ReportNotesField",30,132,656,160,Raised);box.raycastTarget=true;Rounded("ReportNotesField");
                var field=box.gameObject.AddComponent<TMP_InputField>();field.lineType=TMP_InputField.LineType.MultiLineNewline;field.characterLimit=600;field.targetGraphic=box;field.navigation=new Navigation{mode=Navigation.Mode.None};
                var viewport=Create(box.transform,"ReportNotesViewport",14,12,628,136);viewport.gameObject.AddComponent<RectMask2D>();
                var text=Label(viewport,"ReportNotesText","",0,0,628,136,20,Ink);text.enableAutoSizing=false;text.overflowMode=TextOverflowModes.Overflow;field.textComponent=text;field.textViewport=viewport;
                var close=Surface(card.transform,"ReportNotesClose",484,321,202,43,Cyan).gameObject.AddComponent<Button>();close.navigation=new Navigation{mode=Navigation.Mode.None};close.targetGraphic=close.GetComponent<Image>();close.GetComponent<Image>().raycastTarget=true;Rounded("ReportNotesClose");
                Label(close.transform,"ReportNotesCloseLabel","Return to repair",0,0,202,43,16,Background).alignment=TextAlignmentOptions.Center;UnityEventTools.AddPersistentListener(close.onClick,view.CloseScratchpad);
                view.scratchpad=field;view.scratchpadModal=modal.gameObject;modal.gameObject.SetActive(false);
            }
            Quiet("ReportNotesOpen",998,640,254,30);
            view.scratchpad.navigation=new Navigation{mode=Navigation.Mode.None};Find("ReportNotesClose").GetComponent<Button>().navigation=new Navigation{mode=Navigation.Mode.None};
        }
        static void Fidelity(GameObject root,int topic)
        {
            foreach(var button in root.GetComponentsInChildren<Button>(true)){Rounded(button.name);Regular(button.name+"Label",16,button.GetComponent<Image>().color==Cyan?Background:Ink,TextAlignmentOptions.Center);}
            foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))if(label.GetComponentInParent<TMP_InputField>()==null&&label.transform.IsChildOf(Find("Header")))label.fontStyle=FontStyles.Normal;
            At("Brand",51,10,240,38);Text("Brand","CALCULUS\nLAB ESCAPE");Regular("Brand",12,Ink);Find("Brand").GetComponent<TMP_Text>().characterSpacing=2;
            At("MissionEmblem",24,19,19,20);At("Section",650,21,473,19);Regular("Section",12,Muted,TextAlignmentOptions.Right);Find("Section").GetComponent<TMP_Text>().characterSpacing=1.5f;
            if(topic<0)return;
            var puzzle=Find("Puzzle");var view=root.GetComponent<ReportFusionView>();view.useConceptLayout=true;view.mission=root.GetComponent<MissionCanvas>();
            foreach(string name in new[]{"ReportSceneTitleShade","ReportSceneInfoShade","SecondaryProgress","EnvironmentStatus","Component0","Component1","Component2","PowerInstructions","ReportAssistantNote","ReportStepHeading","ReportResultState"})if(Find(name)!=null)Find(name).gameObject.SetActive(false);
            if(topic>0){At("MissionProgress",24,200,560,21);Regular("MissionProgress",14,Ink);}
            var device=topic==0?Find("ReportSceneStrip"):Find("DevicePanel");
            view.scenePreview.uvRect=topic==1?new Rect(0,.366f,1,.268f):new Rect(0,0,1,1);
            var gradient=Surface(device,"ReportSceneGradient",0,0,1280,194,Color.white);gradient.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"SceneGradient.png");gradient.transform.SetSiblingIndex(1);
            At("ReportSceneShade",22,16,308,63,device);Rounded("ReportSceneShade");var shade=Find("ReportSceneShade").GetComponent<Image>();shade.color=new Color(.04f,.115f,.16f,.94f);
            Label(device,"ReportSceneOutlineNote","Learning console open",34,51,280,19,13,new Color(.75f,.85f,.88f));
            At("ReportSceneTitle",34,28,280,20);Regular("ReportSceneTitle",13,Ink);Find("ReportSceneTitle").GetComponent<TMP_Text>().characterSpacing=.5f;
            for(int i=0;i<view.progressSegments.Length;i++)At("ReportProgressSegment"+i,24+i*58,172,52,4);
            Surface(device,"ReportSceneProgressShade",16,133,230,56,new Color(.02f,.07f,.10f,.52f)).transform.SetSiblingIndex(1);
            if(topic==0)RemoveLimitsInspection(root);
            else{
                var target=Surface(device,"ReportInspectionTag",718,58,155,65,new Color(.035f,.11f,.15f,.94f));Rounded("ReportInspectionTag");Surface(target.transform,"ReportInspectionAccent",0,0,2,65,Cyan);
                view.sceneTarget=Label(target.transform,"ReportInspectionTitle",topic==1?"POWER CORE":"CASE MODULE",12,9,132,22,13,Cyan);
                Label(target.transform,"ReportInspectionNote","Inspection point",12,35,132,19,13,Ink);
            }
            Label(device,"ReportSceneOutcome","Repair a component.\nChange the world.",1090,135,166,40,13,Ink).gameObject.SetActive(topic==1);
            if(topic==2)At("CaseBack",1070,213,186,30);
            Regular("LiveStatus",13,Muted);At("LiveStatus",37,692,770,20);At("PuzzleWorld",1107,690,149,25);Quiet("PuzzleWorld",1107,690,149,25);
            At("ReportResultHeading",998,274,256,24);Text("ReportResultHeading","YOUR RESULT PREVIEW");Regular("ReportResultHeading",13,Muted);Find("ReportResultHeading").GetComponent<TMP_Text>().characterSpacing=1.6f;
            At("ReportResultState",998,390,256,54);Regular("ReportResultState",14,Muted);Text("ReportResultState","Your changes appear here before submission.");Find("ReportResultState").gameObject.SetActive(true);
            foreach(string name in new[]{"RepairSubmit","RepairContinue","DialSubmit","SequenceSubmit","GaugeSubmit","CommonC","CalibrationSubmit","CalibrationNext","Submit","Continue","MasterCover","MasterActivate","Rescue"})if(Find(name)!=null){Button(name,998,574,256,48,null,true);Rounded(name);Regular(name+"Label",16,Background,TextAlignmentOptions.Center);}
            At("Feedback",1010,449,232,112);Regular("Feedback",15,Ink);
            var feedbackCard=Surface(puzzle,"ReportFeedbackCard",998,438,256,124,Raised);Rounded("ReportFeedbackCard");feedbackCard.transform.SetSiblingIndex(5);view.feedbackPanel=feedbackCard.gameObject;
            view.feedback=Find("Feedback")?.GetComponent<TMP_Text>();view.resultState=Find("ReportResultState").GetComponent<TMP_Text>();view.sceneProgress=Find("MissionProgress")?.GetComponent<TMP_Text>();view.liveStatus=Find("LiveStatus")?.GetComponent<TMP_Text>();view.overview=Find("CaseOverview")?.gameObject;
            At("ReportAssistantPortrait",244,576,100,112);At("ReportAssistantName",343,599,410,18);Regular("ReportAssistantName",12,Ink);Find("ReportAssistantName").GetComponent<TMP_Text>().characterSpacing=1.1f;
            Label(puzzle,"ReportAssistantPrompt","A little support, whenever you need it.",343,620,590,20,14,Muted);
            Quiet(topic==0?"PuzzleSupport":"SupportOpen",343,643,250,27);
            if(topic==0)
            {
                Regular("QuestionNumber",14,Ink);At("QuestionTitle",24,280,194,100);Regular("QuestionTitle",19,Ink);Regular("QuestionPrompt",20,Ink);Regular("PortalDescription",15,Muted);
                foreach(string name in new[]{"JourneyPhase0","JourneyPhase1","JourneyPhase2","JourneyDetail"})if(Find(name)!=null)Find(name).gameObject.SetActive(false);
                Rounded("Scratchpad");
            }
            if(topic>0)
            {
                At("TaskTitle",24,279,194,74);Regular("TaskTitle",19,Ink);At("TaskPrompt",266,280,686,61);Regular("TaskPrompt",20,Ink);
                At("NextAction",24,394,194,149);Regular("NextAction",15,Muted);Quiet("ControlsOpen",24,626,194,32);Quiet("ManualOpen",24,581,194,32);
                At("JourneyHeading",266,252,686,22);Regular("JourneyHeading",13,Cyan);
                foreach(string name in new[]{"JourneyPhase0","JourneyPhase1","JourneyPhase2","JourneyDetail","AnswerKeyboardHelp"})if(Find(name)!=null)Find(name).gameObject.SetActive(false);
                foreach(string prefix in new[]{"BridgeChoice","CalibrationOption"})for(int i=0;i<3;i++){string name=prefix+i;if(Find(name)!=null){Button(name,266,365+i*66,686,55);Rounded(name);MathLabel(name+"Label",22);}}
                view.taskTitle=Find("TaskTitle").GetComponent<TMP_Text>();view.taskPrompt=Find("TaskPrompt").GetComponent<TMP_Text>();view.nextAction=Find("NextAction").GetComponent<TMP_Text>();
                var instructions=Find("ReportPowerSteps")??Create(puzzle,"ReportPowerSteps",0,0,1280,720);
                Label(instructions,"ReportPowerHeading","Two small steps.\nOne working bridge.",24,273,194,60,18,Ink);
                Label(instructions,"ReportPowerIntro","Apply the power rule to the expression.",24,342,194,43,14,Muted);
                for(int i=0;i<2;i++)
                {
                    var dot=Surface(instructions,"ReportPowerNumber"+i,24,399+i*79,27,27,Raised);Rounded(dot.name,true);Label(dot.transform,"ReportPowerNumberText"+i,(i+1).ToString(),0,0,27,27,13,Cyan).alignment=TextAlignmentOptions.Center;
                    Label(instructions,"ReportPowerStepTitle"+i,i==0?"Move the exponent":"Reduce it by one",63,397+i*79,151,22,14,Ink);
                    Label(instructions,"ReportPowerStepBody"+i,i==0?"Drag 4 into the\ncoefficient slot.":"Turn the exponent dial\nfrom 4 to 3.",63,427+i*79,151,50,14,Muted);
                }
                var power=Find("PowerControls");var expressionCard=Surface(power,"ReportExpressionCard",262,264,688,75,Raised);Rounded("ReportExpressionCard");expressionCard.transform.SetAsFirstSibling();
                At("PowerExpression",483,265,209,72);MathLabel("PowerExpression",41);Text("PowerExpression","f(x) = x");
                Button("BridgeChip",654,268,43,43);Rounded("BridgeChip");Find("BridgeChip").GetComponent<Image>().color=Panel;Find("BridgeChip").GetComponent<Outline>().effectColor=Cyan;MathLabel("BridgeChipLabel",26);
                Label(power,"ReportDragCaption","Drag 4, or select then place",641,312,290,19,13,Muted).alignment=TextAlignmentOptions.Right;
                var coefficient=Surface(power,"ReportCoefficientCard",262,352,337,160,Panel);Rounded("ReportCoefficientCard");var co=coefficient.GetComponent<Outline>()??coefficient.gameObject.AddComponent<Outline>();co.effectColor=Line;co.effectDistance=new Vector2(1,-1);coefficient.transform.SetSiblingIndex(1);
                Label(power,"ReportCoefficientCaption","COEFFICIENT SLOT",280,367,300,21,13,Muted).alignment=TextAlignmentOptions.Center;
                Button("CoefficientSlot",390,394,80,82);Rounded("CoefficientSlot");var slot=Find("CoefficientSlot");slot.GetComponent<Image>().color=Raised;slot.GetComponent<Outline>().effectColor=Cyan;MathLabel("CoefficientSlotLabel",38);
                Label(power,"ReportCoefficientHint","Place the selected 4",280,485,300,19,13,Muted).alignment=TextAlignmentOptions.Center;
                var exponent=Surface(power,"ReportExponentCard",613,352,337,160,Panel);Rounded("ReportExponentCard");var eo=exponent.GetComponent<Outline>()??exponent.gameObject.AddComponent<Outline>();eo.effectColor=Line;eo.effectDistance=new Vector2(1,-1);exponent.transform.SetSiblingIndex(2);
                At("ExponentCaption",630,367,303,21);Text("ExponentCaption","EXPONENT DIAL");Regular("ExponentCaption",13,Muted,TextAlignmentOptions.Center);
                Surface(power,"ReportDialFace",747,397,70,70,Raised).transform.SetSiblingIndex(3);Rounded("ReportDialFace",true);
                At("ExponentValue",747,401,70,58);MathLabel("ExponentValue",35);Button("ExponentMinus",736,477,40,31);Button("ExponentPlus",788,477,40,31);Rounded("ExponentMinus");Rounded("ExponentPlus");Regular("ExponentMinusLabel",19,Ink,TextAlignmentOptions.Center);Regular("ExponentPlusLabel",19,Ink,TextAlignmentOptions.Center);
                var pivot=Rect("ReportDialPivot")??Create(power,"ReportDialPivot",782,432,0,0);pivot.pivot=new Vector2(.5f,.5f);
                Surface(pivot,"ReportDialNeedle",-1,-27,2,9,Cyan);view.dialNeedle=pivot;
                At("PowerPreview",998,304,256,86);MathLabel("PowerPreview",42);At("DialPreview",998,312,256,78);MathLabel("DialPreview",33);
                view.powerControls=power.gameObject;view.powerSteps=instructions.gameObject;view.coefficientPanel=coefficient.gameObject;view.exponentPanel=exponent.gameObject;view.coefficientLabel=Find("CoefficientSlotLabel").GetComponent<TMP_Text>();view.exponentLabel=Find("ExponentValue").GetComponent<TMP_Text>();view.originalExpression=Find("PowerExpression").GetComponent<TMP_Text>();view.draftExpression=Find("PowerPreview").GetComponent<TMP_Text>();
                view.powerChipLabel=Find("BridgeChipLabel").GetComponent<TMP_Text>();view.completionPanel=Find("BridgeReady")?.gameObject;Notes(root);view.scratchpadEntry=Find("ReportNotesOpen")?.gameObject;
            }
            AssistantDock(topic);Quiet(topic==0?"PuzzleSupport":"SupportOpen",104,619,116,36);
            if(topic>0){Quiet("ManualOpen",266,633,320,34);Quiet("ControlsOpen",620,633,332,34);}
            foreach(var selection in root.GetComponentsInChildren<MissionSelectionVisual>(true))selection.Configure();view.Present();
            AnswerPreviewAuthoring.Configure(root,topic);
        }
        public static void RefineToConcept()
        {
            fidelity=true;refining=true;try{ShapeAssets();Apply();Capture();ManualEditingMigration.BuildQA();}finally{fidelity=false;refining=false;}
        }
        public static void UpdateConceptPreview()
        {
            foreach(var canvas in Object.FindObjectsByType<ReportFusionView>(FindObjectsSortMode.None))canvas.Present();
        }
        [MenuItem("Calculus Lab/Manual editing/Preview report fusion power rule")]
        public static void OpenConceptPreview()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Stop Play before opening an editor preview.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save your open scene before opening a different preview.");
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");
            var world=Object.FindFirstObjectByType<Chapter4World>();var canvas=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.GetComponent<ReportFusionView>()!=null);
            SceneAuthoringWindow.ApplyView(world,canvas,"Power Core");canvas.GetComponent<ReportFusionView>().Present();world.transform.Find("Storyboard view").GetComponent<Camera>().Render();Canvas.ForceUpdateCanvases();
            Selection.activeGameObject=canvas.gameObject;EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
    }
}
#endif
