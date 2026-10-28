#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace CalculusLabEscape.Editor
{
    public static partial class ReportFusionLayout
    {
        const string DialogueArt="Assets/_Project/Art/UI/RobotDialogue/";
        static string DialogueEvidence=>LearningApi.Argument("--dialogue-evidence",Path.GetFullPath("../../qa/robot-dialogue-2026-10-24"));
        static Button DialogueButton(Transform parent,string name,string text,float x,float y,float width,RobotDialogue receiver,int index)
        {
            var image=Surface(parent,name,x,y,width,36,Raised);image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(DialogueArt+"Btn_Rectangle04_Transpar.png");image.type=Image.Type.Sliced;image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};
            var label=Label(image.transform,name+"Label",text,8,6,width-16,24,15,Ink);label.alignment=TextAlignmentOptions.Center;
            if(index<0)UnityEventTools.AddPersistentListener(button.onClick,receiver.Close);else UnityEventTools.AddIntPersistentListener(button.onClick,receiver.ChooseTopic,index);
            return button;
        }
        [MenuItem("Calculus Lab/Limits/Save robot conversation UI")]
        public static void AuthorRobotDialogue()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before authoring dialogue.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save current scene edits first.");
            AssetDatabase.Refresh();Directory.CreateDirectory(DialogueEvidence);
            foreach(string name in new[]{"BasicFrame_Rectangle01_l.png","Btn_Rectangle04_Transpar.png"}){
                var importer=(TextureImporter)AssetImporter.GetAtPath(DialogueArt+name);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.spriteBorder=new Vector4(16,16,16,16);importer.SaveAndReimport();
            }
            var ui=PrefabUtility.LoadPrefabContents(Prefabs+"LimitsUI.prefab");
            try{
                Index(ui);var previous=Find("RobotDialogue");if(previous!=null)Object.DestroyImmediate(previous.gameObject);Index(ui);
                var root=Create(null,"RobotDialogue",0,0,1280,720);var dialogue=root.gameObject.AddComponent<RobotDialogue>();
                var overlay=Surface(root,"RobotDialogueOverlay",0,0,1280,720,new Color(.005f,.015f,.025f,.15f));overlay.raycastTarget=true;dialogue.overlay=overlay.gameObject;
                var card=Surface(overlay.transform,"DialoguePanel",128,474,1024,218,new Color(.42f,.65f,.72f,.99f));card.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(DialogueArt+"BasicFrame_Rectangle01_l.png");card.type=Image.Type.Sliced;
                Surface(card.transform,"DialogueAccent",0,18,3,182,Cyan);
                var portrait=Surface(card.transform,"DialoguePortrait",24,51,90,104,Color.white);portrait.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Think.png");portrait.preserveAspect=true;
                Label(card.transform,"DialogueSpeaker","LABORATORY AI",142,16,590,22,15,Cyan).characterSpacing=1.5f;
                dialogue.topic=Label(card.transform,"DialogueTopic","YOUR OBSERVATORY GUIDE",142,42,740,20,13,Muted);
                dialogue.message=Label(card.transform,"DialogueMessage","Hello! I'm your laboratory guide.",142,66,838,78,20,Ink);dialogue.message.enableAutoSizing=false;
                DialogueButton(card.transform,"DialogueTopic0","What should I do?",142,152,260,dialogue,0);
                DialogueButton(card.transform,"DialogueTopic1","Explain limits",416,152,260,dialogue,1);
                DialogueButton(card.transform,"DialogueTopic2","How do hints work?",690,152,290,dialogue,2);
                DialogueButton(card.transform,"DialogueClose","Close",878,12,110,dialogue,-1);
                Label(card.transform,"DialogueFooter","Basic guidance / Free       [Enter] Show message       [Esc] Close",142,197,838,17,12,Muted);
                dialogue.Preview();overlay.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root.gameObject,Prefabs+"RobotDialogue.prefab");Object.DestroyImmediate(root.gameObject);
                Index(ui);var old=Find("RobotDialogue");if(old!=null)Object.DestroyImmediate(old.gameObject);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"RobotDialogue.prefab"),ui.scene);instance.transform.SetParent(Find("DesignFrame"),false);instance.transform.SetAsLastSibling();PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                ManualEditingMigration.ConfigureCanvas(ui);PrefabUtility.SaveAsPrefabAsset(ui,Prefabs+"LimitsUI.prefab");
            }finally{PrefabUtility.UnloadPrefabContents(ui);}
            string path=SceneAuthoring.SceneFolder+"Level01_Limits.unity";var scene=EditorSceneManager.OpenScene(path);var manager=Object.FindFirstObjectByType<GameManager>();
            var controller=manager.menuOrLimitsUI.GetComponentInChildren<RobotDialogue>(true);manager.robotDialogue=controller;controller.manager=manager;controller.guide=manager.world.transform.Find("Limits guide");Index(manager.menuOrLimitsUI.gameObject);controller.worldPrompt=Find("WorldPrompt").GetComponent<TMP_Text>();controller.worldObjective=Find("WorldObjective").GetComponent<TMP_Text>();
            if(controller.guide==null)throw new Exception("Place the saved Limits guide before adding its conversation.");
            controller.lookTarget=controller.guide.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Head");
            controller.hideWhileTalking=new[]{"WorldHUD","WorldPromptPanel","Crosshair"}.Select(Find).Where(t=>t!=null).Select(t=>t.gameObject).ToArray();
            SaveReference(controller);SaveReference(manager);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();ValidateRobotDialogue();CaptureRobotDialogue();ManualEditingMigration.BuildQA();
        }
        public static void ValidateRobotDialogue()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before checking saved dialogue references.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save current scene edits before validating dialogue.");
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"Level01_Limits.unity");var manager=Object.FindFirstObjectByType<GameManager>();var dialogue=manager.robotDialogue;
            if(dialogue==null||dialogue.manager!=manager||dialogue.guide==null||dialogue.overlay.activeSelf||dialogue.IsOpen||dialogue.worldPrompt==null||dialogue.worldObjective==null)throw new Exception("Save idle robot dialogue references before Play.");
            ManualEditingMigration.Validate();Directory.CreateDirectory(DialogueEvidence);File.WriteAllText(Path.Combine(DialogueEvidence,"saved-dialogue.txt"),"PASS: saved robot dialogue prefab, existing guide and manager references, inactive input shield, explicit topic/close button events and all existing scene references.");
        }
        public static void CaptureRobotDialogue()
        {
            Directory.CreateDirectory(DialogueEvidence);string path=SceneAuthoring.SceneFolder+"Level01_Limits.unity";
            try{
                foreach(int topic in new[]{-1,0,1,2}){
                    EditorSceneManager.OpenScene(path);var manager=Object.FindFirstObjectByType<GameManager>();var canvas=manager.menuOrLimitsUI.GetComponent<Canvas>();SceneAuthoringWindow.ApplyView(manager.world,canvas,"World");PrepareDialoguePreviewCanvas(canvas);
                    var camera=manager.world.player.GetComponentInChildren<Camera>();var guide=manager.robotDialogue.guide;camera.transform.position=guide.position+new Vector3(.35f,1.15f,-3.5f);camera.transform.LookAt(manager.robotDialogue.lookTarget.position);
                    manager.robotDialogue.Preview(0,topic);Canvas.ForceUpdateCanvases();SceneAuthoring.Capture(manager.world,canvas,Path.Combine(DialogueEvidence,"dialogue-"+topic+".png"),false);
                }
            }finally{EditorSceneManager.OpenScene(path);}
        }
        [MenuItem("Calculus Lab/Limits/Preview robot conversation")]
        public static void OpenRobotDialoguePreview()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before opening an editor preview.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save current scene edits first.");
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"Level01_Limits.unity");var manager=Object.FindFirstObjectByType<GameManager>();var canvas=manager.menuOrLimitsUI.GetComponent<Canvas>();SceneAuthoringWindow.ApplyView(manager.world,canvas,"World");PrepareDialoguePreviewCanvas(canvas);manager.robotDialogue.Preview();Selection.activeGameObject=manager.robotDialogue.gameObject;EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
        static void PrepareDialoguePreviewCanvas(Canvas canvas)
        {
            canvas.enabled=true;Index(canvas.gameObject);foreach(Transform page in Find("Pages"))page.gameObject.SetActive(page.name=="World");
        }
    }
}
#endif
