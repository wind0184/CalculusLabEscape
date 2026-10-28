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
    // A targeted Edit Mode migration; Play never creates or arranges these controls.
    public static class BeginnerFixAuthoring
    {
        const string Prefabs="Assets/_Project/Prefabs/Resources/Production/";
        static string Evidence=>LearningApi.Argument("--beginner-fix-evidence",Path.GetFullPath("../../qa/beginner-fixes-2026-10-27"));
        static Transform Find(GameObject root,string name)=>root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
        static void Remember(Object value)
        {
            EditorUtility.SetDirty(value);
            if(PrefabUtility.IsPartOfPrefabInstance(value))PrefabUtility.RecordPrefabInstancePropertyModifications(value);
        }
        static void Configure(GameObject root,int topic)
        {
            if(topic>=0)
            {
                var modal=Find(root,"SupportModal");
                var selection=modal.GetComponent<HintPanelSelection>()??modal.gameObject.AddComponent<HintPanelSelection>();
                selection.tiers=Enumerable.Range(0,3).Select(i=>Find(root,"SupportTier"+i).GetComponent<Button>()).ToArray();
                selection.caption=Find(root,"HintContentLabel").GetComponent<TMP_Text>();
                selection.Select(-1);Remember(selection);Remember(selection.caption);
                foreach(var button in selection.tiers){Remember(button.GetComponent<Image>());foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))Remember(label);}
                if(topic==1)
                {
                    var label=Find(root,"SupportText").GetComponent<TMP_Text>();
                    foreach(var q in DifferentiationQuestionBank.Load().questions.Where(q=>q.id=="DX_BASIC_3"||q.id=="DX_APPLY_2"||q.id=="DX_APPLY_3"))
                        foreach(string hint in new[]{q.basicHint,q.ruleReminder,q.demonstration})
                            if(label.GetPreferredValues(MathNotation.Format(hint),label.rectTransform.rect.width,float.PositiveInfinity).y>label.rectTransform.rect.height+1)
                                throw new Exception("Revised hint does not fit the compact panel: "+q.id+" / "+hint);
                }
            }
            if(topic==0)ProductionUIBuilder.AddLimitsSummaryPractice(root);
            if(topic<0)
            {
                var label=Find(root,"InstructionsBody").GetComponent<TMP_Text>();
                label.text=label.text.Replace("Limits: complete five calibrations, then activate the portal.","Limits: five pairs of guided and independent questions (10 total), then activate the portal.");
                Remember(label);
            }
            ManualEditingMigration.ConfigureCanvas(root);
            foreach(var value in root.GetComponentsInChildren<MonoBehaviour>(true))if(value!=null)Remember(value);
        }
        [MenuItem("Calculus Lab/Manual editing/Apply beginner walkthrough fixes")]
        public static void Apply()
        {
            for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
                if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save open scenes before applying beginner fixes.");
            Directory.CreateDirectory(Evidence);
            FeedbackRevisionAuthoring.Apply();
            var limits=AssetDatabase.LoadAssetAtPath<LimitsQuestionBank>("Assets/_Project/Resources/Production/LimitsQuestions.asset");
            limits.questions.Single(q=>q.id=="L1").explanation="Both sides approach 5. For a polynomial, substitute the approach point. Example: 2x + 1 approaches 7 as x approaches 3.";
            EditorUtility.SetDirty(limits);
            string[] names={"MainMenuUI","LimitsUI","DifferentiationUI","IntegrationUI"};
            for(int i=0;i<names.Length;i++)
            {
                string path=Prefabs+names[i]+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try{Configure(root,i-1);PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();
            string[] scenes=new[]{"MainMenu"}.Concat(Chapter4Game.Scenes).ToArray();
            for(int i=0;i<scenes.Length;i++)
            {
                var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+scenes[i]+".unity");var manager=Object.FindFirstObjectByType<GameManager>();
                Configure(manager.missionUI!=null?manager.missionUI.gameObject:manager.menuOrLimitsUI.gameObject,i-1);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            ManualEditingMigration.Validate();AssessmentTrackingChecks.Validate();MissionFeedbackChecks.Validate();
            File.WriteAllText(Path.Combine(Evidence,"authoring-result.txt"),"PASS: saved hint selection in all three gameplay scenes/prefabs; Limits summary practice and separate Replay mission; targeted root/fraction hints; general substitution teaching; unchanged assessment checks.\n");
            Debug.Log("BEGINNER_FIX_AUTHORING_PASS");
        }
        public static void ApplyAndBuild(){Apply();ManualEditingMigration.BuildQA();}
        public static void BuildRelease()
        {
            ManualEditingMigration.Validate();AssessmentTrackingChecks.Validate();
            string target=Path.GetFullPath("Builds/LearningLab/CalculusLabEscape.exe");Directory.CreateDirectory(Path.GetDirectoryName(target));
            var result=UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerOptions
            {
                scenes=new[]{"MainMenu"}.Concat(Chapter4Game.Scenes).Select(name=>SceneAuthoring.SceneFolder+name+".unity").ToArray(),
                locationPathName=target,target=UnityEditor.BuildTarget.StandaloneWindows64,options=UnityEditor.BuildOptions.None
            });
            if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Learning Lab release build failed: "+result.summary.result);
            Debug.Log("BEGINNER_FIX_RELEASE_BUILD_PASS / "+target);
        }
    }
}
#endif
