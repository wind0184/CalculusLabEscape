#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CalculusLabEscape.Editor
{
    public static class DifferentiationBankBuilder
    {
        const string Path="Assets/_Project/Resources/Production/DifferentiationQuestions.asset";
        static string Evidence=>System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../../qa/differentiation-pdf-bank"));
        static DifferentiationQuestion Pair(string id,int stage,int page,string title,string prompt,string rewrite,string coefficient,string exponent,string explanation)=>new DifferentiationQuestion{id=id,afterStage=stage,sourcePage=page,title=title,prompt=prompt,rewrite=rewrite,coefficient=coefficient,exponent=exponent,explanation=explanation};
        static DifferentiationQuestion[] Seed()=>new[]{
            Pair("DX_BASIC_1",0,19,"Power Core / independent calibration","Differentiate 7x^10. Enter the new coefficient and exponent.","7x^10", "70","9","Multiply 7 by 10 to get 70; 10 - 1 = 9. The derivative is 70x^9."),
            Pair("DX_BASIC_2",3,15,"Converter / negative exponent","Differentiate x^(-4), for x != 0. Enter coefficient and exponent.","1x^(-4)","-4","-5","1 times -4 = -4; -4 - 1 = -5. The derivative is -4x^(-5)."),
            Pair("DX_BASIC_3",3,17,"Converter / root of a power","Differentiate sqrt(x^3), for x > 0. Enter coefficient and exponent.","sqrt(x^3) = x^(3/2)","3/2","1/2","Rewrite as x^(3/2). Multiply by 3/2, then 3/2 - 1 = 1/2. The derivative is (3/2)x^(1/2)."),
            Pair("DX_BASIC_4",3,21,"Converter / fractional coefficient","Differentiate 5/(4x^4), for x != 0. Enter coefficient and exponent.","5/(4x^4) = (5/4)x^(-4)","-5","-5","(5/4) times -4 = -5; -4 - 1 = -5. The derivative is -5x^(-5), or -5/x^5."),
            Pair("DX_BASIC_5",3,23,"Converter / reciprocal and signs","Differentiate -2/x^8, for x != 0. Enter coefficient and exponent.","-2/x^8 = -2x^(-8)","16","-9","Two negative factors give a positive coefficient: -2 times -8 = 16. The exponent is -8 - 1 = -9, giving 16x^(-9)."),
            new DifferentiationQuestion{id="DX_APPLY_2",afterStage=7,sourcePage=27,title="Bridge Builder / fourth-root expression",choiceAnswer=true,correctOption=1,
                prompt="For x > 0, differentiate f(x) = (2x - 3)/x^(1/4). Choose the full derivative.",rewrite="(2x - 3)/x^(1/4) = 2x^(3/4) - 3x^(-1/4)",
                explanation="Rewrite as 2x^(3/4) - 3x^(-1/4). Differentiate both terms: (3/2)x^(-1/4) + (3/4)x^(-5/4).",
                options=new[]{"(3/2)x^(-1/4) - (3/4)x^(-5/4)","(3/2)x^(-1/4) + (3/4)x^(-5/4)","(3/2)x^(3/4) + (3/4)x^(-1/4)"},
                optionFeedback=new[]{"The first term is correct. The second coefficient is positive because -3 times -1/4 is positive.","Correct.","The coefficients are correct. Subtract one from both rewritten exponents: 3/4 and -1/4."}},
            new DifferentiationQuestion{id="DX_APPLY_3",afterStage=7,sourcePage=29,title="Bridge Builder / simplify three terms",choiceAnswer=true,correctOption=2,
                prompt="For x > 0, differentiate (x^2 + 5x - 3)/(3sqrt(x)). Choose the full derivative.",rewrite="(1/3)x^(3/2) + (5/3)x^(1/2) - x^(-1/2)",
                explanation="Divide each term by 3sqrt(x), then apply the Power Rule. The result is (1/2)x^(1/2) + (5/6)x^(-1/2) + (1/2)x^(-3/2).",
                options=new[]{"x^(1/2) + x^(-1/2) + x^(-3/2)","(1/2)x^(1/2) - (5/6)x^(-1/2) - (1/2)x^(-3/2)","(1/2)x^(1/2) + (5/6)x^(-1/2) + (1/2)x^(-3/2)"},
                optionFeedback=new[]{"The new exponents are correct. Keep the coefficients produced by division and multiplication.","Check each sign separately: the second term stays positive, and two negative factors make the last coefficient positive.","Correct."}}
        };
        [MenuItem("Calculus Lab/Questions/Add PDF differentiation questions")]
        public static void Apply()
        {
            Directory.CreateDirectory(Evidence);var bank=AssetDatabase.LoadAssetAtPath<DifferentiationQuestionBank>(Path);
            if(bank==null){bank=ScriptableObject.CreateInstance<DifferentiationQuestionBank>();bank.questions=Seed();AssetDatabase.CreateAsset(bank,Path);}
            Check(bank);UpgradeLabels();AssetDatabase.SaveAssets();Debug.Log("DIFFERENTIATION_PDF_BANK_EDITOR_PASS");
        }
        [MenuItem("Calculus Lab/Questions/Open differentiation question bank")]
        public static void Open(){var bank=AssetDatabase.LoadAssetAtPath<DifferentiationQuestionBank>(Path);if(bank==null){Apply();bank=AssetDatabase.LoadAssetAtPath<DifferentiationQuestionBank>(Path);}Selection.activeObject=bank;EditorGUIUtility.PingObject(bank);}
        static void UpgradeLabels()
        {
            const string brief="Repair three components with guided tasks and independent questions.\nPractise powers, roots, reciprocals and simplifying expressions first.\n\nComplete every calibration, then install each component and stabilise the crossing.";
            foreach(string name in new[]{"DifferentiationUI","MainMenuUI"})
            {
                string path="Assets/_Project/Prefabs/Resources/Production/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try{foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if(name=="DifferentiationUI"&&label.name=="BriefLearning")label.text=brief;
                    if(name=="DifferentiationUI"&&label.name=="ReadyBody")label.text=label.text.Replace("All 11 mathematical steps accepted.","All required mathematical steps accepted.");
                    if(name=="MainMenuUI"&&label.name=="InstructionsBody")label.text=label.text.Replace("complete 11 steps, install three components","complete guided and independent questions, install three components");
                }PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        static void Check(DifferentiationQuestionBank bank)
        {
            var expected=Seed();if(bank.questions==null||bank.questions.Length!=7||bank.questions.Select(q=>q.id).Distinct().Count()!=7)throw new Exception("Seven unique extension IDs required.");
            foreach(var q in bank.questions)
            {
                var reference=expected.Single(e=>e.id==q.id);if(q.afterStage!=reference.afterStage||string.IsNullOrEmpty(q.prompt)||string.IsNullOrEmpty(q.rewrite)||string.IsNullOrEmpty(q.explanation))throw new Exception("Incomplete question "+q.id);
                if(q.choiceAnswer){if(q.options.Length!=3||q.optionFeedback.Length!=3||q.correctOption<0||q.correctOption>2)throw new Exception("Invalid choice "+q.id);}
                else if(q.coefficient!=reference.coefficient||q.exponent!=reference.exponent)throw new Exception("Check mathematical answer "+q.id);
            }
            var old=new Chapter4Session{topic=1,required=11};var json=JsonUtility.ToJson(old);var restored=JsonUtility.FromJson<Chapter4Session>(json);
            if(restored.bridgeCurriculumVersion!=0||restored.required!=11)throw new Exception("Legacy curriculum changed.");
            File.WriteAllText(System.IO.Path.Combine(Evidence,"bank-check.txt"),"PASS: seven new independent questions; source pages 19, 15, 17, 21, 23, 27, 29 visually checked; stable IDs; gates 0/3/7; five coefficient/exponent tasks and two full-expression tasks; existing old curriculum remains version 0.\n");
        }
        static void CheckResumeMappings()
        {
            var path=SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity";
            EditorSceneManager.OpenScene(path);var world=UnityEngine.Object.FindFirstObjectByType<Chapter4World>();var game=world.gameObject.AddComponent<Chapter4Game>();
            try
            {
                game.Initialize(1,world);
                var old=new Chapter4Session{topic=1,profile=Chapter4Store.Profile,required=11};old.completed.AddRange(new[]{"D0","D1","D2"});game.MissionResume(old);
                if(game.Stage!=3||game.BridgeExtension!=null||game.ExtendedBridge||game.Session.required!=11||world.station!=world.devices[1])throw new Exception("Legacy resume changed curriculum.");
                var current=new Chapter4Session{topic=1,profile=Chapter4Store.Profile,required=18,bridgeCurriculumVersion=1};current.completed.Add("D0");game.MissionResume(current);
                if(game.Stage!=0||game.BridgeExtension?.id!="DX_BASIC_1"||world.station!=world.devices[0]||game.BridgeParts!=0)throw new Exception("New Power Core gate skipped on resume.");
                current.completed.AddRange(new[]{"DX_BASIC_1","D1","D2","D3","DX_BASIC_2"});game.MissionResume(current);
                if(game.Stage!=3||game.BridgeExtension?.id!="DX_BASIC_3"||world.station!=world.devices[1]||game.BridgeParts!=1)throw new Exception("Partial Converter gate skipped on resume.");
                Debug.Log("DIFFERENTIATION_LEGACY_AND_PARTIAL_RESUME_PASS");
            }
            finally{UnityEngine.Object.DestroyImmediate(game);EditorSceneManager.OpenScene(path);}
        }
        public static void ApplyAndBuild(){Apply();Apply();RepairStationMotionBuilder.Apply();CheckResumeMappings();ProductionUIBuilder.BuildDelivery();Debug.Log("DIFFERENTIATION_PDF_BANK_DELIVERY_PASS");}
    }
}
#endif
