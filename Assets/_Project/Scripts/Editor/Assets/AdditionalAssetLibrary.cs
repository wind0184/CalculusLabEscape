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
    public static class AdditionalAssetLibrary
    {
        static string PrefabFolder(string source) => "Assets/_Project/Prefabs/ThirdParty/" + source.Substring("Assets/ThirdParty/".Length);
        const string Station="Assets/ThirdParty/Kenney/SpaceStationKit";
        const string Animals="Assets/ThirdParty/Quaternius/FarmAnimals";
        const string LabScene="Assets/_Project/Scenes/LabAssetsPreview.unity";
        const string AnimalScene="Assets/_Project/Scenes/AnimalAssetsPreview.unity";
        static readonly string[] LabSamples={"wall","wall-switch","door-single-closed","computer","computer-wide","display-wall","table","container-tall","chair-armrest-headrest"};
        [MenuItem("Calculus Lab/Assets/Open laboratory asset preview")]
        public static void OpenLab()=>Open(LabScene);
        [MenuItem("Calculus Lab/Assets/Open animal asset preview")]
        public static void OpenAnimals()=>Open(AnimalScene);
        static void Open(string path)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(path);var camera=Object.FindFirstObjectByType<Camera>();var view=SceneView.lastActiveSceneView;
            if(view!=null&&camera!=null)view.AlignViewToObject(camera.transform);
        }
        public static void PrepareAndValidate()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var paletteImporter=(TextureImporter)AssetImporter.GetAtPath(Station+"/Models/Textures/colormap.png");paletteImporter.filterMode=FilterMode.Point;paletteImporter.mipmapEnabled=false;paletteImporter.textureCompression=TextureImporterCompression.Uncompressed;paletteImporter.SaveAndReimport();
            foreach(string root in new[]{Station,Animals})
            {
                Directory.CreateDirectory(root+"/Materials");Directory.CreateDirectory(PrefabFolder(root));AssetDatabase.Refresh();
                foreach(string path in Directory.GetFiles(root+"/Models","*.fbx"))
                {
                    var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importCameras=false;importer.importLights=false;importer.importAnimation=root==Animals;
                    if(root==Animals){importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;}
                    importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
                    var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    foreach(var source in model.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct())
                    {
                        if(AssetDatabase.GetAssetPath(source).StartsWith(root+"/Materials/",StringComparison.Ordinal))continue;
                        Color color=source.HasProperty("_Color")?source.color:Color.white;
                        string safe=new string(source.name.Select(c=>char.IsLetterOrDigit(c)||c=='_'?c:'_').ToArray());string materialPath=root+"/Materials/"+safe+"_"+ColorUtility.ToHtmlStringRGBA(color)+".mat";
                        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                        if(material==null)
                        {
                            var texture=root==Station?AssetDatabase.LoadAssetAtPath<Texture2D>(Station+"/Models/Textures/colormap.png"):source.mainTexture;
                            material=new Material(Shader.Find("Standard")){name=source.name,color=color,mainTexture=texture};material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",.15f);AssetDatabase.CreateAsset(material,materialPath);
                        }
                        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),source.name),material);
                    }
                    importer.SaveAndReimport();
                }
            }
            foreach(string name in LabSamples)CreatePrefab(Station,name,name.StartsWith("wall")||name.StartsWith("door")?3:2.2f);
            CreatePrefab(Animals,"Cow",2.8f);AssetDatabase.SaveAssets();
            if(!File.Exists(LabScene))CreatePreview(false);
            if(!File.Exists(AnimalScene))CreatePreview(true);
            ValidateAndCapture();Debug.Log("ADDITIONAL_ASSET_IMPORT_PASS");
        }
        static AnimationClip[] Clips(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__",StringComparison.Ordinal)).ToArray();
        static Bounds BoundsOf(GameObject model)
        {
            var renderers=model.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)throw new Exception("No renderer: "+model.name);
            var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);return bounds;
        }
        static void CreatePrefab(string folder,string name,float size)
        {
            string path=PrefabFolder(folder)+"/"+name+".prefab";if(File.Exists(path))return;
            string modelPath=folder+"/Models/"+name+".fbx";var source=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);if(source==null)throw new Exception("Model unavailable: "+name);
            var root=new GameObject(name);var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);
            if(folder==Animals)
            {
                var clip=Clips(modelPath).FirstOrDefault(c=>c.name.IndexOf("Idle",StringComparison.OrdinalIgnoreCase)>=0);
                if(clip!=null)clip.SampleAnimation(model,clip.length*.25f);
            }
            var bounds=BoundsOf(model);root.transform.localScale=Vector3.one*(size/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z));bounds=BoundsOf(model);model.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);
        }
        static void CreatePreview(bool animals)
        {
            var font=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Resources/Production/MainMenuUI.prefab").GetComponentsInChildren<TMP_Text>(true).First().font;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.42f,.48f,.54f);
            var cameraObject=new GameObject("Asset preview camera");var camera=cameraObject.AddComponent<Camera>();cameraObject.tag="MainCamera";camera.backgroundColor=new Color(.035f,.07f,.1f);camera.clearFlags=CameraClearFlags.SolidColor;camera.fieldOfView=48;
            camera.transform.position=animals?new Vector3(7,5,-8):new Vector3(12,12,-19);camera.transform.LookAt(animals?new Vector3(0,.8f,0):new Vector3(0,1.2f,4));
            var lightObject=new GameObject("Preview daylight");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.9f;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(45,-30,0);
            string materialPath=Station+"/Materials/PreviewGround.mat";var groundMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(groundMaterial==null){groundMaterial=new Material(Shader.Find("Standard")){color=new Color(.18f,.25f,.28f),name="Preview ground"};AssetDatabase.CreateAsset(groundMaterial,materialPath);}
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Preview ground";floor.transform.position=animals?new Vector3(0,-.2f,0):new Vector3(0,-.2f,4);floor.transform.localScale=animals?new Vector3(14,.4f,9):new Vector3(20,.4f,17);floor.GetComponent<Renderer>().sharedMaterial=groundMaterial;
            var collection=new GameObject(animals?"Cow size comparison":"Imported laboratory samples");
            for(int i=0;i<(animals?2:LabSamples.Length);i++)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder(animals?Animals:Station)+"/"+(animals?"Cow":LabSamples[i])+".prefab");var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);instance.transform.SetParent(collection.transform,false);
                instance.transform.position=animals?new Vector3(i==0?-2.5f:2,0,0):new Vector3((i%3-1)*5.5f,0,(2-i/3)*4.7f);
                if(animals&&i==1){instance.name="Smaller cow / size reference";instance.transform.localScale*=.55f;}
                if(animals)instance.transform.rotation=Quaternion.Euler(0,25,0);
                else if(LabSamples[i]=="wall-switch")instance.transform.rotation=Quaternion.Euler(0,90,0);
            }
            var canvasObject=new GameObject("Asset preview labels",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            void Label(string name,string value,int y,int height,int size)
            {
                var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(canvas.transform,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(30,-y);rect.sizeDelta=new Vector2(1220,height);var text=go.GetComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.text=value;text.color=Color.white;text.raycastTarget=false;
            }
            Label("Title",animals?"COW MODEL / SIZE COMPARISON":"LABORATORY ASSET LIBRARY",18,45,30);
            Label("Subtitle",animals?"Quaternius Farm Animals / CC0   |   Imported rig and animation clips":"Kenney Space Station Kit / CC0   |   97 models / Built-in materials",64,30,17);
            Label("Details",animals?"Same cow model shown at two sizes. The smaller copy is a scale reference.\nThis pack does not contain a separate calf model. Gameplay rescue is unchanged.":"Walls / doors / computers / displays / table / storage / chair\nEdit Mode preview. Gameplay environments are unchanged.",635,66,17);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,animals?AnimalScene:LabScene);
        }
        static void Capture(string scenePath,string imagePath,Action pose=null)
        {
            EditorSceneManager.OpenScene(scenePath);pose?.Invoke();if(Object.FindFirstObjectByType<Chapter4Game>()?.Session!=null||Object.FindFirstObjectByType<Chapter4World>()!=null)throw new Exception("Gameplay created in asset preview");
            foreach(var label in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)){label.ForceMeshUpdate();if(label.isTextOverflowing)throw new Exception("Preview text overflow: "+label.name);}
            var camera=Object.FindFirstObjectByType<Camera>();var target=RenderTexture.GetTemporary(1280,720,24);var active=RenderTexture.active;
            try{camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(imagePath,pixels.EncodeToPNG());Object.DestroyImmediate(pixels);}
            finally{camera.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
            EditorSceneManager.OpenScene(scenePath);
        }
        public static void ValidateAndCapture()
        {
            string evidence=Path.GetFullPath("../../qa/additional-assets");Directory.CreateDirectory(evidence);var report=new System.Text.StringBuilder();
            foreach(string root in new[]{Station,Animals})
            {
                var paths=Directory.GetFiles(root+"/Models","*.fbx");if(paths.Length!=(root==Station?97:7))throw new Exception("Model count differs: "+root);
                foreach(string path in paths)
                {
                    var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(BoundsOf(model).size.sqrMagnitude<=0)throw new Exception("Empty model: "+path);
                    foreach(var material in model.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials))
                        if(material==null||material.shader==null||material.shader.name!="Standard"||!AssetDatabase.GetAssetPath(material).StartsWith(root+"/Materials/",StringComparison.Ordinal))throw new Exception("Material unavailable: "+path);
                    if(root==Station&&model.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Any(m=>m.mainTexture==null))throw new Exception("Station palette unavailable");
                    if(root==Animals)foreach(var clip in Clips(path))report.AppendLine(Path.GetFileName(path)+" / "+clip.name+" / "+clip.length.ToString("0.000",System.Globalization.CultureInfo.InvariantCulture)+" seconds");
                }
            }
            string cowPath=Animals+"/Models/Cow.fbx";var cow=AssetDatabase.LoadAssetAtPath<GameObject>(cowPath);
            if(cow.GetComponentInChildren<SkinnedMeshRenderer>()==null||Clips(cowPath).Length==0)throw new Exception("Cow rig or animation unavailable");
            File.WriteAllText(Path.Combine(evidence,"animation-clips.txt"),report.ToString());Capture(LabScene,Path.Combine(evidence,"laboratory-preview.png"));Capture(AnimalScene,Path.Combine(evidence,"cow-preview.png"));EditorSceneManager.OpenScene(LabScene);
            File.WriteAllText(Path.Combine(evidence,"result.txt"),"PASS: 97 textured station FBX models; 7 animal FBX models; persistent Built-in Standard materials; imported cow skin and animation clips; 9 laboratory sample prefabs and 1 cow prefab; two Edit Mode preview scenes without gameplay; labels fit.\n");
        }
        public static void CheckWalkAndRefreshPreview()
        {
            var scene=EditorSceneManager.OpenScene(LabScene);GameObject.Find("wall-switch").transform.rotation=Quaternion.Euler(0,90,0);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);ValidateAndCapture();
            var walk=Clips(Animals+"/Models/Cow.fbx").First(c=>c.name.EndsWith("|Walk",StringComparison.Ordinal));
            string folder=Path.GetFullPath("../../qa/additional-assets");
            Capture(AnimalScene,Path.Combine(folder,"cow-walk-pose.png"),()=>
            {
                foreach(var animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
                {
                    var bones=animator.GetComponentInChildren<SkinnedMeshRenderer>().bones;var posePosition=animator.transform.localPosition;var poseRotation=animator.transform.localRotation;var poseScale=animator.transform.localScale;
                    walk.SampleAnimation(animator.gameObject,walk.length*.05f);var rotations=bones.Select(b=>b.localRotation).ToArray();
                    walk.SampleAnimation(animator.gameObject,walk.length*.35f);
                    if(!bones.Where((bone,i)=>Quaternion.Angle(bone.localRotation,rotations[i])>.1f).Any())throw new Exception("Cow walk did not animate bones");
                    animator.transform.localPosition=posePosition;animator.transform.localRotation=poseRotation;animator.transform.localScale=poseScale;
                }
                GameObject.Find("Subtitle").GetComponent<TMP_Text>().text="Imported Walk / sampled at 35% without Play";
            });
            File.AppendAllText(Path.Combine(folder,"result.txt"),"PASS: Cow Walk sampled at two times changes bone rotations; Edit Mode walk pose captured; canonical cow preview remains unchanged.\n");
            EditorSceneManager.OpenScene(LabScene);Debug.Log("ADDITIONAL_ASSET_IMPORT_PASS");
        }
    }
}
#endif
