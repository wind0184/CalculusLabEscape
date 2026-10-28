using UnityEngine;
using UnityEngine.SceneManagement;

namespace CalculusLabEscape
{
    public static class GameProgress
    {
        public static void MarkCurrentLevelComplete()
        {
            string scene = SceneManager.GetActiveScene().name;
            PlayerPrefs.SetInt("Completed_" + scene, 1);
            PlayerPrefs.Save();
        }

        public static void ReturnToMenu()
        {
            Time.timeScale = 1f;
            GameManager.ChangeScene("MainMenu");
        }
    }
}
