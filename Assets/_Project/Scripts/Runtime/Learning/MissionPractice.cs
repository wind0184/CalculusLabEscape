using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculusLabEscape
{
    public sealed class PracticeQuestion
    {
        public readonly string prompt,explanation;
        public readonly string[] options;
        public readonly int correct;
        public PracticeQuestion(string prompt,int correct,string explanation,params string[] options)
        {this.prompt=prompt;this.correct=correct;this.explanation=explanation;this.options=options;}
    }
    [Serializable] public sealed class PracticeAnswer
    {
        public int question,choice;
        public string prompt,response,explanation;
        public bool correct;
    }
    [Serializable] public sealed class PracticeRun
    {
        public string focus,started=DateTime.UtcNow.ToString("o");
        // Missing in older saves: zero keeps their original question set.
        public int questionSetVersion;
        public int position;
        public List<PracticeAnswer> answers=new List<PracticeAnswer>();
        public bool Complete=>answers.Count==3;
        public bool Submit(int choice)
        {
            var questions=MissionPractice.Questions(focus,questionSetVersion);
            if(position<0||position>=3||answers.Count!=position||choice<0||choice>=3)return false;
            var q=questions[position];answers.Add(new PracticeAnswer{question=position,choice=choice,prompt=q.prompt,response=q.options[choice],correct=choice==q.correct,explanation=q.explanation});return true;
        }
        public void Next(){if(position<3&&answers.Count>position)position++;}
        public string Result=>answers.Count(a=>a.correct)+" / 3 correct on first submission";
    }
    public static partial class MissionPractice
    {
        public static bool CanOpenRecord(Chapter4Session s,string profile)=>s!=null&&s.ownerId==0&&s.profile==profile&&s.status=="Completed"&&s.topic>=0&&s.topic<=2;
        public static string RecordAction(Chapter4Session s)
        {
            var last=s.practice?.LastOrDefault();
            return last==null?"Start focused practice":last.Complete?"View practice review":"Continue practice / "+last.answers.Count+" of 3 submitted";
        }
        public static readonly string[] Focuses={"derivatives","rewriting","constants","integration","constant_c","sequence","bounds","limits_nearby","limits_points","limits_sides"};
        public static string Focus(Chapter4Session s)
        {
            switch(s.CommonError)
            {
                case "NUMERICAL_APPROACH":case "GRAPH_LIMIT":return "limits_nearby";
                case "LIMIT_VS_FUNCTION_VALUE":return "limits_points";
                case "ONE_SIDED_MISMATCH":case "UNEQUAL_SIDES_DNE":return "limits_sides";
                case "ROOT_REWRITE":case "RECIPROCAL_REWRITE":case "EXPONENT_REWRITE":return "rewriting";
                case "CONSTANT_DERIVATIVE":case "LINEAR_DERIVATIVE":return "constants";
                case "MISSING_C":return "constant_c";
                case "SEQUENCE_ORDER":case "SEQUENCE_OR_VALUE":return "sequence";
                case "DEFINITE_ACCUMULATION":return "bounds";
                default:return s.topic==0?"limits_nearby":s.topic==1?"derivatives":"integration";
            }
        }
        public static string Title(string focus)
        {
            switch(focus){case "limits_nearby":return "Limits from nearby values";case "limits_points":return "Limits and point values";case "limits_sides":return "Comparing one-sided limits";case "rewriting":return "Roots and reciprocals";case "constants":return "Constant and linear terms";case "integration":return "Integration Power Rule";case "constant_c":return "When to use +C";case "sequence":return "Integration step order";case "bounds":return "Upper and lower bounds";default:return "Differentiation Power Rule";}
        }
        static PracticeQuestion Q(string prompt,int correct,string explanation,params string[] options)=>new PracticeQuestion(prompt,correct,explanation,options);
        public static PracticeQuestion[] Questions(string focus,int version=1)
        {
            if(version==0)return LegacyQuestions(focus);
            switch(focus)
            {
                case "rewriting":return new[]{
                    Q("Rewrite the cube root of x as a power.",1,"The cube root is x^(1/3). The root index becomes the denominator of the exponent.","x^3","x^(1/3)","x^(-3)"),
                    Q("Rewrite 1/x^4 as a power.",2,"A reciprocal changes the exponent's sign: 1/x^4 = x^(-4), for x != 0.","x^(1/4)","x^4","x^(-4)"),
                    Q("Rewrite 2/sqrt(x) as a power, for x > 0.",0,"Keep the coefficient 2. The reciprocal square root contributes exponent -1/2.","2x^(-1/2)","2x^(1/2)","x^(-2)")};
                case "constants":return new[]{
                    Q("Differentiate f(x) = 9.",2,"A constant has no change as x varies, so its derivative is 0.","9","9x","0"),
                    Q("Differentiate f(x) = -7x.",0,"Multiply -7 by 1, then use x^0 = 1. The derivative is -7.","-7","-7x","0"),
                    Q("Differentiate f(x) = 4x + 9.",1,"The linear term contributes 4 and the constant contributes 0.","4x","4","13")};
                case "integration":return new[]{
                    Q("Choose an antiderivative of 10x^4.",1,"Raise 4 to 5, then divide 10 by 5: 2x^5 + C. Its derivative is 10x^4.","10x^5 + C","2x^5 + C","40x^3 + C"),
                    Q("Integrate x^(2/3), for x > 0.",0,"The new exponent is 5/3. Dividing by 5/3 gives coefficient 3/5.","(3/5)x^(5/3) + C","(5/3)x^(5/3) + C","(2/3)x^(-1/3) + C"),
                    Q("Integrate -12x^5.",2,"Raise 5 to 6 and divide -12 by 6, preserving the negative sign.","-12x^6 + C","-60x^4 + C","-2x^6 + C")};
                case "constant_c":return new[]{
                    Q("Which is the general indefinite integral of 8x?",2,"The derivative of 4x^2 + C is 8x for any constant C. The general answer needs +C.","4x^2","8x^2 + C","4x^2 + C"),
                    Q("Integrate 4x - 5. How should the constant appear?",0,"Integrate each term, then use one arbitrary constant: 2x^2 - 5x + C.","2x^2 - 5x + C","2x^2 - 5x","4x - 5 + C"),
                    Q("Evaluate the definite integral of 6x from 1 to 2.",1,"With F(x) = 3x^2, F(2) - F(1) = 12 - 3 = 9. Constants cancel.","9 + C","9","12")};
                case "sequence":return new[]{
                    Q("To integrate the fourth root of x, for x > 0, what comes first?",0,"Rewrite the root as x^(1/4), then apply the Power Rule.","Rewrite as x^(1/4)","Subtract 1 from the exponent","Drop the root symbol"),
                    Q("After rewriting x^(1/4), what is the raised exponent?",2,"Add one whole unit: 1/4 + 4/4 = 5/4.","-3/4","1/5","5/4"),
                    Q("After raising to 5/4, which finishes the integration?",1,"Divide by the new exponent, then add C: (4/5)x^(5/4) + C.","Multiply by 5/4 and stop","Divide by 5/4, then add C","Subtract 1, then add C")};
                case "bounds":return new[]{
                    Q("Evaluate the integral of 6x from 1 to 3.",1,"Use F(x) = 3x^2. F(3) - F(1) = 27 - 3 = 24.","27","24","30"),
                    Q("Evaluate the integral of 4x^3 from 0 to 2.",2,"Use F(x) = x^4. F(2) - F(0) = 16 - 0 = 16.","32","8","16"),
                    Q("Evaluate the integral of 4 from 2 to 5.",0,"Use F(x) = 4x. Subtract the lower-bound value: 20 - 8 = 12.","12","20","28")};
                case "limits_nearby":return new[]{
                    Q("Find the limit of 2x + 3 as x approaches 4.",1,"A polynomial is continuous. Substituting gives 2(4) + 3 = 11.","8","11","7"),
                    Q("Find the limit of 5x^2 - 2 as x approaches 1.",2,"Substitute the approach point: 5(1^2) - 2 = 3.","5","7","3"),
                    Q("Near x = 2, f(1.99) = 7.98 and f(2.01) = 8.02. Which limit is suggested?",0,"Both sides suggest an approached value of 8. Nearby outputs need not equal the limit exactly.","8","2","Does not exist")};
                case "limits_points":return new[]{
                    Q("For x != 3, g(x) = 2x - 1; g(3) = 10. Choose the limit, then g(3).",2,"Nearby values approach 2(3) - 1 = 5. The assigned point value is 10.","10; 5","10; 10","5; 10"),
                    Q("A curve approaches (2,9) from both sides, but g(2) = 4. Find the limit at x = 2.",0,"Read the limit from the approaching curve: 9. The filled point gives g(2) = 4.","9","4","Does not exist"),
                    Q("h(x) = x^2 + 1 for x != 1; h(1) = -3. Find h(1).",1,"The function value is explicitly assigned as -3. The nearby limit is a different value, 2.","2","-3","Does not exist")};
                case "limits_sides":return new[]{
                    Q("Both one-sided limits at x = 5 equal 4. Find the two-sided limit.",1,"Both sides agree, so the two-sided limit is 4.","5","4","Does not exist"),
                    Q("At x = 0: left limit -2, right limit 6. Find the two-sided limit.",2,"The two sides disagree, so there is no two-sided limit.","-2","6","Does not exist"),
                    Q("At x = 2: h(x) = x + 3 on the left, 7 - x on the right. Find the limit.",0,"The left approaches 2 + 3 = 5, and the right approaches 7 - 2 = 5. Their common limit is 5.","5","2","Does not exist")};
                default:return new[]{
                    Q("Differentiate f(x) = 3x^(-2), for x != 0.",2,"Multiply 3 by -2 to get -6. Subtract one from -2 to get -3.","-6x^(-1)","3x^(-3)","-6x^(-3)"),
                    Q("Differentiate f(x) = x^(1/3), for x > 0.",0,"Multiply by 1/3, then subtract one: 1/3 - 1 = -2/3.","(1/3)x^(-2/3)","(1/3)x^(4/3)","3x^(-2/3)"),
                    Q("Differentiate f(x) = 2x^4 - 5x.",1,"Differentiate term by term: 2(4)x^3 - 5 = 8x^3 - 5.","8x^3 - 5x","8x^3 - 5","2x^3 - 5")};
            }
        }
        public static string History(Chapter4Session s)
        {
            if(s.practice==null||s.practice.Count==0)return "";
            return "\n\nFOCUSED PRACTICE / separate from mission score\n"+string.Join("\n\n",s.practice.Select((r,i)=>"Set "+(i+1)+" / "+Title(r.focus)+" / "+(r.Complete?r.Result:r.answers.Count+"/3 submitted (unfinished)")+"\n"+string.Join("\n",r.answers.Select(a=>(a.question+1)+". "+a.prompt+"\nYour answer: "+a.response+" / "+(a.correct?"CORRECT":"REVIEW")+"\n"+a.explanation))));
        }
    }
}
