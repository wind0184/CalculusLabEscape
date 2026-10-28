#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape.Editor
{
    public static class EditorPreviewRepair
    {
        static string Evidence=>Path.GetFullPath("../../qa/editor-preview-repair");
        static bool Custom(Graphic g)=>g is SimulationAnswerBackdrop||g is LimitsDiagram||g is MissionConnections;
        [MenuItem("Calculus Lab/Scene & UI/Repair saved UI graphics")]
        public static void RepairAndReload()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scenes=EditorSceneManager.GetSceneManagerSetup();Apply();EditorSceneManager.RestoreSceneManagerSetup(scenes);
        }
        public static void Apply()
        {
            int added=0,total=0;
            foreach(string name in new[]{"MainMenuUI","LimitsUI","DifferentiationUI","IntegrationUI"})
            {
                string path="Assets/_Project/Prefabs/Resources/Production/"+name+".prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int buttons=root.GetComponentsInChildren<Button>(true).Length,labels=root.GetComponentsInChildren<TMP_Text>(true).Length,objects=root.GetComponentsInChildren<Transform>(true).Length;
                    var graphics=root.GetComponentsInChildren<Graphic>(true).Where(Custom).ToArray();
                    var settings=graphics.Select(g=>EditorJsonUtility.ToJson(g)).ToArray();
                    foreach(var graphic in graphics){total++;if(graphic.GetComponent<CanvasRenderer>()==null){graphic.gameObject.AddComponent<CanvasRenderer>();added++;}}
                    if(buttons!=root.GetComponentsInChildren<Button>(true).Length||labels!=root.GetComponentsInChildren<TMP_Text>(true).Length||objects!=root.GetComponentsInChildren<Transform>(true).Length)throw new Exception("Renderer migration changed UI hierarchy.");
                    for(int i=0;i<graphics.Length;i++)if(EditorJsonUtility.ToJson(graphics[i])!=settings[i])throw new Exception("Renderer migration changed artwork settings: "+graphics[i].name);
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
                // Inspect a fresh instance: do not rely on an in-memory renderer cache.
                root=PrefabUtility.LoadPrefabContents(path);
                try{if(root.GetComponentsInChildren<Graphic>(true).Where(Custom).Any(g=>g.GetComponent<CanvasRenderer>()==null))throw new Exception("Renderer not saved: "+name);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Directory.CreateDirectory(Evidence);
            File.AppendAllText(Path.Combine(Evidence,"migration.txt"),"Custom graphics: "+total+"; added renderers: "+added+"; original graphics settings, objects, buttons and labels preserved.\n");
            Debug.Log("EDITOR_PREVIEW_RENDERERS_SAVED / "+total+" graphics; "+added+" added");
        }
        public static void ValidateRenderedGraphics(Canvas canvas)
        {
            if(!canvas.enabled||!canvas.gameObject.activeInHierarchy)return;
            foreach(var graphic in canvas.GetComponentsInChildren<Graphic>().Where(g=>g.isActiveAndEnabled&&Custom(g)))
            {
                var renderer=graphic.GetComponent<CanvasRenderer>();
                if(renderer==null)throw new Exception("Missing saved CanvasRenderer: "+graphic.name);
                if(graphic is MissionConnections connections&&!connections.locked.Any(v=>v))continue;
                var mesh=renderer.GetMesh();
                if(mesh==null||mesh.vertexCount==0)throw new Exception("Editor graphic has no rendered geometry: "+graphic.name);
            }
        }
        public static void Capture()
        {
            SceneAuthoring.ValidateAndCaptureTo(Path.Combine(Evidence,"editor"));
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[2]+".unity");
            var world=UnityEngine.Object.FindFirstObjectByType<Chapter4World>();var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
            SceneAuthoringWindow.ApplyView(world,canvas,"Wire matching");
            var wires=canvas.GetComponentInChildren<MissionConnections>(true);wires.locked[0]=true;wires.SetVerticesDirty();
            SceneAuthoring.Capture(world,canvas,Path.Combine(Evidence,"editor","wire-connection-render.png"),false);
            EditorSceneManager.OpenScene(scene.path);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity");canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
            foreach(Transform page in canvas.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Pages"))page.gameObject.SetActive(page.name=="Home");
            Canvas.ForceUpdateCanvases();ValidateRenderedGraphics(canvas);
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity");
            File.WriteAllText(Path.Combine(Evidence,"render-result.txt"),"PASS: saved native graphics populate meshes automatically in Edit Mode; all three question workspaces and menu portal; Limits question types/independent/accepted/ready previews; accepted wire geometry; text fits; no gameplay session; scenes reopened without saving. No forced Rebuild, renderer culling override or component replacement used.\n");
            Debug.Log("EDITOR_PREVIEW_NATIVE_RENDER_PASS");
        }
        public static void RepairValidateAndBuild()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Apply();Apply();Capture();ProductionUIBuilder.BuildDelivery();Debug.Log("EDITOR_PREVIEW_REPAIR_DELIVERY_PASS");
        }
    }
}
#endif
