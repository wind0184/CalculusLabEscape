#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
namespace CalculusLabEscape.Editor
{
    public static partial class ProductionUIBuilder
    {
        static readonly HashSet<string> MathLabels=new HashSet<string>{"ReportResultState","PowerExpression","PowerPreview","TaskPrompt","QuestionPrompt","IndependentGiven","Feedback","SupportText","ManualText","DialPreview","SequenceValueLabel","DragLabel","PracticeQuestion","PracticeFeedback","PracticeResults","DetailAttempts"};
        static bool IsMathLabel(string name)=>MathLabels.Contains(name)||name.StartsWith("CalibrationOption",StringComparison.Ordinal)&&name.EndsWith("Label",StringComparison.Ordinal)||name.StartsWith("BridgeChoice",StringComparison.Ordinal)&&name.EndsWith("Label",StringComparison.Ordinal)||name.StartsWith("WireSource",StringComparison.Ordinal)&&name.EndsWith("Label",StringComparison.Ordinal)||name.StartsWith("WireTarget",StringComparison.Ordinal)&&name.EndsWith("Label",StringComparison.Ordinal)||name.StartsWith("PracticeOption",StringComparison.Ordinal)&&name.EndsWith("Label",StringComparison.Ordinal);
        static void ConfigureMathText(GameObject root)
        {
            foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
                if(IsMathLabel(label.name)&&label.GetComponentInParent<TMP_InputField>()==null&&label.GetComponent<MathText>()==null)
                {var math=label.gameObject.AddComponent<MathText>();math.SetSource(label.text);}
            foreach(var input in root.GetComponentsInChildren<TMP_InputField>(true))
                if(input.name=="BridgeA"||input.name=="BridgeB"||input.name=="SequenceValue")
                {input.richText=false;input.textComponent.richText=false;}
            foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                // Superscripts need additional line height, especially in multi-line reviews.
                if(label.name=="PowerExpression")ResizeMathLabel(label,267,72);
                if(label.name=="PracticeResults")ResizeMathLabel(label,238,310);
                if(label.name=="PracticeFeedback")ResizeMathLabel(label,562,66);
            }
        }
        static void ResizeMathLabel(TMP_Text label,float y,float height)
        {var r=label.rectTransform;r.anchoredPosition=new Vector2(r.anchoredPosition.x,-y);r.sizeDelta=new Vector2(r.sizeDelta.x,height);}
        [MenuItem("Calculus Lab/Production/Upgrade mathematical notation")]
        public static void UpgradeMathText()
        {
            foreach(string name in new[]{"MainMenuUI","DifferentiationUI","IntegrationUI"})
            {
                string path=PrefabsPath+"/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try{ConfigureMathText(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Debug.Log("MATH_NOTATION_PREFABS_UPDATED");
        }
        public static void UpgradeMathAndBuild(){UpgradeMathText();Build();}
    }
    public static class MathNotationChecks
    {
        static void Check(bool condition,string message){if(!condition)throw new Exception("Math notation: "+message);}
        public static void Validate()
        {
            Check(MathNotation.Format("4x^3 + x^(-1/2)")=="4x<sup><size=140%>3</size></sup> + x<sup><size=140%>-1/2</size></sup>","Signed and fractional powers");
            Check(MathNotation.Format("x^(n+1) / (n+1) + C")=="x<sup><size=140%>n+1</size></sup> / (n+1) + C","Symbolic Power Rule");
            Check(MathNotation.Format("x^2.5; x^-2; x^(3-1)")=="x<sup><size=140%>2.5</size></sup>; x<sup><size=140%>-2</size></sup>; x<sup><size=140%>3-1</size></sup>","Decimal and arithmetic exponents");
            Check(MathNotation.Format("sqrt(x) = x^(1/2)")=="\u221ax = x<sup><size=140%>1/2</size></sup>","Square root notation");
            Check(MathNotation.Format("Differentiate x^4.")=="Differentiate x<sup><size=140%>4</size></sup>.","Sentence punctuation stays outside the exponent");
            foreach(string literal in new[]{"1/2","-1/2","3/3 accepted","x^","x^(oops)","x^2oops","x^-1/2","matrix^4"})Check(MathNotation.Format(literal)==literal,"Do not reinterpret input, progress or unsupported expressions: "+literal);
            Check(MathNotation.Format("<color=red>x^4</color>")=="<noparse><</noparse>color=red>x<sup><size=140%>4</size></sup><noparse><</noparse>/color>","Saved text cannot inject TMP formatting");
            Check(MathNotation.Format("</noparse><size=200>").StartsWith("<noparse><</noparse>/noparse>"),"A closing tag stays literal too");
            Debug.Log("MATH_NOTATION_CHECKS_PASS / exponents, roots, literal input, unsupported forms and rich-text isolation");
        }
    }
}
#endif
