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
    // Explicit edit-time layout migration. The panel and its background are saved
    // in each prefab/scene; Play only fills the existing SupportText reference.
    public static class HintPanelAuthoring
    {
        const string Prefabs="Assets/_Project/Prefabs/Resources/Production/";
        static readonly string[] Names={"LimitsUI","DifferentiationUI","IntegrationUI"};
        static readonly Color Ink=new Color(.91f,.95f,.96f),Muted=new Color(.65f,.77f,.81f),Accent=new Color(.25f,.8f,.83f);
        static Transform Find(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
        static RectTransform Rect(Transform node,float x,float y,float width,float height)
        {
            var r=(RectTransform)node;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);r.localScale=Vector3.one;
            EditorUtility.SetDirty(r);return r;
        }
        static Image Surface(Transform parent,string name,float x,float y,float width,float height,Color color,Sprite sprite=null)
        {
            var node=parent.Cast<Transform>().SingleOrDefault(t=>t.name==name);
            if(node==null){var go=new GameObject(name,typeof(RectTransform),typeof(Image));node=go.transform;node.SetParent(parent,false);}
            Rect(node,x,y,width,height);var image=node.GetComponent<Image>();image.color=color;image.raycastTarget=false;
            image.sprite=sprite;if(sprite!=null)image.type=Image.Type.Sliced;EditorUtility.SetDirty(image);return image;
        }
        static void Label(TMP_Text label,float size,Color color,TextAlignmentOptions alignment=TextAlignmentOptions.TopLeft)
        {
            label.fontSize=label.fontSizeMin=label.fontSizeMax=size;label.enableAutoSizing=false;label.fontStyle=FontStyles.Normal;
            label.color=color;label.alignment=alignment;label.textWrappingMode=TextWrappingModes.Normal;
            label.overflowMode=TextOverflowModes.Overflow;label.margin=Vector4.zero;label.characterSpacing=0;label.lineSpacing=2;label.raycastTarget=false;
            EditorUtility.SetDirty(label);
        }
        static void Button(GameObject root,string name,float x,float y,float width,float height)
        {
            var node=Find(root,name);Rect(node,x,y,width,height);
            var text=node.GetComponentInChildren<TMP_Text>(true);Rect(text.transform,10,3,width-20,height-6);
            var textColor=node.GetComponent<Image>().color.grayscale>.45f?new Color(.025f,.065f,.085f):Ink;
            Label(text,14,textColor,TextAlignmentOptions.Center);
        }
        static void Configure(GameObject root)
        {
            var modal=Find(root,"SupportModal");var card=Find(root,"SupportCard");
            var rounded=Find(root,"SupportTier0").GetComponent<Image>().sprite;
            modal.GetComponent<Image>().color=new Color(.005f,.015f,.025f,.62f);
            Rect(card,290,184,700,352);var shell=card.GetComponent<Image>();shell.color=new Color(.045f,.10f,.135f);shell.raycastTarget=true;
            shell.sprite=rounded;shell.type=Image.Type.Sliced;
            var title=Find(root,"SupportTitle").GetComponent<TMP_Text>();Rect(title.transform,24,19,300,30);Label(title,22,Ink);title.text="Hint / support";
            var hearts=Find(root,"SupportHearts").GetComponent<TMP_Text>();Rect(hearts.transform,338,25,338,23);Label(hearts,15,Muted,TextAlignmentOptions.TopRight);
            for(int i=0;i<3;i++)Button(root,"SupportTier"+i,24+i*220,64,210,36);

            var content=Surface(card,"HintContentCard",24,116,652,152,new Color(.075f,.205f,.24f),rounded);
            Surface(content.transform,"HintContentAccent",0,12,3,128,Accent);
            var caption=content.transform.Cast<Transform>().SingleOrDefault(t=>t.name=="HintContentLabel");
            if(caption==null){var go=new GameObject("HintContentLabel",typeof(RectTransform),typeof(TextMeshProUGUI));caption=go.transform;caption.SetParent(content.transform,false);}
            var heading=caption.GetComponent<TMP_Text>();heading.font=title.font;Rect(caption,16,11,620,18);Label(heading,11,Accent);heading.characterSpacing=1.6f;heading.text="HINT";
            var body=Find(root,"SupportText").GetComponent<TMP_Text>();body.transform.SetParent(content.transform,false);Rect(body.transform,16,37,620,101);Label(body,18,Ink);
            Button(root,"SupportClose",476,288,200,40);
            EditorUtility.SetDirty(shell);EditorUtility.SetDirty(modal.GetComponent<Image>());
        }
        static void Check(GameObject root)
        {
            var card=(RectTransform)Find(root,"SupportCard");var body=Find(root,"SupportText").GetComponent<TMP_Text>();
            if(card.sizeDelta!=new Vector2(700,352)||body.fontSize!=18||body.transform.parent.name!="HintContentCard")throw new Exception("Compact hint layout was not saved: "+root.name);
            if(Find(root,"HintContentLabel").GetComponent<TMP_Text>().text!="HINT"||Find(root,"HintContentCard").GetComponent<Image>().color.a!=1)throw new Exception("Hint needs an opaque, labelled background: "+root.name);
            var map=root.GetComponent<UIReferenceMap>();if(map.elements.Single(e=>e.id=="SupportText").target!=body.transform)throw new Exception("Original SupportText reference changed: "+root.name);
            foreach(var button in root.GetComponentsInChildren<Button>(true).Where(b=>b.name.StartsWith("SupportTier")||b.name=="SupportClose"))
                if(button.onClick.GetPersistentEventCount()==0)throw new Exception("Support button lost its saved event: "+button.name);
        }
        [MenuItem("Calculus Lab/Manual editing/Compact hint panel")]
        public static void Apply()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scenes before applying the hint layout.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string name in Names)
                {
                    string path=Prefabs+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                    try{Configure(root);Check(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
                    finally{PrefabUtility.UnloadPrefabContents(root);}
                }
                AssetDatabase.SaveAssets();
                foreach(string name in Chapter4Game.Scenes)
                {
                    var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+name+".unity");var manager=UnityEngine.Object.FindFirstObjectByType<GameManager>();
                    var root=manager.missionUI!=null?manager.missionUI.gameObject:manager.menuOrLimitsUI.gameObject;
                    Configure(root);Check(root);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }
                Debug.Log("COMPACT_HINT_AUTHORING_PASS / three saved prefabs and scenes; 700x352 cards; 18-point hint text; labelled opaque hint background; existing button events and text references");
            }
            finally
            {
                if(setup.Any(s=>s.isLoaded)&&setup.Count(s=>s.isActive)==1)EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+"MainMenu.unity");
            }
        }
        public static void ApplyAndBuild(){Apply();ManualEditingMigration.BuildQA();}
    }
}
#endif
