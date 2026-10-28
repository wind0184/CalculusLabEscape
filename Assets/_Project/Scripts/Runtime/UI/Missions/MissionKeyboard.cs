using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    public sealed partial class MissionCanvas
    {
        int lastInputSubmitFrame=-1;
        readonly string[] answerFields={"BridgeA","BridgeB","SequenceValue"};

        public void OnAnswerEdited(string value){Game.CanvasAnswerChanged();RefreshFeedback();}
        public void OnBridgeASubmitted(string value)=>SubmitInput(Control<TMP_InputField>("BridgeA"),"RepairSubmit");
        public void OnBridgeBSubmitted(string value)=>SubmitInput(Control<TMP_InputField>("BridgeB"),"RepairSubmit");
        public void OnSequenceValueSubmitted(string value)=>SubmitInput(Control<TMP_InputField>("SequenceValue"),"SequenceSubmit");

        void SubmitInput(TMP_InputField field,string buttonName)
        {
            if(Game==null||Game.State!="Puzzle"||modal!=null||Game.CanvasSupportOpen||ScreenTransition.IsBusy||lastInputSubmitFrame==Time.frameCount
                ||!field.isActiveAndEnabled||!field.IsInteractable()
                ||EventSystem.current==null||EventSystem.current.currentSelectedGameObject!=field.gameObject)return;
            var button=Control<Button>(buttonName);
            if(!button.isActiveAndEnabled||!button.IsInteractable())return;
            // Both TMP and the UI input module may report the same Enter press.
            lastInputSubmitFrame=Time.frameCount;
            button.onClick.Invoke();
            EventSystem.current.SetSelectedGameObject(null);
        }

        void HandleAnswerKeyboard()
        {
            var keyboard=Keyboard.current;
            if(Game.State!="Puzzle"||modal!=null||Game.CanvasSupportOpen||ScreenTransition.IsBusy||keyboard==null
                ||keyboard.ctrlKey.isPressed||keyboard.altKey.isPressed||EventSystem.current==null)return;
            // The default UI Submit action includes the main Enter key only.
            if(keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                var field=EventSystem.current.currentSelectedGameObject?.GetComponent<TMP_InputField>();
                if(field!=null&&answerFields.Any(id=>Control<TMP_InputField>(id)==field))SubmitInput(field,field==Control<TMP_InputField>("SequenceValue")?"SequenceSubmit":"RepairSubmit");
                return;
            }
            if(!keyboard.tabKey.wasPressedThisFrame)return;
            var fields=answerFields.Select(Control<TMP_InputField>).Where(f=>f.isActiveAndEnabled&&f.IsInteractable()).ToArray();
            if(fields.Length==0)return;
            int index=System.Array.FindIndex(fields,f=>f.gameObject==EventSystem.current.currentSelectedGameObject);
            int next=index<0?(keyboard.shiftKey.isPressed?fields.Length-1:0)
                :(index+(keyboard.shiftKey.isPressed?fields.Length-1:1))%fields.Length;
            EventSystem.current.SetSelectedGameObject(fields[next].gameObject);
            fields[next].ActivateInputField();
        }
    }
}
