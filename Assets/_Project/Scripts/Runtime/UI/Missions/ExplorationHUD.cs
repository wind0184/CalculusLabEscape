using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    // The same saved layout is used by Edit Mode previews and the live mission.
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class ExplorationHUD : MonoBehaviour
    {
        public RectTransform objectivePanel, guidePanel, promptPanel;
        public TMP_Text progress, objective, guideTitle, guideBody, guideKeys, prompt, interactionState;
        public SimulationAnswerBackdrop promptSurface;
        public Image[] progressSegments;
        [Header("Guide folding — relative to your saved RectTransforms")]
        public Vector2 compactGuideOffset = new Vector2(0, -70);
        public float compactGuideHeight = 68, compactBodyHeight = 23, compactBodyOffsetY = 8;
        public float compactTitleSize = 16, compactBodySize = 16;
        public bool IsCompact { get; private set; }
        bool configured, previousNear;
        int previousCompleted = -1;
        Vector2 expandedGuidePosition, expandedGuideSize, expandedBodyPosition, expandedBodySize;
        float expandedTitleSize, expandedBodyFont;

        void OnEnable()
        {
            configured = false;
            Present(false, false, 0);
        }

        void OnDisable()
        {
            if(IsCompact)RestoreExpandedGuide();
            IsCompact=false;
        }

        void FoldGuide()
        {
            // Capture the user's current layout just before folding, not hardcoded
            // screen coordinates. Moving the panel in Edit Mode survives Play.
            expandedGuidePosition=guidePanel.anchoredPosition;expandedGuideSize=guidePanel.sizeDelta;
            expandedBodyPosition=guideBody.rectTransform.anchoredPosition;expandedBodySize=guideBody.rectTransform.sizeDelta;
            expandedTitleSize=guideTitle.fontSize;expandedBodyFont=guideBody.fontSize;
            guidePanel.anchoredPosition=expandedGuidePosition+compactGuideOffset;
            guidePanel.sizeDelta=new Vector2(expandedGuideSize.x,compactGuideHeight);
            guideBody.rectTransform.anchoredPosition=expandedBodyPosition+Vector2.up*compactBodyOffsetY;
            guideBody.rectTransform.sizeDelta=new Vector2(expandedBodySize.x,compactBodyHeight);
            guideTitle.fontSize=compactTitleSize;guideBody.fontSize=compactBodySize;
        }

        void RestoreExpandedGuide()
        {
            if(guidePanel==null||guideBody==null||guideTitle==null)return;
            guidePanel.anchoredPosition=expandedGuidePosition;guidePanel.sizeDelta=expandedGuideSize;
            guideBody.rectTransform.anchoredPosition=expandedBodyPosition;guideBody.rectTransform.sizeDelta=expandedBodySize;
            guideTitle.fontSize=expandedTitleSize;guideBody.fontSize=expandedBodyFont;
        }

        public void Present(bool moved, bool near, int completed, bool robotNearby=false)
        {
            if (objectivePanel == null || interactionState == null) return;
            bool compact = moved || near;
            if (!configured || compact != IsCompact)
            {
                if(compact&&!IsCompact)FoldGuide();
                else if(!compact&&IsCompact)RestoreExpandedGuide();
                guideKeys.gameObject.SetActive(!compact);
                IsCompact = compact;
            }

            // Keep a short reminder after moving; G still controls guide visibility.
            guideBody.text = robotNearby?"Press E to talk to your laboratory guide.":compact
                ? near ? "Press E to open the device controls." : "Move closer until the [E] prompt appears."
                : "Move with WASD. Use the mouse to look.\nFollow the cyan device marker.";

            if (!configured || near != previousNear)
            {
                interactionState.text = near ? "IN RANGE" : "APPROACH";
                interactionState.color = near ? new Color(.3f, .94f, .86f) : new Color(.5f, .68f, .73f);
                prompt.color = near ? new Color(.85f, 1f, .96f) : new Color(.75f, .85f, .89f);
                prompt.fontStyle = near ? FontStyles.Bold : FontStyles.Normal;
                promptSurface.rim = near ? new Color(.21f, .79f, .72f, .95f) : new Color(.18f, .35f, .43f, .8f);
                promptSurface.SetVerticesDirty();
                previousNear = near;
            }
            if (!configured || completed != previousCompleted)
            {
                for (int i = 0; i < progressSegments.Length; i++)
                    progressSegments[i].color = i < completed ? new Color(.3f, .83f, .58f)
                        : i == completed ? new Color(.2f, .75f, .82f) : new Color(.17f, .29f, .35f);
                previousCompleted = completed;
            }
            configured = true;
        }
    }
}
