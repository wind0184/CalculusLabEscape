using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CalculusLabEscape
{
    public sealed class FrontendMenu : MonoBehaviour
    {
        private enum Page { Login, Main, Levels, Dashboard, Instructions }

        private const float ReferenceWidth = 1280f;
        private const float ReferenceHeight = 720f;
        private Page page = Page.Login;
        private string studentId = "student01";
        private string accessKey = "calculus";
        private string loginMessage = "TRAINING ENVIRONMENT READY";
        private GUIStyle logoStyle, heroStyle, outlineHeroStyle, overlineStyle, bodyStyle;
        private GUIStyle buttonStyle, cardStyle, smallStyle, symbolStyle, statusStyle, fieldStyle;
        private Texture2D dark, panel, cyan, cyanSoft, magenta, orange, green;

        private void Awake()
        {
            dark = MakeTexture(new Color(.008f, .022f, .032f, 1f));
            panel = MakeTexture(new Color(.018f, .055f, .07f, .96f));
            cyan = MakeTexture(new Color(.04f, .76f, .86f, 1f));
            cyanSoft = MakeTexture(new Color(.025f, .18f, .22f, 1f));
            magenta = MakeTexture(new Color(.53f, .24f, .86f, 1f));
            orange = MakeTexture(new Color(.95f, .58f, .16f, 1f));
            green = MakeTexture(new Color(.17f, .9f, .55f, 1f));
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnGUI()
        {
            float scale = Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight);
            Vector2 offset = new Vector2((Screen.width - ReferenceWidth * scale) * .5f, (Screen.height - ReferenceHeight * scale) * .5f);
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale);
            EnsureStyles();
            GUI.DrawTexture(new Rect(0, 0, ReferenceWidth, ReferenceHeight), dark);
            DrawHeader();
            switch (page)
            {
                case Page.Login: DrawLogin(); break;
                case Page.Main: DrawMain(); break;
                case Page.Levels: DrawLevels(); break;
                case Page.Dashboard: DrawDashboard(); break;
                case Page.Instructions: DrawInstructions(); break;
            }
            DrawFooter();
            GUI.matrix = oldMatrix;
        }

        private void DrawHeader()
        {
            GUI.Label(new Rect(48, 20, 420, 38), "∫  CALCULUS LAB", logoStyle);
            GUI.Label(new Rect(92, 52, 310, 20), "ESCAPE PROTOCOL", smallStyle);
            GUI.Label(new Rect(1010, 27, 210, 30), "●  SYSTEM ONLINE   |   v1.0", statusStyle);
            GUI.DrawTexture(new Rect(0, 77, ReferenceWidth, 1), cyanSoft);
        }

        private void DrawLogin()
        {
            GUI.Label(new Rect(88, 132, 600, 25), "RESTRICTED EDUCATIONAL FACILITY", overlineStyle);
            GUI.Label(new Rect(88, 165, 700, 80), "REPAIR THE", heroStyle);
            GUI.Label(new Rect(88, 235, 700, 80), "MATHEMATICS.", outlineHeroStyle);
            GUI.Label(new Rect(88, 305, 700, 80), "ESCAPE THE", heroStyle);
            GUI.Label(new Rect(88, 375, 700, 80), "LAB.", heroStyle);
            GUI.Label(new Rect(90, 466, 630, 60), "A corrupted virtual laboratory. Three calculus chambers.\nEvery correct solution restores the world around you.", bodyStyle);

            Rect box = new Rect(845, 142, 285, 450);
            GUI.Box(box, GUIContent.none, cardStyle);
            GUI.Label(new Rect(885, 187, 210, 20), "IDENTITY VERIFICATION", smallStyle);
            GUI.Label(new Rect(885, 217, 210, 58), "SECURE\nLOGIN", logoStyle);
            GUI.Label(new Rect(885, 307, 210, 18), "STUDENT ID", smallStyle);
            studentId = GUI.TextField(new Rect(885, 330, 205, 42), studentId, fieldStyle);
            GUI.Label(new Rect(885, 388, 210, 18), "ACCESS KEY", smallStyle);
            accessKey = GUI.PasswordField(new Rect(885, 411, 205, 42), accessKey, '•', fieldStyle);
            if (GUI.Button(new Rect(885, 480, 205, 50), "AUTHENTICATE   →", buttonStyle))
            {
                if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(accessKey)) loginMessage = "ENTER BOTH ID AND ACCESS KEY";
                else { PlayerPrefs.SetString("StudentId", studentId.Trim()); PlayerPrefs.Save(); page = Page.Main; }
            }
            GUI.Label(new Rect(885, 548, 220, 22), "●  " + loginMessage, statusStyle);
        }

        private void DrawMain()
        {
            GUI.Label(new Rect(88, 180, 620, 25), "WELCOME BACK, " + PlayerPrefs.GetString("StudentId", "TRAINEE").ToUpperInvariant(), overlineStyle);
            GUI.Label(new Rect(88, 220, 640, 78), "CALCULUS", heroStyle);
            GUI.Label(new Rect(88, 288, 650, 80), "LAB ESCAPE", outlineHeroStyle);
            GUI.Label(new Rect(90, 382, 600, 60), "Restore all three laboratory systems by solving limits,\ndifferentiation, and integration challenges.", bodyStyle);
            DrawProgress(new Rect(90, 485, 365, 110));

            if (MenuButton(new Rect(765, 195, 420, 80), "01", "START / CONTINUE", "Choose a calculus chamber")) page = Page.Levels;
            if (MenuButton(new Rect(775, 285, 420, 80), "02", "PERFORMANCE", "Review learning analytics")) page = Page.Dashboard;
            if (MenuButton(new Rect(775, 375, 420, 80), "03", "INSTRUCTIONS", "Controls and puzzle guide")) page = Page.Instructions;
            if (MenuButton(new Rect(765, 465, 420, 80), "04", "LOG OUT", "Return to identity verification")) page = Page.Login;
            if (MenuButton(new Rect(775, 555, 420, 60), "05", "EXIT", "Close the application")) Application.Quit();
        }

        private void DrawLevels()
        {
            GUI.Label(new Rect(88, 120, 620, 25), "LABORATORY MAP", overlineStyle);
            GUI.Label(new Rect(88, 155, 760, 70), "SELECT A CHAMBER", heroStyle);
            if (GUI.Button(new Rect(1050, 167, 140, 38), "← MAIN MENU", buttonStyle)) page = Page.Main;
            DrawLevelCard(new Rect(88, 248, 346, 360), "01", "lim", "LIMITS", "SECURITY CALIBRATION", "30 MIN  •  NOVICE", cyan, "Level01_Limits");
            DrawLevelCard(new Rect(467, 248, 346, 360), "02", "d/dx", "DIFFERENTIATION", "MOVING CABLE LOCK", "27 MIN  •  INTERMEDIATE", orange, "Level02_Differentiation");
            DrawLevelCard(new Rect(846, 248, 346, 360), "03", "∫", "INTEGRATION", "ENERGY BRIDGE", "38 MIN  •  ADVANCED", magenta, "Level03_Integration");
        }

        private void DrawDashboard()
        {
            GUI.Label(new Rect(88, 120, 650, 25), "LEARNING ANALYTICS", overlineStyle);
            GUI.Label(new Rect(88, 155, 700, 70), "PERFORMANCE", heroStyle);
            if (GUI.Button(new Rect(1050, 167, 140, 38), "← MAIN MENU", buttonStyle)) page = Page.Main;
            int completed = CompletedCount();
            MetricCard(new Rect(88, 260, 245, 125), "CHAMBERS", completed + " / 3");
            MetricCard(new Rect(350, 260, 245, 125), "OVERALL PROGRESS", Mathf.RoundToInt(completed / 3f * 100f) + "%");
            MetricCard(new Rect(612, 260, 245, 125), "STATUS", completed == 3 ? "RESTORED" : "IN TRAINING");
            MetricCard(new Rect(874, 260, 245, 125), "ACTIVE USER", PlayerPrefs.GetString("StudentId", "TRAINEE"));
            GUI.Label(new Rect(88, 430, 1000, 30), "CHAMBER PROGRESS", logoStyle);
            DashboardRow(88, 485, "01  LIMITS", PlayerPrefs.GetInt("Completed_Level01_Limits", 0) == 1);
            DashboardRow(88, 535, "02  DIFFERENTIATION", PlayerPrefs.GetInt("Completed_Level02_Differentiation", 0) == 1);
            DashboardRow(88, 585, "03  INTEGRATION", PlayerPrefs.GetInt("Completed_Level03_Integration", 0) == 1);
        }

        private void DrawInstructions()
        {
            GUI.Label(new Rect(88, 120, 650, 25), "FIELD MANUAL", overlineStyle);
            GUI.Label(new Rect(88, 155, 700, 70), "INSTRUCTIONS", heroStyle);
            if (GUI.Button(new Rect(1050, 167, 140, 38), "← MAIN MENU", buttonStyle)) page = Page.Main;
            InstructionCard(new Rect(88, 265, 330, 250), "01  NAVIGATE", "W A S D", "Move through the laboratory.\nUse the mouse to look around.");
            InstructionCard(new Rect(475, 265, 330, 250), "02  INTERACT", "E", "Approach a glowing terminal,\nthen press E to access it.");
            InstructionCard(new Rect(862, 265, 330, 250), "03  SYSTEM", "ESC", "Pause gameplay or close\nthe active puzzle interface.");
            GUI.Label(new Rect(88, 555, 1100, 50), "Solve carefully. Incorrect attempts affect accuracy; hints remain available when you need support.", bodyStyle);
        }

        private void DrawProgress(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, cardStyle);
            int completed = CompletedCount();
            GUI.Label(new Rect(rect.x + 22, rect.y + 18, rect.width - 44, 18), "CURRENT PROGRESS", smallStyle);
            GUI.Label(new Rect(rect.x + 22, rect.y + 43, rect.width - 44, 25), completed == 3 ? "Laboratory Restored" : "Calculus Training", logoStyle);
            GUI.DrawTexture(new Rect(rect.x + 22, rect.y + 79, rect.width - 44, 5), cyanSoft);
            GUI.DrawTexture(new Rect(rect.x + 22, rect.y + 79, (rect.width - 44) * completed / 3f, 5), green);
            GUI.Label(new Rect(rect.x + 22, rect.y + 88, rect.width - 44, 18), completed + "/3 chambers restored", smallStyle);
        }

        private bool MenuButton(Rect rect, string number, string title, string subtitle)
        {
            GUI.Box(rect, GUIContent.none, cardStyle);
            GUI.Label(new Rect(rect.x + 20, rect.y + 17, 45, 22), number, overlineStyle);
            GUI.Label(new Rect(rect.x + 85, rect.y + 13, 280, 26), title, logoStyle);
            GUI.Label(new Rect(rect.x + 85, rect.y + 42, 280, 20), subtitle, smallStyle);
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private void DrawLevelCard(Rect rect, string number, string symbol, string title, string subtitle, string meta, Texture2D accent, string scene)
        {
            GUI.Box(rect, GUIContent.none, cardStyle);
            GUI.Label(new Rect(rect.x + 25, rect.y + 25, 60, 24), number, overlineStyle);
            Color old = GUI.color; GUI.color = accent.GetPixel(0, 0);
            GUI.Label(new Rect(rect.x + 20, rect.y + 90, rect.width - 40, 105), symbol, symbolStyle);
            GUI.color = old;
            GUI.Label(new Rect(rect.x + 25, rect.y + 210, rect.width - 50, 32), title, logoStyle);
            GUI.Label(new Rect(rect.x + 25, rect.y + 250, rect.width - 50, 20), subtitle, overlineStyle);
            GUI.Label(new Rect(rect.x + 25, rect.y + 290, rect.width - 50, 20), meta, smallStyle);
            bool complete = PlayerPrefs.GetInt("Completed_" + scene, 0) == 1;
            GUI.Label(new Rect(rect.x + 25, rect.y + 320, 150, 20), complete ? "● RESTORED" : "○ AVAILABLE", complete ? statusStyle : smallStyle);
            if (GUI.Button(new Rect(rect.x + 210, rect.y + 310, 110, 35), "ENTER →", buttonStyle)) SceneManager.LoadScene(scene);
        }

        private void MetricCard(Rect rect, string label, string value)
        {
            GUI.Box(rect, GUIContent.none, cardStyle);
            GUI.Label(new Rect(rect.x + 20, rect.y + 20, rect.width - 40, 20), label, smallStyle);
            GUI.Label(new Rect(rect.x + 20, rect.y + 57, rect.width - 40, 42), value, logoStyle);
        }

        private void DashboardRow(float x, float y, string title, bool complete)
        {
            GUI.Box(new Rect(x, y, 1030, 38), GUIContent.none, cardStyle);
            GUI.Label(new Rect(x + 20, y + 7, 500, 25), title, logoStyle);
            GUI.Label(new Rect(x + 790, y + 7, 210, 25), complete ? "● RESTORED" : "○ NOT COMPLETED", complete ? statusStyle : smallStyle);
        }

        private void InstructionCard(Rect rect, string title, string key, string text)
        {
            GUI.Box(rect, GUIContent.none, cardStyle);
            GUI.Label(new Rect(rect.x + 25, rect.y + 25, rect.width - 50, 25), title, overlineStyle);
            GUI.Label(new Rect(rect.x + 25, rect.y + 70, rect.width - 50, 75), key, symbolStyle);
            GUI.Label(new Rect(rect.x + 25, rect.y + 165, rect.width - 50, 60), text, bodyStyle);
        }

        private int CompletedCount()
        {
            return PlayerPrefs.GetInt("Completed_Level01_Limits", 0) + PlayerPrefs.GetInt("Completed_Level02_Differentiation", 0) + PlayerPrefs.GetInt("Completed_Level03_Integration", 0);
        }

        private void DrawFooter()
        {
            GUI.DrawTexture(new Rect(0, 678, ReferenceWidth, 1), cyanSoft);
            GUI.Label(new Rect(48, 690, 350, 18), "FYP INTERACTIVE FLOW", smallStyle);
            GUI.Label(new Rect(1020, 690, 210, 18), "CALCULUS LAB ESCAPE", smallStyle);
        }

        private static Texture2D MakeTexture(Color color) { Texture2D texture = new Texture2D(1, 1); texture.SetPixel(0, 0, color); texture.Apply(); return texture; }

        private void EnsureStyles()
        {
            if (logoStyle != null) return;
            logoStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold };
            logoStyle.normal.textColor = new Color(.88f, .95f, .97f);
            heroStyle = new GUIStyle(logoStyle) { fontSize = 68 };
            outlineHeroStyle = new GUIStyle(heroStyle); outlineHeroStyle.normal.textColor = new Color(.08f, .78f, .86f);
            overlineStyle = new GUIStyle(logoStyle) { fontSize = 12 }; overlineStyle.normal.textColor = new Color(.09f, .8f, .88f);
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true }; bodyStyle.normal.textColor = new Color(.7f, .78f, .8f);
            smallStyle = new GUIStyle(bodyStyle) { fontSize = 10 }; smallStyle.normal.textColor = new Color(.5f, .62f, .65f);
            statusStyle = new GUIStyle(smallStyle); statusStyle.normal.textColor = new Color(.2f, .95f, .57f);
            symbolStyle = new GUIStyle(logoStyle) { fontSize = 62, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            buttonStyle.normal.background = cyan; buttonStyle.hover.background = cyanSoft; buttonStyle.active.background = magenta; buttonStyle.normal.textColor = Color.white;
            cardStyle = new GUIStyle(GUI.skin.box); cardStyle.normal.background = panel;
            fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 14, padding = new RectOffset(14, 14, 12, 8) }; fieldStyle.normal.background = cyanSoft; fieldStyle.normal.textColor = Color.white;
        }
    }
}
