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
    // Built-in material creation and adaptation of glass, lights, and solid colours.
    public static partial class AssetTrials
    {
        static Material Solid(string name, Color color, bool emission = false)
        {
            string path = Base + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard")) { name = name, color = color };
                mat.SetFloat("_Glossiness", .22f);
                if (emission)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * .7f);
                }
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }
        static void RefineMaterials()
        {
            foreach (string path in Directory.GetFiles(Base + "/Materials", "*.mat"))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                string name = mat.name.ToLowerInvariant();
                if (name.Contains("glass"))
                {
                    mat.mainTexture = null;
                    mat.color = new Color(.4f, .78f, .86f, .23f);
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", 5);
                    mat.SetInt("_DstBlend", 10);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = 3000;
                }
                else if (name.StartsWith("m_alien") || name == "m_black" || name.StartsWith("m_light"))
                {
                    mat.mainTexture = null;
                    mat.DisableKeyword("_NORMALMAP");
                    mat.SetTexture("_BumpMap", null);
                    mat.SetTexture("_EmissionMap", null);
                    mat.color = name.Contains("orange") ? new Color(.95f, .35f, .08f) : name.Contains("cyan") ? Cyan : name.Contains("black") ? new Color(.04f, .05f, .06f) : name.Contains("eyes") ? Color.black : new Color(.12f, .37f, .65f);
                    if (name.StartsWith("m_light"))
                    {
                        mat.color = Color.white;
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", name.Contains("red") ? Color.red : Cyan);
                    }
                    else
                        mat.DisableKeyword("_EMISSION");
                }
                else if (name.Contains("trim_03_cables"))
                    mat.mainTexture = Texture(Roots[2], "T_Trim_03_Cables.png");
                EditorUtility.SetDirty(mat);
            }
        }
    }
}
#endif
