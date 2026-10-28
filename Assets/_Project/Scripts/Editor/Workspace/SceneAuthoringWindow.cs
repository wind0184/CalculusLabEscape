#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape.Editor
{
    public sealed partial class SceneAuthoringWindow
    {
        static void PreviewIndependentCheck(Chapter4World world,Dictionary<string,Transform> nodes,string view)
        {
            if(view!="Independent check"&&view!="Check accepted")return;
            if(!nodes.TryGetValue("QuestionJourney",out var journey))return;
            bool bridge=world.topic==1,accepted=view=="Check accepted";
            void Show(string key,bool show){if(nodes.TryGetValue(key,out var t))t.gameObject.SetActive(show);}
            void Text(string key,string text){if(nodes.TryGetValue(key,out var t)&&t.TryGetComponent<TMP_Text>(out var label))MathText.Set(label,text);}
            Show("QuestionJourney",true);Show("BridgeOptions",bridge);Show("CaseCalibration",!bridge);
            Show("RepairSubmit",bridge&&!accepted);Show("RepairContinue",bridge&&accepted);Show("CalibrationSubmit",!bridge&&!accepted);Show("CalibrationNext",!bridge&&accepted);
            string title,prompt,explanation;string[] options;int correct;
            if(bridge)
            {
                var q=DifferentiationQuestionBank.Load().questions.First(q=>q.id=="DX_APPLY_2");title=q.title;prompt=q.prompt;options=q.options;correct=q.correctOption;explanation=q.explanation;
                Text("MissionProgress","Bridge repair: 2/3");
                Text("SecondaryProgress","Accepted steps: "+(accepted?14:13)+"/18");
                Text("Component0","[OK] Power regulator");Text("Component1","[OK] Bridge component");Text("Component2","[  ] Bridge anchor");
                world.Restore(2);world.SetBridgeStage(7);
            }
            else
            {
                var q=IntegrationQuestionBank.Load().questions[0];title=q.title;prompt=q.prompt;options=q.options;correct=q.correctOption;explanation=q.explanation;world.FocusCase(0,true);
                Text("SecondaryProgress","Accepted steps: "+(accepted?2:1)+"/18");
            }
            Text("TaskTitle",title);Text("TaskPrompt",prompt);Text("Feedback",accepted?"CORRECT / "+explanation:"");
            nodes["Feedback"].GetComponent<TMP_Text>().color=new Color(.35f,.92f,.59f);
            for(int i=0;i<3;i++){string key=(bridge?"BridgeChoice":"CalibrationOption")+i;Text(key+"Label",(accepted&&i==correct?"[X] ":"[ ] ")+options[i]);QuestionJourney.Choice(nodes[key].GetComponent<Button>(),accepted&&i==correct,accepted);}
            journey.GetComponent<QuestionJourney>().Present("INDEPENDENT CHECK 1 / 2   |   "+(bridge?"POWER RULE":"ANTIDERIVATIVES"),accepted?"Check accepted. Continue to the next calibration.":"Select an expression, then check your answer.",new[]{"01  GUIDED TASK","02  CHECKS "+(accepted?1:0)+" / 2",bridge?"03  INSTALL REPAIR":"03  MODULE STABLE"},1);
            Text("RepairContinueLabel","Next calibration");Text("CalibrationNextLabel","Next calibration");Text("NextAction","NEXT ACTION\n"+(accepted?"Check accepted. Click Next calibration.":"Select an expression, then check your answer."));
        }
        public static void ValidateQuestionPreviews()
        {
            string folder=Path.GetFullPath("../../qa/question-detail-polish/editor");Directory.CreateDirectory(folder);
            foreach(int topic in new[]{1,2})
            {
                var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity");
                var world=UnityEngine.Object.FindFirstObjectByType<Chapter4World>();var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
                foreach(string view in new[]{"Independent check","Check accepted","Power Core","Case overview"})
                {
                    if(topic==1&&view=="Case overview"||topic==2&&view=="Power Core")continue;
                    ApplyView(world,canvas,view);Canvas.ForceUpdateCanvases();
                    foreach(var label in canvas.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();if(label.isTextOverflowing)throw new Exception("Question preview overflow: "+view+" / "+label.name);}
                    bool active=canvas.GetComponentsInChildren<QuestionJourney>(true).Single().gameObject.activeSelf;
                    if(active!=(view=="Independent check"||view=="Check accepted"))throw new Exception("Stale question preview.");
                    SceneAuthoring.Capture(world,canvas,Path.Combine(folder,topic+"-"+view.Replace(" ","-")+".png"),false);
                }
                if(UnityEngine.Object.FindFirstObjectByType<Chapter4Game>()?.Session!=null)throw new Exception("Preview started gameplay.");
                EditorSceneManager.OpenScene(scene.path);
            }
            File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: independent and accepted previews render without Play; text fits; leaving preview hides calibration panels; no gameplay/session created; scenes reopened without saving.");
            Debug.Log("QUESTION_EDITOR_PREVIEW_PASS");
        }
    }
}
#endif
