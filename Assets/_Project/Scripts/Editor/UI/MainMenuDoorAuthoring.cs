#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static partial class ReportFusionLayout
    {
        const string DoorPrefab=Prefabs+"MainMenuLobby.prefab";
        static CanvasGroup Group(Transform node)
        {
            var group=node.GetComponent<CanvasGroup>();if(group==null)group=node.gameObject.AddComponent<CanvasGroup>();return group;
        }
        static void SaveReference(Component component)
        {
            EditorUtility.SetDirty(component);PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        [MenuItem("Calculus Lab/Main menu/Add opening-door entrance")]
        public static void ApplyDoorEntrance()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save the open scene before authoring the doorway.");
            var room=PrefabUtility.LoadPrefabContents(DoorPrefab);
            try{
                var entrance=room.GetComponent<MainMenuEntrance>()??room.AddComponent<MainMenuEntrance>();
                foreach(int side in new[]{-1,1}){
                    string name=side<0?"Left door hinge":"Right door hinge";
                    var hinge=room.transform.Find(name);
                    if(hinge==null){
                        hinge=new GameObject(name).transform;hinge.SetParent(room.transform,false);hinge.localPosition=new Vector3(side*1.22f,0,-2.2f);
                        // Each leaf keeps its own window and handle while swinging inward.
                        var leafNames=new[]{side<0?"Left door":"Right door","Window frame","Window glass","Door handle"};
                        foreach(var part in room.transform.Cast<Transform>().Where(t=>leafNames.Contains(t.name)&&Mathf.Sign(t.localPosition.x)==side).ToArray())part.SetParent(hinge,true);
                    }
                    if(side<0)entrance.leftHinge=hinge;else entrance.rightHinge=hinge;
                }
                // Leave a real opening so the leaves remain visible as they swing inside.
                var backWall=room.transform.Find("Back wall");
                if(backWall!=null){
                    backWall.name="Back wall - left";backWall.localPosition=new Vector3(-2.125f,1.7f,-2.45f);backWall.localScale=new Vector3(1.55f,3.4f,.1f);
                    Box(room.transform,"Back wall - right",new Vector3(2.125f,1.7f,-2.45f),new Vector3(1.55f,3.4f,.1f),"1b3541");
                    Box(room.transform,"Back wall - above door",new Vector3(0,3.105f,-2.45f),new Vector3(2.7f,.59f,.1f),"1b3541");
                }
                var light=room.transform.Find("Light beyond doorway");if(light==null)light=Box(room.transform,"Light beyond doorway",Vector3.zero,Vector3.one,"e5faff");
                light.localPosition=new Vector3(0,1.5f,-3.65f);light.localScale=new Vector3(5.6f,4.8f,.012f);
                entrance.centreSeam=room.transform.Find("Door centre seam").gameObject;
                entrance.lobbyCamera=room.GetComponentInChildren<Camera>();
                var bloom=entrance.lobbyCamera.GetComponent<LaboratoryAIBloom>();if(bloom==null)bloom=entrance.lobbyCamera.gameObject.AddComponent<LaboratoryAIBloom>();
                bloom.shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Project/Art/LaboratoryAI/Shaders/LaboratoryAIBloom.shader");bloom.threshold=.65f;bloom.intensity=.45f;
                PrefabUtility.SaveAsPrefabAsset(room,DoorPrefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(room);}
            var ui=PrefabUtility.LoadPrefabContents(Prefabs+"MainMenuUI.prefab");
            try{
                Index(ui);
                foreach(string name in DoorForegroundNames)Group(Find(name));Group(Find("RoomFade"));
                var shield=Find("DoorLightCover");
                if(shield==null){
                    var image=Surface(Find("DesignFrame"),"DoorLightCover",0,0,1280,720,Hex("e5faff"));image.raycastTarget=true;shield=image.transform;shield.SetAsLastSibling();
                }
                var group=Group(shield);group.alpha=0;group.blocksRaycasts=false;group.interactable=false;
                PrefabUtility.SaveAsPrefabAsset(ui,Prefabs+"MainMenuUI.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(ui);}
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");
            var manager=Object.FindFirstObjectByType<GameManager>();var actor=Object.FindFirstObjectByType<MainMenuEntrance>();
            Index(manager.menuOrLimitsUI.gameObject);
            actor.roomPreview=Rect("LobbyPreview");actor.designFrame=Rect("DesignFrame");actor.edgeFade=Group(Find("RoomFade"));actor.lightCover=Group(Find("DoorLightCover"));
            actor.foreground=DoorForegroundNames.Select(name=>Group(Find(name))).ToArray();manager.menuEntrance=actor;
            SaveReference(actor);SaveReference(manager);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            ValidateDoorEntrance();ManualEditingMigration.Validate();EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");
            Selection.activeGameObject=Object.FindFirstObjectByType<GameManager>().gameObject;
            Debug.Log("DOOR_ENTRANCE_AUTHORED_PASS");
        }
        static readonly string[] DoorForegroundNames={"Header","HomeTitle","MenuSubtitle","HomeCard","ReportWelcomePortrait","MenuRobotHint","MenuTopicsFooter","MenuSaveFooter","HomeContinue"};
        public static void ValidateDoorEntrance()
        {
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");
            var manager=Object.FindFirstObjectByType<GameManager>();var actor=manager.menuEntrance;
            if(actor==null||actor.leftHinge==null||actor.rightHinge==null||actor.leftHinge.childCount!=4||actor.rightHinge.childCount!=4)throw new Exception("The saved doorway needs two four-part leaves.");
            if(actor.roomPreview==null||actor.designFrame==null||actor.edgeFade==null||actor.lightCover==null||actor.foreground.Length!=DoorForegroundNames.Length||actor.foreground.Any(g=>g==null))throw new Exception("Save the entrance's UI and camera references in MainMenu.");
            if(actor.roomPreview.GetComponent<RawImage>().texture!=actor.lobbyCamera.targetTexture||actor.lightCover.alpha!=0||actor.lightCover.blocksRaycasts)throw new Exception("Idle entrance must retain its preview and release all input.");
            Debug.Log("DOOR_ENTRANCE_REFERENCES_PASS");
        }
        public static void ApplyDoorEntranceAndBuild(){ApplyDoorEntrance();ManualEditingMigration.BuildQA();}
    }
}
#endif
