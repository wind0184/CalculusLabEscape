using System;
using System.Linq;
using UnityEngine;

namespace CalculusLabEscape
{
    public enum LimitsAnswerKind { Number, GraphMarker, ThreeValues, LimitAndValue, Conclusion, Choice }

    [Serializable]
    public sealed class LimitsQuestion
    {
        public string id, title;
        [TextArea(2, 6)] public string prompt;
        [TextArea(1,4)] public string display;
        public string approachPoint;
        public LimitsAnswerKind kind;
        public string[] options;
        public int correctOption;
        public float[] expected;
        public string errorCode;
        [TextArea(2, 5)] public string explanation, basicHint, ruleReminder, demonstration;
    }

    [CreateAssetMenu(menuName = "Calculus Lab/Limits question bank")]
    public sealed class LimitsQuestionBank : ScriptableObject
    {
        public LimitsQuestion[] questions;
    }

    public sealed class LimitsAnswer
    {
        public string[] values = { "", "", "" };
        public int marker = 1, conclusion = -1;
        public int[] reason = { -1, -1, -1 };
    }

    public readonly struct LimitsEvaluation
    {
        public readonly bool complete, correct;
        public readonly string response;
        public LimitsEvaluation(bool complete, bool correct, string response)
        { this.complete = complete; this.correct = correct; this.response = response; }
    }

    // Independent of the view: Canvas, automated tests and future input methods share these rules.
    public static class LimitsRules
    {
        public static LimitsEvaluation Evaluate(LimitsQuestion q, LimitsAnswer a)
        {
            if(q.kind==LimitsAnswerKind.Choice)return new LimitsEvaluation(a.conclusion>=0&&a.conclusion<3,a.conclusion==q.correctOption,"choice="+a.conclusion+" | "+(a.conclusion>=0&&a.conclusion<3?q.options[a.conclusion]:""));
            if (q.kind == LimitsAnswerKind.GraphMarker)
                return new LimitsEvaluation(true, a.marker == q.expected[0], "marker=" + a.marker);
            if (q.kind == LimitsAnswerKind.Conclusion)
                return new LimitsEvaluation(a.conclusion >= 0 && a.reason.All(n => n >= 0),
                    a.conclusion == 2 && a.reason.SequenceEqual(new[] { 0, 1, 2 }),
                    "conclusion=" + a.conclusion + ";reason=" + string.Join(",", a.reason));
            int count = q.expected.Length;
            bool complete = a.values.Take(count).All(v => !string.IsNullOrWhiteSpace(v));
            bool correct = complete && Enumerable.Range(0, count).All(i => Chapter4Game.NumericAnswer(a.values[i], q.expected[i]));
            return new LimitsEvaluation(complete, correct, string.Join(" | ", a.values.Take(count)));
        }
    }
}
