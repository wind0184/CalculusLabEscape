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
    // Animation sampling and preview regeneration for the imported sample set.
    public static partial class AssetTrials
    {
        public static void PolishAndVerify()
        {
            RefineMaterials();
            var report = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                var paths = Directory.GetFiles(Roots[i], "*.fbx", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/')).ToArray();
                foreach (string name in Samples[i])
                {
                    string path = paths.First(p => Path.GetFileNameWithoutExtension(p) == name);
                    string prefabPath = PrefabRoot + "/" + Names[i] + "_" + name + ".prefab";
                    if (!File.Exists(prefabPath))
                    {
                        var wrapper = new GameObject(name);
                        var mesh = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                        mesh.transform.SetParent(wrapper.transform, false);
                        var b = BoundsOf(mesh);
                        wrapper.transform.localScale = Vector3.one * (3.2f / Mathf.Max(b.size.x, b.size.y, b.size.z));
                        b = BoundsOf(mesh);
                        mesh.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
                        PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
                        Object.DestroyImmediate(wrapper);
                    }
                    foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal) && c.length > 0))
                    {
                        var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                        clip.SampleAnimation(instance, clip.length * .15f);
                        var transforms = instance.GetComponentsInChildren<Transform>();
                        var positions = transforms.Select(t => t.localPosition).ToArray();
                        var rotations = transforms.Select(t => t.localRotation).ToArray();
                        var scales = transforms.Select(t => t.localScale).ToArray();
                        clip.SampleAnimation(instance, clip.length * .65f);
                        int changes = transforms.Where((t, k) => (t.localPosition - positions[k]).sqrMagnitude > .000001f || Quaternion.Angle(t.localRotation, rotations[k]) > .1f || (t.localScale - scales[k]).sqrMagnitude > .000001f).Count();
                        report.AppendLine(Names[i] + " / " + name + " / " + clip.name + " / moving transforms: " + changes);
                        Object.DestroyImmediate(instance);
                    }
                }
            }
            for (int i = 0; i < 4; i++)
                CreateScene(i);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Path.GetFullPath("../../qa/asset-trials-2026-10-18/animation-sampling.txt"), report.ToString());
            CaptureAll();
            Debug.Log("ASSET_TRIAL_POLISH_PASS");
        }
    }
}
#endif
