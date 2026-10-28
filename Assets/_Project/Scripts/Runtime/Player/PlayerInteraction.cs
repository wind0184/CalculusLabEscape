using UnityEngine;

namespace CalculusLabEscape
{
    // Movement belongs to LabPlayerController. E / Escape belong to this component.
    [DisallowMultipleComponent]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        public GameManager scene;
        [Min(0.1f)] public float interactionDistance = 3.8f;

        void Update()
        {
            if(scene==null||scene.Game==null)return;
            if(scene.robotDialogue!=null&&scene.robotDialogue.HandlePlayerInput())return;
            scene.Game.HandlePlayerInput();
        }
    }
}
