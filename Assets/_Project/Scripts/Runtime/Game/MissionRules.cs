using System;

namespace CalculusLabEscape
{
    public static class MissionRules
    {
        public static readonly string[] BridgeTitles={"Power Core / regulator","Converter / rewrite a root","Converter / differentiate a root","Converter / reciprocal power","Bridge Builder / first term","Bridge Builder / second term","Bridge Builder / constant","Bridge Builder / assemble anchor","River challenge / rewrite","River challenge / linear term","River challenge / final assembly"};
        public static readonly string[] BridgePrompts={"Differentiate f(x) = x^4. Move the 4 into the coefficient slot, then reduce the exponent by one.","For x >= 0, rewrite sqrt(x) as x^n. Enter the exponent n.","For x > 0, differentiate x^(1/2). Enter the new coefficient and exponent.","For x != 0, differentiate 3/x^6 = 3x^(-6). Enter the new coefficient and exponent.","For f(x) = x^5 + 2x^(-3) + 7, x != 0, differentiate the first term x^5.","For f(x) = x^5 + 2x^(-3) + 7, x != 0, differentiate the term 2x^(-3).","For f(x) = x^5 + 2x^(-3) + 7, x != 0, differentiate the constant term 7.","For f(x) = x^5 + 2x^(-3) + 7, x != 0, choose the full derivative to install the Bridge Anchor.","For f(x) = x^4 - 2x + 1/x^2, x != 0, rewrite 1/x^2 as x^n. Enter n.","For f(x) = x^4 - 2x + 1/x^2, x != 0, differentiate the term -2x. Enter its constant derivative.","For f(x) = x^4 - 2x + x^(-2), x != 0, choose the full derivative to stabilise the crossing."};
        public static readonly string[] BridgeExplanations={"Multiply by 4 and subtract one from the exponent: 4x^3.","The square root is a power of 1/2: sqrt(x) = x^(1/2).","(1/2)x^(-1/2): multiply by 1/2, then subtract 1.","3 x (-6) = -18 and -6 - 1 = -7: -18x^(-7).","The first term becomes 5x^4.","2 x (-3) = -6 and -3 - 1 = -4: -6x^(-4).","A constant does not change, so its derivative is 0.","The combined derivative is 5x^4 - 6x^(-4).","A reciprocal uses a negative exponent: 1/x^2 = x^(-2).","The derivative of -2x is -2.","The complete derivative is 4x^3 - 2 - 2x^(-3)."};
        public static readonly string[] WireInputs={"3x^2","4x","-5"},WireOutputs={"x^3","2x^2","-5x","3x^3","4x^2"};
        public static readonly string[] SequenceCards={"Rewrite","Raise","Divide","Add C"};
        public static bool BridgePair(int stage)=>stage>=2&&stage<=5;
        public static string[] BridgeChoices(int stage)=>stage==7?new[]{"5x^4 - 6x^(-4)","5x^4 + 2x^(-4) + 7","x^4 - 6x^(-2)"}:new[]{"4x^3 - 2 - 2x^(-3)","4x^3 - 2 + 2x^(-1)","x^3 - 2x - 2x^(-3)"};
        public static LimitsEvaluation Bridge(int stage,string a,string b,bool chip,int exponent,int choice)
        {
            if(stage<0||stage>10)return new LimitsEvaluation(false,false,"");
            if(stage==0)return new LimitsEvaluation(chip,chip&&exponent==3,"coefficient="+(chip?4:0)+";exponent="+exponent);
            if(stage==7||stage==10)return new LimitsEvaluation(choice>=0&&choice<3,choice==0,choice>=0&&choice<3?BridgeChoices(stage)[choice]:"");
            bool pair=BridgePair(stage),complete=!string.IsNullOrWhiteSpace(a)&&(!pair||!string.IsNullOrWhiteSpace(b));
            float[] ca={0,.5f,.5f,-18,5,-6,0,0,-2,-2,0},cb={0,0,-.5f,-7,4,-4,0,0,0,0,0};
            return new LimitsEvaluation(complete,complete&&Chapter4Game.NumericAnswer(a,ca[stage])&&(!pair||Chapter4Game.NumericAnswer(b,cb[stage])),pair?a+" | "+b:a);
        }
        public static bool SequenceValue(int step,string value)=>step==3?string.Equals(value.Trim(),"C",StringComparison.OrdinalIgnoreCase):Chapter4Game.NumericAnswer(value,step==0?.5f:step==1?1.5f:2f/3);
    }
}
