using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CalculusLabEscape
{
    public sealed class IntegrationPrototypeGame : MonoBehaviour
    {
        private enum GameState { Menu, Playing, Console, Charging, Completed, Paused }

        private GameState state = GameState.Menu;
        private LabPlayerController player;
        private Transform terminal;
        private Transform door;
        private Transform[] bridge;
        private GUIStyle titleStyle, headingStyle, bodyStyle, buttonStyle, hudStyle, centerStyle;
        private Texture2D panelTexture, cyanTexture, magentaTexture, darkTexture;
        private float elapsed;
        private int attempts;
        private int hintsUsed;
        private int selectedAntiderivative = -1;
        private int selectedArea = -1;
        private bool nearTerminal;
        private string feedback = string.Empty;

        public void Configure(Transform puzzleTerminal, Transform exitDoor, Transform[] bridgeSegments)
        {
            terminal = puzzleTerminal;
            door = exitDoor;
            bridge = bridgeSegments;
        }

        private void Awake()
        {
            player = FindFirstObjectByType<LabPlayerController>();
            panelTexture = MakeTexture(new Color(.015f, .025f, .075f, .97f));
            darkTexture = MakeTexture(new Color(.01f, .03f, .08f, .9f));
            cyanTexture = MakeTexture(new Color(.02f, .45f, .58f, .95f));
            magentaTexture = MakeTexture(new Color(.48f, .04f, .4f, .95f));
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (state == GameState.Playing) SetState(GameState.Paused);
                else if (state == GameState.Paused) SetState(GameState.Playing);
                else if (state == GameState.Console) SetState(GameState.Playing);
            }
            if (state != GameState.Playing) return;
            elapsed += Time.deltaTime;
            nearTerminal = player != null && terminal != null && Vector3.Distance(player.transform.position, terminal.position) < 3.3f;
            if (nearTerminal && keyboard != null && keyboard.eKey.wasPressedThisFrame) SetState(GameState.Console);
        }

        private void SetState(GameState next)
        {
            state = next;
            bool playing = next == GameState.Playing;
            if (player != null) player.ControlsEnabled = playing;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
        }

        private void Submit()
        {
            if (selectedAntiderivative < 0 || selectedArea < 0)
            {
                feedback = "SELECT THE ANTIDERIVATIVE AND TOTAL ENERGY";
                return;
            }
            attempts++;
            if (selectedAntiderivative == 1 && selectedArea == 2)
            {
                feedback = "INTEGRAL VERIFIED // RECONSTRUCTING ENERGY BRIDGE";
                StartCoroutine(ChargeBridge());
            }
            else feedback = "ENERGY MISMATCH // Evaluate F(3) - F(0)";
        }

        private IEnumerator ChargeBridge()
        {
            state = GameState.Charging;
            for (int i = 0; i < bridge.Length; i++)
            {
                Vector3 start = bridge[i].position;
                Vector3 target = new Vector3(start.x, -.05f, start.z);
                for (float t = 0; t < .3f; t += Time.deltaTime)
                {
                    bridge[i].position = Vector3.Lerp(start, target, t / .3f);
                    yield return null;
                }
                bridge[i].position = target;
            }
            Vector3 closed = door.position;
            Vector3 open = closed + Vector3.up * 5f;
            for (float t = 0; t < 1.2f; t += Time.deltaTime)
            {
                door.position = Vector3.Lerp(closed, open, t / 1.2f);
                yield return null;
            }
            door.position = open;
            feedback = "ACCUMULATION STABLE // LAB CORE RESTORED";
            GameProgress.MarkCurrentLevelComplete();
            SetState(GameState.Completed);
        }

        private void OnGUI()
        {
            EnsureStyles();
            GUI.Label(new Rect(26, 18, 540, 40), "CALCULUS LAB // ESCAPE", headingStyle);
            switch (state)
            {
                case GameState.Menu: DrawMenu(); break;
                case GameState.Playing: DrawHud(); break;
                case GameState.Console: DrawHud(); DrawConsole(); break;
                case GameState.Charging: DrawHud(); DrawBanner(); break;
                case GameState.Paused: DrawPause(); break;
                case GameState.Completed: DrawCompleted(); break;
            }
        }

        private void DrawMenu()
        {
            Rect panel = CenterRect(780, 480);
            GUI.Box(panel, GUIContent.none, new GUIStyle { normal = { background = panelTexture } });
            GUI.Label(new Rect(panel.x + 45, panel.y + 38, panel.width - 90, 65), "ACCUMULATION ENERGY BRIDGE", titleStyle);
            GUI.Label(new Rect(panel.x + 65, panel.y + 120, panel.width - 130, 125), "The bridge has lost its stored energy. Integrate the incoming power rate over time to reconstruct a safe path to the laboratory core.", bodyStyle);
            GUI.Label(new Rect(panel.x + 65, panel.y + 260, panel.width - 130, 45), "WASD MOVE   //   MOUSE LOOK   //   E USE TERMINAL", centerStyle);
            if (GUI.Button(new Rect(panel.x + 180, panel.y + 350, panel.width - 360, 58), "ENTER CHAMBER", buttonStyle)) SetState(GameState.Playing);
        }

        private void DrawHud()
        {
            GUI.Box(new Rect(24, 72, 410, 100), GUIContent.none, new GUIStyle { normal = { background = darkTexture } });
            GUI.Label(new Rect(42, 84, 370, 30), "OBJECTIVE // RESTORE ENERGY BRIDGE", headingStyle);
            GUI.Label(new Rect(42, 120, 370, 25), $"TIME {TimeSpan.FromSeconds(elapsed):mm\\:ss}   ATTEMPTS {attempts}", hudStyle);
            if (state == GameState.Playing && nearTerminal)
            {
                Rect prompt = new Rect(Screen.width / 2f - 215, Screen.height - 145, 430, 58);
                GUI.Box(prompt, GUIContent.none, new GUIStyle { normal = { background = cyanTexture } });
                GUI.Label(prompt, "PRESS E TO ACCESS INTEGRATION CORE", centerStyle);
            }
            if (state == GameState.Playing) DrawCrosshair();
        }

        private void DrawConsole()
        {
            Rect panel = CenterRect(940, 680);
            GUI.Box(panel, GUIContent.none, new GUIStyle { normal = { background = panelTexture } });
            GUI.Label(new Rect(panel.x + 45, panel.y + 25, panel.width - 90, 38), "ENERGY ACCUMULATION CONTROL", headingStyle);
            GUI.Label(new Rect(panel.x + 55, panel.y + 68, panel.width - 110, 90), "Power enters at r(t) = 2t + 1 units/s from t = 0 to t = 3 s.\nFind the total accumulated energy:  integral from 0 to 3 of r(t) dt.", bodyStyle);
            GUI.Label(new Rect(panel.x + 55, panel.y + 170, 500, 30), "1 // Select an antiderivative F(t)", headingStyle);
            string[] antiderivatives = { "2t² + t", "t² + t", "t² + 1" };
            for (int i = 0; i < antiderivatives.Length; i++)
                if (ChoiceButton(new Rect(panel.x + 55 + i * 275, panel.y + 210, 250, 52), antiderivatives[i], selectedAntiderivative == i)) selectedAntiderivative = i;
            GUI.Label(new Rect(panel.x + 55, panel.y + 285, 500, 30), "2 // Compute F(3) - F(0)", headingStyle);
            string[] totals = { "9 units", "10 units", "12 units" };
            for (int i = 0; i < totals.Length; i++)
                if (ChoiceButton(new Rect(panel.x + 55 + i * 275, panel.y + 325, 250, 52), totals[i], selectedArea == i)) selectedArea = i;
            if (GUI.Button(new Rect(panel.x + 250, panel.y + 410, panel.width - 500, 58), "CHARGE BRIDGE", buttonStyle)) Submit();
            if (GUI.Button(new Rect(panel.x + 330, panel.y + 485, panel.width - 660, 44), "REQUEST HINT", buttonStyle))
            {
                hintsUsed++;
                feedback = "Hint: integrate each term, then substitute the upper and lower bounds.";
            }
            GUI.Label(new Rect(panel.x + 55, panel.y + 540, panel.width - 110, 55), feedback, centerStyle);
            GUI.Label(new Rect(panel.x + 55, panel.y + 625, panel.width - 110, 25), "ESC // RETURN TO CHAMBER", centerStyle);
        }

        private void DrawBanner()
        {
            Rect banner = new Rect(Screen.width / 2f - 340, Screen.height - 150, 680, 58);
            GUI.Box(banner, GUIContent.none, new GUIStyle { normal = { background = magentaTexture } });
            GUI.Label(banner, feedback, centerStyle);
        }

        private void DrawPause()
        {
            Rect panel = CenterRect(520, 330);
            GUI.Box(panel, GUIContent.none, new GUIStyle { normal = { background = panelTexture } });
            GUI.Label(new Rect(panel.x + 40, panel.y + 45, panel.width - 80, 55), "SYSTEM PAUSED", titleStyle);
            if (GUI.Button(new Rect(panel.x + 95, panel.y + 145, panel.width - 190, 55), "RESUME", buttonStyle)) SetState(GameState.Playing);
            if (GUI.Button(new Rect(panel.x + 95, panel.y + 220, panel.width - 190, 55), "RESTART", buttonStyle)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void DrawCompleted()
        {
            Rect panel = CenterRect(760, 470);
            GUI.Box(panel, GUIContent.none, new GUIStyle { normal = { background = panelTexture } });
            GUI.Label(new Rect(panel.x + 45, panel.y + 45, panel.width - 90, 60), "LAB CORE RESTORED", titleStyle);
            float accuracy = attempts == 0 ? 100f : 100f / attempts;
            GUI.Label(new Rect(panel.x + 70, panel.y + 130, panel.width - 140, 155), $"Integration converted a rate into accumulated energy.\n\nTIME {TimeSpan.FromSeconds(elapsed):mm\\:ss}   //   ATTEMPTS {attempts}\nACCURACY {accuracy:0}%   //   HINTS {hintsUsed}", centerStyle);
            if (GUI.Button(new Rect(panel.x + 90, panel.y + 350, 270, 58), "PLAY AGAIN", buttonStyle)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            if (GUI.Button(new Rect(panel.x + 400, panel.y + 350, 270, 58), "MAIN MENU", buttonStyle)) GameProgress.ReturnToMenu();
        }

        private bool ChoiceButton(Rect rect, string label, bool selected)
        {
            Color old = GUI.color;
            GUI.color = selected ? new Color(.35f, 1f, .55f) : Color.white;
            bool clicked = GUI.Button(rect, label, buttonStyle);
            GUI.color = old;
            return clicked;
        }

        private void DrawCrosshair()
        {
            GUI.color = Color.cyan;
            GUI.DrawTexture(new Rect(Screen.width / 2f - 8, Screen.height / 2f - 1, 16, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width / 2f - 1, Screen.height / 2f - 8, 2, 16), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private Rect CenterRect(float width, float height) => new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
        private static Texture2D MakeTexture(Color color) { Texture2D texture = new Texture2D(1, 1); texture.SetPixel(0, 0, color); texture.Apply(); return texture; }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = new Color(.2f, .95f, 1f);
            headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold };
            headingStyle.normal.textColor = new Color(.3f, .95f, 1f);
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            bodyStyle.normal.textColor = Color.white;
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            hudStyle.normal.textColor = new Color(.72f, .9f, 1f);
            centerStyle = new GUIStyle(hudStyle) { fontSize = 17, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            buttonStyle.normal.background = cyanTexture;
            buttonStyle.hover.background = magentaTexture;
            buttonStyle.active.background = darkTexture;
            buttonStyle.normal.textColor = buttonStyle.hover.textColor = Color.white;
        }
    }
}
