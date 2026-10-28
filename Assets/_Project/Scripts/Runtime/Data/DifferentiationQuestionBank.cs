using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CalculusLabEscape
{
    [Serializable] public sealed class DifferentiationQuestion
    {
        public string id,title;
        public int afterStage,sourcePage;
        [TextArea(2,5)] public string prompt,rewrite,explanation;
        [TextArea(2,5)] public string basicHint,ruleReminder,demonstration;
        public bool choiceAnswer;
        public string coefficient,exponent;
        public string[] options,optionFeedback;
        public int correctOption;
    }
    [CreateAssetMenu(menuName="Calculus Lab/Differentiation question bank")]
    public sealed class DifferentiationQuestionBank:ScriptableObject
    {
        public string version="pdf-power-rule-1";
        public DifferentiationQuestion[] questions;
        public static DifferentiationQuestionBank Load()=>Resources.Load<DifferentiationQuestionBank>("Production/DifferentiationQuestions");
        public DifferentiationQuestion Next(int stage,ICollection<string> completed)=>questions.FirstOrDefault(q=>q.afterStage==stage&&!completed.Contains(q.id));
    }
}
