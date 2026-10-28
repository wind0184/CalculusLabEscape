using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    // Saved interface and scene references only. GameManager owns progress and assessment.
    public sealed class LimitsObservatoryView:MonoBehaviour
    {
        public GameManager manager;
        public Image[] stepCards;
        public TMP_Text[] stepStates;
        public TMP_Text heading;
        public Renderer gateSurface;
        int lastKey=-1;
        MaterialPropertyBlock properties;
        void LateUpdate()
        {
            if(manager==null||manager.Session==null)return;
            Present(manager.Stage,manager.LimitsExtension!=null,manager.CanvasAccepted);
        }
        public void Present(int completed,bool independent=false,bool accepted=false)
        {
            completed=Mathf.Clamp(completed,0,5);
            int key=completed*4+(independent?2:0)+(accepted?1:0);if(key==lastKey)return;lastKey=key;
            for(int i=0;i<stepCards.Length;i++){
                stepCards[i].color=i<completed?new Color(.07f,.25f,.25f,.97f):i==completed?new Color(.10f,.25f,.32f,.98f):new Color(.035f,.09f,.13f,.94f);
                stepStates[i].text=i<completed?"CALIBRATED":i>completed?"WAITING":independent?(accepted?"READY TO APPLY":"INDEPENDENT CHECK"):"GUIDED TASK";
                stepStates[i].color=i<completed?new Color(.48f,.92f,.76f):i==completed?new Color(.56f,.9f,.98f):new Color(.49f,.61f,.67f);
            }
            if(heading!=null)heading.text=completed==5?"ALL FIVE CALIBRATIONS RESTORED":"CALIBRATION "+(completed+1)+" / 5";
            if(gateSurface!=null){if(properties==null)properties=new MaterialPropertyBlock();gateSurface.GetPropertyBlock(properties);properties.SetFloat("_Progress",completed/5f);gateSurface.SetPropertyBlock(properties);}
        }
    }
}
