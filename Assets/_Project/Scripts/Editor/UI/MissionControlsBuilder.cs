#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace CalculusLabEscape.Editor
{
    public static partial class ProductionUIBuilder
    {
        static void AddControlsGuide(GameObject root,int topic)
        {
            Transform Find(string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
            if(Find("ControlsModal")!=null)return;
            var puzzle=Find("Puzzle");var frame=Find("DesignFrame");
            void MoveLabel(string name,float y,float height,int size)
            {
                var label=Find(name).GetComponent<TMP_Text>();label.rectTransform.SetParent(puzzle,false);
                label.rectTransform.anchoredPosition=new Vector2(50,-y);label.rectTransform.sizeDelta=new Vector2(315,height);label.fontSize=size;
            }
            MoveLabel("SecondaryProgress",350,25,17);
            for(int i=0;i<3;i++)MoveLabel("Component"+i,382+i*26,24,17);
            MoveLabel("EnvironmentStatus",610,32,14);
            if(topic==2)Find("CaseGuidance").gameObject.SetActive(false);
            Text(puzzle,"NextAction","",50,topic==1?471:390,315,topic==1?92:127,18,Accent);
            Button(puzzle,"ControlsOpen","How to play / Free",50,topic==1?574:524,315,32);
            var manual=Find("ManualOpen").GetComponent<RectTransform>();manual.anchoredPosition=new Vector2(50,-568);manual.sizeDelta=new Vector2(315,32);
            var overlay=Panel(frame,"ControlsModal",0,0,1280,720,Paper);
            Text(overlay.transform,"ControlsEyebrow","OPERATION HELP / FREE",69,91,1135,30,18,Accent,true);
            Text(overlay.transform,"ControlsTitle","",69,137,1135,56,37,Ink,true);
            Text(overlay.transform,"ControlsBody","",71,227,1128,353,23);
            Text(overlay.transform,"ControlsEscape","Esc also returns to your device.",71,634,670,35,19,Muted);
            Button(overlay.transform,"ControlsClose","Return to device",855,625,348,49,true);
            overlay.gameObject.SetActive(false);
        }

        [MenuItem("Calculus Lab/Production/Upgrade mission operation guidance")]
        public static void UpgradeControlsGuide()
        {
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            foreach(string name in new[]{"DifferentiationUI","IntegrationUI"})
            {
                string path=PrefabsPath+"/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try{AddControlsGuide(root,name=="DifferentiationUI"?1:2);PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Debug.Log("MISSION_CONTROLS_PREFABS_UPDATED");
        }
        public static void UpgradeControlsAndBuild(){UpgradeControlsGuide();Build();}
    }
}
#endif
