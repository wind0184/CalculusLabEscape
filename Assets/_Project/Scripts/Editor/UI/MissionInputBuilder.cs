#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CalculusLabEscape.Editor
{
    public static partial class ProductionUIBuilder
    {
        static void AddMissionInput(GameObject root)
        {
            Transform Find(string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
            foreach(string name in new[]{"BridgeChip"}.Concat(Enumerable.Range(0,3).Select(i=>"WireSource"+i)).Concat(Enumerable.Range(0,4).Select(i=>"SequenceCard"+i)))
            {
                var button=Find(name);
                if(button.GetComponent<MissionSelectionVisual>()==null)button.gameObject.AddComponent<MissionSelectionVisual>().Configure();
            }
            if(!root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="AnswerKeyboardHelp"))
                Text(Find("BridgeInputs"),"AnswerKeyboardHelp","Tab / Shift+Tab: switch boxes    Enter: test repair\nUse / for fractions. Accepted answers still need installation.",449,457,747,74,19,Muted);
            Find("SequenceValuePlaceholder").GetComponent<TMP_Text>().text="Number, fraction, or C / Enter to check";
        }

        [MenuItem("Calculus Lab/Production/Upgrade mission keyboard and selection")]
        public static void UpgradeMissionInput()
        {
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            foreach(string name in new[]{"DifferentiationUI","IntegrationUI"})
            {
                string path=PrefabsPath+"/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try{AddMissionInput(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Debug.Log("MISSION_INPUT_PREFABS_UPDATED");
        }

        public static void UpgradeMissionInputAndBuild()
        {
            UpgradeMissionInput();
            SceneAuthoring.ValidateAndCaptureTo(Path.GetFullPath("../../qa/editor-mission-input"));
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");
            Build();
        }
    }
}
#endif
