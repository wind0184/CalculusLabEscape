using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CalculusLabEscape
{
    public sealed class MissionDropTarget:MonoBehaviour,IDropHandler
    {
        public int kind,target;
        public void OnDrop(PointerEventData data){if(GetComponent<Button>().IsInteractable()){var canvas=GetComponentInParent<MissionCanvas>();if(canvas.DragKind==kind)GetComponent<ButtonAudio>()?.Play();canvas.Drop(kind,target);}}
    }
}
