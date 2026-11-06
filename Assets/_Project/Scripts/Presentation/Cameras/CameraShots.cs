using Gliding.Domain.Flight;
using UnityEngine;

namespace Gliding.Presentation.Cameras
{
    /// <summary>
    /// The three shots of the demo, one after another with the same key:
    /// side (following the character) → overview (the whole lane) → chase (behind it) → side…
    /// Only one camera is on at a time. Both are tagged MainCamera, so whatever faces the camera
    /// (arrow heads, the stamina wheel) faces the one that is on.
    /// </summary>
    public sealed class CameraShots : MonoBehaviour
    {
        public enum Shot { Side, Overview, Chase }

        [SerializeField] SideFollowCamera side;
        [SerializeField] ChaseCamera chase;
        [SerializeField] Shot shot = Shot.Side;

        public Shot Current => shot;

        /// <summary>The character both cameras look at.</summary>
        public void Bind(System.Func<FlightState> state)
        {
            if (side != null) side.Bind(state);
            if (chase != null) chase.Bind(state);
        }

        public void Next() => Show((Shot)(((int)shot + 1) % 3));

        public void Show(Shot next)
        {
            shot = next;
            Apply();
        }

        void Start() => Apply();

        void Apply()
        {
            bool chasing = shot == Shot.Chase && chase != null;
            if (side != null)
            {
                side.Follow = shot != Shot.Overview;
                side.GetComponent<Camera>().enabled = !chasing;
            }
            if (chase != null) chase.gameObject.SetActive(chasing);
        }
    }
}
