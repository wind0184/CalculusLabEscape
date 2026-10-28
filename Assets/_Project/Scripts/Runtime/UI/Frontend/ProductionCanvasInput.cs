using UnityEngine;

namespace CalculusLabEscape
{
    public sealed partial class ProductionCanvas
    {
        // These are dynamic Inspector events: the field/slider supplies its value.
        public void OnVolumeChanged(float value)=>AudioListener.volume=value;
        public void OnProfileEdited(string value)=>SaveProfile();
        public void OnAnswer0Changed(string value)=>SetAnswerValue(0,value);
        public void OnAnswer1Changed(string value)=>SetAnswerValue(1,value);
        public void OnAnswer2Changed(string value)=>SetAnswerValue(2,value);
        void SetAnswerValue(int index,string value){answer.values[index]=value;LimitsAnswerChanged();}
        public void OnProbeChanged(float value)=>RefreshGraph();
        public void OnMarkerChanged(float value)
        {
            int marker=Mathf.RoundToInt(value);
            if(answer.marker!=marker){answer.marker=marker;LimitsAnswerChanged();}
            RefreshGraph();
        }
        public void OnPracticeClose()=>recordPractice.Close();
        public void OnPracticeOption(int index)=>recordPractice.Choose(index);
        public void OnPracticeSubmit()=>recordPractice.Submit();
        public void OnPracticeNext()=>recordPractice.Next();
        public void OnPracticeRepeat()=>recordPractice.Repeat();
    }
}
