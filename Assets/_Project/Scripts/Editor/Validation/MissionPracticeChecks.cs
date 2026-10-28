#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
namespace CalculusLabEscape.Editor
{
    public static class MissionPracticeChecks
    {
        static void Check(bool condition,string message){if(!condition)throw new Exception("Focused practice: "+message);}
        public static void Validate()
        {
            var s=new Chapter4Session{topic=1,required=11,status="Completed"};
            Check(MissionPractice.Focus(s)=="derivatives","No-error differentiation gets general practice");
            s.topic=2;Check(MissionPractice.Focus(s)=="integration","No-error integration gets general practice");
            s.attempts.Add(new LearningAttempt{error="MISSING_C"});Check(MissionPractice.Focus(s)=="constant_c","Select practice from recorded error");
            s.attempts.Add(new LearningAttempt{error="DEFINITE_ACCUMULATION"});s.attempts.Add(new LearningAttempt{error="DEFINITE_ACCUMULATION"});
            Check(MissionPractice.Focus(s)=="bounds","Most frequent error takes priority");
            string original=s.Summary;
            var run=new PracticeRun{focus="derivatives"};s.practice.Add(run);
            run.Next();Check(run.position==0&&!run.Submit(-1)&&run.answers.Count==0,"Empty selection and premature next do not advance");
            Check(run.Submit(0)&&!run.answers[0].correct&&!run.Submit(2),"First response is final even when wrong");run.Next();
            Check(run.Submit(0)&&run.answers[1].correct,"Second fractional-power answer correct");run.Next();
            Check(run.Submit(1)&&run.Complete&&run.Result=="2 / 3 correct on first submission","Mixed-term derivative and practice score");run.Next();
            Check(!run.Submit(1)&&run.position==3&&s.Summary==original,"Practice never changes mission metrics");
            var loaded=JsonUtility.FromJson<Chapter4Session>(JsonUtility.ToJson(s));
            Check(loaded.practice[0].position==3&&loaded.practice[0].answers[0].response=="-6x^(-1)"&&MissionPractice.History(loaded).Contains("2 / 3 correct"),"Practice evidence survives disk format round trip");
            Check(MissionPractice.History(JsonUtility.FromJson<Chapter4Session>("{\"topic\":1}")).Length==0,"Old sessions need no migration");
            s.profile="Local player";
            Check(MissionPractice.CanOpenRecord(s,"Local player")&&!MissionPractice.CanOpenRecord(s,"Someone else"),"Practice record is owned by the current profile");
            Check(MissionPractice.RecordAction(s)=="View practice review","Completed practice opens review");
            s.practice.Add(new PracticeRun{focus="bounds"});s.practice.Last().Submit(0);
            var partial=JsonUtility.FromJson<Chapter4Session>(JsonUtility.ToJson(s));
            Check(MissionPractice.RecordAction(partial)=="Continue practice / 1 of 3 submitted"&&!partial.practice.Last().Submit(1),"Restarted partial practice preserves submitted-answer lock");
            partial.practice.Last().Next();Check(partial.practice.Last().position==1,"Restarted practice advances to the next question");
            s.ownerId=4;Check(!MissionPractice.CanOpenRecord(s,"Local player"),"Authenticated records remain excluded");s.ownerId=0;
            s.status="InProgress";Check(!MissionPractice.CanOpenRecord(s,"Local player"),"Unfinished mission cannot open completion practice");s.status="Completed";
            s.topic=0;Check(MissionPractice.CanOpenRecord(s,"Local player"),"Limits can open its own focused practice from a completed learning record");
            s.topic=1;s.practice=null;Check(MissionPractice.RecordAction(s)=="Start focused practice","Old completed record can start its first set");
            foreach(string focus in MissionPractice.Focuses)
            {
                var questions=MissionPractice.Questions(focus);Check(questions.Length==3,"Each focus has three questions");
                foreach(var q in questions)Check(q.options.Length==3&&q.options.Distinct().Count()==3&&q.correct>=0&&q.correct<3&&!string.IsNullOrEmpty(q.explanation),"Question is complete and unambiguous in structure: "+q.prompt);
            }
            Debug.Log("MISSION_PRACTICE_CHECKS_PASS / targeting, submission locks, progression, independent score, old saves, practice evidence");
        }
    }
}
#endif
