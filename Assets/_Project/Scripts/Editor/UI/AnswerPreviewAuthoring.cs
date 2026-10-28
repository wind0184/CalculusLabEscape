#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape.Editor
{
    // Updates existing saved labels. This is never invoked by normal game startup.
    public static class AnswerPreviewAuthoring
    {
        const string Prefabs="Assets/_Project/Prefabs/Resources/Production/";
        public static string Evidence=>Path.GetFullPath("../../qa/question-preview-fix-2026-10-25");
        static T Find<T>(GameObject root,string name)where T:Component=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name).GetComponent<T>();
        internal static void Configure(GameObject root,int topic)
        {
            var view=root.GetComponent<ReportFusionView>();
            view.limits=root.GetComponent<ProductionCanvas>();view.mission=root.GetComponent<MissionCanvas>();
            view.resultHeading=Find<TMP_Text>(root,"ReportResultHeading");
            var result=view.resultState;
            result.rectTransform.anchoredPosition=new Vector2(998,-310);result.rectTransform.sizeDelta=new Vector2(256,120);
            result.fontSize=result.fontSizeMax=21;result.fontSizeMin=18;result.enableAutoSizing=true;
            result.textWrappingMode=TextWrappingModes.Normal;result.alignment=TextAlignmentOptions.TopLeft;result.characterSpacing=0;result.lineSpacing=2;
            var math=result.GetComponent<MathText>()??result.gameObject.AddComponent<MathText>();
            math.SetSource(topic==0?"lim(x→3) f(x) = □":topic==1?"f'(x) = □x^(4)":"F(x) = x^(4) / 1\nConstant: □");
            result.gameObject.SetActive(true);
            foreach(string name in new[]{"PowerPreview","DialPreview"})
            {var old=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);if(old!=null)old.gameObject.SetActive(false);}
            var prompt=Find<TMP_Text>(root,topic==0?"QuestionPrompt":"TaskPrompt");
            if(topic>0){prompt.rectTransform.sizeDelta=new Vector2(686,78);prompt.fontSizeMin=18;}
            view.resultHeading.text="YOUR RESULT PREVIEW";
            if(topic>0)
            {
                Find<TMP_Text>(root,"BridgeALabel").text="Coefficient";
                var connections=Find<MissionConnections>(root,"WireConnections");
                connections.sources=Enumerable.Range(0,3).Select(i=>Find<RectTransform>(root,"WireSource"+i)).ToArray();
                connections.targets=Enumerable.Range(0,5).Select(i=>Find<RectTransform>(root,"WireTarget"+i)).ToArray();
                EditorUtility.SetDirty(connections);
            }
            EditorUtility.SetDirty(view);EditorUtility.SetDirty(result);
        }
        static void UpdateBanks()
        {
            var guided=AssetDatabase.LoadAssetAtPath<LimitsQuestionBank>("Assets/_Project/Resources/Production/LimitsQuestions.asset");
            var independent=AssetDatabase.LoadAssetAtPath<LimitsQuestionBank>("Assets/_Project/Resources/Production/LimitsIndependentQuestions.asset");
            string[] first={"3","2","2","2",""},second={"2","3","1","2","4"};
            for(int i=0;i<5;i++){guided.questions[i].approachPoint=first[i];independent.questions[i].approachPoint=second[i];}
            EditorUtility.SetDirty(guided);EditorUtility.SetDirty(independent);
            var diff=AssetDatabase.LoadAssetAtPath<DifferentiationQuestionBank>("Assets/_Project/Resources/Production/DifferentiationQuestions.asset");
            diff.questions.Single(q=>q.id=="DX_APPLY_2").prompt="For x > 0, differentiate f(x) = (2x - 3)/x^(1/4). Choose the full derivative.";
            EditorUtility.SetDirty(diff);
            var integration=AssetDatabase.LoadAssetAtPath<IntegrationQuestionBank>("Assets/_Project/Resources/Production/IntegrationQuestions.asset");
            integration.questions.Single(q=>q.id=="IX_SEQ1").prompt="For x > 0, choose the general integral of x^(1/3), the cube root of x.";
            EditorUtility.SetDirty(integration);
        }
        public static void Apply()
        {
            Directory.CreateDirectory(Evidence);UpdateBanks();
            string[] names={"LimitsUI","DifferentiationUI","IntegrationUI"};
            for(int topic=0;topic<3;topic++)
            {
                string path=Prefabs+names[topic]+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try{Configure(root,topic);PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();
            for(int topic=0;topic<3;topic++)
            {
                var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity");
                var view=UnityEngine.Object.FindFirstObjectByType<ReportFusionView>(FindObjectsInactive.Include);
                Configure(view.gameObject,topic);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            AnswerPreviewChecks.Validate();
            Debug.Log("ANSWER_PREVIEW_AUTHORING_PASS / three saved prefabs and scenes");
        }
        public static void ApplyAndBuild()
        {
            Apply();
            string target=Path.Combine(Evidence,"player/LearningLab.exe");Directory.CreateDirectory(Path.GetDirectoryName(target));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"MainMenu"}.Concat(Chapter4Game.Scenes).Select(n=>SceneAuthoring.SceneFolder+n+".unity").ToArray(),locationPathName=target,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Preview QA build failed: "+report.summary.result);
            File.WriteAllText(Path.Combine(Evidence,"build.txt"),"PASS: saved scene/prefab preview authoring, presentation checks and development build.\n"+target);
        }
    }
}
#endif
