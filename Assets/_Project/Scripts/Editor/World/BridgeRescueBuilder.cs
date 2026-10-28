#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static partial class ProductionUIBuilder
    {
        static Transform RescueNode(Transform parent,string name,Vector3 position)
        {var node=new GameObject(name).transform;node.SetParent(parent,false);node.position=position;return node;}
        static Transform CowDetail(Transform parent,string name,Vector3 local,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Sphere)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent,false);go.transform.localPosition=local;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        static BridgeRescueSequence.AnimalRig RigCow(Transform root,Material pink)
        {
            var original=root.GetComponentsInChildren<Transform>().Where(t=>t!=root).ToArray();
            var legs=original.Where(t=>t.name==root.name+" leg").ToArray();
            var hooves=original.Where(t=>t.name==root.name+" hoof").ToArray();
            if(legs.Length!=4||hooves.Length!=4)throw new Exception("Expected four cow legs and hooves");
            float scale=legs[0].localScale.y/.8f;
            var head=root.Find(root.name+" head");var cream=head.GetComponent<Renderer>().sharedMaterial;
            var brown=hooves[0].GetComponent<Renderer>().sharedMaterial;
            var eye=root.Find(root.name+" eye").GetComponent<Renderer>().sharedMaterial;
            var headPivot=RescueNode(root,"Head pivot",root.TransformPoint(new Vector3(0,1.3f,-.62f)*scale));
            foreach(var part in original.Where(t=>t.name.EndsWith(" head")||t.name.EndsWith(" muzzle")||t.name.EndsWith(" ear")||t.name.EndsWith(" eye")))part.SetParent(headPivot,true);
            var pivots=new Transform[4];
            for(int i=0;i<4;i++)
            {
                var p=legs[i].position+root.up*.38f*scale;
                pivots[i]=RescueNode(root,(i<2?"Front":"Rear")+(i%2==0?" left":" right")+" leg pivot",p);
                legs[i].SetParent(pivots[i],true);hooves[i].SetParent(pivots[i],true);
            }
            for(int side=-1;side<=1;side+=2)
            {
                var nostril=CowDetail(root,"Nostril",new Vector3(side*.15f,1.27f,-1.508f)*scale,new Vector3(.075f,.05f,.025f)*scale,eye);nostril.SetParent(headPivot,true);
                var inner=CowDetail(root,"Inner ear",new Vector3(side*.48f,1.66f,-1.087f)*scale,new Vector3(.21f,.065f,.025f)*scale,pink);inner.SetParent(headPivot,true);
                if(root.name=="Mother")
                {
                    var horn=CowDetail(root,"Horn",new Vector3(side*.25f,1.87f,-.92f)*scale,new Vector3(.075f,.24f,.075f)*scale,cream,PrimitiveType.Cylinder);horn.localRotation=Quaternion.Euler(0,0,-side*18);horn.SetParent(headPivot,true);
                }
            }
            var tail=RescueNode(root,"Tail pivot",root.TransformPoint(new Vector3(0,1.26f,.79f)*scale));
            CowDetail(tail,"Tail",new Vector3(0,-.26f,.08f)*scale,new Vector3(.065f,.53f,.065f)*scale,cream,PrimitiveType.Cylinder);
            CowDetail(tail,"Tail tuft",new Vector3(0,-.53f,.08f)*scale,new Vector3(.13f,.17f,.14f)*scale,brown);
            return new BridgeRescueSequence.AnimalRig{root=root,head=headPivot,tail=tail,legs=pivots,homePosition=root.position,homeRotation=root.rotation,headRest=headPivot.localRotation,tailRest=tail.localRotation,legRest=pivots.Select(p=>p.localRotation).ToArray()};
        }
        [MenuItem("Calculus Lab/Production/Upgrade bridge rescue presentation")]
        public static void UpgradeBridgeRescue()
        {
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");
            var world=Object.FindFirstObjectByType<Chapter4World>();
            var renderers=world.GetComponentsInChildren<Renderer>(true);var poses=renderers.Select(r=>r.transform.localToWorldMatrix).ToArray();
            var rescue=world.GetComponent<BridgeRescueSequence>();
            if(rescue==null)
            {
                var pink=new Material(Shader.Find("Standard")){name="Cow inner ears",color=new Color(.65f,.38f,.3f)};pink.SetFloat("_Glossiness",.05f);
                string folder="Assets/_Project/Art/AuthoredWorlds/"+Chapter4Game.Scenes[1];
                AssetDatabase.CreateAsset(pink,AssetDatabase.GenerateUniqueAssetPath(folder+"/Cow_inner_ears.mat"));
                rescue=world.gameObject.AddComponent<BridgeRescueSequence>();rescue.calf=RigCow(world.calf,pink);rescue.mother=RigCow(world.mother,pink);
                var parent=RescueNode(world.transform.Find("Animals"),"Rescue sequence",Vector3.zero);
                rescue.route=new[]{RescueNode(parent,"01 - Start",world.calf.position),RescueNode(parent,"02 - Bridge entrance",new Vector3(0,0,6.45f)),RescueNode(parent,"03 - Far bank",new Vector3(0,0,12.65f)),RescueNode(parent,"04 - Reunion",world.mother.position+new Vector3(-1.8f,0,-1.15f))};
                rescue.cameraPose=RescueNode(parent,"Rescue camera",new Vector3(7,5,6));rescue.cameraTarget=RescueNode(parent,"Camera target",new Vector3(0,.7f,11));
                var camera=world.player.GetComponentInChildren<Camera>().transform;rescue.playerCameraHomePosition=camera.localPosition;rescue.playerCameraHomeRotation=camera.localRotation;
                for(int i=0;i<renderers.Length;i++)if(!MatrixClose(renderers[i].transform.localToWorldMatrix,poses[i]))throw new Exception("Rig upgrade moved existing art: "+renderers[i].name);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            string prefabPath=PrefabsPath+"/DifferentiationUI.prefab";var prefab=PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var nodes=prefab.GetComponentsInChildren<Transform>(true);var card=(RectTransform)nodes.First(t=>t.name=="FinishingCard");
                // Only migrate the previous generated layout; later authored edits are preserved.
                if(Mathf.Approximately(card.sizeDelta.y,146))
                {
                    card.anchoredPosition=new Vector2(165,-550);card.sizeDelta=new Vector2(950,105);
                    var title=nodes.First(t=>t.name=="FinishingTitle").GetComponent<TMP_Text>();title.rectTransform.anchoredPosition=new Vector2(27,-12);title.rectTransform.sizeDelta=new Vector2(899,41);title.fontSize=29;
                    var text=nodes.First(t=>t.name=="FinishingText").GetComponent<TMP_Text>();text.rectTransform.anchoredPosition=new Vector2(28,-58);text.fontSize=21;
                }
                PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            AssetDatabase.SaveAssets();
            ValidateBridgeRescue();
            Debug.Log("BRIDGE_RESCUE_UPGRADE_PASS");
        }
        static bool MatrixClose(Matrix4x4 a,Matrix4x4 b){for(int i=0;i<16;i++)if(Mathf.Abs(a[i]-b[i])>.0001f)return false;return true;}
        public static void ValidateBridgeRescue()
        {
            var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");var world=Object.FindFirstObjectByType<Chapter4World>();var rescue=world.GetComponent<BridgeRescueSequence>();
            if(rescue==null||rescue.calf.legs.Length!=4||rescue.mother.legs.Length!=4||rescue.route.Length!=4)throw new Exception("Missing persisted rescue rig");
            for(int i=0;i<=100;i++)
            {
                rescue.Sample(i/100f,false);var p=world.calf.position;
                if(p.z>6.7f&&p.z<12.3f&&(Mathf.Abs(p.x)>.01f||p.y<rescue.deckHeight-.001f))throw new Exception("Calf leaves deck centre or sinks below deck");
            }
            rescue.ResetPose();
            string folder=Path.GetFullPath("../../qa/editor-rescue");Directory.CreateDirectory(folder);var canvas=Object.FindFirstObjectByType<Canvas>();
            foreach(string view in new[]{"Rescue crossing","Rescue reunion"})
            {SceneAuthoringWindow.ApplyView(world,canvas,view);foreach(var label in canvas.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();if(label.isTextOverflowing)throw new Exception("Rescue text overflow: "+label.name);}SceneAuthoring.Capture(world,canvas,Path.Combine(folder,view.Replace(" ","-")+".png"),false);}
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");
            File.WriteAllText(Path.Combine(folder,"result.txt"),"PASS: persisted four-leg rigs; 101 route samples keep calf on deck; crossing/reunion preview renders without a gameplay session; text fits; scene reloaded after preview.\n");
            SceneAuthoring.ValidateAndCaptureTo(Path.GetFullPath("../../qa/editor-rescue-all"));
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");
        }
        public static void UpgradeBridgeRescueAndBuild(){UpgradeBridgeRescue();Build();}
    }

    [CustomEditor(typeof(BridgeRescueSequence))]
    public sealed class BridgeRescueSequenceInspector:UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();if(Application.isPlaying)return;
            var sequence=(BridgeRescueSequence)target;
            EditorGUILayout.Space();EditorGUILayout.LabelField("Rescue preview (no Play)",EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();float next=EditorGUILayout.Slider("Progress",sequence.Progress,0,1);
            if(EditorGUI.EndChangeCheck())
            {
                Undo.RegisterFullObjectHierarchyUndo(sequence.gameObject,"Preview calf rescue");
                sequence.GetComponent<Chapter4World>().EditorRescueState(true);sequence.Sample(next);
                EditorSceneManager.MarkSceneDirty(sequence.gameObject.scene);SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();
            }
            if(GUILayout.Button("Reset animal and camera poses")){Undo.RegisterFullObjectHierarchyUndo(sequence.gameObject,"Reset rescue preview");sequence.ResetPose();sequence.GetComponent<Chapter4World>().EditorRescueState(false);EditorSceneManager.MarkSceneDirty(sequence.gameObject.scene);}
        }
    }
}
#endif
