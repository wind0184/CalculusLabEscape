using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CalculusLabEscape
{
    [Serializable]
    public sealed class MissionIntroduction
    {
        public string key,title,screen;
        [TextArea(6,15)] public string instructions;
    }

    [CreateAssetMenu(menuName="Calculus Lab/Mission introductions")]
    public sealed class MissionOnboarding:ScriptableObject
    {
        public List<MissionIntroduction> cards=new List<MissionIntroduction>();
        public MissionIntroduction Find(string key)=>cards.FirstOrDefault(c=>c.key==key);
        public static string[] PreviewViews(int topic)=>Defaults().Where(c=>c.key.StartsWith(topic==1?"bridge-":"case-",StringComparison.Ordinal)).Select(c=>"Guide - "+c.screen).ToArray();
        public static MissionIntroduction[] Defaults()=>new[]{
            Card("bridge-power","Power Core","Your first bridge repair",
                "1. Choose the number chip, then place it in the coefficient slot. Drag it, or click chip then slot. [X] marks your selection.\n\n2. Adjust the exponent with - / +. The expression preview follows your controls. Work out the values from the question.\n\n3. Test repair checks your answer. Retry if needed. Use Next calibration when shown. Complete all checks, then choose Install regulator to apply the repair."),
            Card("bridge-numbers","Number inputs","Working with number answers",
                "1. Read the requested value above each box. Enter a number or a fraction using /, such as 3/7.\n\n2. Tab / Shift+Tab switches available boxes. Enter or Test repair checks your values. Switching boxes preserves your draft.\n\n3. Read the feedback, then edit and retry if needed. Use Next calibration when shown. Finish all checks, then install or continue when prompted. Follow the next device marker."),
            Card("bridge-choices","Expression choices","Assembling a repair",
                "1. Compare the expressions with the calculation in the question. Click one to select it; [X] marks your choice.\n\n2. Test repair checks the selected expression. An incorrect choice keeps your accepted work.\n\n3. Apply the accepted repair with the install / continue button. Some mathematical steps remain after all three components are installed."),
            Card("bridge-rescue","Rescue","Complete the rescue",
                "1. All required mathematical steps and the three bridge components are now complete.\n\n2. Click Open crossing / rescue calf to release the crossing. Watch the calf reach its mother.\n\n3. Results appear after the rescue. They separate completion, accuracy, support and skill feedback. Focused practice is available afterwards."),
            Card("case-overview","Case overview","Inspect the containment device",
                "1. Open Exponent dial first. Completing it unlocks the remaining three modules.\n\n2. Then choose Wires, Sequence or Pressure in any order. Case overview returns here; STABLE marks accepted work.\n\n3. Complete all four modules before using the separate Master Deactivation. The device stays powered until that final action. The countdown enters Safety Mode at zero; you can keep working."),
            Card("case-dial","Exponent dial","Using the integration dials",
                "1. Read the integral in the question. Use - / + to adjust the raised exponent and divisor separately.\n\n2. Insert +C toggles the constant. Check the expression preview, then use Test dials.\n\n3. Feedback identifies what to review without clearing accepted work. When the module is STABLE, return to Case overview to choose another module."),
            Card("case-wires","Wire matching","Connecting the terms",
                "1. Choose a source, then connect it to a socket. Drag source to socket, or click source then socket. [X] marks the selected source.\n\n2. Each connection is checked immediately. Accepted wires lock; incorrect wires can be retried. You decide which antiderivative matches each source.\n\n3. After all three wires are accepted, connect one common +C. Then return to Case overview."),
            Card("case-sequence","Symbol sequence","Using the procedure cards",
                "1. Read the question. Put procedure cards into numbered slots by dragging, or by clicking a card then a slot. Unaccepted cards can be rearranged.\n\n2. Work through the slots from 1 to 4. Enter the requested value; / enters a fraction.\n\n3. Enter or Run next step checks one step. Accepted cards lock and the input clears for the next value. Return to Case overview once STABLE."),
            Card("case-pressure","Pressure gauge","Controlling the pressure",
                "1. Read the bounds and integrand. Work out the accumulated value using the upper and lower bounds.\n\n2. Use - / + to set the release amount. Pull release lever checks your setting. The world gauge follows the result.\n\n3. Retry after feedback if needed. Once STABLE, return to Case overview. No constant chip is used for this definite-integral result."),
            Card("case-shutdown","Master deactivation","Deactivate the whole device",
                "1. All four modules are STABLE. The separate master action is now unlocked.\n\n2. Lift the master switch cover. This opens the protection; it does not yet shut down the device.\n\n3. Click Master Deactivation. Power turns off during the shutdown animation, then your results and focused practice become available.")
        };
        static MissionIntroduction Card(string key,string screen,string title,string instructions)=>new MissionIntroduction{key=key,screen=screen,title=title,instructions=instructions};
    }
}
