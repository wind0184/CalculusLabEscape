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
    public static class OtherTopicBankBuilder
    {
        const string Root="Assets/_Project/Resources/Production/";
        static string Evidence=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../../qa/other-topic-banks"));
        static LimitsQuestion L(string id,string title,string prompt,string display,float expected,string explanation)=>new LimitsQuestion{id=id,title=title,prompt=prompt,display=display,kind=LimitsAnswerKind.Number,expected=new[]{expected},errorCode="NUMERICAL_APPROACH",explanation=explanation,basicHint="Use the nearby expression, or compare the two approaching values.",ruleReminder="A limit concerns nearby values. A separately assigned point value does not change the approaching value.",demonstration=explanation};
        static LimitsQuestion C(string id,string title,string prompt,int correct,string explanation,string hint,string error,params string[] options)=>new LimitsQuestion{id=id,title=title,prompt=prompt,kind=LimitsAnswerKind.Choice,expected=new float[0],correctOption=correct,options=options,errorCode=error,explanation=explanation,basicHint=hint,ruleReminder="A two-sided limit exists when both sides approach the same value. Distinguish that value from the filled point.",demonstration=explanation};
        static IntegrationCalibration I(string id,int module,string title,string prompt,int correct,string hint,string explanation,string[] options,string[] feedback,string[] codes)=>new IntegrationCalibration{id=id,module=module,title=title,prompt=prompt,correctOption=correct,hint=hint,explanation=explanation,options=options,wrongFeedback=feedback,errorCodes=codes};
        static LimitsQuestion[] Limits()=>new[]{
            L("LX1","Independent / numerical approach","Find the limit of 3x^2 - 1 as x approaches 2.","f(x) = 3x^2 - 1\nx approaches 2",11,"3(2^2) - 1 = 12 - 1 = 11. The nearby polynomial values approach 11."),
            C("LX2","Independent / a hole in a graph","Both sides approach the open point (3,6), while f(3) = 9. What is the limit as x approaches 3?",0,"The nearby values approach 6 from both sides. The separate value f(3) = 9 does not change this limit.","Follow the approaching value, not the filled point.","LIMIT_VS_FUNCTION_VALUE","Limit = 6","Limit = 9","Does not exist"),
            L("LX3","Independent / agreeing sides","Near x = 1, f(x) = x + 2 on the left and 2x + 1 on the right. Enter the two-sided limit.","Left: x + 2\nRight: 2x + 1\nx approaches 1",3,"The left expression approaches 1 + 2 = 3 and the right approaches 2(1) + 1 = 3. Their agreement gives limit 3."),
            C("LX4","Independent / limit and point value","For x != 2, f(x) = x^2 - 1; f(2) = 8. Choose the limit and the point value, in that order.",1,"Nearby values approach 2^2 - 1 = 3. The assigned point value is f(2) = 8.","Calculate the nearby limit separately from the explicitly assigned point value.","LIMIT_VS_FUNCTION_VALUE","Limit 8; value 3","Limit 3; value 8","Both values are 3"),
            C("LX5","Independent / unequal sides","At x = 4, the left-hand limit is -1 and the right-hand limit is 3. Choose the two-sided limit.",2,"The one-sided limits -1 and 3 disagree, so there is no single two-sided limit.","A two-sided limit needs the same approached value from both sides.","UNEQUAL_SIDES_DNE","Limit = -1","Limit = 3","Does not exist")
        };
        static IntegrationCalibration[] Integrals()=>new[]{
            I("IX_DIAL1",0,"Exponent Dial / coefficient check","Choose the general indefinite integral of 6x^2.",1,"Raise the exponent, then divide 6 by the new exponent.","Raise 2 to 3 and divide 6 by 3: 2x^3 + C.",new[]{"18x + C","2x^3 + C","2x^3"},new[]{"This differentiates the term. Integration raises the exponent and divides by the raised value.","Correct.","The expression is an antiderivative, but the general indefinite integral needs +C."},new[]{"INT_EXPONENT","","MISSING_C"}),
            I("IX_DIAL2",0,"Exponent Dial / signed coefficient","Choose the general indefinite integral of -8x^3.",2,"Preserve the negative sign. Raise 3 to 4 and divide by 4.","-8 divided by 4 is -2, giving -2x^4 + C.",new[]{"2x^4 + C","-32x^4 + C","-2x^4 + C"},new[]{"The sign should remain negative when dividing -8 by 4.","Divide by the new exponent rather than multiplying.","Correct."},new[]{"INT_DIVISOR","INT_DIVISOR",""}),
            I("IX_WIRE1",1,"Wires / one shared constant","Choose the general indefinite integral of 2x + 3.",0,"Integrate each term and finish with one common +C.","The two antiderivatives are x^2 and 3x. One shared +C gives x^2 + 3x + C.",new[]{"x^2 + 3x + C","x^2 + 3x","2x^2 + 3 + C"},new[]{"Correct.","Include one arbitrary constant for the full indefinite integral.","Integrate each term: divide the quadratic coefficient and turn the constant 3 into 3x."},new[]{"","MISSING_C","TERM_ANTIDERIVATIVE"}),
            I("IX_WIRE2",1,"Wires / three independent terms","Choose the general indefinite integral of 3x^2 - 4x + 5.",1,"Keep each term's coefficient and sign separate.","Integrate term by term: x^3 - 2x^2 + 5x + C.",new[]{"x^3 + 2x^2 + 5x + C","x^3 - 2x^2 + 5x + C","x^3 - 2x^2 + 5 + C"},new[]{"The negative linear term produces a negative quadratic term.","Correct.","A constant integrates to a linear term: 5 becomes 5x."},new[]{"TERM_ANTIDERIVATIVE","","INTEGRATION_CONSTANT"}),
            I("IX_SEQ1",2,"Sequence / cube-root power","For x > 0, choose the general integral of the cube root of x.",0,"Rewrite as x^(1/3). Add one, then divide by the resulting exponent.","1/3 + 1 = 4/3. Dividing by 4/3 gives (3/4)x^(4/3) + C.",new[]{"(3/4)x^(4/3) + C","(4/3)x^(4/3) + C","(1/3)x^(-2/3) + C"},new[]{"Correct.","Divide by 4/3, which multiplies by its reciprocal 3/4.","This lowers the exponent. Integration raises it by one."},new[]{"","INT_DIVISOR","INT_EXPONENT"}),
            I("IX_SEQ2",2,"Sequence / reciprocal square root","For x > 0, choose the general indefinite integral of 2/sqrt(x).",2,"Rewrite as 2x^(-1/2), then raise the exponent to 1/2.","Divide 2 by 1/2 to obtain 4: the integral is 4sqrt(x) + C.",new[]{"-x^(-3/2) + C","sqrt(x) + C","4sqrt(x) + C"},new[]{"That is the derivative. Integration raises -1/2 to 1/2.","Divide 2 by 1/2 rather than multiplying.","Correct."},new[]{"INT_EXPONENT","INT_DIVISOR",""}),
            I("IX_BOUND1",3,"Pressure / nonzero lower bound","Evaluate the definite integral of 2x from 1 to 3.",2,"Use F(x) = x^2 and compute F(3) - F(1).","F(3) - F(1) = 9 - 1 = 8. The constants cancel; no +C appears in the numerical result.",new[]{"8 + C","10","8"},new[]{"A definite integral gives a number; the arbitrary constants cancel.","Subtract the lower-bound value instead of adding it.","Correct."},new[]{"DEFINITE_ACCUMULATION","DEFINITE_ACCUMULATION",""}),
            I("IX_BOUND2",3,"Pressure / quadratic accumulation","Evaluate the definite integral of 3x^2 from 0 to 2.",1,"First integrate to obtain F(x) = x^3.","F(2) - F(0) = 2^3 - 0^3 = 8.",new[]{"4","8","12"},new[]{"Find the antiderivative before evaluating the bounds.","Correct.","12 is the integrand at 2, not the accumulated integral."},new[]{"DEFINITE_ACCUMULATION","","DEFINITE_ACCUMULATION"})
        };
        [MenuItem("Calculus Lab/Questions/Add Limits and Integration questions")]
        public static void Apply()
        {
            Directory.CreateDirectory(Evidence);
            var limits=AssetDatabase.LoadAssetAtPath<LimitsQuestionBank>(Root+"LimitsIndependentQuestions.asset");if(limits==null){limits=ScriptableObject.CreateInstance<LimitsQuestionBank>();limits.questions=Limits();AssetDatabase.CreateAsset(limits,Root+"LimitsIndependentQuestions.asset");}
            var integrals=AssetDatabase.LoadAssetAtPath<IntegrationQuestionBank>(Root+"IntegrationQuestions.asset");if(integrals==null){integrals=ScriptableObject.CreateInstance<IntegrationQuestionBank>();integrals.questions=Integrals();AssetDatabase.CreateAsset(integrals,Root+"IntegrationQuestions.asset");}
            if(limits.questions.Length!=5||limits.questions.Select(q=>q.id).Distinct().Count()!=5||integrals.questions.Length!=8||integrals.questions.Select(q=>q.id).Distinct().Count()!=8||Enumerable.Range(0,4).Any(i=>integrals.questions.Count(q=>q.module==i)!=2))throw new Exception("Incomplete independent topic banks.");
            if(integrals.questions.Any(q=>q.options.Length!=3||q.wrongFeedback.Length!=3||q.errorCodes.Length!=3||q.correctOption<0||q.correctOption>2))throw new Exception("Invalid calibration choices.");
            ProductionUIBuilder.UpgradeOtherTopicCalibrationUI();AssetDatabase.SaveAssets();Debug.Log("OTHER_TOPIC_BANKS_EDITOR_PASS");
        }
        [MenuItem("Calculus Lab/Questions/Open Limits independent questions")]
        static void OpenLimits(){Selection.activeObject=AssetDatabase.LoadAssetAtPath<LimitsQuestionBank>(Root+"LimitsIndependentQuestions.asset");}
        [MenuItem("Calculus Lab/Questions/Open Integration question bank")]
        static void OpenCase(){Selection.activeObject=AssetDatabase.LoadAssetAtPath<IntegrationQuestionBank>(Root+"IntegrationQuestions.asset");}
        static void CheckLegacyAndGates()
        {
            foreach(int topic in new[]{0,2})
            {
                string path=SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity";EditorSceneManager.OpenScene(path);var world=UnityEngine.Object.FindFirstObjectByType<Chapter4World>();var game=world.gameObject.AddComponent<Chapter4Game>();
                try
                {
                    game.Initialize(topic,world);
                    var old=new Chapter4Session{topic=topic,profile=Chapter4Store.Profile,required=topic==0?5:10};old.completed.Add(topic==0?"L1":"I_DIAL");
                    if(topic==0){game.CanvasResume(old);if(game.Stage!=1||game.LimitsExtension!=null||game.Session.required!=5)throw new Exception("Legacy Limits changed.");}
                    else{game.MissionResume(old);if(game.ModulesComplete!=1||!game.CaseModuleComplete(0)||game.Session.required!=10)throw new Exception("Legacy Integration changed.");}
                    var current=new Chapter4Session{topic=topic,profile=Chapter4Store.Profile,required=topic==0?10:18,limitsCurriculumVersion=topic==0?1:0,integrationCurriculumVersion=topic==2?1:0};current.completed.Add(topic==0?"L1":"I_DIAL");
                    if(topic==0){game.CanvasResume(current);if(game.Stage!=0||game.LimitsExtension?.id!="LX1")throw new Exception("Limits independent gate skipped.");}
                    else
                    {
                        game.MissionResume(current);game.SetState("Puzzle");if(game.ModulesComplete!=0||game.CaseCalibration?.id!="IX_DIAL1"||game.MissionSelectModule(1))throw new Exception("Case calibration/unlock skipped.");
                        current.completed.Add("IX_DIAL1");game.MissionResume(current);if(game.CaseCalibration?.id!="IX_DIAL2"||game.ModulesComplete!=0)throw new Exception("Partial dial calibration skipped.");
                        current.completed.Add("IX_DIAL2");game.MissionResume(current);game.SetState("Puzzle");if(!game.CaseModuleComplete(0)||!game.MissionSelectModule(3)||game.ModulesComplete!=1)throw new Exception("Completed dial did not unlock other modules.");
                    }
                }
                finally{UnityEngine.Object.DestroyImmediate(game);EditorSceneManager.OpenScene(path);}
            }
            Debug.Log("OTHER_TOPIC_LEGACY_AND_GATES_PASS");
        }
        public static void ApplyAndBuild(){Apply();Apply();CheckLegacyAndGates();ProductionUIBuilder.BuildDelivery();Debug.Log("OTHER_TOPIC_BANKS_DELIVERY_PASS");}
    }
    public static partial class ProductionUIBuilder
    {
        public static void UpgradeOtherTopicCalibrationUI()
        {
            foreach(string name in new[]{"LimitsUI","IntegrationUI","MainMenuUI"})
            {
                string path=PrefabsPath+"/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var all=root.GetComponentsInChildren<Transform>(true);font=root.GetComponentsInChildren<TMP_Text>(true).First().font;
                    if(name=="LimitsUI")
                    {
                        if(!all.Any(t=>t.name=="IndependentGiven")){var given=Text(all.First(t=>t.name=="AnswerControls"),"IndependentGiven","Independent check",432,300,770,145,29,Ink,true);given.gameObject.SetActive(false);}
                        foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
                        {
                            if(label.name=="BriefObjective")label.text="Restore five portal sections with guided and independent questions.";
                            if(label.name=="PortalDescription")label.text="Five calibrations.\nGuided task + independent check.\n\nEach completed pair restores\n20% portal charge.";
                        }
                    }
                    else if(name=="IntegrationUI")
                    {
                        if(!all.Any(t=>t.name=="CaseCalibration"))
                        {
                            var panel=Rect(all.First(t=>t.name=="CaseModule"),"CaseCalibration",0,0,1280,720);
                            for(int i=0;i<3;i++)Button(panel,"CalibrationOption"+i,"Select an answer",448,301+i*82,750,61);
                            Button(panel,"CalibrationSubmit","Check calibration",929,648,289,45,true);Button(panel,"CalibrationNext","Next calibration",913,648,305,45,true);panel.gameObject.SetActive(false);
                        }
                        foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))if(label.name=="BriefLearning")label.text="Complete the guided controls and two independent questions per module.\nPractise powers, term-by-term integration, roots and definite accumulation.\n\nComplete all four modules before using Master Deactivation.";
                    }
                    else foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))if(label.text.Contains("Calibrate five questions"))label.text=label.text.Replace("Calibrate five questions","Complete ten questions");
                    ConfigureMathText(root);PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
    }
}
#endif
