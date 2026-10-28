using TMPro;
using UnityEngine;

namespace CalculusLabEscape
{
    // An opening greeting only. Never participates in the entrance Timeline.
    public sealed class MainMenuWelcome:MonoBehaviour
    {
        public MainMenuEntrance entrance;
        public CanvasGroup surface;
        public TMP_Text message;
        public bool Dismissed {get;private set;}
        float elapsed;
        void Awake(){if(Application.isPlaying){surface.alpha=0;message.maxVisibleCharacters=0;}}
        void LateUpdate()
        {
            if(entrance!=null&&entrance.Busy){HideForEntrance();return;}
            elapsed+=Time.unscaledDeltaTime;surface.alpha=Mathf.Clamp01(elapsed/.22f);message.maxVisibleCharacters=Mathf.FloorToInt(elapsed*54);
        }
        public void HideForEntrance(){Dismissed=true;gameObject.SetActive(false);}
        public void Preview(){surface.alpha=1;message.maxVisibleCharacters=int.MaxValue;}
    }
}
