using UnityEngine;

namespace CalculusLabEscape
{
    public sealed class MovingPowerSocket : MonoBehaviour
    {
        [SerializeField] private float originX = -7f;
        [SerializeField] private float unitsPerMetre = 0.7f;
        [SerializeField] private float sampleTime = 2f;

        public float SampleTime => sampleTime;
        public float PositionMetres(float time) => 0.5f * time * time + 2f * time;
        public float VelocityMetresPerSecond(float time) => time + 2f;
        public Vector3 WorldPositionAt(float time)
        {
            Vector3 position = transform.position;
            position.x = originX + PositionMetres(time) * unitsPerMetre;
            return position;
        }

        public void SetSimulationTime(float time)
        {
            transform.position = WorldPositionAt(time);
        }
    }
}
