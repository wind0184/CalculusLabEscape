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
    public static class KenneyAssetLibrary
    {
        public const string Root="Assets/ThirdParty/Kenney";
        public const string PrefabRoot="Assets/_Project/Prefabs/ThirdParty/Kenney";
        public const string PreviewScene="Assets/_Project/Scenes/AssetLibraryPreview.unity";
        static readonly string[] Samples={"tree_pineDefaultA","tree_pineRoundA","tree_pineSmallA","tree_oak","tree_simple","rock_largeA","rock_largeC","grass_large","bridge_center_wood","bridge_side_wood"};

        [MenuItem("Calculus Lab/Assets/Open imported asset preview")]
        public static void OpenPreview()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(PreviewScene);var view=SceneView.lastActiveSceneView;
            if(view!=null)view.LookAt(new Vector3(0,1,3),Quaternion.Euler(35,0,0),18);
        }

        public static void PrepareAndValidate()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Directory.CreateDirectory(Root+"/NatureKit/Materials");Directory.CreateDirectory(PrefabRoot+"/NatureKit");AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(Root+"/NatureKit/Models","*.fbx"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach(var source in model.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct())
                {
                    if(AssetDatabase.GetAssetPath(source).StartsWith(Root+"/NatureKit/Materials/",StringComparison.Ordinal))continue;
                    Color color=source.HasProperty("_Color")?source.color:Color.white;
                    string safe=new string(source.name.Select(c=>char.IsLetterOrDigit(c)||c=='_'?c:'_').ToArray());
                    string materialPath=Root+"/NatureKit/Materials/"+safe+"_"+ColorUtility.ToHtmlStringRGBA(color)+".mat";
                    var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if(material==null)
                    {
                        material=new Material(Shader.Find("Standard")){name=source.name,color=color};
                        if(source.HasProperty("_MainTex"))material.mainTexture=source.mainTexture;
                        material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",.15f);AssetDatabase.CreateAsset(material,materialPath);
                    }
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),source.name),material);
                }
                importer.SaveAndReimport();
            }
            foreach(string path in Directory.GetFiles(Root+"/SciFiUI/Sprites","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            foreach(string path in Directory.GetFiles(Root+"/InterfaceSounds/Audio","*.ogg"))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);importer.forceToMono=true;var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            }
            CreateSamplePrefabs();AssetDatabase.SaveAssets();
            if(!File.Exists(PreviewScene))CreatePreview();
            ValidateAndCapture();Debug.Log("KENNEY_ASSET_IMPORT_PASS");
        }

        static Bounds BoundsOf(GameObject model)
        {
            var renderers=model.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new Exception("No renderers: "+model.name);
            var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);return bounds;
        }
        static void CreateSamplePrefabs()
        {
            foreach(string name in Samples)
            {
                string path=PrefabRoot+"/NatureKit/"+name+".prefab";if(File.Exists(path))continue;
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/NatureKit/Models/"+name+".fbx");if(source==null)throw new Exception("Sample model unavailable: "+name);
                var root=new GameObject(name);var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);
                var bounds=BoundsOf(model);float size=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
                model.transform.localScale*=((name.StartsWith("tree")?4.5f:name.StartsWith("bridge")?4:2)/size);
                bounds=BoundsOf(model);model.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);
            }
        }
        static Material PreviewMaterial(string name,Color color)
        {
            string path=Root+"/NatureKit/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            material=new Material(Shader.Find("Standard")){name=name,color=color};material.SetFloat("_Glossiness",.1f);AssetDatabase.CreateAsset(material,path);return material;
        }
        static void CreatePreview()
        {
            var font=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Resources/Production/MainMenuUI.prefab").GetComponentsInChildren<TMP_Text>(true).First().font;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.42f,.48f,.54f);
            var cameraObject=new GameObject("Asset preview camera");var camera=cameraObject.AddComponent<Camera>();cameraObject.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.07f,.1f);camera.fieldOfView=48;
            camera.transform.position=new Vector3(14,15,-24);camera.transform.LookAt(new Vector3(0,1.6f,3));
            var lightObject=new GameObject("Preview daylight");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.9f;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(45,-30,0);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Preview ground";floor.transform.position=new Vector3(0,-.2f,3);floor.transform.localScale=new Vector3(29,.4f,18);floor.GetComponent<Renderer>().sharedMaterial=PreviewMaterial("PreviewGround",new Color(.21f,.3f,.28f));
            var collection=new GameObject("Imported model samples");
            for(int i=0;i<Samples.Length;i++)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot+"/NatureKit/"+Samples[i]+".prefab");var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);instance.transform.SetParent(collection.transform,false);instance.transform.position=new Vector3((i%5-2)*5,0,i<5?7:0);
            }
            var canvasObject=new GameObject("Asset preview UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            TMP_Text Label(string name,string content,float x,float y,float width,float height,int size)
            {
                var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(canvas.transform,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
                var text=go.GetComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.text=content;text.color=Color.white;text.raycastTarget=false;return text;
            }
            Label("Title","IMPORTED ASSET LIBRARY",28,18,1000,45,30);Label("Subtitle","Kenney / CC0   |   329 models   /   60 blue UI sprites   /   100 sounds",30,64,1160,30,17);
            Label("Instructions","Edit Mode preview / Drag NatureKit prefabs into your scene.\nSelect an InterfaceSounds audio clip in Project to listen.",30,637,875,66,17);
            var uiSample=new GameObject("Imported button artwork",typeof(RectTransform),typeof(Image));uiSample.transform.SetParent(canvas.transform,false);var uiRect=uiSample.GetComponent<RectTransform>();uiRect.anchorMin=uiRect.anchorMax=new Vector2(0,1);uiRect.pivot=new Vector2(0,1);uiRect.anchoredPosition=new Vector2(978,-612);uiRect.sizeDelta=new Vector2(268,75);
            var image=uiSample.GetComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/SciFiUI/Sprites/button_square_header_large_rectangle.png");image.preserveAspect=true;image.raycastTarget=false;
            var sampleText=Label("Button sample label","UI SPRITE SAMPLE",991,646,240,26,16);sampleText.alignment=TextAlignmentOptions.Center;sampleText.color=new Color(.035f,.07f,.1f);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,PreviewScene);
        }

        public static void ValidateAndCapture()
        {
            var modelPaths=Directory.GetFiles(Root+"/NatureKit/Models","*.fbx");var spritePaths=Directory.GetFiles(Root+"/SciFiUI/Sprites","*.png");var audioPaths=Directory.GetFiles(Root+"/InterfaceSounds/Audio","*.ogg");
            if(modelPaths.Length!=329||spritePaths.Length!=60||audioPaths.Length!=100)throw new Exception("Imported asset counts differ");
            foreach(string path in modelPaths)
            {
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var bounds=BoundsOf(model);if(bounds.size.sqrMagnitude<=0)throw new Exception("Empty model: "+path);
                foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)
                    if(material==null||material.shader==null||material.shader.name!="Standard"||!AssetDatabase.GetAssetPath(material).StartsWith(Root+"/NatureKit/Materials/",StringComparison.Ordinal))throw new Exception("Missing Built-in material: "+path);
            }
            foreach(string path in spritePaths)if(AssetDatabase.LoadAssetAtPath<Sprite>(path)==null)throw new Exception("Sprite unavailable: "+path);
            foreach(string path in audioPaths){var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(clip==null||clip.length<=0)throw new Exception("Audio unavailable: "+path);}
            EditorSceneManager.OpenScene(PreviewScene);
            if(Object.FindFirstObjectByType<Chapter4Game>()?.Session!=null||Object.FindFirstObjectByType<Chapter4World>()!=null)throw new Exception("Asset preview must not create gameplay");
            foreach(var label in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)){label.ForceMeshUpdate();if(label.isTextOverflowing)throw new Exception("Preview text overflow: "+label.name);}
            string folder=Path.GetFullPath("../../qa/asset-import");Directory.CreateDirectory(folder);
            var camera=Object.FindFirstObjectByType<Camera>();var target=RenderTexture.GetTemporary(1280,720,24);var active=RenderTexture.active;
            try
            {
                camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,"asset-library-preview.png"),pixels.EncodeToPNG());Object.DestroyImmediate(pixels);
            }
            finally{camera.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
            EditorSceneManager.OpenScene(PreviewScene);
            File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: 329 FBX models with persistent Built-in Standard materials; 60 Sprite assets; 100 nonempty AudioClips; 10 reusable sample prefabs; saved preview scene without gameplay or session; labels fit.\n");
        }

        public static void UpdatePreviewPresentationAndValidate()
        {
            var scene=EditorSceneManager.OpenScene(PreviewScene);RenderSettings.ambientLight=new Color(.42f,.48f,.54f);
            GameObject.Find("Preview daylight").GetComponent<Light>().intensity=.9f;
            var label=GameObject.Find("Button sample label").GetComponent<TMP_Text>();label.color=new Color(.035f,.07f,.1f);
            var rect=label.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(991,-646);rect.sizeDelta=new Vector2(240,26);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);ValidateAndCapture();Debug.Log("KENNEY_ASSET_IMPORT_PASS");
        }
    }
}
#endif
