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
    // Model and texture import, material remapping, and sample prefab creation.
    public static partial class AssetTrials
    {
        static Texture2D Texture(string root, string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Directory.GetFiles(root, "*.png", SearchOption.AllDirectories).First(p => Path.GetFileName(p) == name).Replace('\\', '/'));
        static Bounds BoundsOf(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0)
                throw new Exception("No mesh " + go.name);
            var b = rs[0].bounds;
            foreach (var r in rs.Skip(1))
                b.Encapsulate(r.bounds);
            return b;
        }
        public static void PrepareAndValidate()
        {
            Directory.CreateDirectory(Base + "/Materials");
            Directory.CreateDirectory(PrefabRoot);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var report = new StringBuilder();
            for (int i = 0; i < Roots.Length; i++)
            {
                string root = Roots[i];
                foreach (string p in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
                {
                    var t = (TextureImporter)AssetImporter.GetAtPath(p);
                    t.mipmapEnabled = i != 3;
                    t.textureCompression = TextureImporterCompression.Uncompressed;
                    t.textureType = i == 3 ? TextureImporterType.Sprite : p.Contains("_Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                    t.alphaIsTransparency = i == 3 || p.Contains("Decals");
                    t.filterMode = i < 2 ? FilterMode.Point : FilterMode.Bilinear;
                    if (i == 3)
                    {
                        t.spriteImportMode = SpriteImportMode.Single;
                        t.spritePixelsPerUnit = 100;
                    }
                    t.SaveAndReimport();
                }
                if (i == 3)
                    continue;
                var models = Directory.GetFiles(root, "*.fbx", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/')).ToArray();
                foreach (string path in models)
                {
                    var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                    importer.importCameras = false;
                    importer.importLights = false;
                    importer.importAnimation = true;
                    importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                    importer.SaveAndReimport();
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    foreach (var source in model.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct())
                    {
                        if (AssetDatabase.GetAssetPath(source).StartsWith(Base + "/Materials/", StringComparison.Ordinal))
                            continue;
                        string safe = new string(source.name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
                        string matPath = Base + "/Materials/" + Names[i] + "_" + safe + ".mat";
                        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if (mat == null)
                        {
                            mat = new Material(Shader.Find("Standard")) { name = source.name, color = Color.white };
                            if (i < 2)
                            {
                                mat.mainTexture = Texture(root, "colormap.png");
                                mat.color = source.HasProperty("_Color") ? source.color : Color.white;
                            }
                            else
                            {
                                string stem = source.name.Contains("02") ? "T_Trim_02" : source.name.Contains("03") ? "T_Trim_03" : source.name.ToLowerInvariant().Contains("padd") ? "T_PaddedWall" : "T_Trim_01";
                                if (source.name.ToLowerInvariant().Contains("decal"))
                                {
                                    mat.mainTexture = Texture(root, "T_Decals.png");
                                    mat.SetFloat("_Mode", 1);
                                    mat.SetInt("_SrcBlend", 1);
                                    mat.SetInt("_DstBlend", 0);
                                    mat.SetInt("_ZWrite", 1);
                                    mat.EnableKeyword("_ALPHATEST_ON");
                                    mat.renderQueue = 2450;
                                }
                                else
                                {
                                    mat.mainTexture = Texture(root, stem + "_BaseColor.png");
                                    mat.SetTexture("_BumpMap", Texture(root, stem + "_Normal.png"));
                                    mat.EnableKeyword("_NORMALMAP");
                                    if (stem == "T_Trim_01")
                                    {
                                        mat.SetTexture("_EmissionMap", Texture(root, "T_Trim_01_Emissive.png"));
                                        mat.SetColor("_EmissionColor", Color.white * .65f);
                                        mat.EnableKeyword("_EMISSION");
                                    }
                                }
                            }
                            mat.SetFloat("_Metallic", i == 2 ? .25f : 0);
                            mat.SetFloat("_Glossiness", .25f);
                            AssetDatabase.CreateAsset(mat, matPath);
                        }
                        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), mat);
                    }
                    importer.SaveAndReimport();
                    model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (BoundsOf(model).size.sqrMagnitude <= 0)
                        throw new Exception("Empty mesh " + path);
                    foreach (var m in model.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials))
                        if (m == null || m.shader == null || m.shader.name != "Standard" || (m.mainTexture == null && (m.name.Contains("colormap") || m.name.StartsWith("MI_", StringComparison.Ordinal) || m.name.StartsWith("M_Decal", StringComparison.Ordinal))))
                            throw new Exception("Invalid material " + path);
                    var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
                    if (clips.Length > 0)
                        report.AppendLine(Names[i] + " / " + Path.GetFileName(path) + " / clips: " + string.Join(", ", clips.Select(c => c.name)));
                }
                report.AppendLine(Names[i] + ": " + models.Length + " model files imported and checked.");
                foreach (string name in Samples[i])
                {
                    string path = models.First(p => Path.GetFileNameWithoutExtension(p) == name);
                    var rootObject = new GameObject(name);
                    var child = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                    child.transform.SetParent(rootObject.transform, false);
                    var b = BoundsOf(child);
                    float size = name.Contains("room") ? 4.8f : 3.2f;
                    rootObject.transform.localScale = Vector3.one * (size / Mathf.Max(b.size.x, b.size.y, b.size.z));
                    b = BoundsOf(child);
                    child.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
                    PrefabUtility.SaveAsPrefabAsset(rootObject, PrefabRoot + "/" + Names[i] + "_" + name + ".prefab");
                    Object.DestroyImmediate(rootObject);
                }
            }
            RefineMaterials();
            AssetDatabase.SaveAssets();
            for (int i = 0; i < 4; i++)
                CreateScene(i);
            string evidence = Path.GetFullPath("../../qa/asset-trials-2026-10-18");
            Directory.CreateDirectory(evidence);
            File.WriteAllText(evidence + "/import-report.txt", report.ToString());
            CaptureAll();
            Debug.Log("ASSET_TRIALS_PASS");
        }
    }
}
#endif
