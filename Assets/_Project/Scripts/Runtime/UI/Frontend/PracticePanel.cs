using System;
using System.Linq;

namespace CalculusLabEscape
{
    // One presenter for the mission results and the persisted record entry points.
    public sealed class PracticePanel
    {
        readonly Action<string,string> Text;
        readonly Action<string,bool> Show,Enabled;
        readonly Action onClose;
        readonly string closeLabel;
        Chapter4Session session;
        PracticeRun practice;
        int practiceChoice=-1;
        bool dirty,reviewOnOpen;
        public PracticePanel(Action<string,string> text,Action<string,bool> show,Action<string,bool> enabled,Action close,string closeLabel)
        {
            Text=text;Show=show;Enabled=enabled;onClose=close;this.closeLabel=closeLabel;
        }
        public void Choose(int index){practiceChoice=index;RefreshPractice();}
        public void Submit(){if(practice.Submit(practiceChoice)){SceneSoundscape.Active?.Play(practice.answers.Last().correct?"correct":"retry");Save();}RefreshPractice();}
        public void Next(){practice.Next();practiceChoice=-1;Save();RefreshPractice();}
        public void Repeat()=>Open(session,true);
        void Save(){dirty=true;Chapter4Store.Save(session);dirty=!string.IsNullOrEmpty(Chapter4Store.LastError);}
        public void Close()
        {
            if(dirty){Save();if(dirty){RefreshPractice();return;}}
            onClose();
        }
        public void Open(Chapter4Session saved,bool repeat=false)
        {
            session=saved;
            if(session.practice==null)session.practice=new System.Collections.Generic.List<PracticeRun>();
            practice=session.practice.LastOrDefault();
            if(practice==null||repeat){practice=new PracticeRun{focus=MissionPractice.Focus(session),questionSetVersion=1};session.practice.Add(practice);Save();}
            reviewOnOpen=practice.Complete;
            practiceChoice=-1;Show("PracticeModal",true);Text("PracticeCloseLabel",closeLabel);RefreshPractice();
        }
        void RefreshPractice()
        {
            bool done=reviewOnOpen||practice.position>=3,answered=practice.answers.Count>practice.position;
            Text("PracticeTitle","Focused practice / "+MissionPractice.Title(practice.focus));
            Text("PracticeProgress",done?"Three-question review complete. Mission score unchanged.":"Question "+(practice.position+1)+" / 3  |  Choose an answer, then check it. Mission score unchanged.");
            Show("PracticeQuestion",!done);Show("PracticeResults",done);
            for(int i=0;i<3;i++)Show("PracticeOption"+i,!done);
            Show("PracticeSubmit",!done&&!answered);Show("PracticeNext",!done&&answered);Show("PracticeRepeat",done);
            Text("PracticeFeedback",done?"You can repeat this same set for guided review. Each run is saved separately.":answered?(practice.answers.Last().correct?"CORRECT / ":"REVIEW / ")+practice.answers.Last().explanation:"Each question is scored once. The explanation appears after you submit.");
            Text("PracticeSaveStatus",dirty?"Could not save practice. Your answers are still here; use Back to retry.":"");
            if(done){Text("PracticeResults",practice.Result+"\n\n"+string.Join("\n\n",practice.answers.Select(a=>(a.correct?"[OK] ":"[REVIEW] ")+a.prompt+"\n"+a.explanation)));return;}
            var question=MissionPractice.Questions(practice.focus,practice.questionSetVersion)[practice.position];Text("PracticeQuestion",question.prompt);
            for(int i=0;i<3;i++){Text("PracticeOption"+i+"Label",(answered?(i==question.correct?"[ANSWER] ":i==practice.answers.Last().choice?"[YOUR CHOICE] ":""):practiceChoice==i?"> ":"")+question.options[i]);Enabled("PracticeOption"+i,!answered);}
            Enabled("PracticeSubmit",practiceChoice>=0);Text("PracticeNextLabel",practice.position==2?"View practice review":"Next question");
        }
    }
}
