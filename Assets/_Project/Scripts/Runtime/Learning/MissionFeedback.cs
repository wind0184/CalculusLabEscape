using System;
using System.Globalization;

namespace CalculusLabEscape
{
    public readonly struct MissionFeedbackNote
    {
        public readonly string code,message;
        public MissionFeedbackNote(string code,string message){this.code=code;this.message=message;}
    }
    public static class MissionFeedback
    {
        static MissionFeedbackNote Note(string code,string message)=>new MissionFeedbackNote(code,message);
        static bool Finite(string s,out float value)=>float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out value)&&!float.IsNaN(value)&&!float.IsInfinity(value);
        public static bool NumberFormat(string value)
        {
            if(string.IsNullOrWhiteSpace(value))return false;
            var parts=value.Trim().Split('/');
            if(parts.Length==1)return Finite(parts[0],out _);
            return parts.Length==2&&Finite(parts[0],out var n)&&Finite(parts[1],out var d)&&d!=0&&!float.IsInfinity(n/d);
        }
        static MissionFeedbackNote Format()=>Note("INPUT_FORMAT","Enter a finite number or a fraction such as 1/2. A fraction's denominator cannot be zero.");
        public static MissionFeedbackNote Limits(LimitsQuestion q,LimitsAnswer a)
        {
            var result=LimitsRules.Evaluate(q,a);
            if(result.correct)return Note("",q.explanation);
            if(q.kind!=LimitsAnswerKind.Choice&&q.kind!=LimitsAnswerKind.Conclusion&&q.kind!=LimitsAnswerKind.GraphMarker)
            {
                for(int i=0;i<q.expected.Length;i++)if(!NumberFormat(a.values[i]))return Format();
            }
            if(q.kind==LimitsAnswerKind.ThreeValues)
            {
                bool left=Chapter4Game.NumericAnswer(a.values[0],q.expected[0]),right=Chapter4Game.NumericAnswer(a.values[1],q.expected[1]);
                if(left&&right)return Note(q.errorCode,"Both one-sided values are correct. Compare them before deciding the two-sided limit.");
                if(left)return Note(q.errorCode,"The left-hand value is correct. Recheck the expression used on the right before comparing the two sides.");
                if(right)return Note(q.errorCode,"The right-hand value is correct. Recheck the expression used on the left before comparing the two sides.");
                return Note(q.errorCode,"Evaluate each side using its own expression, then compare the approached values.");
            }
            if(q.kind==LimitsAnswerKind.LimitAndValue)
            {
                bool limit=Chapter4Game.NumericAnswer(a.values[0],q.expected[0]),point=Chapter4Game.NumericAnswer(a.values[1],q.expected[1]);
                return Note(q.errorCode,limit?"The limit is correct. Read the assigned function value from the filled point.":point?"The function value is correct. Follow the nearby curve from both sides to find the limit.":"Read the limit from the nearby curve and the function value from the filled point. Check both fields separately.");
            }
            if(q.kind==LimitsAnswerKind.Conclusion)return Note(q.errorCode,a.conclusion==2?"The conclusion is correct. Arrange the reason as a comparison of the left-hand and right-hand limits.":"Compare the two one-sided values. A two-sided limit requires agreement; use that comparison to choose your conclusion.");
            if(q.kind==LimitsAnswerKind.GraphMarker)return Note(q.errorCode,"Trace the curve towards the target from both sides. Move the marker to the y-value the curve approaches.");
            if(q.id=="LX2")return Note(q.errorCode,"Follow the nearby curve towards the open point from both sides. Check whether the filled point changes that behaviour.");
            if(q.id=="LX4")return Note(q.errorCode,"Calculate the nearby limit using the expression for x away from the target. Read the assigned point value separately, then check their order.");
            if(q.id=="LX5")return Note(q.errorCode,"Compare the left-hand and right-hand values. Can one value describe the approach from both directions?");
            if(q.id=="LX3")return Note(q.errorCode,"Evaluate the left and right expressions at the approach point separately. Do they approach the same value?");
            return Note(q.errorCode,"Recheck the nearby outputs from both sides of the target. For a polynomial, substitute the approach point and check the arithmetic.");
        }
        public static MissionFeedbackNote BridgeIndependent(DifferentiationQuestion q,string a,string b)
        {
            if(!NumberFormat(a)||!NumberFormat(b))return Format();
            float Number(string s){var p=s.Split('/');return float.Parse(p[0],CultureInfo.InvariantCulture)/(p.Length==2?float.Parse(p[1],CultureInfo.InvariantCulture):1);}
            bool coefficient=Chapter4Game.NumericAnswer(a,Number(q.coefficient)),power=Chapter4Game.NumericAnswer(b,Number(q.exponent));
            if(coefficient&&power)return Note("",q.explanation);
            if(coefficient)return Note("DIFF_EXPONENT","The coefficient is correct. Subtract one from the rewritten exponent; keep fractions exact.");
            if(power)return Note("DIFF_COEFFICIENT","The new exponent is correct. Multiply the rewritten coefficient by the old exponent, preserving signs.");
            return Note("POWER_RULE","Check your power form first. Multiply the original coefficient by its exponent, then subtract one from that exponent. Recheck both controls.");
        }
        public static MissionFeedbackNote Bridge(int stage,string a,string b,bool chip,int exponent,int choice)
        {
            var result=MissionRules.Bridge(stage,a,b,chip,exponent,choice);
            if(stage<0||stage>10)return Note("INPUT_FORMAT","Return to an active repair step.");
            if(result.correct)return Note("",MissionRules.BridgeExplanations[stage]);
            if(stage==0)return Note("DIFF_EXPONENT","The coefficient chip is in place. For differentiation, subtract one from the old exponent; adjust the exponent dial.");
            if(stage==7)return Note("TERM_ASSEMBLY",choice==1?"The constant term should not remain. Recheck the coefficient and sign of the negative-power term.":"Combine the derivatives you already accepted. Keep each new coefficient and exponent unchanged.");
            if(stage==10)return choice==1?Note("NEGATIVE_POWER","Recheck the reciprocal term: multiply by its negative exponent, then subtract one from that exponent."):Note("TERM_ASSEMBLY","The x^4 and -2x terms still need differentiation. Combine all three derivatives, not the original terms.");
            if(!NumberFormat(a)||(MissionRules.BridgePair(stage)&&!NumberFormat(b)))return Format();
            if(stage==1)return Note("ROOT_REWRITE","A square root is a fractional power. Check which denominator represents a square root.");
            if(stage==8)return Note("RECIPROCAL_REWRITE","Moving a reciprocal power into the numerator changes the sign of its exponent.");
            if(stage==6)return Note("CONSTANT_DERIVATIVE","The graph of a constant is flat. Think about its rate of change as x varies.");
            if(stage==9)return Note("LINEAR_DERIVATIVE","The term -2x has exponent 1. Apply the Power Rule and simplify the resulting power of x.");
            float[] coefficients={0,0,.5f,-18,5,-6},powers={0,0,-.5f,-7,4,-4};
            bool coefficientCorrect=Chapter4Game.NumericAnswer(a,coefficients[stage]),powerCorrect=Chapter4Game.NumericAnswer(b,powers[stage]);
            if(coefficientCorrect)return Note("DIFF_EXPONENT","The coefficient is correct. Subtract one from the old exponent; a negative exponent becomes more negative.");
            if(powerCorrect)return Note("DIFF_COEFFICIENT","The new exponent is correct. Multiply the original coefficient by the original exponent, keeping its sign.");
            return Note("POWER_RULE","Check both controls: multiply the original coefficient by its exponent, then subtract one from the exponent.");
        }
        public static MissionFeedbackNote Dial(int exponent,int divisor,bool constant)
        {
            if(exponent!=5)return Note("INT_EXPONENT","For integration, raise the original exponent by one. Start with the exponent dial.");
            if(divisor!=5)return Note("INT_DIVISOR","The raised exponent is correct. Use that same new exponent as the divisor.");
            if(!constant)return Note("MISSING_C","Both dials are correct. An indefinite integral needs an arbitrary constant; insert one +C.");
            return Note("","Raise 4 to 5, divide by 5, then add C: x^5/5 + C.");
        }
        public static MissionFeedbackNote Wire(int source,int target)
        {
            if(source==target)return Note("",MissionRules.WireInputs[source]+" integrates to "+MissionRules.WireOutputs[target]+". This connection is locked; one common +C is added after all terms.");
            string[] derivatives={"3x^2","4x","-5","9x^2","8x"};
            string code=source==0&&target==3||source==1&&target==4?"INT_DIVISOR":source==2?"INTEGRATION_CONSTANT":"TERM_ANTIDERIVATIVE";
            return Note(code,"Your chosen output differentiates to "+derivatives[target]+". The source is "+MissionRules.WireInputs[source]+"; recheck the coefficient and power.");
        }
        public static MissionFeedbackNote Sequence(int step,int card,string value)
        {
            if(card!=step)return Note("SEQUENCE_ORDER","This is the "+MissionRules.SequenceCards[step]+" stage. Place that operation in the current slot before checking its value.");
            if(step<3&&!NumberFormat(value))return Format();
            if(MissionRules.SequenceValue(step,value))return Note("",new[]{"Rewrite sqrt(x) as x^(1/2).","Raise 1/2 by one to get 3/2.","Divide 1 by 3/2 to get the coefficient 2/3.","Add C: (2/3)x^(3/2) + C. The sequence is complete."}[step]);
            return Note(new[]{"ROOT_REWRITE","INT_EXPONENT","INT_DIVISOR","MISSING_C"}[step],new[]{"The Rewrite card is correct. Express the square root using a fractional exponent.","The Raise card is correct. Add one whole unit to the fractional exponent.","The Divide card is correct. Divide the original coefficient by the new exponent; dividing by a fraction multiplies by its reciprocal.","The Add C card is correct. Enter C to represent the arbitrary constant."}[step]);
        }
        public static string ErrorLabel(string code)
        {
            switch(code)
            {
                case null:case "":case "NONE":return "No errors recorded";
                case "NUMERICAL_APPROACH":return "Reading a limit from nearby values";
                case "GRAPH_LIMIT":return "Following the curve near the target";
                case "ONE_SIDED_MISMATCH":return "Comparing left- and right-hand limits";
                case "LIMIT_VS_FUNCTION_VALUE":return "Distinguishing a limit from the point value";
                case "UNEQUAL_SIDES_DNE":return "Recognising when the two sides disagree";
                case "INPUT_FORMAT":return "Number / fraction format";
                case "DIFF_COEFFICIENT":return "Differentiation coefficient";
                case "DIFF_EXPONENT":return "Differentiation exponent";
                case "INT_EXPONENT":return "Integration exponent";
                case "INT_DIVISOR":return "Integration divisor";
                case "MISSING_C":return "Arbitrary constant (+C)";
                case "ROOT_REWRITE":return "Root as a fractional power";
                case "RECIPROCAL_REWRITE":return "Reciprocal as a negative power";
                case "NEGATIVE_POWER":return "Negative-power differentiation";
                case "TERM_ASSEMBLY":return "Combining differentiated terms";
                case "SEQUENCE_ORDER":return "Integration operation order";
                case "CONSTANT_DERIVATIVE":return "Derivative of a constant";
                case "LINEAR_DERIVATIVE":return "Derivative of a linear term";
                case "INTEGRATION_CONSTANT":return "Integrating a constant term";
                case "TERM_ANTIDERIVATIVE":return "Matching antiderivatives";
                case "DEFINITE_ACCUMULATION":return "Evaluating upper and lower bounds";
                case "POWER_RULE":return "Differentiation Power Rule";
                case "EXPONENT_REWRITE":return "Rewriting roots and reciprocals";
                case "INTEGRATION_POWER_OR_C":return "Integration Power Rule / +C";
                case "SEQUENCE_OR_VALUE":return "Integration order / step value";
                default:return code.Replace('_',' ');
            }
        }
        public static string LimitsReviewAdvice(string code)
        {
            switch(code){
                case null:case "":case "NONE":return "Review your independent and assisted completions in Learning records. Try a new example without hints and explain why the limit exists or does not exist.";
                case "NUMERICAL_APPROACH":return "Compare outputs on both sides of the target. Look for the value they approach. Open Learning records to revisit your retries and feedback.";
                case "GRAPH_LIMIT":return "Trace the curve towards the target from both directions. A hole can still mark a limit. Review the graph feedback in Learning records.";
                case "ONE_SIDED_MISMATCH":return "Work out the left-hand and right-hand values separately, then compare them. A two-sided limit needs both values to agree.";
                case "LIMIT_VS_FUNCTION_VALUE":return "Read the limit from nearby values and the function value from the filled point. They can be different. Review the two values in your saved feedback.";
                case "UNEQUAL_SIDES_DNE":return "If the two sides approach different values, the two-sided limit does not exist. Revisit your left/right comparison in Learning records.";
                default:return "Revisit the unsuccessful submissions in Learning records. Read the saved feedback, then practise the same skill again.";
            }
        }
    }
}
