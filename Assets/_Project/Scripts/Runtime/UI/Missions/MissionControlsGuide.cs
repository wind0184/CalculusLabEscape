using System.Linq;

namespace CalculusLabEscape
{
    public sealed partial class MissionCanvas
    {

        void RefreshControlsGuide()=>Text("NextAction","NEXT ACTION\n"+NextControlAction());

        string NextControlAction()
        {
            if(topic==1)
            {
                if(Game.Ready)return "Click Open crossing to rescue the calf.";
                if(Game.CanvasAccepted)
                {
                    string next=Control<TMPro.TMP_Text>("RepairContinueLabel").text;
                    return "Answer accepted. Click "+next+(next=="Next calibration"?" for the next check.":" to apply the repair.");
                }
                if(Game.BridgeExtension!=null)return Game.BridgeExtension.choiceAnswer?"Select an expression, then click Test repair.":"Enter coefficient and exponent. Tab switches boxes; Enter checks both values.";
                if(Game.Stage==0)return !coefficient
                    ?DragKind==0?"Chip selected. Click the coefficient slot.":"Drag the chip to its slot, or click chip then slot."
                    :"Adjust the exponent with - / +, then click Test repair.";
                if(Game.Stage==7||Game.Stage==10)return choice<0?"Select an expression, then click Test repair.":"Selection ready. Click Test repair to check it.";
                return MissionRules.BridgePair(Game.Stage)?"Enter both values. Tab switches boxes; Enter or Test repair checks them.":"Enter the value, then press Enter or click Test repair. Fractions use /.";
            }
            if(caseOverview)
            {
                if(Game.Ready)return cover?"Click Master Deactivation to shut down the device.":"All modules stable. Click Lift master switch cover.";
                return Game.CaseModuleComplete(0)?"Open any unfinished module. Green STABLE means saved.":"Open Exponent dial to unlock the other modules.";
            }
            string[] ids={"I_DIAL","I_C","I_SEQ_3","I_GAUGE"};
            if(Game.CaseGuidedReview)return "Review the result, then click Continue to independent check.";
            if(Game.CaseCalibration!=null)return Game.MissionDone(Game.CaseCalibration.id)?"Answer accepted. Click "+Control<TMPro.TMP_Text>("CalibrationNextLabel").text+".":"Select a full answer, then click Check calibration.";
            if(Game.CaseModuleComplete(Game.Module))return "Module stable. Click Case overview to continue.";
            if(Game.Module==0)return "Use - / + on both dials, insert +C, then click Test dials.";
            if(Game.Module==1)
            {
                if(Enumerable.Range(0,3).All(i=>Game.MissionDone("I_W"+i)))return "Wires accepted. Click Connect one common +C.";
                return selectedWire>=0&&!Game.MissionDone("I_W"+selectedWire)
                    ?"Source "+(selectedWire+1)+" selected. Click a matching socket to check it."
                    :"Drag a source to a socket, or click source then socket.";
            }
            if(Game.Module==2)
            {
                int next=SequenceStep();
                if(next>=4)return "Sequence accepted. Review the result before continuing.";
                return order[next]<0?selectedCard>=0&&Control<UnityEngine.UI.Button>("SequenceCard"+selectedCard).interactable
                    ?MissionRules.SequenceCards[selectedCard]+" card selected. Click slot "+(next+1)+" to place it."
                    :"Place a card in slot "+(next+1)+". Drag it, or click card then slot."
                    :"Slot "+(next+1)+" ready. Enter its value; press Enter or click Run next step.";
            }
            return "Set the release with - / +, then click Pull release lever.";
        }

        string ControlsInstructions()
        {
            string instructions;
            if(topic==1)
                instructions=Game.Ready?"1. Click Open crossing / rescue calf.\n2. Watch the calf cross the completed bridge.\n3. Your results appear after the rescue."
                    :Game.BridgeExtension==null&&Game.Stage==0?"1. Drag the number chip into the coefficient slot. You can also click the chip, then the slot.\n2. Use the - / + buttons to change the exponent. Check your expression preview.\n3. Click Test repair. After the independent calibration, install the regulator."
                    :Game.BridgeExtension?.choiceAnswer==true||Game.BridgeExtension==null&&(Game.Stage==7||Game.Stage==10)?"1. Click an expression to select it. The selected option shows [X].\n2. Click Test repair to check your selection. You can retry after feedback.\n3. After acceptance, click the install / continue button to apply the repair."
                    :"1. Type each requested value. Use / for fractions, such as 1/2.\n2. Tab / Shift+Tab switches boxes. Enter or Test repair checks your values. Read the feedback and retry if needed.\n3. After acceptance, click the install / continue button. Follow the world marker to the next device.";
            else if(!caseOverview&&Game.CaseGuidedReview)
                instructions="1. Review your accepted expression and the correct explanation.\n2. Click Continue to independent check.\n3. Complete both independent checks before returning to the device overview.";
            else if(!caseOverview&&Game.CaseCalibration!=null)
                instructions="1. Read the independent question and select a complete answer.\n2. Click Check calibration. Read the feedback and retry if needed.\n3. Click Next calibration or Finish module after acceptance. A module becomes stable after its controls and calibrations are complete.";
            else if(caseOverview)
                instructions="1. Open Exponent dial first. Completing it unlocks the other three modules.\n2. Complete the remaining modules in any order. Use Case overview to switch; STABLE marks accepted work.\n3. When all four modules are stable, lift the master switch cover, then click Master Deactivation.";
            else
                instructions=new[]{
                    "1. Use - / + to adjust the exponent and divisor separately.\n2. Click Insert +C to toggle the constant. Check the expression preview.\n3. Click Test dials. Once stable, click Case overview to choose another module.",
                    "1. Drag a source expression onto a socket. Alternatively, click a source, then click a socket.\n2. Each connection is checked immediately. Accepted wires lock; incorrect wires can be tried again.\n3. After all three wires are accepted, click Connect one common +C. Then return to Case overview.",
                    "1. Drag a card into a numbered slot, or click a card, then a slot. [X] marks the selected card. Unaccepted cards can be rearranged.\n2. Work from slot 1 to slot 4. Type the requested value; use / for fractions.\n3. Press Enter or click Run next step. Accepted cards lock. Continue, then return to Case overview.",
                    "1. Work out the accumulated value using the bounds in the question.\n2. Use - / + to set the release amount.\n3. Click Pull release lever to check it. When stable, return to Case overview."
                }[Game.Module];
            return instructions+"\n\nWrong answers keep accepted progress, but count as attempts.\nOperation help is free. This screen pauses active time and environmental pressure.";
        }
    }
}
