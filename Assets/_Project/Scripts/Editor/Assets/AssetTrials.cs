#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    // Configuration and Unity menu entry points.
    public static partial class AssetTrials
    {
        const string Base = "Assets/_Project/AssetTrials";
        const string PrefabRoot = "Assets/_Project/Prefabs/AssetTrials";
        static readonly string[] Roots = { "Assets/ThirdParty/Kenney/FactoryKit", "Assets/ThirdParty/Kenney/ModularSpaceKit", "Assets/ThirdParty/Quaternius/ModularSciFiMegaKit", "Assets/ThirdParty/Kenney/InputPrompts" };
        static readonly string[] Names = { "Factory", "ModularSpace", "SciFiMega", "InputPrompts" };
        static readonly string[][] Samples ={
            new[]{"robot-arm-a","machine-window","scanner-high","pipe-large-valve","screen-panel-wide","lever-double","crane","catwalk-stairs","conveyor-stripe-sides"},
            new[]{"room-small","corridor-corner","room-wide","gate","gate-door-window","gate-lasers","stairs","corridor","cables"},
            new[]{"Door_Frame_A","Column_Pipes","WallAstra_Straight_Window","Prop_Computer","Prop_AccessPoint","Prop_Chest","Platform_Stairs_4","Prop_Light_Wide","Prop_Vent_Big"}
        };
        static readonly Color Cyan = new Color(.22f, .88f, .91f);
        static string ScenePath(int i) => "Assets/_Project/Scenes/" + Names[i] + "AssetsPreview.unity";
        [MenuItem("Calculus Lab/Assets/Trials/1 Factory Kit")]
        public static void OpenFactory() => Open(0);
        [MenuItem("Calculus Lab/Assets/Trials/2 Modular Space Kit")]
        public static void OpenSpace() => Open(1);
        [MenuItem("Calculus Lab/Assets/Trials/3 Sci-Fi MegaKit")]
        public static void OpenMega() => Open(2);
        [MenuItem("Calculus Lab/Assets/Trials/4 Input Prompts")]
        public static void OpenPrompts() => Open(3);
        static void Open(int i)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(ScenePath(i));
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null && SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.AlignViewToObject(cam.transform);
        }
    }
}
#endif
