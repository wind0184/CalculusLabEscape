using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CalculusLabEscape
{
    [Serializable] public sealed class LearningAttempt
    {
        public string step, response, error, consequence, timestamp, feedback;
        public bool correct;
        public float elapsed;
        public int number;
        public int assessmentVersion;
        public string category;
        public bool assisted;
        public string[] hintTypes;
    }
    [Serializable] public sealed class LearningSupport
    {
        public string step, type, timestamp;
        public int cost;
        public float elapsed;
    }
    [Serializable] public sealed partial class Chapter4Session
    {
        public string id = Guid.NewGuid().ToString("N"), profile, started = DateTime.UtcNow.ToString("o"), status = "InProgress";
        public int topic, required, heartsUsed;
        public int bridgeCurriculumVersion;
        public int limitsCurriculumVersion,integrationCurriculumVersion;
        public float elapsed;
        public bool safetyMode;
        public long ownerId;
        public int revision;
        public float environmentElapsed,waterHeight=-.6f;
        public bool pressureHeld;
        public List<string> completed = new List<string>();
        public List<LearningAttempt> attempts = new List<LearningAttempt>();
        public List<LearningSupport> support = new List<LearningSupport>();
        public List<PracticeRun> practice = new List<PracticeRun>();
        public List<string> guideSeen = new List<string>();
        public bool guideSkipped,guideMoved;
        public float Accuracy => MathematicalAttemptCount == 0 ? -1 : 100f * MathematicalCorrectCount / MathematicalAttemptCount;
        public float Completion => required == 0 ? 0 : 100f * completed.Count / required;
        public string CommonError => attempts.Where(a => !a.correct&&!IsInvalidInput(a)).GroupBy(a => a.error).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault() ?? "NONE";
        public const int MinimumIndependentCompletions = 3;
        public string Band
        {
            get
            {
                if(Accuracy<0)return "NOT YET ASSESSED";
                if(IndependentAccuracy<0)return AssistedCompletionCount>0?"ASSISTED PRACTICE":"GUIDED PRACTICE";
                if(IndependentCompletionCount<MinimumIndependentCompletions)
                    return IndependentCompletionCount>0?"INDEPENDENT CHECK COMPLETED":"INDEPENDENT PRACTICE IN PROGRESS";
                return IndependentAccuracy>=80?"STRONG INDEPENDENT UNDERSTANDING":IndependentAccuracy>=60?"DEVELOPING INDEPENDENT UNDERSTANDING":"INDEPENDENT PRACTICE RECOMMENDED";
            }
        }
        public bool Submit(string step, string response, bool correct, string error)
        {
            if (completed.Contains(step) || status != "InProgress") return false;
            var hints=SupportForStep(step).Select(s=>s.type).Where(t=>!string.IsNullOrEmpty(t)).Distinct().ToArray();
            bool invalid=!correct&&error=="INPUT_FORMAT",helped=hints.Length>0;
            attempts.Add(new LearningAttempt {step = step, response = response, correct = correct, error = correct ? "" : error,
                consequence = invalid?"INPUT_VALIDATION":correct ? "RESTORE" : topic == 0 ? "PORTAL_GLITCH" : topic == 1 ? "RIVER_RISE" : "MODULE_WARNING",
                timestamp = DateTime.UtcNow.ToString("o"), elapsed = elapsed, number = attempts.Count + 1,
                assessmentVersion=1,assisted=helped,hintTypes=hints,
                category=invalid?"InvalidInput":!correct?"Incorrect":helped?"AssistedCompletion":IsIndependentQuestion(step)?"IndependentCompletion":"GuidedCompletion"});
            if (correct) completed.Add(step);
            return correct;
        }
        public bool Hint(string step, bool strong)
        {
            if (strong && heartsUsed >= 3) return false;
            if (strong) heartsUsed++;
            support.Add(new LearningSupport {step = step, cost = strong ? 1 : 0, type = strong ? "RuleReminder" : "BasicHint", elapsed = elapsed, timestamp = DateTime.UtcNow.ToString("o")});
            return true;
        }
        public string Summary => Band + "\nMath Accuracy  " + Percent(Accuracy) + "     |     Independent Accuracy  " + Percent(IndependentAccuracy)
            + "\nCompletion  " + Completion.ToString("0") + "%     |     Guided  "+GuidedCompletionCount+"     |     Independent  "+IndependentCompletionCount+"     |     Assisted  "+AssistedCompletionCount
            + "\nIncorrect  "+MathematicalIncorrectCount+"     |     Invalid Input  "+InvalidInputCount+"     |     Support Hearts used  "+heartsUsed+"/3"
            + "\nActive time  " + TimeSpan.FromSeconds(elapsed).ToString(@"mm\:ss")
            + (topic == 2 ? "     |     Safety Mode  " + (safetyMode ? "Yes" : "No") : "") + "\nCommon math error  " + MissionFeedback.ErrorLabel(CommonError);
    }
    public static class Chapter4Store
    {
        public static string Profile = "Trainee";
        public static bool TestMode;
        public static string LastError = "";
        // Test runs exercise actual disk persistence in their own folder, never learner records.
        public static string IsolatedFolder;
        public static bool IsolatedRun => !string.IsNullOrEmpty(IsolatedFolder);
        public static string Folder => IsolatedFolder ?? (!LearningApi.LocalPlay&&LearningApi.Account!=null?LearningApi.CacheFolder:Path.Combine(Application.persistentDataPath, "Chapter4Sessions"));
        public static void Save(Chapter4Session session)
        {
            if (TestMode) return;
            if(!LearningApi.LocalPlay)
            {
                if(LearningApi.Account==null||LearningApi.Account.role!="Student"||session.topic!=0){LastError="Sign in as a student before saving a Limits session.";return;}
                session.ownerId=LearningApi.Account.id;session.profile=LearningApi.Account.username;session.revision++;
            }
            WriteCache(session);
            if(string.IsNullOrEmpty(LastError)&&!LearningApi.LocalPlay)LearningApi.Instance.Enqueue(session);
        }
        public static void WriteCache(Chapter4Session session)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                string target = Path.Combine(Folder, session.id + ".json"), temp = target + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(session, true));
                if (File.Exists(target)) File.Replace(temp, target, null); else File.Move(temp, target);
                LastError = "";
            }
            catch (Exception e) { LastError = "Could not save this session: " + e.Message; Debug.LogWarning(LastError); }
        }
        public static List<Chapter4Session> Read(bool allProfiles = false)
        {
            var rows = new List<Chapter4Session>();
            if (!Directory.Exists(Folder)) return rows;
            foreach (string file in Directory.GetFiles(Folder, "*.json"))
                try { var s = JsonUtility.FromJson<Chapter4Session>(File.ReadAllText(file)); if (s != null && (LearningApi.LocalPlay?(s.ownerId==0 && (allProfiles || s.profile == Profile)):(LearningApi.Account!=null&&LearningApi.Account.role=="Student"&&s.ownerId==LearningApi.Account.id))) rows.Add(s); }
                catch (Exception e) { Debug.LogWarning("Unreadable session: " + e.Message); }
            return rows.OrderByDescending(s => s.started).ToList();
        }
    }
}

