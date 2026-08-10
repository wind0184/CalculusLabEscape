using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CalculusLabEscape
{
    public sealed class LimitsPrototypeGame : MonoBehaviour
    {
        private enum GameState { Menu, Playing, Question, Paused, Completed }

        [Serializable]
        private sealed class Question
        {
            public string prompt;
            public string concept;
            public string[] answers;
            public int correct;
        }

        private readonly Question[] questions =
        {
            new Question
            {
                concept = "Direct Substitution",
                prompt = "Evaluate  lim  (x² - 1) / (x - 1) as x → 1",
                answers = new[] { "0", "1", "2", "Undefined" }, correct = 2
            },
            new Question
            {
                concept = "One-sided Limits",
                prompt = "If f(x)=3 for x<2 and f(x)=5 for x≥2, what is lim f(x) as x→2?",
                answers = new[] { "3", "4", "5", "Does not exist" }, correct = 3
            },
            new Question
            {
                concept = "Limit at Infinity",
                prompt = "Evaluate  lim  (4x² + 1) / (2x² - 3) as x → ∞",
                answers = new[] { "0", "1", "2", "∞" }, correct = 2
            }
        };

        private GameState state = GameState.Menu;
        private LabPlayerController player;
        private CalculusTerminal activeTerminal;
        private GUIStyle titleStyle, headingStyle, bodyStyle, buttonStyle, hudStyle, centerStyle;
        private Texture2D panelTexture, cyanTexture, magentaTexture, darkTexture;
        private int completedCount;
        private int attempts;
        private float elapsed;
        private string feedback = string.Empty;
        private float feedbackUntil;
        private Rect interactionRect;

        private void Awake()
        {
            player = FindFirstObjectByType<LabPlayerController>();
            CreateTextures();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (state == GameState.Playing) SetState(GameState.Paused);
                else if (state == GameState.Paused) SetState(GameState.Playing);
                else if (state == GameState.Question) CloseQuestion();
            }

            if (state != GameState.Playing)
                return;

            elapsed += Time.deltaTime;
            DetectTerminal();
            if (activeTerminal != null && Input.GetKeyDown(KeyCode.E))
                SetState(GameState.Question);
        }

        private void DetectTerminal()
        {
            activeTerminal = null;
            if (player == null) return;
            foreach (CalculusTerminal terminal in FindObjectsByType<CalculusTerminal>(FindObjectsSortMode.None))
            {
                if (!terminal.completed && Vector3.Distance(player.transform.position, terminal.transform.position) < 3.2f)
                {
                    activeTerminal = terminal;
                    break;
                }
            }
        }

        private void SetState(GameState next)
        {
            state = next;
            bool playing = next == GameState.Playing;
            if (player != null) player.ControlsEnabled = playing;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
        }

        private void CloseQuestion()
        {
            feedback = string.Empty;
            SetState(GameState.Playing);
        }

        private void SubmitAnswer(int answer)
        {
            attempts++;
            Question q = questions[Mathf.Clamp(activeTerminal.questionIndex, 0, questions.Length - 1)];
            if (answer == q.correct)
            {
                activeTerminal.completed = true;
                completedCount++;
                activeTerminal.GetComponent<Renderer>().material.color = new Color(0.1f, 1f, 0.45f);
                feedback = "ACCESS GRANTED • Correct reasoning";
                feedbackUntil = Time.time + 1.1f;
                Invoke(nameof(FinishCorrectAnswer), 1.1f);
            }
            else
            {
                feedback = "ACCESS DENIED • Inspect the limit behavior and try again";
                feedbackUntil = Time.time + 2.2f;
            }
        }

        private void FinishCorrectAnswer()
        {
            if (completedCount >= questions.Length) SetState(GameState.Completed);
            else CloseQuestion();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawTopBrand();
            switch (state)
            {
                case GameState.Menu: DrawMenu(); break;
                case GameState.Playing: DrawHud(); break;
                case GameState.Question: DrawHud(); DrawQuestion(); break;
                case GameState.Paused: DrawHud(); DrawPause(); break;
                case GameState.Completed: DrawCompleted(); break;
            }
        }

        private void DrawTopBrand()
        {
            GUI.Label(new Rect(26, 18, 500, 40), "CALCULUS LAB // ESCAPE", headingStyle);
            GUI.Label(new Rect(Screen.width - 250, 22, 220, 30), "PROTOTYPE v1.0", hudStyle);
        }

        private void DrawMenu()
        {
            Rect panel = CenterRect(760, 470);
            GUI.Box(panel, GUIContent.none, new GUIStyle { normal = { background = panelTexture } });
            GUI.Label(new Rect(panel.x + 55, panel.y + 45, panel.width - 110, 65), "LIMITS LAB", titleStyle);
            GUI.Label(new Rect(panel.x + 58, panel.y + 115, panel.width - 116, 70),
                "Restore three corrupted calculus terminals and unlock the laboratory exit.", bodyStyle);
            GUI.Label(new Rect(panel.x + 58, panel.y + 205, panel.width - 116, 75),
                "WASD  MOVE     MOUSE  LOOK     E  INTERACT     ESC  PAUSE", hudStyle);
            if (NeonButton(new Rect(panel.x + 160, panel.y + 325, panel.width - 320, 58), "ENTER LAB"))
                SetState(GameState.Playing);
            GUI.Label(new Rect(panel.x + 60, panel.y + 410, panel.width - 120, 30),
                "LEVEL 01 • LIMITS • ESTIMATED TIME 5 MIN", centerStyle);
        }

        private void DrawHud()
        {
            GUI.Box(new Rect(24, 72, 320, 86), GUIContent.none, new GUIStyle { normal = { background = darkTexture } });
            GUI.Label(new Rect(42, 86, 280, 28), $"TERMINALS  {completedCount} / {questions.Length}", headingStyle);
            GUI.Label(new Rect(42, 120, 280, 25), $"TIME  {TimeSpan.FromSeconds(elapsed):mm\\:ss}", hudStyle);
            GUI.Label(new Rect(Screen.width - 320, Screen.height - 54, 290, 30), "WASD MOVE  •  E INTERACT  •  ESC", hudStyle);

            if (state == GameState.Playing && activeTerminal != null)
            {
                interactionRect = new Rect(Screen.width / 2f - 210, Screen.height - 145, 420, 58);
                GUI.Box(interactionRect, GUIContent.none, new GUIStyle { normal = { background = cyanTexture } });
                GUI.Label(interactionRect, "PRESS  E  TO ACCESS TERMINAL", centerStyle);
            }
            DrawCrosshair();
        }

        private void DrawQuestion()
        {
            if (activeTerminal == null) { CloseQuestion(); return; }
            Question q = questions[activeTerminal.questionIndex];
            Rect panel = CenterRect(900, 560);
            GUI.Box(panel, GUIContent.none, new GUIStyle { normal = { background = panelTexture } });
            GUI.Label(new Rect(panel.x + 55, panel.y + 35, panel.width - 110, 34),
                $"TERMINAL 0{activeTerminal.questionIndex + 1} // {q.concept.ToUpperInvariant()}", headingStyle);
            GUI.Label(new Rect(panel.x + 55, panel.y + 95, panel.width - 110, 105), q.prompt, bodyStyle);

            for (int i = 0; i < q.answers.Length; i++)
            {
                int row = i / 2, col = i % 2;
                Rect r = new Rect(panel.x + 55 + col * 405, panel.y + 230 + row * 86, 375, 60);
                if (NeonButton(r, $"{(char)('A' + i)}   {q.answers[i]}")) SubmitAnswer(i);
            }

            if (!string.IsNullOrEmpty(feedback) && Time.time <= feedbackUntil)
            {
                GUI.color = feedback.StartsWith("ACCESS GRANTED") ? Color.green : new Color(1f, .3f, .5f);
                GUI.Label(new Rect(panel.x + 55, panel.y + 425, panel.width - 110, 42), feedback, centerStyle);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(panel.x + 55, panel.y + 500, panel.width - 110, 25), "ESC  RETURN TO LAB", centerStyle);
        }

        private void DrawPause()
        {
            Rect panel = CenterRect(520, 350);
            GUI.Box(panel, GUIContent.none, new GUIStyle { normal = { background = panelTexture } });
            GUI.Label(new Rect(panel.x + 40, panel.y + 45, panel.width - 80, 60), "SYSTEM PAUSED", titleStyle);
            if (NeonButton(new Rect(panel.x + 90, panel.y + 145, panel.width - 180, 55), "RESUME")) SetState(GameState.Playing);
            if (NeonButton(new Rect(panel.x + 90, panel.y + 220, panel.width - 180, 55), "RESTART LEVEL"))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void DrawCompleted()
        {
            Rect panel = CenterRect(760, 500);
            GUI.Box(panel, GUIContent.none, new GUIStyle { normal = { background = panelTexture } });
            GUI.Label(new Rect(panel.x + 45, panel.y + 50, panel.width - 90, 60), "LAB ESCAPED", titleStyle);
            GUI.Label(new Rect(panel.x + 45, panel.y + 125, panel.width - 90, 45), "LEVEL 01 // LIMITS COMPLETE", centerStyle);
            float accuracy = attempts == 0 ? 100f : completedCount / (float)attempts * 100f;
            GUI.Label(new Rect(panel.x + 105, panel.y + 210, panel.width - 210, 100),
                $"TIME       {TimeSpan.FromSeconds(elapsed):mm\\:ss}\nACCURACY   {accuracy:0}%\nTERMINALS  {completedCount}/{questions.Length}", bodyStyle);
            if (NeonButton(new Rect(panel.x + 180, panel.y + 375, panel.width - 360, 58), "PLAY AGAIN"))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void DrawCrosshair()
        {
            if (state != GameState.Playing) return;
            GUI.color = Color.cyan;
            GUI.DrawTexture(new Rect(Screen.width / 2f - 8, Screen.height / 2f - 1, 16, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width / 2f - 1, Screen.height / 2f - 8, 2, 16), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private bool NeonButton(Rect rect, string text)
        {
            return GUI.Button(rect, text, buttonStyle);
        }

        private Rect CenterRect(float width, float height) =>
            new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);

        private void CreateTextures()
        {
            panelTexture = MakeTexture(new Color(0.015f, 0.025f, 0.075f, 0.96f));
            darkTexture = MakeTexture(new Color(0.01f, 0.03f, 0.08f, 0.85f));
            cyanTexture = MakeTexture(new Color(0.02f, 0.45f, 0.58f, 0.92f));
            magentaTexture = MakeTexture(new Color(0.4f, 0.04f, 0.38f, 0.92f));
        }

        private static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = new Color(0.2f, 0.95f, 1f);
            headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            headingStyle.normal.textColor = new Color(0.3f, 0.95f, 1f);
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 23, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            bodyStyle.normal.textColor = Color.white;
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            hudStyle.normal.textColor = new Color(0.72f, 0.9f, 1f);
            centerStyle = new GUIStyle(hudStyle) { fontSize = 17, alignment = TextAnchor.MiddleCenter };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 19, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            buttonStyle.normal.background = cyanTexture;
            buttonStyle.hover.background = magentaTexture;
            buttonStyle.active.background = darkTexture;
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = Color.white;
        }
    }
}
