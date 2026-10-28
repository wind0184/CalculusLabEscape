#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CalculusLabEscape.Editor
{
    // Nested-prefab conversion can replace serialized local IDs. Resolve saved links by hierarchy.
    internal static class StableUIReferences
    {
        internal sealed class Link
        {
            public string owner,target,ownerType,targetType,property;
            public int ownerIndex,targetIndex;
        }
        static string Path(Transform node,GameObject[] roots)
        {
            var indices=new List<int>();while(node.parent!=null){indices.Add(node.GetSiblingIndex());node=node.parent;}
            int root=Array.FindIndex(roots,r=>r.transform==node);if(root<0)return null;
            indices.Reverse();return root+"/"+string.Join("/",indices);
        }
        static Transform Resolve(string path,GameObject[] roots)
        {
            var indices=path.Split('/');var node=roots[int.Parse(indices[0])].transform;
            foreach(string index in indices.Skip(1))if(index.Length>0)node=node.GetChild(int.Parse(index));return node;
        }
        static int Index(Component component)=>Array.IndexOf(component.GetComponents(component.GetType()),component);
        internal static List<Link> Capture(GameObject[] roots)
        {
            var links=new List<Link>();
            foreach(var component in roots.SelectMany(root=>root.GetComponentsInChildren<Component>(true))){
                if(component==null||component is Transform)continue;
                var data=new SerializedObject(component);var property=data.GetIterator();
                while(property.Next(true)){
                    if(property.propertyType!=SerializedPropertyType.ObjectReference||property.propertyPath=="m_GameObject"||property.propertyPath=="m_Script")continue;
                    var value=property.objectReferenceValue;var target=value is GameObject go?go.transform:value is Component other?other.transform:null;if(target==null)continue;
                    string targetPath=Path(target,roots);if(targetPath==null)continue;
                    links.Add(new Link{owner=Path(component.transform,roots),ownerType=component.GetType().AssemblyQualifiedName,ownerIndex=Index(component),property=property.propertyPath,target=targetPath,targetType=value is Component targetComponent?targetComponent.GetType().AssemblyQualifiedName:null,targetIndex=value is Component item?Index(item):0});
                }
            }
            return links;
        }
        internal static int Restore(List<Link> links,GameObject[] roots)
        {
            int repaired=0;
            foreach(var link in links){
                var owner=Resolve(link.owner,roots).GetComponents(Type.GetType(link.ownerType))[link.ownerIndex];
                var node=Resolve(link.target,roots);Object target=link.targetType==null?(Object)node.gameObject:node.GetComponents(Type.GetType(link.targetType))[link.targetIndex];
                var data=new SerializedObject(owner);var property=data.FindProperty(link.property);if(property==null)throw new Exception("Converted UI property missing: "+link.property);
                if(property.objectReferenceValue==target)continue;
                property.objectReferenceValue=target;data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(owner);
                if(PrefabUtility.IsPartOfPrefabInstance(owner))PrefabUtility.RecordPrefabInstancePropertyModifications(owner);repaired++;
            }
            return repaired;
        }
    }
}
#endif
