using UnityEngine;

namespace CalculusLabEscape
{
    [DisallowMultipleComponent,RequireComponent(typeof(BoxCollider))]
    public sealed class MissionGuideZone:MonoBehaviour
    {
        public Transform target;
        public string deviceName;
        public bool Contains(Vector3 position)=>GetComponent<BoxCollider>().bounds.Contains(position);
    }
}
