using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CalculusLabEscape
{
    public sealed class DifferentiationPrototypeGame : MonoBehaviour
    {
        private enum GameState { Menu, Playing, Console, Launching, Completed, Paused }

        private GameState state = GameState.Menu;
        private LabPlayerController player;
        private MovingPowerSocket socket;
        private Transform console;
        private Transform cableOrigin;
        private Transform door;
        private LineRenderer cable;
        private GUIStyle titleStyle, headingStyle, bodyStyle, buttonStyle, hudStyle, centerStyle;
        private Texture2D panelTexture, cyanTexture, magentaTexture, darkTexture;
        private float elapsed;
        private int attempts;
        private int selectedVelocity = -1;
        private int selectedTarget = -1;
        private string feedback = string.Empty;
        private bool nearConsole;

        public void Configure(Transform interactionConsole, Transform cableStart, Transform exitDoor,
            MovingPowerSocket movingSocket, LineRenderer cableRenderer)
        {
            console = interactionConsole;
            cableOrigin = cableStart;
            door = exitDoor;
            socket = movingSocket;
            cable = cableRenderer;
            cable.positionCount = 2;
            cable.enabled = false;
        }

        private void Awake()
        {
            player = FindFirstObjectByType<LabPlayerController>();
            CreateTextures();
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
            nearConsole = player != null && console != null &&
                Vector3.Distance(player.transform.position, console.position) < 3.3f;
            if (nearConsole && keyboard != null && keyboard.eKey.wasPressedThisFrame)
                SetState(GameState.Console);
        }

        private void SetState(GameState next)
        {
            state = next;
            bool playing = next == GameState.Playing;
            if (player != null) player.ControlsEnabled = playing;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
        }

        private void SubmitPrediction()
        {
            if (selectedVelocity < 0 || selectedTarget < 0)
            {
                feedback = "SELECT BOTH THE RATE OF CHANGE AND TARGET POSITION";
                return;
            }

            attempts++;
            // s'(t) = t + 2, so s'(2) = 4 m/s. Current s(2) = 6 m.
            // With a 1.5 s cable flight time, predicted position = 6 + 4(1.5) = 12 m.
            if (selectedVelocity == 1 && selectedTarget == 2)
            {
                feedback = "TRAJECTORY ACCEPTED // LAUNCHING POWER CABLE";
                StartCoroutine(LaunchCable(true));
            }
            else
            {
                feedback = "CONNECTION MISSED // Differentiate s(t), then predict s + v(1.5)";
                StartCoroutine(LaunchCable(false));
            }
        }

        private IEnumerator LaunchCable(bool correct)
        {
            state = GameState.Launching;
            cable.enabled = true;
            Vector3 start = cableOrigin.position;
            Vector3 target = correct ? socket.WorldPositionAt(3.5f) : socket.WorldPositionAt(3.5f) + Vector3.right * 3f;
            float duration = 1.5f;
            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                float progress = time / duration;
                socket.SetSimulationTime(Mathf.Lerp(2f, 3.5f, progress));
                cable.SetPosition(0, start);
                cable.SetPosition(1, Vector3.Lerp(start, target, progress));
                yield return null;
            }

            socket.SetSimulationTime(3.5f);
            cable.SetPosition(0, start);
            cable.SetPosition(1, target);
            if (correct)
            {
                feedback = "POWER LINK STABLE // EXIT UNLOCKED";
                yield return StartCoroutine(OpenDoor());
                GameProgress.MarkCurrentLevelComplete();
                SetState(GameState.Completed);
            }
            else
            {
                yield return new WaitForSeconds(1f);
                cable.enabled = false;
                socket.SetSimulationTime(2f);
                feedback = "Hint: s'(2) is the socket speed; cable flight time is 1.5 s.";
                SetState(GameState.Console);
            }
        }

        private IEnumerator OpenDoor()
        {
            Vector3 closed = door.position;
            Vector3 open = closed + Vector3.up * 5f;
            for (float time = 0; time < 1.5f; time += Time.deltaTime)
            {
                door.position = Vector3.Lerp(closed, open, time / 1.5f);
                yield return null;
            }
            door.position = open;
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
                case GameState.Launching: DrawHud(); DrawLaunching(); break;
                case GameState.Paused: DrawPause(); break;
                case GameState.Completed: DrawCompleted(); break;
            }
        }

        private void DrawMenu()
        {
            Rect panel = CenterRect(780, 480);
            GUI.Box(panel, GUIContent.none, new GUIStyle { normal = { background = panelTexture } });
            GUI.Label(new Rect(panel.x + 45, panel.y + 38, panel.width - 90, 65), "RATE-OF-CHANGE CABLE LOCK", titleStyle);
            GUI.Label(new Rect(panel.x + 65, panel.y + 120, panel.width - 130, 125),
                "A power socket is moving across the opposite rail. Calculate its instantaneous rate of change and predict where it will be when the cable arrives.", bodyStyle);
            GUI.Label(new Rect(panel.x + 65, panel.y + 260, panel.width - 130, 45),
                "WASD MOVE   //   MOUSE LOOK   //   E USE CONSOLE", centerStyle);
            if (GUI.Button(new Rect(panel.x + 180, panel.y + 350, panel.width - 360, 58), "BEGIN CALIBRATION", buttonStyle))
            {
                socket.SetSimulationTime(2f);
                SetState(GameState.Playing);
            }
        }

        private void DrawHud()
        {
            GUI.Box(new Rect(24, 72, 390, 100), GUIContent.none, new GUIStyle { normal = { background = darkTexture } });
            GUI.Label(new Rect(42, 84, 350, 30), "OBJECTIVE // CONNECT MOVING SOCKET", headingStyle);
            GUI.Label(new Rect(42, 120, 350, 25), $"TIME {TimeSpan.FromSeconds(elapsed):mm\\:ss}   ATTEMPTS {attempts}", hudStyle);
            if (state == GameState.Playing && nearConsole)
            {
                Rect prompt = new Rect(Screen.width / 2f - 215, Screen.height - 145, 430, 58);
                GUI.Box(prompt, GUIContent.none, new GUIStyle { normal = { background = cyanTexture } });
                GUI.Label(prompt, "PRESS E TO CALCULATE TRAJECTORY", centerStyle);
            }
            if (state == GameState.Playing) DrawCrosshair();
        }

        private void DrawConsole()
        {
            Rect panel = CenterRect(940, 650);
            GUI.Box(panel, GUIContent.none, new GUIStyle { normal = { background = panelTexture } });
            GUI.Label(new Rect(panel.x + 45, panel.y + 28, panel.width - 90, 38), "MOVING SOCKET TRAJECTORY", headingStyle);
            GUI.Label(new Rect(panel.x + 55, panel.y + 75, panel.width - 110, 100),
                "Socket position:  s(t) = 0.5t² + 2t metres\nCable launches at t = 2 s and takes 1.5 s to cross.", bodyStyle);
            GUI.Label(new Rect(panel.x + 55, panel.y + 182, 400, 32), "1 // Find instantaneous rate s'(2)", headingStyle);
            string[] velocities = { "2 m/s", "4 m/s", "6 m/s" };
            for (int i = 0; i < velocities.Length; i++)
                if (ChoiceButton(new Rect(panel.x + 55 + i * 275, panel.y + 225, 250, 52), velocities[i], selectedVelocity == i)) selectedVelocity = i;

            GUI.Label(new Rect(panel.x + 55, panel.y + 300, 540, 32), "2 // Predict socket position when cable arrives", headingStyle);
            string[] targets = { "8 m", "10 m", "12 m" };
            for (int i = 0; i < targets.Length; i++)
                if (ChoiceButton(new Rect(panel.x + 55 + i * 275, panel.y + 343, 250, 52), targets[i], selectedTarget == i)) selectedTarget = i;

            if (GUI.Button(new Rect(panel.x + 250, panel.y + 430, panel.width - 500, 58), "LAUNCH CABLE", buttonStyle)) SubmitPrediction();
            GUI.Label(new Rect(panel.x + 55, panel.y + 505, panel.width - 110, 55), feedback, centerStyle);
            GUI.Label(new Rect(panel.x + 55, panel.y + 590, panel.width - 110, 25), "ESC // RETURN TO CHAMBER", centerStyle);
        }

        private void DrawLaunching()
        {
            Rect banner = new Rect(Screen.width / 2f - 330, Screen.height - 150, 660, 58);
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
            Rect panel = CenterRect(760, 450);
            GUI.Box(panel, GUIContent.none, new GUIStyle { normal = { background = panelTexture } });
            GUI.Label(new Rect(panel.x + 45, panel.y + 48, panel.width - 90, 60), "CIRCUIT RESTORED", titleStyle);
            GUI.Label(new Rect(panel.x + 70, panel.y + 135, panel.width - 140, 120),
                $"The derivative predicted the moving target.\n\nTIME {TimeSpan.FromSeconds(elapsed):mm\\:ss}   //   ATTEMPTS {attempts}", centerStyle);
            if (GUI.Button(new Rect(panel.x + 90, panel.y + 330, 270, 58), "PLAY AGAIN", buttonStyle)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            if (GUI.Button(new Rect(panel.x + 400, panel.y + 330, 270, 58), "MAIN MENU", buttonStyle)) GameProgress.ReturnToMenu();
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
        private void CreateTextures()
        {
            panelTexture = MakeTexture(new Color(.015f, .025f, .075f, .97f));
            darkTexture = MakeTexture(new Color(.01f, .03f, .08f, .88f));
            cyanTexture = MakeTexture(new Color(.02f, .45f, .58f, .94f));
            magentaTexture = MakeTexture(new Color(.48f, .04f, .4f, .94f));
        }
        private static Texture2D MakeTexture(Color color) { var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, color); texture.Apply(); return texture; }
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
