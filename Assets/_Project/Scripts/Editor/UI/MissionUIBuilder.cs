#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace CalculusLabEscape.Editor
{
    public static partial class ProductionUIBuilder
    {
        static void MakeMission(int topic)
        {
            string name=topic==1?"DifferentiationUI":"IntegrationUI",title=topic==1?"The Broken Bridge Rescue":"The Integration Containment Case";
            var root=Root(name,false,null,out var pages,out var frame);Object.DestroyImmediate(root.GetComponent<ProductionCanvas>());root.AddComponent<MissionCanvas>().topic=topic;
            var brief=Page(pages,"Brief");Text(brief,"BriefKicker","MISSION 0"+(topic+1)+" / "+(topic==1?"DIFFERENTIATION":"INTEGRATION"),50,117,1150,30,17,Accent,true);
            Text(brief,"BriefTitle",title,50,174,1166,63,39,Ink,true);
            var card=Panel(brief,"BriefCard",51,275,1175,278);
            Text(card.transform,"BriefObjective",topic==1?"Restore the bridge and reunite the calf with its mother.":"Complete all four modules and deactivate the device.",29,27,1109,65,29,Ink,true);
            Text(card.transform,"BriefLearning",topic==1?"Repair three components using the Power Rule, roots, reciprocals and constants.\nThen assemble the final derivative to stabilise the crossing.\n\nFollow the active device marker. Each accepted repair stays in place.":"Start with Exponent Dial, then inspect the other modules in any order.\nUse the integration power rule, one common +C and definite accumulation.\n\nThe 10-minute countdown enters Safety Mode at zero. You can keep working.",30,111,1105,147,23,Muted);
            Text(brief,"BriefSupport","Basic hints are free. Stronger support uses 1 of 3 hearts. Wrong answers preserve progress.",53,576,1170,38,19,Muted);
            Button(brief,"BriefBack","Main menu",52,638,250,48);Button(brief,"BriefStart",topic==1?"Start rescue":"Inspect case",875,638,350,48,true);
            var world=Page(pages,"World",true);var hud=Panel(world,"WorldHUD",29,103,1020,105,Paper);Text(hud.transform,"WorldProgress","",19,15,979,32,25,Ink,true);Text(hud.transform,"WorldObjective","",20,61,975,32,19,Muted);
            Button(world,"WorldPause","Pause",1080,117,160,46);var prompt=Panel(world,"PromptPanel",140,612,1000,63,Paper);Text(prompt.transform,"WorldPrompt","",23,18,954,34,21).alignment=TextAlignmentOptions.Center;
            Text(world,"Crosshair","+",619,338,42,42,24,Ink).alignment=TextAlignmentOptions.Center;
            var puzzle=Page(pages,"Puzzle");var sidebar=Panel(puzzle,"DevicePanel",29,108,359,532);
            Text(sidebar.transform,"MissionProgress","",20,18,320,39,25,Accent,true);
            var preview=Rect(sidebar.transform,"ScenePreview",18,81,323,150).gameObject.AddComponent<RawImage>();preview.raycastTarget=false;
            Text(sidebar.transform,"SecondaryProgress","",21,249,316,30,19,Muted);
            for(int i=0;i<3;i++){var label=Text(sidebar.transform,"Component"+i,"",21,305+i*43,319,34,19);label.gameObject.SetActive(topic==1);}
            if(topic==2)Text(sidebar.transform,"CaseGuidance","Complete each module.\nA green STABLE state marks\naccepted work.",21,303,315,105,20,Muted);
            Text(sidebar.transform,"EnvironmentStatus","",21,475,319,46,19,Muted);
            var bridge=Rect(puzzle,"BridgeBody",0,0,1280,720);
            var answers=Rect(bridge,"BridgeAnswers",0,0,1280,720);answers.gameObject.AddComponent<CanvasGroup>();
            var power=Rect(answers,"PowerControls",0,0,1280,720);
            Text(power,"PowerExpression","d(x^4)/dx",449,273,736,51,34,Ink,true);
            var chip=Button(power,"BridgeChip","4",449,357,112,58,true);Drag(chip,0,4);
            var slot=Button(power,"CoefficientSlot","Coefficient slot",598,357,284,58);Drop(slot,0,0);
            DialControls(power,"Exponent",929,355,"New exponent",260);
            Text(power,"PowerPreview","?x^4",447,457,747,62,39,Accent,true);
            Text(power,"PowerInstructions","Drag the 4 into its slot, or click 4 and then the slot.",449,531,750,34,19,Muted);
            var inputs=Rect(answers,"BridgeInputs",0,0,1280,720);Text(inputs,"BridgeALabel","",449,331,343,32,21,Muted);Input(inputs,"BridgeA","Enter a number or fraction",449,377,336,54);
            var second=Rect(inputs,"BridgeBGroup",0,0,1280,720);Text(second,"BridgeBLabel","New exponent",845,331,349,32,21,Muted);Input(second,"BridgeB","Enter a number or fraction",845,377,350,54);
            var options=Rect(answers,"BridgeOptions",0,0,1280,720);for(int i=0;i<3;i++)Button(options,"BridgeChoice"+i,"",448,301+i*82,750,61);
            Button(bridge,"RepairSubmit","Test repair",937,648,281,45,true);Button(bridge,"RepairContinue","Place / continue",913,648,305,45,true);
            var ready=Rect(puzzle,"BridgeReady",424,169,801,416);Text(ready,"ReadyTitle","The bridge is secured.",23,75,756,70,39,Ink,true);Text(ready,"ReadyBody","Three components installed.\nAll 11 mathematical steps accepted.\n\nOpen the crossing to reunite the calf with its mother.",24,177,742,140,24,Muted);Button(ready,"Rescue","Open crossing / rescue calf",24,347,744,57,true);
            var overview=Rect(puzzle,"CaseOverview",0,0,1280,720);Text(overview,"CaseTitle","Containment case",427,122,762,51,32,Ink,true);
            for(int i=0;i<4;i++){float x=429+i%2*397,y=212+i/2*142;var m=Panel(overview,"ModuleCard"+i,x,y,377,122);Button(m.transform,"Module"+i,"",13,13,351,49);Text(m.transform,"ModuleState"+i,"",16,77,343,33,16,Muted);}
            Text(overview,"MasterStatus","",430,516,763,62,20,Muted);Button(overview,"MasterCover","Lift master switch cover",430,610,770,62,true);Button(overview,"MasterActivate","MASTER DEACTIVATION",430,610,770,62,true);
            var modules=Rect(puzzle,"CaseModule",0,0,1280,720);
            var dial=Rect(modules,"ModuleControls0",0,0,1280,720);var dialAnswers=Rect(dial,"DialAnswers",0,0,1280,720);dialAnswers.gameObject.AddComponent<CanvasGroup>();
            DialControls(dialAnswers,"DialExponent",447,327,"Raised exponent",250);DialControls(dialAnswers,"Divisor",725,327,"Divisor",250);Button(dialAnswers,"InsertC","Insert +C",1007,328,194,51);
            Text(dial,"DialPreview","",449,445,745,77,42,Accent,true);Button(dial,"DialSubmit","Test dials",946,648,269,45,true);
            var wires=Rect(modules,"ModuleControls1",0,0,1280,720);var lines=Rect(wires,"WireConnections",0,0,1280,720).gameObject.AddComponent<MissionConnections>();lines.raycastTarget=false;
            for(int i=0;i<3;i++){var source=Button(wires,"WireSource"+i,MissionRules.WireInputs[i],445,305+i*72,210,46);Drag(source,1,i);}
            for(int i=0;i<5;i++){var target=Button(wires,"WireTarget"+i,MissionRules.WireOutputs[i],990,278+i*56,228,46);Drop(target,1,i);}
            Button(wires,"CommonC","Connect one common +C",714,648,504,45,true);
            var sequence=Rect(modules,"ModuleControls2",0,0,1280,720);
            for(int i=0;i<4;i++){var source=Button(sequence,"SequenceCard"+i,MissionRules.SequenceCards[i],432+i*196,308,181,45);Drag(source,2,i);var target=Button(sequence,"SequenceSlot"+i,"Place card",432+i*196,383,181,61);Drop(target,2,i);}
            Text(sequence,"SequenceValueLabel","",434,474,776,37,22,Muted);Input(sequence,"SequenceValue","Number, fraction, or C",435,521,755,49);Button(sequence,"SequenceSubmit","Run next step",939,648,277,45,true);
            var gauge=Rect(modules,"ModuleControls3",0,0,1280,720);Text(gauge,"GaugeFormula","F(2) - F(0)",449,295,731,57,34,Accent,true);
            var pressure=Slider(gauge,"Pressure",450,397,417,0,1);pressure.interactable=false;
            Text(gauge,"GaugeState","",449,461,740,53,22,Muted);var gaugeAnswers=Rect(gauge,"GaugeAnswers",0,0,1280,720);gaugeAnswers.gameObject.AddComponent<CanvasGroup>();DialControls(gaugeAnswers,"Release",941,394,"Release amount",252);Button(gauge,"GaugeSubmit","Pull release lever",931,648,287,45,true);
            // Shared question heading is above all answer groups, with space reserved for every module.
            Text(puzzle,"TaskTitle","",428,120,790,47,29,Ink,true);Text(puzzle,"TaskPrompt","",429,191,788,73,21);
            Text(puzzle,"Feedback","",429,574,787,64,17,Accent);
            Button(puzzle,"PuzzleWorld","Return to world",30,655,229,44);Button(puzzle,"PuzzlePause","Pause",277,655,111,44);
            Button(puzzle,"SupportOpen","Hint / support",429,648,221,45);Button(puzzle,"ManualOpen","Open manual / Free",51,537,315,34);Button(puzzle,"CaseBack","Case overview",1009,80,217,30);
            var pause=Page(pages,"Pause");Text(pause,"PauseTitle","Take a moment.",234,210,850,74,43,Ink,true);Text(pause,"PauseBody","Time and environmental pressure are paused.\n\nSave & return preserves accepted work, support and progress.",236,320,860,137,24,Muted);Button(pause,"PauseResume","Resume",235,508,373,54,true);Button(pause,"PauseLeave","Save & return",657,508,373,54);
            var finishing=Page(pages,"Finishing",true);var fp=Panel(finishing,"FinishingCard",165,topic==1?550:494,950,topic==1?105:146,Paper);
            Text(fp.transform,"FinishingTitle",topic==1?"BRIDGE RESTORED":"DEVICE DEACTIVATED",27,topic==1?12:22,899,topic==1?41:51,topic==1?29:35,Accent,true);
            Text(fp.transform,"FinishingText",topic==1?"The calf is crossing to its mother.":"All four modules are stable. Recording your results...",28,topic==1?58:86,895,35,topic==1?21:23,Muted);
            if(topic==2)AddCaseShutdownUI(root);
            var summary=Page(pages,"Summary");Text(summary,"SummaryTitle",topic==1?"The crossing is safe.":"The device is safely deactivated.",55,129,1168,72,40,Ink,true);Text(summary,"SummaryMetrics","",57,237,1155,210,22);AddSkillReview(summary);Button(summary,"SummaryReplay","Practise again",54,625,344,54);Button(summary,"SummaryRecords","View learning records",464,625,345,54,true);Button(summary,"SummaryHome","Main menu",873,625,345,54);
            Header(frame,"MISSION 0"+(topic+1)+" / "+(topic==1?"DIFFERENTIATION":"INTEGRATION"));Text(frame,"LiveStatus","",32,703,900,17,12,Muted);Text(frame,"SaveStatus","",429,78,568,28,14,new Color(1f,.74f,.33f));
            var support=Panel(frame,"SupportModal",0,0,1280,720,new Color(.01f,.025f,.04f,.96f));var sc=Panel(support.transform,"SupportCard",169,122,942,483,Paper);Text(sc.transform,"SupportTitle","Choose your support",29,25,880,51,33,Ink,true);Text(sc.transform,"SupportHearts","",30,91,876,33,22,Muted);
            for(int i=0;i<3;i++)Button(sc.transform,"SupportTier"+i,new[]{"Basic hint / Free","Rule / 1 heart","Example / 1 heart"}[i],30+i*300,151,283,52,i==0);
            Text(sc.transform,"SupportText","",32,240,875,133,23);Button(sc.transform,"SupportClose","Return to device",547,405,363,48);
            var manual=Panel(frame,"ManualModal",0,0,1280,720,Paper);Text(manual.transform,"ManualTitle","Integration manual",69,131,1135,56,37,Ink,true);Text(manual.transform,"ManualText","POWER RULE  /  n != -1\nIntegral of a*x^n dx = a*x^(n+1)/(n+1) + C\n\nTERM BY TERM\nIntegrate each term separately, then add one common +C.\n\nROOTS AND RECIPROCALS\nRewrite sqrt(x) as x^(1/2) before raising the exponent.\n\nDEFINITE INTEGRALS\nFind an antiderivative F, then compute F(upper) - F(lower). No +C in the result.",71,219,1128,350,23);Button(manual.transform,"ManualClose","Return to case",855,632,348,49,true);
            var leave=Panel(frame,"LeaveModal",0,0,1280,720,new Color(.01f,.025f,.04f,.96f));var lc=Panel(leave.transform,"LeaveCard",235,215,811,292,Paper);Text(lc.transform,"LeaveTitle","Save and return to the menu?",29,28,750,58,32,Ink,true);Text(lc.transform,"LeaveBody","Accepted steps, hearts and timers are saved.\nUnsubmitted inputs return to their starting values.",30,112,747,79,23,Muted);Button(lc.transform,"LeaveCancel","Stay",30,216,347,47);Button(lc.transform,"LeaveConfirm","Save & return",436,216,347,47,true);
            var ghost=Panel(frame,"DragGhost",0,0,235,49,Accent);ghost.raycastTarget=false;ghost.rectTransform.anchorMin=ghost.rectTransform.anchorMax=new Vector2(.5f,.5f);Text(ghost.transform,"DragLabel","",10,7,215,35,23,Paper,true);
            support.gameObject.SetActive(false);manual.gameObject.SetActive(false);leave.gameObject.SetActive(false);ghost.gameObject.SetActive(false);
            AddMissionPractice(frame,summary);
            AddCompletionNavigation(root,topic);
            AddMissionRestart(root);
            AddControlsGuide(root,topic);
            AddMissionInput(root);
            AddMissionOnboarding(root);
            foreach(Transform p in pages)p.gameObject.SetActive(p.name=="Brief");Save(root,name);
        }
        static void DialControls(Transform parent,string name,float x,float y,string label,float width)
        {Text(parent,name+"Caption",label,x,y-43,width,31,19,Muted);Button(parent,name+"Minus","-",x,y,56,50);Text(parent,name+"Value","",x+70,y+4,width-140,45,30,Ink,true).alignment=TextAlignmentOptions.Center;Button(parent,name+"Plus","+",x+width-56,y,56,50);}
        static void Drag(Button button,int kind,int value){var drag=button.gameObject.AddComponent<MissionDragSource>();drag.kind=kind;drag.value=value;}
        static void Drop(Button button,int kind,int target){var drop=button.gameObject.AddComponent<MissionDropTarget>();drop.kind=kind;drop.target=target;}
    }
}
#endif
