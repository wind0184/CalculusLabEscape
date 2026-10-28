using System.Collections;
using UnityEngine;

namespace CalculusLabEscape
{
    // Keeps the student's RectTransform and existing input/assessment logic.
    [DisallowMultipleComponent, RequireComponent(typeof(CanvasGroup))]
    public sealed class PageEntrance : MonoBehaviour
    {
        public CanvasGroup canvasGroup;
        [Range(.05f,.5f)] public float seconds=.16f;
        public bool IsAnimating { get; private set; }
        void OnEnable()
        {
            if(!Application.isPlaying||canvasGroup==null)return;
            if(ScreenTransition.InstantQA||ScreenTransition.Active!=null&&ScreenTransition.Active.reduceMotion){canvasGroup.alpha=1;return;}
            StartCoroutine(Reveal());
        }
        IEnumerator Reveal()
        {
            IsAnimating=true;canvasGroup.alpha=0;float elapsed=0;double started=Time.realtimeSinceStartupAsDouble;
            while(elapsed<seconds)
            {
                elapsed=(float)(Time.realtimeSinceStartupAsDouble-started);float t=Mathf.Clamp01(elapsed/seconds);
                canvasGroup.alpha=t*t*(3-2*t);yield return null;
            }
            canvasGroup.alpha=1;IsAnimating=false;
        }
        void OnDisable(){StopAllCoroutines();IsAnimating=false;if(canvasGroup!=null)canvasGroup.alpha=1;}
    }
}
