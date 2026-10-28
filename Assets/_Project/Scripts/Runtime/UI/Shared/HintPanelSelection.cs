using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    // Existing buttons and their colours are authored in the saved UI.
    [DisallowMultipleComponent]
    public sealed class HintPanelSelection : MonoBehaviour
    {
        public Button[] tiers;
        public TMP_Text caption;
        public Color idleFill = new Color(.045f,.09f,.12f);
        public Color activeFill = new Color(.2f,.79f,.83f);
        public Color idleInk = new Color(.91f,.95f,.96f);
        public Color activeInk = new Color(.025f,.065f,.085f);
        public int SelectedTier { get; private set; } = -1;

        public void Select(int tier)
        {
            SelectedTier = tier;
            if(caption!=null)caption.text=tier<0?"HINT / CHOOSE SUPPORT":new[]{"HINT / BASIC","HINT / RULE REMINDER","HINT / GUIDED EXAMPLE"}[tier];
            if(tiers==null)return;
            for(int i=0;i<tiers.Length;i++)
            {
                var button=tiers[i];if(button==null)continue;
                bool selected=i==tier;
                button.GetComponent<Image>().color=selected?activeFill:idleFill;
                var border=button.GetComponent<Outline>();
                if(border!=null)border.effectColor=selected?activeFill:Color.Lerp(idleFill,idleInk,.18f);
                foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))label.color=selected?activeInk:idleInk;
            }
        }
    }
}
