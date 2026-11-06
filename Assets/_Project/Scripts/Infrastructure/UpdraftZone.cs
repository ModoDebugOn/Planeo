using Gliding.Domain.Air;
using UnityEngine;

namespace Gliding.Infrastructure
{
    /// <summary>
    /// An updraft placed in the scene: a box from this object's position (the bottom centre) up to
    /// Size.y. The composition root reads every zone into the air of the simulation and gives each
    /// one its wind streaks.
    /// </summary>
    public sealed class UpdraftZone : MonoBehaviour
    {
        [Tooltip("Width (X), height (Y) and length along the lane (Z), in metres. Not rotated.")]
        [SerializeField] Vector3 size = new Vector3(10f, 45f, 20f);
        [Tooltip("Speed going up inside it, in m/s.")]
        [SerializeField, Range(0.5f, 30f)] float liftSpeed = 6f;

        public Vector3 Size => size;
        public float LiftSpeed => liftSpeed;

        /// <summary>The bottom centre of the box, in the world.</summary>
        public Vector3 Base => transform.position;

        public Updraft ToDomain()
        {
            var p = transform.position;
            return new Updraft(p.x - size.x / 2f, p.x + size.x / 2f, p.z - size.z / 2f, p.z + size.z / 2f,
                               p.y, p.y + size.y, liftSpeed);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * size.y / 2f, size);
        }
    }
}
