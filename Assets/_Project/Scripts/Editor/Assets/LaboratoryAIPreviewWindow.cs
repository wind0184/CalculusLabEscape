#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CalculusLabEscape.Editor
{
    public sealed class LaboratoryAIPreviewWindow : EditorWindow
    {
        LaboratoryAIPreview preview;
        double lastTick;
        bool animate = true;
        Vector2 scroll;
        static readonly string[] Notes = {
            "Relaxed hover, small head turns and curious eyes.",
            "A welcome wave, surprise, a smile and a wink.",
            "The panel opens. The bridge bends, hangers follow and a bright scan moves left.",
            "Palm up under the chin, with the head leaning onto the hand and thoughtful eyes.",
            "A small celebration with vivid green eyes, halo, core and hover light.",
            "A gentle open-hand cue with attentive eyes and amber light.",
            "A loopable in-place floating walk with alternating arms.",
            "Forward floating movement over 0.7 metres.",
            "A ninety-degree left turn with eye and head movement."
        };
        [MenuItem("Calculus Lab/Laboratory AI/Open preview")]
        public static void OpenShowcase()
        {
            if (!File.Exists(LaboratoryAIImport.ScenePath)) { Debug.LogError("Import the Laboratory AI assets first."); return; }
            if (EditorSceneManager.GetActiveScene().path != LaboratoryAIImport.ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(LaboratoryAIImport.ScenePath);
            }
            var window = GetWindow<LaboratoryAIPreviewWindow>("Laboratory AI");
            window.minSize = new Vector2(340, 440);
            window.preview = Object.FindFirstObjectByType<LaboratoryAIPreview>();
            window.lastTick = EditorApplication.timeSinceStartup;
            window.animate = true;
            if (window.preview != null)
            {
                window.preview.playing = true;
                window.preview.cycleAll = true;
                window.preview.Sample(2, 1.8f);
                Selection.activeGameObject = window.preview.gameObject;
            }
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            window.Show();
            Debug.Log("LABORATORY_AI_EDITOR_OPEN");
        }
        void OnEnable() { lastTick = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
        void OnDisable() { EditorApplication.update -= Tick; }
        void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            double delta = now - lastTick;
            if (delta < 1d / 30) return;
            lastTick = now;
            if (EditorApplication.isPlaying || !animate || preview == null) return;
            preview.Advance((float)System.Math.Min(delta, .1));
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            Repaint();
        }
        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("LABORATORY AI / UNITY", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Preview all nine motions without entering Play Mode. The scene also has working buttons in Play Mode.", MessageType.Info);
            if (preview == null)
            {
                if (GUILayout.Button("Open the AI preview scene")) OpenShowcase();
                EditorGUILayout.EndScrollView(); return;
            }
            for (int row = 0; row < 3; row++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int column = 0; column < 3; column++)
                {
                    int index = row * 3 + column;
                    Color old = GUI.backgroundColor;
                    if (index == preview.motionIndex) GUI.backgroundColor = new Color(.3f, .85f, 1);
                    if (GUILayout.Button(LaboratoryAIPreview.MotionNames[index], GUILayout.Height(35))) preview.PlayIndex(index);
                    GUI.backgroundColor = old;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space(8);
            animate = EditorGUILayout.Toggle("Animate in Edit Mode", animate);
            preview.cycleAll = EditorGUILayout.Toggle("Cycle through all motions", preview.cycleAll);
            var clip = preview.clips[preview.motionIndex];
            if (clip != null)
            {
                EditorGUI.BeginChangeCheck();
                float time = EditorGUILayout.Slider("Sample time", preview.previewTime, 0, clip.length);
                if (EditorGUI.EndChangeCheck()) { animate = false; preview.Sample(preview.motionIndex, time); SceneView.RepaintAll(); }
            }
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(LaboratoryAIPreview.MotionNames[preview.motionIndex].ToUpperInvariant(), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Notes[preview.motionIndex], MessageType.None);
            if (GUILayout.Button("Select editable AI prefab instance")) Selection.activeGameObject = preview.gameObject;
            if (GUILayout.Button("Select source prefab")) Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(LaboratoryAIImport.PrefabPath);
            if (GUILayout.Button("Capture this Unity frame")) LaboratoryAIImport.Capture(Object.FindFirstObjectByType<Camera>(), Path.Combine(LaboratoryAIImport.Evidence, "editor-selected.png"));
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
