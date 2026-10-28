#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape.Editor
{
    public static partial class ProductionUIBuilder
    {
        [MenuItem("Calculus Lab/UI/Apply simulation answer backgrounds")]
        public static void ApplyAnswerBackgrounds()
        {
            string evidence=Path.GetFullPath("../../qa/answer-background");Directory.CreateDirectory(evidence);
            foreach(string name in new[]{"LimitsUI","DifferentiationUI","IntegrationUI"})
            {
                string path=PrefabsPath+"/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var all=root.GetComponentsInChildren<Transform>(true);var puzzle=all.Single(t=>t.name=="Puzzle");
                    int buttons=root.GetComponentsInChildren<Button>(true).Length,labels=root.GetComponentsInChildren<TMP_Text>(true).Length;
                    bool limits=name=="LimitsUI";Color tint=limits?new Color(.16f,.63f,.94f):name=="DifferentiationUI"?new Color(.18f,.75f,.63f):new Color(.38f,.55f,.95f);
                    if(!all.Any(t=>t.name=="SimulationAnswerArt"))
                    {
                        var art=Rect(puzzle,"SimulationAnswerArt",0,0,1280,720);art.SetAsFirstSibling();
                        var backdrop=Rect(art,"SimulationAnswerSky",0,0,1280,720).gameObject.AddComponent<SimulationAnswerBackdrop>();backdrop.raycastTarget=false;backdrop.lightTint=tint;
                        Surface(art,"AnswerGlassSurface",412,limits?98:108,817,limits?609:599,new Color(.065f,.13f,.195f,.94f),tint);
                        Surface(art,"DeviceGlassSurface",limits?32:29,limits?104:108,357+(limits?0:2),limits?524:532,new Color(.06f,.135f,.19f,.96f),tint);
                        var trim=Panel(art,"AnswerSurfaceLight",435,limits?98:108,116,2,new Color(tint.r,tint.g,tint.b,.8f));trim.raycastTarget=false;
                        for(int i=0;i<3;i++){var light=Panel(art,"AnswerSurfaceIndicator"+i,1179+i*10,limits?108:118,4,4,new Color(tint.r,tint.g,tint.b,.3f+i*.2f));light.raycastTarget=false;}
                    }
                    var decoration=puzzle.Find("SimulationAnswerArt");
                    // Migrate the initial background draft without touching answer controls.
                    var oldFooter=decoration.Find("WorldActionsSurface");if(oldFooter!=null)UnityEngine.Object.DestroyImmediate(oldFooter.gameObject);
                    var surface=decoration.Find("AnswerGlassSurface").GetComponent<RectTransform>();
                    if(limits&&surface.anchoredPosition.y==-108&&surface.sizeDelta.y==599)
                    {
                        surface.anchoredPosition=new Vector2(412,-98);surface.sizeDelta=new Vector2(817,609);
                        var trim=decoration.Find("AnswerSurfaceLight").GetComponent<RectTransform>();trim.anchoredPosition=new Vector2(435,-98);
                        for(int i=0;i<3;i++)decoration.Find("AnswerSurfaceIndicator"+i).GetComponent<RectTransform>().anchoredPosition=new Vector2(1179+i*10,-108);
                    }
                    all=root.GetComponentsInChildren<Transform>(true);
                    puzzle.GetComponent<Image>().color=Color.clear;
                    all.Single(t=>t.name==(limits?"PortalCard":"DevicePanel")).GetComponent<Image>().color=Color.clear;
                    all.Single(t=>t.name=="Header").GetComponent<Image>().color=new Color(.035f,.075f,.12f,.98f);
                    if(root.GetComponentsInChildren<Button>(true).Length!=buttons||root.GetComponentsInChildren<TMP_Text>(true).Length!=labels)throw new Exception("Background upgrade changed controls or text.");
                    if(decoration.GetComponentsInChildren<Graphic>(true).Any(g=>g.raycastTarget))throw new Exception("Decoration blocks pointer input.");
                    if(decoration.GetComponentsInChildren<SimulationAnswerBackdrop>().Length!=3)throw new Exception("Missing saved answer surfaces.");
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Debug.Log("ANSWER_BACKGROUND_PREFABS_PASS");
        }
        static void Surface(Transform parent,string name,float x,float y,float w,float h,Color fill,Color tint)
        {
            var graphic=Rect(parent,name,x,y,w,h).gameObject.AddComponent<SimulationAnswerBackdrop>();graphic.panel=true;graphic.color=fill;graphic.lightTint=tint;graphic.rim=new Color(.18f,.33f,.43f,.75f);graphic.raycastTarget=false;
        }
        public static void ApplyAnswerBackgroundsAndBuild(){ApplyAnswerBackgrounds();ApplyAnswerBackgrounds();BuildDelivery();Debug.Log("ANSWER_BACKGROUND_DELIVERY_PASS");}
        public static void CaptureAnswerBackgrounds()=>SceneAuthoring.ValidateAndCaptureTo(Path.GetFullPath("../../qa/answer-background/editor"));
    }
}
#endif
