using UnityEngine;

namespace CalculusLabEscape
{
    public sealed class CalculusTerminal : MonoBehaviour
    {
        public int questionIndex;
        public bool completed;

        private void OnDrawGizmos()
        {
            Gizmos.color = completed ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 1.2f);
        }
    }
}
