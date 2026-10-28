using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CalculusLabEscape
{
    public sealed class MissionDragSource:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public int kind,value;
        bool Allowed=>GetComponent<Button>().IsInteractable();
        public void OnBeginDrag(PointerEventData data){if(Allowed){GetComponent<ButtonAudio>()?.Play();GetComponentInParent<MissionCanvas>().Drag(kind,value,data);}}
        public void OnDrag(PointerEventData data){if(Allowed)GetComponentInParent<MissionCanvas>().Drag(kind,value,data);}
        public void OnEndDrag(PointerEventData data)=>GetComponentInParent<MissionCanvas>().EndDrag();
    }
}
