using System;
using System.Collections.Generic;
using UnityEngine;

namespace CalculusLabEscape
{
    // Stable IDs keep existing game rules readable while the actual objects can be
    // renamed, rearranged, or replaced through ordinary Inspector references.
    [DisallowMultipleComponent]
    public sealed class UIReferenceMap : MonoBehaviour
    {
        [Serializable]
        public sealed class Element
        {
            public string id;
            public Transform target;
        }

        [Tooltip("Saved UI references. IDs belong to the code; targets are editable scene/prefab objects.")]
        public Element[] elements = Array.Empty<Element>();

        public void CopyTo(Dictionary<string, Transform> destination)
        {
            destination.Clear();
            foreach (var element in elements)
            {
                if (element == null || string.IsNullOrEmpty(element.id) || element.target == null)
                    throw new InvalidOperationException("Missing UI reference on " + name + ". Check UI Reference Map in the Inspector.");
                if (!destination.TryAdd(element.id, element.target))
                    throw new InvalidOperationException("Duplicate UI reference ID: " + element.id);
            }
        }
    }
}
