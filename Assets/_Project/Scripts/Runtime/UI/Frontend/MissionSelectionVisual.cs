using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    [DisallowMultipleComponent]
    public sealed class MissionSelectionVisual:MonoBehaviour
    {
        [SerializeField] Image fill;
        [SerializeField] Outline border;
        [SerializeField] Color normalFill,normalBorder;
        [SerializeField] Vector2 normalDistance;
        [field:SerializeField] public bool IsSelected {get;private set;}

        public void Configure()
        {
            fill=GetComponent<Image>();border=GetComponent<Outline>();
            normalFill=fill.color;normalBorder=border.effectColor;normalDistance=border.effectDistance;
        }

        public void SetSelected(bool selected)
        {
            IsSelected=selected;
            fill.color=selected?Color.Lerp(normalFill,Color.white,.16f):normalFill;
            border.effectColor=selected?new Color(.2f,.82f,.88f):normalBorder;
            border.effectDistance=selected?normalDistance*3:normalDistance;
        }
    }
}
