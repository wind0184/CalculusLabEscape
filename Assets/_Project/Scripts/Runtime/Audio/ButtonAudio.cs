using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    [RequireComponent(typeof(Button)),DisallowMultipleComponent]
    public sealed class ButtonAudio : MonoBehaviour
    {
        public string cue="click";
        // First saved On Click entry: works for pointer and keyboard, before navigation hides this button.
        public void Play(){var button=GetComponent<Button>();if(button.IsActive()&&button.IsInteractable())SceneSoundscape.Active?.Play(cue);}
    }
}
