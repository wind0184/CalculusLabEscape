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
        const string MenuArt="Assets/_Project/Art/UI/MainMenu/";
        static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var color);return color;}
        static Transform lobby;
        static Material RoomMaterial(string hex)
        {
            string path=MenuArt+"Room-"+hex+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Unlit/Color"));mat.color=Hex(hex);AssetDatabase.CreateAsset(mat,path);}
            return mat;
        }
        static Transform Box(Transform parent,string name,Vector3 position,Vector3 size,string color)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=30;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;
            Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=RoomMaterial(color);return go.transform;
        }
        static void Segment(Transform parent,string name,Vector3 a,Vector3 b,string color,float width=.008f)
        {
            var line=new GameObject(name,typeof(LineRenderer)).GetComponent<LineRenderer>();line.gameObject.layer=30;line.transform.SetParent(parent,false);line.useWorldSpace=false;line.positionCount=2;line.SetPositions(new[]{a,b});line.startWidth=line.endWidth=width;line.sharedMaterial=RoomMaterial(color);line.numCapVertices=2;
        }
        static void RoomText(Transform parent,string name,string value,Vector3 pos,float size,float width,float height)
        {
            var label=new GameObject(name,typeof(TextMeshPro)).GetComponent<TextMeshPro>();label.gameObject.layer=30;label.transform.SetParent(parent,false);label.transform.localPosition=pos;label.transform.localRotation=Quaternion.Euler(0,180,0);label.rectTransform.sizeDelta=new Vector2(width,height);label.font=font;label.text=value;label.fontSize=size;label.color=Hex("b4d4dc");label.alignment=TextAlignmentOptions.Center;label.textWrappingMode=TextWrappingModes.NoWrap;
        }
        static void Poster(string name,Vector3 position,float yaw,bool integral)
        {
            var panel=new GameObject(name).transform;panel.SetParent(lobby,false);panel.localPosition=position;panel.localRotation=Quaternion.Euler(0,yaw,0);
            Box(panel,"Poster frame",Vector3.zero,new Vector3(2.04f,1.69f,.018f),"456775");
            Box(panel,"Poster board",new Vector3(0,0,.014f),new Vector3(2,1.65f,.012f),"17333f");
            RoomText(panel,"Subject",integral?"INTEGRATION":"DIFFERENTIATION",new Vector3(0,.64f,.03f),.95f,1.9f,.18f);
            RoomText(panel,"Formula",integral?"∫ f(x) dx":"d/dx   x² = 2x",new Vector3(0,.29f,.03f),2.3f,1.9f,.4f);
            for(int i=0;i<5;i++){
                float x=-.78f+i*.39f;Segment(panel,"Graph grid vertical "+i,new Vector3(x,-.65f,.032f),new Vector3(x,.02f,.032f),"294e5b",.004f);
                float y=-.65f+i*.1675f;Segment(panel,"Graph grid horizontal "+i,new Vector3(-.78f,y,.032f),new Vector3(.78f,y,.032f),"294e5b",.004f);
            }
            for(int i=0;i<30;i++){
                float x=i/30f,x2=(i+1)/30f;Segment(panel,"Function "+i,new Vector3(.78f-x*1.56f,-.6f+x*x*.57f,.04f),new Vector3(.78f-x2*1.56f,-.6f+x2*x2*.57f,.04f),"9cc6d1",.009f);
                if(integral&&i%2==0)Segment(panel,"Area strip "+i,new Vector3(.78f-x*1.56f,-.65f,.035f),new Vector3(.78f-x*1.56f,-.6f+x*x*.57f,.035f),"4b7c8b",.012f);
            }
        }
        static RenderTexture AuthorLobby()
        {
            Directory.CreateDirectory(MenuArt);AssetDatabase.Refresh();
            var texture=AssetDatabase.LoadAssetAtPath<RenderTexture>(MenuArt+"LobbyPreview.renderTexture");
            if(texture==null){texture=new RenderTexture(1280,728,24){name="LobbyPreview",antiAliasing=4};AssetDatabase.CreateAsset(texture,MenuArt+"LobbyPreview.renderTexture");}
            var root=new GameObject("Main Menu Lobby");lobby=root.transform;
            Box(lobby,"Back wall",new Vector3(0,1.7f,-2.45f),new Vector3(5.8f,3.4f,.1f),"1b3541");
            Box(lobby,"Left wall",new Vector3(-2.95f,1.7f,.4f),new Vector3(.1f,3.4f,5.6f),"18343f");
            Box(lobby,"Right wall",new Vector3(2.95f,1.7f,.4f),new Vector3(.1f,3.4f,5.6f),"132d37");
            Box(lobby,"Ceiling",new Vector3(0,3.45f,.4f),new Vector3(5.8f,.1f,5.6f),"142c37");
            Box(lobby,"Floor",new Vector3(0,-.05f,.4f),new Vector3(5.8f,.1f,5.6f),"152e39");
            for(float x=-2.9f;x<3;x+=.58f)Segment(lobby,"Floor grid",new Vector3(x,.008f,-2.4f),new Vector3(x,.008f,3.2f),"263f4a",.006f);
            for(float z=-2.4f;z<3.3f;z+=.7f)Segment(lobby,"Floor grid",new Vector3(-2.9f,.008f,z),new Vector3(2.9f,.008f,z),"263f4a",.006f);
            foreach(float x in new[]{-2.89f,2.89f}){
                Segment(lobby,"Wall joint",new Vector3(x,0,-2.39f),new Vector3(x,3.4f,-2.39f),"355360");
                Segment(lobby,"Skirting",new Vector3(x,.12f,-2.4f),new Vector3(x,.12f,3.2f),"345865");
            }
            foreach(float x in new[]{-1.7f,1.7f})Box(lobby,"Ceiling light strip",new Vector3(x,3.38f,.35f),new Vector3(.025f,.015f,4.5f),"4e7e8b");
            foreach(float x in new[]{-1.285f,1.285f})Box(lobby,"Door frame",new Vector3(x,1.42f,-2.225f),new Vector3(.13f,2.78f,.29f),"365764");
            Box(lobby,"Door lintel",new Vector3(0,2.74f,-2.225f),new Vector3(2.7f,.14f,.29f),"365764");
            foreach(float side in new[]{-1f,1f}){
                Box(lobby,side<0?"Left door":"Right door",new Vector3(side*.619f,1.347f,-2.28f),new Vector3(1.202f,2.645f,.14f),"24414f");
                Box(lobby,"Window frame",new Vector3(side*.605f,1.735f,-2.198f),new Vector3(.77f,.81f,.018f),"4c7180");
                Box(lobby,"Window glass",new Vector3(side*.605f,1.735f,-2.183f),new Vector3(.65f,.69f,.012f),"183f4d");
                Box(lobby,"Door handle",new Vector3(side*.1475f,.785f,-2.165f),new Vector3(.045f,.47f,.07f),"86b5bf");
            }
            Box(lobby,"Threshold",new Vector3(0,.045f,-2.215f),new Vector3(2.8f,.055f,.51f),"355561");
            Segment(lobby,"Door centre seam",new Vector3(0,.08f,-2.19f),new Vector3(0,2.65f,-2.19f),"86cbd7",.012f);
            Box(lobby,"Keypad",new Vector3(-1.695f,1.35f,-2.295f),new Vector3(.25f,.6f,.15f),"355561");
            for(float y=1.15f;y<1.57f;y+=.12f)for(float x=1.63f;x<1.79f;x+=.06f)Segment(lobby,"Keypad key",new Vector3(-x,y,-2.21f),new Vector3(-x-.025f,y,-2.21f),"82b6c1",.012f);
            Box(lobby,"Laboratory sign frame",new Vector3(0,3.03f,-2.1f),new Vector3(2.8f,.34f,.04f),"688e9a");
            Box(lobby,"Laboratory sign",new Vector3(0,3.03f,-2.07f),new Vector3(2.76f,.30f,.015f),"23414e");
            RoomText(lobby,"Sign lettering","CALCULUS LAB",new Vector3(0,3.03f,-2.05f),1.8f,2.7f,.28f);
            Poster("Integration poster",new Vector3(-2.88f,1.725f,-.7f),90,true);
            Poster("Differentiation poster",new Vector3(2.88f,1.725f,-.7f),-90,false);
            var camera=new GameObject("Lobby Camera",typeof(Camera)).GetComponent<Camera>();camera.transform.SetParent(lobby,false);camera.transform.localPosition=new Vector3(0,1.65f,3.8f);camera.transform.LookAt(new Vector3(0,1.55f,-2.4f));camera.fieldOfView=52;camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Hex("0b1b24");camera.cullingMask=1<<30;camera.targetTexture=texture;camera.allowHDR=false;camera.depth=-10;
            // Match the concept's slightly raised vanishing point with a physical lens shift.
            camera.usePhysicalProperties=true;camera.sensorSize=new Vector2(36,36f*728/1280);camera.fieldOfView=52;camera.lensShift=new Vector2(0,-.06f);
            PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"MainMenuLobby.prefab");Object.DestroyImmediate(root);return texture;
        }
        static void MenuButton(string name,string caption,float y,bool primary=false)
        {
            Button(name,20,y,306,58,Find("HomeCard"),primary);Rounded(name);Text(name+"Label",caption);Regular(name+"Label",17,primary?Background:Ink,TextAlignmentOptions.Center);
            if(!primary)Find(name).GetComponent<Image>().color=Hex("112832");
        }
        static Button NewMenuButton(Transform parent,string name,string caption,float x,float y,float w,float h)
        {
            var image=Surface(parent,name,x,y,w,h,Raised);Rounded(name);image.raycastTarget=true;var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            Label(button.transform,name+"Label",caption,0,0,w,h,17,Ink).alignment=TextAlignmentOptions.Center;return button;
        }
        [MenuItem("Calculus Lab/Main menu/Apply approved entrance design")]
        public static void ApplyMainMenu()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save the open scene before applying the menu design.");
            var root=PrefabUtility.LoadPrefabContents(Prefabs+"MainMenuUI.prefab");
            RenderTexture texture;
            try
            {
                Index(root);
                // Reapplying the editor command preserves the original referenced controls.
                if(Find("MenuProfileModal")!=null){
                    foreach(string id in new[]{"ProfileLabel","ProfileInput","HomeStatus"})Find(id).SetParent(Find("HomeCard"),false);
                    foreach(string id in new[]{"MenuSubtitle","LobbyPreview","RoomFade","MenuRobotHint","MenuTopicsFooter","MenuSaveFooter","MenuProfile","MenuProfileModal","MenuStartArrow","MenuFlask"})if(Find(id)!=null)Object.DestroyImmediate(Find(id).gameObject);
                    var savedMap=root.GetComponent<UIReferenceMap>();savedMap.elements=savedMap.elements.Where(e=>e.target!=null).ToArray();
                    Index(root);
                }
                roundSprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"Rounded.png");texture=AuthorLobby();var home=Find("Home");
                var presentation=root.GetComponent<MainMenuPresentation>()??root.AddComponent<MainMenuPresentation>();
                foreach(string name in new[]{"HomeIntro","HomeKicker","HomeCaption","HomePortal","HomeLogout","Section"})Find(name)?.gameObject.SetActive(false);
                At("Header",0,0,1280,65);At("HeaderRule",0,64,1280,1);At("Brand",62,22,300,24);Text("Brand","CALCULUS LAB");Regular("Brand",14,Ink);At("MissionEmblem",32,22,18,20);
                At("HomeTitle",100,96,1080,53);Text("HomeTitle","CALCULUS LAB ESCAPE");Regular("HomeTitle",44,Ink,TextAlignmentOptions.Center);Find("HomeTitle").GetComponent<TMP_Text>().characterSpacing=5;
                Find("HomeTitle").GetComponent<TMP_Text>().fontStyle=FontStyles.Bold;
                var flask=Create(Find("Header"),"MenuFlask",32,22,18,22);
                Surface(flask,"Flask lip",5,1,8,1,Cyan);Surface(flask,"Flask neck left",6,2,1,7,Cyan);Surface(flask,"Flask neck right",11,2,1,7,Cyan);
                var left=Surface(flask,"Flask left",2,9,1,11,Cyan).rectTransform;left.localRotation=Quaternion.Euler(0,0,-25);
                var right=Surface(flask,"Flask right",13,9,1,11,Cyan).rectTransform;right.localRotation=Quaternion.Euler(0,0,25);
                Surface(flask,"Flask base",2,19,12,1,Cyan);Surface(flask,"Flask liquid",4,15,8,1,Cyan);
                var subtitle=Label(home,"MenuSubtitle","MAIN MENU",200,163,880,24,15,Cyan);subtitle.alignment=TextAlignmentOptions.Center;subtitle.characterSpacing=4;
                var roomRect=Create(home,"LobbyPreview",25,198,808,459);roomRect.SetAsFirstSibling();var raw=roomRect.gameObject.AddComponent<RawImage>();raw.texture=texture;raw.raycastTarget=false;
                // A code-authored alpha gradient softly joins the 3D room to the page.
                var gradient=new Texture2D(128,128,TextureFormat.RGBA32,false);
                for(int y=0;y<128;y++)for(int x=0;x<128;x++){
                    float u=x/127f,v=y/127f;float edge=Mathf.Pow(Mathf.Abs(u-.5f)*2,3)*.30f;float bottom=Mathf.Pow(Mathf.Clamp01((.32f-v)/.32f),1.3f);float top=Mathf.Pow(Mathf.Clamp01((v-.87f)/.13f),1.4f)*.7f;var c=Background;c.a=Mathf.Clamp01(Mathf.Max(bottom,top)+edge);gradient.SetPixel(x,y,c);
                }
                gradient.Apply();File.WriteAllBytes(MenuArt+"RoomFade.png",gradient.EncodeToPNG());Object.DestroyImmediate(gradient);AssetDatabase.ImportAsset(MenuArt+"RoomFade.png");
                var importer=(TextureImporter)AssetImporter.GetAtPath(MenuArt+"RoomFade.png");importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
                var fadeSprite=AssetDatabase.LoadAssetAtPath<Sprite>(MenuArt+"RoomFade.png");if(fadeSprite==null)throw new Exception("Room fade must import as a single Sprite.");
                Surface(home,"RoomFade",25,198,808,459,Color.white).sprite=fadeSprite;Find("RoomFade").SetSiblingIndex(1);
                At("HomeCard",870,235,346,377);Find("HomeCard").GetComponent<Image>().color=Hex("10252e");Rounded("HomeCard");
                var outline=Find("HomeCard").GetComponent<Outline>()??Find("HomeCard").gameObject.AddComponent<Outline>();outline.effectColor=Hex("2d4b58");outline.effectDistance=new Vector2(1,-1);
                MenuButton("HomeStart","START",20,true);Regular("HomeStartLabel",17,Background,TextAlignmentOptions.MidlineLeft);Find("HomeStartLabel").GetComponent<TMP_Text>().margin=new Vector4(12,0,40,0);
                Label(Find("HomeStart"),"MenuStartArrow","→",264,0,22,58,24,Background).alignment=TextAlignmentOptions.Center;
                MenuButton("HomeRecords","STUDENT DASHBOARD",90);MenuButton("HomeInstructions","INSTRUCTIONS",160);MenuButton("HomeSettings","SETTINGS",230);MenuButton("HomeExit","EXIT",304);
                Find("HomeExit").GetComponent<Image>().color=Color.clear;Find("HomeExit").GetComponent<Outline>().effectColor=Color.clear;
                Button("HomeContinue",890,618,306,27,home);Find("HomeContinue").GetComponent<Image>().color=Color.clear;Find("HomeContinue").GetComponent<Outline>().effectColor=Color.clear;Regular("HomeContinueLabel",14,Cyan,TextAlignmentOptions.Center);Find("HomeContinue").gameObject.SetActive(false);
                presentation.continueButton=Find("HomeContinue").GetComponent<Button>();
                At("ReportWelcomePortrait",486,436,205,218);Label(home,"MenuRobotHint","Ready when\nyou are.",688,528,135,51,14,Muted);
                Label(home,"MenuTopicsFooter","LIMITS   ·   DIFFERENTIATION   ·   INTEGRATION",50,685,740,23,13,Muted);
                Label(home,"MenuSaveFooter","Progress saved on this computer",900,685,330,23,13,Muted).alignment=TextAlignmentOptions.Right;
                var profile=NewMenuButton(Find("Header"),"MenuProfile","Local player",1020,14,210,37);profile.GetComponent<Image>().color=Color.clear;presentation.profileLabel=profile.GetComponentInChildren<TMP_Text>();presentation.profileLabel.fontSize=14;
                UnityEventTools.AddPersistentListener(profile.onClick,presentation.OpenProfile);
                var modal=Surface(Find("DesignFrame"),"MenuProfileModal",0,0,1280,720,new Color(.01f,.03f,.05f,.94f));modal.raycastTarget=true;modal.transform.SetAsLastSibling();presentation.profileModal=modal.gameObject;
                var card=Surface(modal.transform,"MenuProfileCard",365,212,550,300,Panel);Rounded("MenuProfileCard");
                Label(card.transform,"MenuProfileTitle","Local player",30,25,490,43,28,Ink);
                At("ProfileLabel",30,82,490,26,card.transform);Text("ProfileLabel","LEARNER NAME");Regular("ProfileLabel",13,Muted);
                At("ProfileInput",30,116,490,48,card.transform);Input("ProfileInput",30,116,490,48);presentation.profileInput=Find("ProfileInput").GetComponent<TMP_InputField>();
                At("HomeStatus",30,176,490,45,card.transform);Regular("HomeStatus",14,Muted);
                var done=NewMenuButton(card.transform,"MenuProfileDone","DONE",330,237,190,40);done.GetComponent<Image>().color=Cyan;done.GetComponentInChildren<TMP_Text>().color=Background;UnityEventTools.AddPersistentListener(done.onClick,presentation.CloseProfile);
                modal.gameObject.SetActive(false);
                var map=root.GetComponent<UIReferenceMap>();map.elements=map.elements.Concat(new[]{new UIReferenceMap.Element{id="MenuProfile",target=profile.transform},new UIReferenceMap.Element{id="MenuProfileDone",target=done.transform},new UIReferenceMap.Element{id="MenuProfileModal",target=modal.transform}}).ToArray();
                PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"MainMenuUI.prefab");
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");
            foreach(var old in scene.GetRootGameObjects().Where(g=>g.name=="Main Menu Lobby"||g.name=="MainMenuLobby"))Object.DestroyImmediate(old);
            var room=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"MainMenuLobby.prefab"),scene);
            room.name="Main Menu Lobby";PrefabUtility.RecordPrefabInstancePropertyModifications(room);
            foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))if(!camera.transform.IsChildOf(room.transform)){
                camera.cullingMask&=~(1<<30);EditorUtility.SetDirty(camera);PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            ManualEditingMigration.Validate();
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");
            Selection.activeGameObject=Object.FindFirstObjectByType<MainMenuPresentation>().gameObject;
            Debug.Log("APPROVED MAIN MENU: saved UI, editable lobby prefab, five existing routes, compact local profile and conditional resume.");
        }
        static double nextLobbyDraw;
        [InitializeOnLoadMethod] static void PreviewLobby()=>EditorApplication.update+=DrawLobby;
        static void DrawLobby()
        {
            if(EditorApplication.timeSinceStartup<nextLobbyDraw||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||BuildPipeline.isBuildingPlayer)return;
            nextLobbyDraw=EditorApplication.timeSinceStartup+.25;
            foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))if(camera.isActiveAndEnabled&&camera.targetTexture!=null&&camera.targetTexture.name=="LobbyPreview")camera.Render();
        }
        [MenuItem("Calculus Lab/Main menu/Open editable main menu")]
        public static void OpenMainMenu()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save the current scene before opening MainMenu.");
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");DrawLobby();
            Selection.activeGameObject=Object.FindFirstObjectByType<GameManager>().gameObject;
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
        public static void CaptureMainMenu()
        {
            string folder=LearningApi.Argument("--menu-evidence",Path.GetFullPath("../../qa/main-menu-2026-10-24"));Directory.CreateDirectory(folder);
            var camera=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Single(c=>c.targetTexture!=null&&c.targetTexture.name=="LobbyPreview");camera.Render();
            var prior=RenderTexture.active;RenderTexture.active=camera.targetTexture;
            var pixels=new Texture2D(camera.targetTexture.width,camera.targetTexture.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,pixels.width,pixels.height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,"editor-room.png"),pixels.EncodeToPNG());Object.DestroyImmediate(pixels);RenderTexture.active=prior;
            File.WriteAllText(Path.Combine(folder,"editor-lobby.txt"),camera.transform.root.name+" / culling "+camera.cullingMask+" / renderers "+camera.transform.root.GetComponentsInChildren<Renderer>().Length);
        }
        public static void ApplyMainMenuAndBuild(){ApplyMainMenu();CaptureMainMenu();ManualEditingMigration.BuildQA();}
    }
}
#endif
