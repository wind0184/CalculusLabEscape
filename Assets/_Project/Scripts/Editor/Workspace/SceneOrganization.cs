#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    public static class SceneOrganization
    {
        static readonly string[] RootGroups={"Environment","Bridge","Repair devices","Animals","Lighting","Room","Containment device","Portal","Examination console","Status indicators","Boundaries","Guide zones"};

        public static void Organize(Chapter4World world)
        {
            var original=world.transform.Cast<Transform>().ToArray();
            var groups=new Dictionary<string,Transform>();
            Transform Group(string path)
            {
                if(groups.TryGetValue(path,out var existing))return existing;
                int split=path.LastIndexOf('/');var parent=split<0?world.transform:Group(path.Substring(0,split));
                string name=split<0?path:path.Substring(split+1);var result=parent.Find(name);
                if(result==null){result=new GameObject(name).transform;result.SetParent(parent,false);}
                groups[path]=result;return result;
            }
            var serialized=new SerializedObject(world);
            var repairSigns=serialized.FindProperty("repairSigns");
            if(world.topic==1)for(int i=0;i<repairSigns.arraySize;i++)
            {var sign=(TextMesh)repairSigns.GetArrayElementAtIndex(i).objectReferenceValue;sign.gameObject.name=Chapter4World.RepairDeviceNames[i]+" sign";}
            string ClosestDevice(Transform item)
            {
                int index=Enumerable.Range(0,world.devices.Count).OrderBy(i=>(item.position-world.devices[i].position).sqrMagnitude).First();
                return world.topic==1?"Repair devices/"+Chapter4World.RepairDeviceNames[index]:"Containment device/"+new[]{"Exponent dial","Wire matching","Symbol sequence","Pressure gauge"}[index];
            }
            foreach(var item in original)
            {
                string name=item.name,path;
                // These are direct-child runtime lookups; their names and parents stay accessible.
                if(item==world.player.transform||name=="Storyboard view"||RootGroups.Contains(name))continue;
                if(item.GetComponent<Light>()!=null)path="Lighting";
                else if(name.Contains("boundary",StringComparison.OrdinalIgnoreCase))path="Boundaries";
                else if(world.topic==1)
                {
                    if(item==world.calf||item==world.mother)path="Animals";
                    else if(name.StartsWith("Pine",StringComparison.Ordinal))path="Environment/Trees";
                    else if(name.StartsWith("Bridge",StringComparison.Ordinal))path="Bridge";
                    else if(item==world.water)path="Environment/River";
                    else if(name.StartsWith("Trail",StringComparison.Ordinal)||name.StartsWith("Bank rock",StringComparison.Ordinal)||name.StartsWith("Grass",StringComparison.Ordinal))path="Environment/Path and banks";
                    else if(world.devices.Contains(item)||item.GetComponent<TextMesh>()!=null||name.StartsWith("Power Core",StringComparison.Ordinal)||name.StartsWith("Converter",StringComparison.Ordinal)||name.StartsWith("Builder",StringComparison.Ordinal)||name.StartsWith("Station",StringComparison.Ordinal)||name=="Repair glow")path=ClosestDevice(item);
                    else path="Environment";
                }
                else if(world.topic==2)
                {
                    int module=world.devices.IndexOf(item);
                    int indicator=world.indicators.FindIndex(r=>r.transform==item);
                    if(module>=0||indicator>=0)path="Containment device/"+new[]{"Exponent dial","Wire matching","Symbol sequence","Pressure gauge"}[module>=0?module:indicator];
                    else if(name.StartsWith("Exponent needle",StringComparison.Ordinal)||name.StartsWith("Divisor needle",StringComparison.Ordinal)||name=="DIAL")path="Containment device/Exponent dial";
                    else if(name.StartsWith("Accepted wire",StringComparison.Ordinal)||name.StartsWith("Wire socket",StringComparison.Ordinal)||name=="Common constant socket"||name=="WIRES")path="Containment device/Wire matching";
                    else if(name.StartsWith("Procedure card",StringComparison.Ordinal)||name=="STEPS")path="Containment device/Symbol sequence";
                    else if(name.StartsWith("Pressure",StringComparison.Ordinal)||name=="GAUGE")path="Containment device/Pressure gauge";
                    else if(name.StartsWith("Master",StringComparison.Ordinal)||name.StartsWith("Device power bar",StringComparison.Ordinal))path="Containment device/Master deactivation";
                    else if(name=="Module screw")path=ClosestDevice(item);
                    else if(item==world.station||name.StartsWith("Case",StringComparison.Ordinal)||name=="Hinged case lid"||name=="Console screen"||name=="03 / CONTAINMENT CASE")path="Containment device/Case shell";
                    else path="Room";
                }
                else
                {
                    if(name.StartsWith("Portal",StringComparison.Ordinal)||name=="Activation lever")path="Portal";
                    else if(item==world.station||name=="Console screen"||name=="EXAMINATION CONSOLE")path="Examination console";
                    else if(world.indicators.Any(r=>r.transform==item))path="Status indicators";
                    else path="Room";
                }
                item.SetParent(Group(path),true);
            }
        }

        public static void OrganizeAndBuild()
        {
            string evidence=Path.GetFullPath("../../qa/editor-workspace");Directory.CreateDirectory(evidence);
            var report=new List<string>();
            for(int topic=0;topic<3;topic++)
            {
                var scene=EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[topic]+".unity");
                var world=Object.FindFirstObjectByType<Chapter4World>();if(world==null||!world.IsAuthored)throw new Exception("An authored world is required: "+scene.path);
                var renderers=world.GetComponentsInChildren<Renderer>(true);
                var matrices=renderers.ToDictionary(r=>r.GetInstanceID(),r=>r.transform.localToWorldMatrix);
                var colliders=world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(id=>id).ToArray();
                string references=JsonUtility.ToJson(world);
                Organize(world);
                int count=world.GetComponentsInChildren<Transform>(true).Length;
                Organize(world);
                if(count!=world.GetComponentsInChildren<Transform>(true).Length)throw new Exception("Repeated organization added extra groups");
                foreach(var renderer in renderers)
                {
                    var before=matrices[renderer.GetInstanceID()];var after=renderer.transform.localToWorldMatrix;
                    for(int i=0;i<16;i++)if(Mathf.Abs(before[i]-after[i])>.0001f)throw new Exception("Organization moved/scaled a renderer: "+renderer.name);
                }
                if(references!=JsonUtility.ToJson(world)||!colliders.SequenceEqual(world.GetComponentsInChildren<Collider>(true).Select(c=>c.GetInstanceID()).OrderBy(id=>id)))throw new Exception("Organization changed gameplay references or colliders");
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                report.Add(Chapter4Game.Scenes[topic]+": "+renderers.Length+" renderers unchanged; collider identities and gameplay references unchanged; repeated organization stable.");
            }
            File.WriteAllLines(Path.Combine(evidence,"organization.txt"),report);
            SceneAuthoring.ValidateAndCaptureTo(evidence);
            EditorSceneManager.OpenScene(SceneAuthoring.SceneFolder+Chapter4Game.Scenes[1]+".unity");
            ProductionUIBuilder.Build();
        }
    }
}
#endif
