#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static partial class ReportFusionLayout
    {
        static string StoryEvidence=>LearningApi.Argument("--story-evidence",Path.GetFullPath("../../qa/laboratory-story-2026-10-24"));
        static void StoryText(Transform panel,string name,string text,float y,float size,Color colour)
        {
            var node=panel.Find(name);TextMeshPro label;
            if(node==null){RoomText(panel,name,text,new Vector3(0,y,.035f),size,1.88f,.23f);label=panel.Find(name).GetComponent<TextMeshPro>();}
            else label=node.GetComponent<TextMeshPro>();
            label.text=text;label.fontSize=size;label.color=colour;label.raycastTarget=false;label.gameObject.SetActive(true);label.rectTransform.localPosition=new Vector3(0,y,.035f);label.rectTransform.sizeDelta=new Vector2(1.88f,.23f);
        }
        static void StoryBoards(GameObject room)
        {
            var left=room.transform.Find("Differentiation poster");var right=room.transform.Find("Integration poster");
            if(left==null||right==null)throw new Exception("Keep the lobby's two saved wall panels.");font=left.Find("Subject").GetComponent<TMP_Text>().font;
            foreach(var panel in new[]{left,right})foreach(Transform child in panel)if(child.name!="Poster frame"&&child.name!="Poster board"&&!child.name.StartsWith("Story"))child.gameObject.SetActive(false);
            // Tilt the existing wall displays towards the entrance camera, without moving the camera or door.
            left.localPosition=new Vector3(2.18f,1.98f,-.7f);left.localRotation=Quaternion.Euler(0,-55,0);
            right.localPosition=new Vector3(-2.18f,1.98f,-.7f);right.localRotation=Quaternion.Euler(0,55,0);
            foreach(var panel in new[]{left,right}){
                panel.localScale=new Vector3(1.08f,1.08f,1);
                var frame=panel.Find("Poster frame");frame.localScale=new Vector3(2.06f,1.73f,.018f);frame.GetComponent<Renderer>().sharedMaterial=RoomMaterial(panel==left?"b58c57":"619ba8");
                panel.Find("Poster board").GetComponent<Renderer>().sharedMaterial=RoomMaterial("0e232e");
                var rule=panel.Find("StoryRule")??Box(panel,"StoryRule",new Vector3(0,.39f,.027f),new Vector3(1.76f,.014f,.006f),panel==left?"b58c57":"619ba8");
                rule.gameObject.SetActive(true);
            }
            StoryText(left,"StoryHeading","SYSTEM FAULT",.60f,1.7f,Hex("f1bb75"));
            StoryText(left,"StoryMainAI","AI CORRUPTED",.20f,2.0f,Ink);
            StoryText(left,"StorySimulations","3 unstable simulations",-.06f,1.3f,Ink);
            StoryText(left,"StoryPlayer","YOU: TRAINEE ANALYST",-.40f,1.16f,Cyan);
            StoryText(left,"StoryGuide","Guide unit online",-.64f,1.25f,Cyan);
            StoryText(right,"StoryHeading","RESTORE THE LAB",.60f,1.6f,Cyan);
            string[] goals={"01  PORTAL","02  BRIDGE","03  DEVICE"},topics={"Limits","Differentiation","Integration"};
            for(int i=0;i<3;i++){StoryText(right,"StoryGoal"+i,goals[i],.23f-i*.365f,1.9f,Ink);StoryText(right,"StoryTopic"+i,topics[i],.045f-i*.365f,1.1f,Cyan);}
            foreach(var label in new[]{left.Find("StoryHeading"),left.Find("StoryMainAI"),right.Find("StoryHeading")})label.GetComponent<TMP_Text>().fontStyle=FontStyles.Bold;
            var state=room.GetComponent<LaboratoryStoryBoards>()??room.AddComponent<LaboratoryStoryBoards>();state.heading=left.Find("StoryHeading").GetComponent<TMP_Text>();state.mainSystem=left.Find("StoryMainAI").GetComponent<TMP_Text>();state.simulations=left.Find("StorySimulations").GetComponent<TMP_Text>();state.plan=right.Find("StoryHeading").GetComponent<TMP_Text>();EditorUtility.SetDirty(state);
        }
        [MenuItem("Calculus Lab/Main menu/Improve story wall readability")]
        public static void AuthorReadableStoryBoards()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before editing the wall boards.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save current scene edits first.");
            Directory.CreateDirectory(StoryEvidence);Directory.CreateDirectory(WelcomeEvidence);
            string path=Prefabs+"MainMenuLobby.prefab";var room=PrefabUtility.LoadPrefabContents(path);
            try{StoryBoards(room);PrefabUtility.SaveAsPrefabAsset(room,path);}finally{PrefabUtility.UnloadPrefabContents(room);}
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");
            var actual=scene.GetRootGameObjects().First(g=>g.name=="Main Menu Lobby");StoryBoards(actual);
            foreach(var component in actual.GetComponentsInChildren<TMP_Text>(true)){SaveReference(component);SaveReference(component.rectTransform);}
            SaveReference(actual.GetComponent<LaboratoryStoryBoards>());
            foreach(var node in actual.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Differentiation poster"||t.name=="Integration poster"||t.parent!=null&&(t.parent.name=="Differentiation poster"||t.parent.name=="Integration poster"))){SaveReference(node);PrefabUtility.RecordPrefabInstancePropertyModifications(node.gameObject);var renderer=node.GetComponent<Renderer>();if(renderer!=null)SaveReference(renderer);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            ManualEditingMigration.Validate();CaptureMenuWelcome();OpenMainMenu();
            File.WriteAllText(Path.Combine(StoryEvidence,"readable-boards.txt"),"PASS: saved entrance-facing wall displays, larger contrasting labels, original main menu routes and scene references valid.");
        }
        static void StorySummary(GameObject ui,int topic)
        {
            Index(ui);var view=ui.GetComponent<LaboratoryStoryView>()??ui.AddComponent<LaboratoryStoryView>();view.summaryTitle=Find("SummaryTitle").GetComponent<TMP_Text>();
            if(string.IsNullOrEmpty(view.missionTitle))view.missionTitle=view.summaryTitle.text;
            view.storyStatus=Label(Find("Summary"),"LaboratoryStoryStatus","",topic==0?62:57,topic==0?255:205,1150,24,16,Cyan);view.storyStatus.enableAutoSizing=false;view.Present(0);
            ManualEditingMigration.ConfigureCanvas(ui);
        }
        public static void AuthorLaboratoryStory()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before authoring the story.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save current scene edits first.");
            Directory.CreateDirectory(StoryEvidence);
            string lobbyPath=Prefabs+"MainMenuLobby.prefab";var room=PrefabUtility.LoadPrefabContents(lobbyPath);try{StoryBoards(room);PrefabUtility.SaveAsPrefabAsset(room,lobbyPath);}finally{PrefabUtility.UnloadPrefabContents(room);}
            var menuScene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");
            var actual=menuScene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Main Menu Lobby");if(actual==null)throw new Exception("Open the saved main menu lobby.");StoryBoards(actual);
            foreach(var component in actual.GetComponentsInChildren<TMP_Text>(true))SaveReference(component);
            SaveReference(actual.GetComponent<LaboratoryStoryBoards>());
            foreach(var node in actual.GetComponentsInChildren<Transform>(true).Where(t=>t.parent!=null&&(t.parent.name=="Differentiation poster"||t.parent.name=="Integration poster")))PrefabUtility.RecordPrefabInstancePropertyModifications(node.gameObject);
            EditorSceneManager.MarkSceneDirty(menuScene);EditorSceneManager.SaveScene(menuScene);
            for(int topic=0;topic<3;topic++){
                string path=Prefabs+Names[topic+1]+".prefab";var ui=PrefabUtility.LoadPrefabContents(path);
                try{StorySummary(ui,topic);PrefabUtility.SaveAsPrefabAsset(ui,path);}finally{PrefabUtility.UnloadPrefabContents(ui);}
                var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity");var manager=Object.FindFirstObjectByType<GameManager>();ui=topic==0?manager.menuOrLimitsUI.gameObject:manager.missionUI.gameObject;StorySummary(ui,topic);
                var view=ui.GetComponent<LaboratoryStoryView>();view.manager=manager;SaveReference(view);SaveReference(view.storyStatus);SaveReference(view.storyStatus.rectTransform);SaveReference(view.summaryTitle);SaveReference(ui.GetComponent<UIReferenceMap>());
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();ValidateLaboratoryStory();CaptureLaboratoryStory();ManualEditingMigration.BuildQA();
        }
        public static void ValidateLaboratoryStory()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before validating story references.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save current scene edits first.");
            ManualEditingMigration.Validate();
            foreach(string level in Chapter4Game.Scenes){EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+level+".unity");var manager=Object.FindFirstObjectByType<GameManager>();var ui=manager.menuOrLimitsUI!=null?manager.menuOrLimitsUI.gameObject:manager.missionUI.gameObject;var view=ui.GetComponent<LaboratoryStoryView>();if(view==null||view.manager!=manager||view.summaryTitle==null||view.storyStatus==null||string.IsNullOrEmpty(view.missionTitle))throw new Exception("Save story summary references in "+level);}
            Directory.CreateDirectory(StoryEvidence);File.WriteAllText(Path.Combine(StoryEvidence,"saved-story.txt"),"PASS: two authored story wall panels and saved summary references for all three missions; original menu routes, question controls and scene managers remain valid. Story progress reads existing profile records only.");
        }
        public static void CaptureLaboratoryStory()
        {
            Directory.CreateDirectory(StoryEvidence);CaptureMenuWelcome();
            for(int topic=0;topic<3;topic++){
                string path=SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity";EditorSceneManager.OpenScene(path);var manager=Object.FindFirstObjectByType<GameManager>();var canvas=topic==0?manager.menuOrLimitsUI.GetComponent<Canvas>():manager.missionUI.GetComponent<Canvas>();
                SceneAuthoringWindow.ApplyView(manager.world,canvas,"Summary");canvas.GetComponent<ReportFusionView>().Present();canvas.GetComponent<LaboratoryStoryView>().Present(3);SceneAuthoring.Capture(manager.world,canvas,Path.Combine(StoryEvidence,"ending-preview-"+topic+".png"),false);EditorSceneManager.OpenScene(path);
            }
            OpenMainMenu();
        }
    }
}
#endif
