#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace CalculusLabEscape.Editor
{
    // Explicit authoring command: updates saved question assets once, never during Play.
    public static class FeedbackRevisionAuthoring
    {
        const string Root="Assets/_Project/Resources/Production/";
        [MenuItem("Calculus Lab/Questions/Apply feedback and hint revision")]
        public static void Apply()
        {
            foreach(string name in new[]{"LimitsQuestions","LimitsIndependentQuestions"})
            {
                var bank=AssetDatabase.LoadAssetAtPath<LimitsQuestionBank>(Root+name+".asset");
                foreach(var q in bank.questions){var h=Limits(q.id);q.basicHint=h[0];q.ruleReminder=h[1];q.demonstration=h[2];}
                EditorUtility.SetDirty(bank);
            }
            var diff=DifferentiationQuestionBank.Load();
            foreach(var q in diff.questions){var h=Derivative(q.id);q.basicHint=h[0];q.ruleReminder=MissionHints.DerivativeRuleFor(q.id);q.demonstration=h[1];}
            EditorUtility.SetDirty(diff);
            var integral=IntegrationQuestionBank.Load();
            foreach(var q in integral.questions){var h=Integral(q.id);q.hint=h[0];q.ruleReminder=q.id=="IX_WIRE1"?"Integrate each term separately. For a*x, the integral is (a/2)x^2. For a constant b, the integral is b*x. Combine the results with one +C.":q.id=="IX_WIRE2"?"For a polynomial, integrate each term separately using the Power Rule. Preserve the sign of every coefficient. A constant term becomes a linear term, and the combined result needs one +C.":q.module==3?MissionHints.BoundsRule:MissionHints.IntegralRule;q.demonstration=h[1];}
            integral.questions[1].wrongFeedback[0]="Check the coefficient's sign. Dividing by a positive raised exponent preserves the original sign.";
            integral.questions[2].wrongFeedback[2]="Apply the integration rule to each term separately. Check both the linear term and the constant term.";
            integral.questions[3].wrongFeedback[2]="A constant integrates to a linear term. Check the final term's dependence on x.";
            integral.questions[4].wrongFeedback[1]="Divide the original coefficient by the raised exponent. Dividing by a fraction multiplies by its reciprocal.";
            integral.questions[5].wrongFeedback[0]="That choice uses differentiation. Integration raises the rewritten exponent by one.";
            integral.questions[5].wrongFeedback[1]="Divide the original coefficient by the raised exponent rather than multiplying.";
            EditorUtility.SetDirty(integral);
            foreach(string name in new[]{"MainMenuUI","DifferentiationUI","IntegrationUI"})
            {
                string path="Assets/_Project/Prefabs/Resources/Production/"+name+".prefab";
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var label=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="PracticeQuestion");
                    var size=label.rectTransform.sizeDelta;size.y=78;label.rectTransform.sizeDelta=size;
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();
            Debug.Log("FEEDBACK_REVISION_ASSETS_SAVED / 25 question hint sets; answers and options preserved");
        }
        static string[] H(string basic,string rule,string demo)=>new[]{basic,rule,demo};
        static string[] Limits(string id)
        {
            const string nearby="A two-sided limit describes the common value approached from both sides. For a polynomial, evaluate the expression at the approach point.";
            const string point="A limit describes nearby behaviour. A separately assigned point value is read from the filled point.";
            const string sides="Find the left-hand and right-hand limits separately. A two-sided limit exists only if they agree.";
            const string suppliedSides="Compare the two supplied one-sided limits. A two-sided limit exists only when they are equal; if they differ, it does not exist.";
            switch(id)
            {
                case "L1":return H("Compare the table outputs on either side of the target. Which value do they move towards?",nearby,"Similar example: for g(x) = 2x + 1 as x approaches 4, g(3.99) = 8.98 and g(4.01) = 9.02. Both sides approach 9.");
                case "LX1":return H("Substitute the approach point into the polynomial. Square first, then multiply and subtract.",nearby,"Similar example: the limit of 2x^2 + 3 as x approaches 3 is 2(3^2) + 3 = 21. Apply the same order of operations to your polynomial.");
                case "L2":return H("Probe the curve closer to the target from each side. Track the approached y-value.","A limit describes the value approached by a function from both sides. An open point can indicate the limit even when the function is not defined at that point.","Similar example: a curve approaches an open point (1,6) from both sides. Its limit at x = 1 is 6, even if no value is assigned at the point.");
                case "LX2":return H("Follow the nearby curve from both directions. Distinguish that approach from the filled point.",point,"Similar example: a curve approaches an open point (-2,4), but g(-2) = 10. The limit is 4 while the assigned function value is 10.");
                case "L3":return H("Use the left expression for the left-hand field and the right expression for the right-hand field. Compare them last.",sides,"Similar example: at x = 2, left expression x + 4 approaches 6 and right expression 8 - x approaches 6. The two-sided limit is 6.");
                case "LX3":return H("Evaluate each side with its own expression at the approach point. Compare the two results.",sides,"Similar example: at x = 0, x + 6 on the left and 2x + 6 on the right both approach 6, so the two-sided limit is 6.");
                case "L4":return H("Use the nearby curve for the limit field and the filled point for the function-value field.",point,"Similar example: an open point (1,6) is approached from both sides, while the filled point is (1,9). The limit is 6 and g(1) is 9.");
                case "LX4":return H("Compute the limit using the expression away from the target. Read the assigned value separately, then check their order.",point,"Similar example: g(x) = x^2 + 2 for x != 3, and g(3) = 20. The limit is 11; the point value is 20.");
                case "L5":return H("Compare the two supplied one-sided values before choosing a conclusion and arranging the reason cards.",suppliedSides,"Similar example: if the left-hand limit is 5 and the right-hand limit is 9, they disagree. There is no two-sided limit because the left-hand limit is not equal to the right-hand limit.");
                case "LX5":return H("Ask whether one value can describe the approach from both directions. Compare the two one-sided values.",suppliedSides,"Similar example: one side approaches 7 and the other approaches 10. A single two-sided limit cannot describe both approaches.");
                default:throw new Exception("Unknown Limits question "+id);
            }
        }
        static string[] Derivative(string id)
        {
            switch(id)
            {
                case "DX_BASIC_1":return new[]{"Identify the coefficient and exponent. Calculate the new coefficient and new exponent separately.","Similar example: d(4x^7)/dx = 4(7)x^(7-1) = 28x^6. Apply these operations to your own coefficient and exponent."};
                case "DX_BASIC_2":return new[]{"Keep the exponent's negative sign when multiplying. Subtract one whole unit for the new exponent.","Similar example: d(x^(-7))/dx = -7x^(-8), for x != 0. Subtracting one makes a negative exponent more negative."};
                case "DX_BASIC_3":return new[]{"First rewrite the square root as a fractional power. Think about how the power inside the root and the root index determine the exponent.","Similar example: the cube root of x^2 is x^(2/3): inside power 2 divided by root index 3. Then 1(2/3)x^(2/3-1) = (2/3)x^(-1/3), for x > 0."};
                case "DX_BASIC_4":return new[]{"Separate the numerical fraction from the reciprocal power. Preserve both when applying the rule.","Similar example: 3/(2x^6) = (3/2)x^(-6). Its derivative is -9x^(-7), for x != 0."};
                case "DX_BASIC_5":return new[]{"Rewrite the reciprocal as a negative power. Track the signs in the coefficient multiplication separately.","Similar example: -3/x^5 = -3x^(-5), whose derivative is 15x^(-6), for x != 0. Two negative factors produce a positive coefficient."};
                case "DX_APPLY_2":return new[]{"Try splitting the fraction into separate terms before differentiating. Pay attention to how dividing by a power of x changes each term's exponent.","Similar example: for x > 0, (3x + 2)/x^(1/3). Split: 3x^(1-1/3) + 2x^(0-1/3) = 3x^(2/3) + 2x^(-1/3). Differentiate: 2x^(-1/3) - (2/3)x^(-4/3)."};
                case "DX_APPLY_3":return new[]{"Look carefully at the denominator. How does it affect the coefficient and exponent of each numerator term? Rewrite the expression before differentiating.","Similar example: for x > 0, (6x^2 - 4)/(2sqrt(x)). Divide coefficients by 2 and subtract 1/2 from each power: 3x^(3/2) - 2x^(-1/2). Differentiate: (9/2)x^(1/2) + x^(-3/2)."};
                default:throw new Exception("Unknown differentiation question "+id);
            }
        }
        static string[] Integral(string id)
        {
            switch(id)
            {
                case "IX_DIAL1":return new[]{"Raise the exponent first. Divide the original coefficient by that new exponent and check the constant.","Similar example: integrate 4x^3 to get (4/4)x^4 + C = x^4 + C."};
                case "IX_DIAL2":return new[]{"Raise the exponent and divide by it. Check that the coefficient keeps its original sign.","Similar example: integrate -15x^4 to get (-15/5)x^5 + C = -3x^5 + C."};
                case "IX_WIRE1":return new[]{"Integrate the linear term and the constant term separately. Use one constant after combining.","Similar example: the integral of 6x + 4 is 3x^2 + 4x + C. Differentiate both terms to check."};
                case "IX_WIRE2":return new[]{"Work on each term separately, preserving signs. Check how a constant term changes when integrated.","Similar example: the integral of 8x^3 - 6x + 7 is 2x^4 - 3x^2 + 7x + C."};
                case "IX_SEQ1":return new[]{"Express the root as a fractional power. Add one whole unit, then divide by the raised exponent.","Similar example: integrate x^(1/4), for x > 0. Raising gives 5/4; dividing by it gives (4/5)x^(5/4) + C."};
                case "IX_SEQ2":return new[]{"Rewrite the reciprocal root as a negative fractional power. Increase the exponent by one, then divide the original coefficient by this new exponent.","Similar example: integrate 3/x^(1/3), for x > 0. Rewrite as 3x^(-1/3), raise to 2/3 and divide to get (9/2)x^(2/3) + C."};
                case "IX_BOUND1":return new[]{"Find an antiderivative first. Evaluate it at both bounds and subtract in the correct order.","Similar example: integrate 4x from 2 to 4. With F(x) = 2x^2, the result is 32 - 8 = 24. The constants cancel."};
                case "IX_BOUND2":return new[]{"Integrate the power before substituting the bounds. Check that you are evaluating accumulation rather than an endpoint output.","Similar example: integrate 5x^4 from 0 to 2. With F(x) = x^5, F(2) - F(0) = 32."};
                default:throw new Exception("Unknown integration question "+id);
            }
        }
    }
}
#endif
