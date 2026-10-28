#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static class SharedButtonAuthoring
    {
        public const string Root="Assets/_Project/Prefabs/UI/Buttons/";
        static string Evidence=>LearningApi.Argument("--button-audio-evidence");
        static string Signature(GameObject root)
        {
            var text=new StringBuilder();
            foreach(var transform in root.GetComponentsInChildren<Transform>(true)){
                string path=transform.name;for(var parent=transform.parent;parent!=null&&parent!=root.transform.parent;parent=parent.parent)path=parent.name+"/"+path;
                text.Append(path).Append(transform.gameObject.activeSelf).Append(transform.localPosition.ToString("R")).Append(transform.localRotation.ToString("R")).Append(transform.localScale.ToString("R"));
                if(transform is RectTransform rect)text.Append(rect.anchorMin.ToString("R")).Append(rect.anchorMax.ToString("R")).Append(rect.pivot.ToString("R")).Append(rect.sizeDelta.ToString("R"));
                foreach(var graphic in transform.GetComponents<Graphic>()){
                    text.Append(graphic.GetType().Name).Append(graphic.color.ToString("R")).Append(graphic.raycastTarget).Append(AssetDatabase.GetAssetPath(graphic.material));
                    if(graphic is Image image)text.Append(AssetDatabase.GetAssetPath(image.sprite)).Append(image.type).Append(image.fillAmount.ToString("R"));
                    if(graphic is TMP_Text label)text.Append(label.text).Append(label.fontSize.ToString("R")).Append(label.alignment).Append(AssetDatabase.GetAssetPath(label.font));
                }
                foreach(var button in transform.GetComponents<Button>()){
                    text.Append(button.interactable).Append(button.transition).Append(button.colors.normalColor.ToString("R")).Append(button.colors.disabledColor.ToString("R"));
                    for(int i=0;i<button.onClick.GetPersistentEventCount();i++){
                        var target=button.onClick.GetPersistentTarget(i);text.Append(TargetPath(target)).Append(button.onClick.GetPersistentMethodName(i)).Append(button.onClick.GetPersistentListenerState(i));
                    }
                }
            }
            using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
        }
        static string TargetPath(Object value)
        {
            if(value==null)return "null";var node=value is Component component?component.transform:value is GameObject go?go.transform:null;
            if(node==null)return AssetDatabase.GetAssetPath(value);string path=node.name;while(node.parent!=null){node=node.parent;path=node.name+"/"+path;}return path+"/"+value.GetType().FullName;
        }
        static void EnsurePrefabs()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            if(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Button.prefab")==null){
                var root=new GameObject("Button",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button),typeof(ButtonAudio));
                try{
                    var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(180,44);
                    root.GetComponent<Image>().color=new Color(.83f,.91f,.91f);root.GetComponent<Button>().targetGraphic=root.GetComponent<Image>();
                    UnityEventTools.AddPersistentListener(root.GetComponent<Button>().onClick,root.GetComponent<ButtonAudio>().Play);
                    PrefabUtility.SaveAsPrefabAsset(root,Root+"Button.prefab");
                }finally{Object.DestroyImmediate(root);}
            }
            if(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"HintButton.prefab")==null){
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Button.prefab"));
                try{instance.name="HintButton";instance.GetComponent<ButtonAudio>().cue="hint";PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<ButtonAudio>());PrefabUtility.SaveAsPrefabAsset(instance,Root+"HintButton.prefab");}
                finally{Object.DestroyImmediate(instance);}
            }
        }
        static bool Shared(Button button)=>PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(button.gameObject)==Root+"Button.prefab"||PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(button.gameObject)==Root+"HintButton.prefab";
        static int Convert(GameObject root)
        {
            int changed=0;
            foreach(var button in root.GetComponentsInChildren<Button>(true).OrderByDescending(b=>Depth(b.transform)).ToArray()){
                if(Shared(button))continue;
                var sound=button.GetComponent<ButtonAudio>();if(sound==null)throw new Exception("Missing button sound before conversion: "+button.name);
                string path=Root+(sound.cue=="hint"?"HintButton.prefab":"Button.prefab");
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                PrefabUtility.ConvertToPrefabInstance(button.gameObject,prefab,new ConvertToPrefabInstanceSettings{
                    objectMatchMode=ObjectMatchMode.ByHierarchy,changeRootNameToAssetName=false,
                    componentsNotMatchedBecomesOverride=true,gameObjectsNotMatchedBecomesOverride=true,recordPropertyOverridesOfMatches=true
                },InteractionMode.AutomatedAction);
                // Cue type inherits from the ordinary/hint prefab rather than a local override.
                PrefabUtility.RevertPropertyOverride(new SerializedObject(sound).FindProperty("cue"),InteractionMode.AutomatedAction);
                changed++;
            }
            return changed;
        }
        static int Depth(Transform node){int depth=0;while(node.parent!=null){node=node.parent;depth++;}return depth;}
        [MenuItem("Calculus Lab/Audio/Convert buttons to shared prefabs")]
        public static void Apply()
        {
            EnsurePrefabs();var reports=new List<string>();var sceneLinks=new Dictionary<string,List<StableUIReferences.Link>>();
            foreach(string name in new[]{"MainMenu"}.Concat(Chapter4Game.Scenes)){
                var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+name+".unity");sceneLinks.Add(name,StableUIReferences.Capture(scene.GetRootGameObjects()));
            }
            var paths=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Project/Prefabs/Resources/Production"}).Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path=>AssetDatabase.GetDependencies(path,true).Count(dependency=>dependency.EndsWith(".prefab"))).ToArray();
            // Capture parents before converting any nested dependency: its old local IDs may disappear.
            var prefabLinks=new Dictionary<string,List<StableUIReferences.Link>>();var signatures=new Dictionary<string,string>();
            foreach(string path in paths){var original=PrefabUtility.LoadPrefabContents(path);try{prefabLinks.Add(path,StableUIReferences.Capture(new[]{original}));signatures.Add(path,Signature(original));}finally{PrefabUtility.UnloadPrefabContents(original);}}
            foreach(string path in paths){
                var root=PrefabUtility.LoadPrefabContents(path);string before=signatures[path];var links=prefabLinks[path];int count;
                StableUIReferences.Restore(links,new[]{root});
                try{count=Convert(root);if(Signature(root)!=before)throw new Exception("Conversion changed existing UI or actions: "+path);if(count>0)PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
                root=PrefabUtility.LoadPrefabContents(path);
                try{int repaired=StableUIReferences.Restore(links,new[]{root});if(repaired>0)PrefabUtility.SaveAsPrefabAsset(root,path);if(Signature(root)!=before)throw new Exception("Reload changed UI or actions: "+path);reports.Add(path+" / converted "+count+" / rebound "+repaired+" / visuals and actions preserved");}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            foreach(string name in new[]{"MainMenu"}.Concat(Chapter4Game.Scenes)){
                var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+name+".unity");
                StableUIReferences.Restore(sceneLinks[name],scene.GetRootGameObjects());
                foreach(var root in scene.GetRootGameObjects()){
                    string before=Signature(root);int count=Convert(root);if(Signature(root)!=before)throw new Exception("Conversion changed scene UI or actions: "+name+"/"+root.name);
                    if(count>0)reports.Add(name+"/"+root.name+" / converted "+count+" / visuals and actions preserved");
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+name+".unity");
                int repaired=StableUIReferences.Restore(sceneLinks[name],scene.GetRootGameObjects());if(repaired>0){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
            }
            AssetDatabase.SaveAssets();Validate();Directory.CreateDirectory(Evidence);File.WriteAllLines(Path.Combine(Evidence,"prefab-conversion.txt"),reports);
        }
        public static void Validate()
        {
            int total=0;
            foreach(string name in new[]{"MainMenu"}.Concat(Chapter4Game.Scenes)){
                var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+name+".unity");
                foreach(var button in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Button>(true))){
                    if(!Shared(button))throw new Exception("Button is not a shared prefab instance: "+name+"/"+button.name);
                    if(button.onClick.GetPersistentEventCount()==0||button.onClick.GetPersistentTarget(0)!=button.GetComponent<ButtonAudio>()||button.onClick.GetPersistentMethodName(0)!="Play"||button.onClick.GetPersistentListenerState(0)==UnityEngine.Events.UnityEventCallState.Off)throw new Exception("Button sound event missing or ordered after gameplay: "+name+"/"+button.name);
                    var cue=new SerializedObject(button.GetComponent<ButtonAudio>()).FindProperty("cue");if(cue.prefabOverride)throw new Exception("Cue should inherit its shared prefab: "+button.name);
                    total++;
                }
            }
            Debug.Log("SHARED_BUTTON_PREFABS_PASS / "+total+" scene buttons inherit shared Button or HintButton prefab");
        }
        public static void ApplyAndBuild(){GameAudioAuthoring.Apply();Apply();ButtonSoundCoverageAuthoring.Audit();ManualEditingMigration.BuildQA();}
        public static void BuildRelease(){Validate();GameAudioAuthoring.BuildRelease();}
    }
}
#endif
