using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    // Presentation only. Game Manager and the existing Canvas events own navigation.
    public sealed class MainMenuPresentation : MonoBehaviour
    {
        public GameObject profileModal;
        public TMP_InputField profileInput;
        public TMP_Text profileLabel;
        public Button continueButton;
        string previousName;

        void Update()
        {
            if(profileModal.activeSelf&&!ScreenTransition.IsBusy&&UnityEngine.InputSystem.Keyboard.current!=null&&UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)CloseProfile();
        }

        void LateUpdate()
        {
            bool hasSession=continueButton.interactable;
            if(continueButton.gameObject.activeSelf!=hasSession)continueButton.gameObject.SetActive(hasSession);
            string name=Chapter4Store.Profile;
            if(profileLabel.text!=name)profileLabel.text=name;
        }
        public void OpenProfile()
        {
            previousName=Chapter4Store.Profile;
            profileModal.SetActive(true);
            profileInput.ActivateInputField();
        }
        public void CloseProfile()
        {
            if(string.IsNullOrWhiteSpace(profileInput.text))profileInput.text=previousName;
            profileInput.onEndEdit.Invoke(profileInput.text);
            profileModal.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }
}
