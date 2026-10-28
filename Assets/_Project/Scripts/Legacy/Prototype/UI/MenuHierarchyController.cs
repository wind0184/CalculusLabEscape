using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CalculusLabEscape
{
    public sealed class MenuHierarchyController : MonoBehaviour
    {
        private GameObject loginPage, mainPage, levelsPage, dashboardPage, instructionsPage;
        private InputField studentIdField, accessKeyField;
        private Text loginStatus, welcomeText, dashboardSummary;

        private void Awake()
        {
            // Keep the existing editable canvas as a legacy reference.
            foreach (Transform child in transform) child.gameObject.SetActive(false);
            var image = GetComponent<UnityEngine.UI.Image>();
            if (image != null) image.enabled = false;
            if(!ProductionCanvas.Install(true))gameObject.AddComponent<Chapter4Frontend>();
        }

        private void Authenticate()
        {
            if (string.IsNullOrWhiteSpace(studentIdField.text) || string.IsNullOrWhiteSpace(accessKeyField.text))
            {
                loginStatus.text = "ENTER BOTH ID AND ACCESS KEY";
                return;
            }
            PlayerPrefs.SetString("StudentId", studentIdField.text.Trim());
            PlayerPrefs.Save();
            welcomeText.text = "WELCOME BACK, " + studentIdField.text.Trim().ToUpperInvariant();
            Show(mainPage);
        }

        private void OpenDashboard()
        {
            int completed = PlayerPrefs.GetInt("Completed_Level01_Limits", 0)
                + PlayerPrefs.GetInt("Completed_Level02_Differentiation", 0)
                + PlayerPrefs.GetInt("Completed_Level03_Integration", 0);
            dashboardSummary.text = "CHAMBERS RESTORED     " + completed + " / 3\n\n"
                + "OVERALL PROGRESS      " + Mathf.RoundToInt(completed / 3f * 100f) + "%\n\n"
                + "ACTIVE USER           " + PlayerPrefs.GetString("StudentId", "TRAINEE");
            Show(dashboardPage);
        }

        private void Show(GameObject target)
        {
            loginPage.SetActive(target == loginPage);
            mainPage.SetActive(target == mainPage);
            levelsPage.SetActive(target == levelsPage);
            dashboardPage.SetActive(target == dashboardPage);
            instructionsPage.SetActive(target == instructionsPage);
        }

        private static void Bind(GameObject root, string path, UnityEngine.Events.UnityAction action)
        {
            root.transform.Find(path).GetComponent<Button>().onClick.AddListener(action);
        }
    }
}
