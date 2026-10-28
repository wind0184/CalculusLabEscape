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
    // A separate, static design study; production UI and gameplay are not restyled.
    public static class LightLabStyleStudy
    {
        const string Folder="Assets/_Project/EditorPreviews/LightLab";
        const string PrefabFolder="Assets/_Project/Prefabs/Studies/LightLab";
        static readonly Color Ink=new Color(.12f,.22f,.23f),Muted=new Color(.32f,.44f,.44f),Jade=new Color(.055f,.43f,.37f),Green=new Color(.11f,.44f,.28f);
        static readonly string[] Primary={"RepairSubmit","RepairContinue","CalibrationSubmit","CalibrationNext","Rescue","MasterActivate","MasterCover","BriefStart"};

        [MenuItem("Calculus Lab/UI studies/Generate light laboratory previews")]
        public static void Generate()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Directory.CreateDirectory(Folder);Directory.CreateDirectory(PrefabFolder);AssetDatabase.Refresh();
            string evidence=Path.GetFullPath("../../qa/light-lab-style");Directory.CreateDirectory(evidence);
            foreach(int topic in new[]{1,2})foreach(string view in new[]{"Independent check","Check accepted"})
            {
                string scenePath=SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity";
                EditorSceneManager.OpenScene(scenePath);
                try
                {
                    var world=UnityEngine.Object.FindFirstObjectByType<Chapter4World>();var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
                    PrefabUtility.UnpackPrefabInstance(canvas.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                    PrepareStudyGraphics(canvas.gameObject);
                    SceneAuthoringWindow.ApplyView(world,canvas,view);
                    int buttons=canvas.GetComponentsInChildren<Button>(true).Length,labels=canvas.GetComponentsInChildren<TMP_Text>(true).Length;
                    ApplyPalette(canvas.gameObject,view=="Check accepted");
                    SampleProgress(canvas.gameObject,topic,view=="Check accepted");
                    string name=(topic==1?"Bridge":"Integration")+"_LightLab_"+(view=="Check accepted"?"Accepted":"Question");
                    SetMonitorSnapshot(world,canvas,topic==1?"Bridge":"Integration");
                    canvas.GetComponent<MissionCanvas>().enabled=false;
                    Canvas.ForceUpdateCanvases();
                    foreach(var surface in canvas.GetComponentsInChildren<SimulationAnswerBackdrop>())
                    {
                        if(surface.GetComponent<CanvasRenderer>()==null)surface.gameObject.AddComponent<CanvasRenderer>();
                        surface.canvasRenderer.cull=false;surface.canvasRenderer.SetColor(Color.white);surface.SetAllDirty();surface.Rebuild(CanvasUpdate.PreRender);
                        var mesh=surface.canvasRenderer.GetMesh();
                        if(mesh==null||mesh.vertexCount==0)throw new Exception("Unrendered study surface: "+surface.name);
                    }
                    foreach(var text in canvas.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();if(text.isTextOverflowing)throw new Exception("Light study overflow: "+name+" / "+text.name);}
                    if(canvas.GetComponentsInChildren<Button>(true).Length!=buttons||canvas.GetComponentsInChildren<TMP_Text>(true).Length!=labels)throw new Exception("Study changed question controls or labels.");
                    if(UnityEngine.Object.FindFirstObjectByType<Chapter4Game>()?.Session!=null)throw new Exception("Style preview started a gameplay session.");
                    PrefabUtility.SaveAsPrefabAsset(canvas.gameObject,PrefabFolder+"/"+name+".prefab");
                    SceneAuthoring.Capture(world,canvas,Path.Combine(evidence,name+".png"),false);
                }
                finally{EditorSceneManager.OpenScene(scenePath);}
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText(Path.Combine(evidence,"result.txt"),"PASS: four native Unity light-theme study previews; question/control counts preserved; active text fits; monitor images saved as persistent assets; no gameplay session; original scenes reopened without saving. Study presenters disabled, production UI and players unchanged.");
            Debug.Log("LIGHT_LAB_STYLE_STUDY_PASS");
        }
        static void PrepareStudyGraphics(GameObject root)
        {
            // Source graphics acquire renderers lazily in Play. Study prefabs need
            // persistent renderers before Graphic.OnEnable runs in Edit Mode.
            foreach(var old in root.GetComponentsInChildren<SimulationAnswerBackdrop>(true))
            {
                var owner=old.gameObject;string values=EditorJsonUtility.ToJson(old);
                UnityEngine.Object.DestroyImmediate(old);
                if(owner.GetComponent<CanvasRenderer>()==null)owner.AddComponent<CanvasRenderer>();
                var graphic=owner.AddComponent<SimulationAnswerBackdrop>();EditorJsonUtility.FromJsonOverwrite(values,graphic);
            }
        }
        static void SampleProgress(GameObject root,int topic,bool accepted)
        {
            void Text(string name,string value)=>root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name==name).text=value;
            Text("SecondaryProgress","Accepted steps: "+(topic==1?(accepted?14:13):(accepted?2:1))+"/18");
            if(topic==1){Text("Component0","[OK] Power regulator");Text("Component1","[OK] Bridge component");Text("Component2","[  ] Bridge anchor");}
        }
        static void SetMonitorSnapshot(Chapter4World world,Canvas canvas,string topic)
        {
            string path=Folder+"/"+topic+"Monitor.png";
            var target=world.Preview;var previous=RenderTexture.active;Texture2D pixels=null;
            try
            {
                RenderTexture.active=target;pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
            }
            finally{RenderTexture.active=previous;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);}
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.mipmapEnabled=false;importer.SaveAndReimport();
            canvas.GetComponentsInChildren<RawImage>(true).Single(i=>i.name=="ScenePreview").texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static void ApplyPalette(GameObject root,bool accepted)
        {
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                float alpha=image.color.a;
                image.color=image.color.g>.6f&&image.color.r<.4f?new Color(Jade.r,Jade.g,Jade.b,alpha):new Color(.95f,.97f,.95f,alpha);
                var border=image.GetComponent<Outline>();if(border!=null){border.effectColor=new Color(.66f,.77f,.73f);border.effectDistance=new Vector2(1,-1);}
            }
            foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                var old=label.color;
                label.color=old.g>.7f&&old.r<.6f&&old.b<.7f?Green:old.g>.65f&&old.b>.65f&&old.r<.5f?Jade:old.r>.4f&&old.r<.75f?Muted:Ink;
            }
            foreach(var button in root.GetComponentsInChildren<Button>(true))
            {
                bool primary=Primary.Contains(button.name);var fill=button.GetComponent<Image>();
                if(fill!=null)fill.color=primary?Jade:new Color(.98f,.985f,.97f);
                foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))label.color=primary?new Color(.97f,.99f,.97f):Ink;
                var colors=button.colors;colors.highlightedColor=new Color(.88f,.95f,.92f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.76f,.88f,.83f);button.colors=colors;
                bool selected=button.GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text.StartsWith("[X]",StringComparison.Ordinal));
                if(selected&&fill!=null)
                {
                    fill.color=accepted?new Color(.84f,.94f,.87f):new Color(.86f,.95f,.92f);
                    var border=button.GetComponent<Outline>();if(border!=null){border.effectColor=accepted?Green:Jade;border.effectDistance=new Vector2(2,-2);}
                }
            }
            foreach(var field in root.GetComponentsInChildren<TMP_InputField>(true))
            {field.caretColor=Jade;field.selectionColor=new Color(.18f,.55f,.43f,.2f);field.textComponent.color=Ink;if(field.placeholder!=null)field.placeholder.color=Muted;}
            var all=root.GetComponentsInChildren<Transform>(true);
            var art=all.Single(t=>t.name=="SimulationAnswerArt");
            art.Find("SimulationAnswerSky").gameObject.SetActive(false);
            var veil=new GameObject("LightStudyAtmosphereVeil",typeof(RectTransform),typeof(Image));var rect=veil.GetComponent<RectTransform>();rect.SetParent(art,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(1280,720);rect.SetSiblingIndex(1);
            var shade=veil.GetComponent<Image>();shade.color=new Color(.86f,.92f,.89f,.92f);shade.raycastTarget=false;
            foreach(var surface in root.GetComponentsInChildren<SimulationAnswerBackdrop>(true))
            {
                surface.lightTint=Jade;surface.consoleDetails=false;surface.roomDetails=false;
                if(surface.panel){surface.color=new Color(.955f,.97f,.945f,.96f);surface.rim=new Color(.62f,.75f,.69f,.7f);surface.cornerRadius=24;}
                surface.SetVerticesDirty();
            }
            all.Single(t=>t.name=="Header").GetComponent<Image>().color=new Color(.97f,.98f,.95f,.98f);
            foreach(var phase in all.Where(t=>t.name.StartsWith("JourneyPhase",StringComparison.Ordinal)&&t.TryGetComponent<Image>(out _)))
            {var label=phase.GetComponentInChildren<TMP_Text>();phase.GetComponent<Image>().color=label.text.StartsWith(">",StringComparison.Ordinal)?new Color(.82f,.92f,.86f):new Color(.91f,.95f,.91f);}
        }
        [MenuItem("Calculus Lab/UI studies/Open light laboratory - Bridge")]
        public static void OpenBridge()=>Open("Bridge");
        [MenuItem("Calculus Lab/UI studies/Open light laboratory - Integration")]
        public static void OpenIntegration()=>Open("Integration");
        static void Open(string topic)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder+"/"+topic+"_LightLab_Question.prefab");
            if(prefab==null){Debug.LogWarning("Generate the light laboratory previews first.");return;}
            Selection.activeObject=prefab;AssetDatabase.OpenAsset(prefab);
        }
    }
}
#endif
