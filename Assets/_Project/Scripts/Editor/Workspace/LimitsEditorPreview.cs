#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape.Editor
{
    // Static sample presentation only: never initialize a Chapter4Game or save a run.
    public static class LimitsEditorPreview
    {
        public static readonly string[] Views={"Brief","Numerical approach","Graph marker","Agreeing sides","Limit and point value","Unequal sides","Independent numerical","Independent check","Check accepted","Portal ready"};
        public static void Apply(Chapter4World world,Canvas canvas,string view)
        {
            var nodes=canvas.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            void Show(string name,bool value){if(nodes.TryGetValue(name,out var t))t.gameObject.SetActive(value);}
            void Text(string name,string value){if(nodes.TryGetValue(name,out var t)&&t.TryGetComponent<TMP_Text>(out var label))MathText.Set(label,value);}
            bool extra=view=="Independent numerical"||view=="Independent check"||view=="Check accepted",accepted=view=="Check accepted",ready=view=="Portal ready";
            int stage=view=="Graph marker"?1:view=="Agreeing sides"?2:view=="Limit and point value"||view=="Independent check"||accepted?3:view=="Unequal sides"?4:0;
            int charge=ready?5:stage;
            Show("Backdrop",view!="World");Show("QuestionBody",!ready);Show("ReadyBody",ready);Show("QuestionJourney",!ready);Show("PortalDescription",false);
            Text("LiveStatus","");Text("SaveStatus","");Text("ChargeLabel",charge+" / 5");
            Text("WorldProgress","OBSERVATORY CALIBRATION   "+charge+" / 5 RESTORED");Text("WorldPrompt","WASD move / Mouse look / Walk towards the calibration deck");
            foreach(var diagram in canvas.GetComponentsInChildren<LimitsDiagram>(true))if(diagram.portal){diagram.charge=charge;diagram.SetVerticesDirty();}
            if(view!="Brief"&&view!="World"&&!ready)
            {
                var bank=extra?Resources.Load<LimitsQuestionBank>("Production/LimitsIndependentQuestions"):canvas.GetComponent<ProductionCanvas>().questionBank;
                var q=bank.questions[stage];bool choice=q.kind==LimitsAnswerKind.Choice,conclusion=q.kind==LimitsAnswerKind.Conclusion;
                Text("QuestionNumber","CALIBRATION "+(stage+1)+" / 5 / "+(extra?"INDEPENDENT CHECK":"GUIDED TASK"));Text("QuestionTitle",q.title);Text("QuestionPrompt",q.prompt);
                Show("IndependentGiven",extra&&q.kind==LimitsAnswerKind.Number);Text("IndependentGiven",q.display??"");
                Show("NumberTable",!extra&&q.kind==LimitsAnswerKind.Number);Show("Piecewise",!extra&&q.kind==LimitsAnswerKind.ThreeValues);
                Show("GraphPanel",!extra&&(q.kind==LimitsAnswerKind.GraphMarker||q.kind==LimitsAnswerKind.LimitAndValue));Show("GraphSliders",!extra&&q.kind==LimitsAnswerKind.GraphMarker);
                Show("NumberInputs",!choice&&!conclusion&&q.kind!=LimitsAnswerKind.GraphMarker);Show("ReasonPanel",choice||conclusion);
                string[] names=q.kind==LimitsAnswerKind.ThreeValues?new[]{"Left-hand limit","Right-hand limit","Two-sided limit"}:q.kind==LimitsAnswerKind.LimitAndValue?new[]{"Limit as x approaches 2","Function value f(2)",""}:new[]{"Approached value","",""};
                for(int i=0;i<3;i++)
                {
                    Show("AnswerGroup"+i,i<q.expected.Length);Text("AnswerLabel"+i,names[i]);nodes["Answer"+i].GetComponent<TMP_InputField>().SetTextWithoutNotify("");
                    Show("ReasonCard"+i,!choice);Show("ReasonSlot"+i,!choice);
                    bool selected=accepted&&q.correctOption==i;
                    Text("Conclusion"+i+"Label",(selected?"[X] ":"[ ] ")+(choice?q.options[i]:new[]{"Limit = 2","Limit = 5","Does not exist"}[i]));
                    QuestionJourney.Choice(nodes["Conclusion"+i].GetComponent<Button>(),selected,accepted);
                    Text("ReasonCard"+i+"Label",new[]{"Left-hand limit","is not equal to","Right-hand limit"}[i]);Text("ReasonSlot"+i+"Label","Place card "+(i+1));
                }
                Show("ReasonHint",!choice);Show("ReasonPrompt",!choice);
                nodes["Probe"].GetComponent<Slider>().SetValueWithoutNotify(1);nodes["Marker"].GetComponent<Slider>().SetValueWithoutNotify(1);
                Text("ProbeLabel","Probe x = 1.00");Text("MarkerLabel","Answer marker y = 1");
                var plot=nodes["Graph"].GetComponent<LimitsDiagram>();plot.probe=1;plot.marker=1;plot.filledPoint=q.kind==LimitsAnswerKind.LimitAndValue;plot.showProbe=!plot.filledPoint;plot.SetVerticesDirty();
                Text("Feedback",accepted?"CORRECT / "+q.explanation:"");nodes["Feedback"].GetComponent<TMP_Text>().color=new Color(.35f,.92f,.59f);
                Show("Submit",!accepted);Show("Continue",accepted);Text("ContinueLabel",extra?"Apply calibration":"Independent check");
                nodes["AnswerControls"].GetComponent<CanvasGroup>().interactable=!accepted;nodes["Scratchpad"].GetComponent<TMP_InputField>().SetTextWithoutNotify("");
                string note=accepted?"Both answers accepted. Apply calibration to restore one gate segment.":extra?"Guided task accepted. Complete the independent check to unlock calibration.":"Complete both questions, then apply this calibration to restore the gate.";
                nodes["QuestionJourney"].GetComponent<QuestionJourney>().Present("CALIBRATION "+(stage+1)+" / 5",note,new[]{"01  GUIDED TASK","02  INDEPENDENT CHECK","03  RESTORE GATE SEGMENT"},accepted?2:extra?1:0);
            }
            Canvas.ForceUpdateCanvases();
            foreach(var component in canvas.GetComponentsInChildren<Component>(true))if(component!=null&&PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            foreach(var t in nodes.Values)if(PrefabUtility.IsPartOfPrefabInstance(t.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
        }
    }
}
#endif
