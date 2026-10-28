#if UNITY_EDITOR
using System;
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
    public static partial class ReportFusionLayout
    {
        static string WelcomeEvidence=>LearningApi.Argument("--welcome-evidence",Path.GetFullPath("../../qa/main-menu-welcome-2026-10-24"));
        public static void AuthorMenuWelcome()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before editing the menu.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save current scene edits first.");
            Directory.CreateDirectory(WelcomeEvidence);var ui=PrefabUtility.LoadPrefabContents(Prefabs+"MainMenuUI.prefab");
            try{
                foreach(var old in ui.GetComponentsInChildren<MainMenuWelcome>(true))Object.DestroyImmediate(old.gameObject);Index(ui);
                var card=Surface(null,"MenuWelcome",660,468,168,74,Ink);card.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Rounded.png");card.type=Image.Type.Sliced;
                var outline=card.gameObject.AddComponent<Outline>();outline.effectColor=Cyan;outline.effectDistance=new Vector2(1,-1);
                var group=card.gameObject.AddComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
                var tail=Surface(card.transform,"MenuWelcomeTail",-5,29,12,12,Ink);tail.rectTransform.localRotation=Quaternion.Euler(0,0,45);
                var text=Label(card.transform,"MenuWelcomeMessage","Ready?\nLet's go!",14,11,140,52,18,Background);text.alignment=TextAlignmentOptions.Center;text.enableAutoSizing=false;
                var welcome=card.gameObject.AddComponent<MainMenuWelcome>();welcome.surface=group;welcome.message=text;welcome.Preview();
                PrefabUtility.SaveAsPrefabAsset(card.gameObject,Prefabs+"MainMenuWelcome.prefab");Object.DestroyImmediate(card.gameObject);Index(ui);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"MainMenuWelcome.prefab"),ui.scene);instance.transform.SetParent(Find("Home"),false);instance.transform.SetAsLastSibling();PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                Find("MenuRobotHint").gameObject.SetActive(false);ManualEditingMigration.ConfigureCanvas(ui);PrefabUtility.SaveAsPrefabAsset(ui,Prefabs+"MainMenuUI.prefab");
            }finally{PrefabUtility.UnloadPrefabContents(ui);}
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");var manager=Object.FindFirstObjectByType<GameManager>();var controller=manager.menuOrLimitsUI.GetComponentInChildren<MainMenuWelcome>(true);controller.entrance=manager.menuEntrance;
            Index(manager.menuOrLimitsUI.gameObject);Find("MenuRobotHint").gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(Find("MenuRobotHint").gameObject);SaveReference(controller);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();ValidateMenuWelcome();CaptureMenuWelcome();ManualEditingMigration.BuildQA();
        }
        public static void ValidateMenuWelcome()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play before checking saved references.");
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save current scene edits first.");
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");var manager=Object.FindFirstObjectByType<GameManager>();var controller=manager.menuOrLimitsUI.GetComponentInChildren<MainMenuWelcome>(true);
            if(controller==null||controller.message==null||controller.surface==null||controller.entrance!=manager.menuEntrance||!controller.gameObject.activeSelf||controller.surface.blocksRaycasts||controller.GetComponentsInChildren<Graphic>(true).Any(g=>g.raycastTarget))throw new Exception("Save a non-blocking welcome message on Home.");
            ManualEditingMigration.Validate();Directory.CreateDirectory(WelcomeEvidence);File.WriteAllText(Path.Combine(WelcomeEvidence,"saved-welcome.txt"),"PASS: saved Home-only welcome prefab, non-blocking graphics, existing entrance reference; original menu buttons and scene references valid. Start explicitly hides the greeting before Timeline.");
        }
        public static void CaptureMenuWelcome()
        {
            string path=SceneAuthoring.SceneFolder+"MainMenu.unity";EditorSceneManager.OpenScene(path);var manager=Object.FindFirstObjectByType<GameManager>();var root=manager.menuOrLimitsUI;var canvas=root.GetComponent<Canvas>();Index(root.gameObject);foreach(Transform page in Find("Pages"))page.gameObject.SetActive(page.name=="Home");root.GetComponentInChildren<MainMenuWelcome>(true).Preview();
            foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))if(camera.isActiveAndEnabled&&camera.targetTexture!=null)camera.Render();
            var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float distance=canvas.planeDistance;var transforms=root.GetComponentsInChildren<Transform>(true);var layers=transforms.Select(t=>t.gameObject.layer).ToArray();var active=RenderTexture.active;
            var target=RenderTexture.GetTemporary(1280,720,24);var go=new GameObject("Welcome preview camera",typeof(Camera));var lens=go.GetComponent<Camera>();lens.orthographic=true;lens.orthographicSize=360;lens.nearClipPlane=.1f;lens.farClipPlane=10;lens.clearFlags=CameraClearFlags.SolidColor;lens.backgroundColor=Background;lens.cullingMask=1<<31;lens.enabled=false;Texture2D pixels=null;
            try{
                foreach(var node in transforms)node.gameObject.layer=31;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=lens;canvas.planeDistance=1;lens.targetTexture=target;Canvas.ForceUpdateCanvases();lens.Render();RenderTexture.active=target;pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(WelcomeEvidence,"main-menu-welcome.png"),pixels.EncodeToPNG());
            }finally{for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=distance;RenderTexture.active=active;Object.DestroyImmediate(go);if(pixels!=null)Object.DestroyImmediate(pixels);RenderTexture.ReleaseTemporary(target);EditorSceneManager.OpenScene(path);}
        }
        public static void OpenMenuWelcome()
        {
            OpenMainMenu();Object.FindFirstObjectByType<ProductionCanvas>().GetComponentInChildren<MainMenuWelcome>(true).Preview();EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
    }
}
#endif
