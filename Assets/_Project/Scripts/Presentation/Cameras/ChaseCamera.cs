using Gliding.Domain.Flight;
using UnityEngine;

namespace Gliding.Presentation.Cameras
{
    /// <summary>
    /// Chase camera: behind the character and a little above, looking where it is flying. It does
    /// not stay level: it sits behind along the path, so in Batman's dive the shot tips down with
    /// him and in the pull-up it comes back level. The tilt is capped so the character always stays
    /// in frame. The shot for the jumps that should look spectacular; the side camera is the one
    /// for comparing.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ChaseCamera : MonoBehaviour
    {
        [Tooltip("Metres behind the character, along the way it flies.")]
        [SerializeField, Range(2f, 30f)] float distance = 9f;
        [Tooltip("Metres above that point.")]
        [SerializeField, Range(-5f, 15f)] float height = 2.5f;
        [Tooltip("It looks at a point this many metres ahead of the character, along the way it flies.")]
        [SerializeField, Range(0f, 20f)] float lookAhead = 6f;
        [Tooltip("Most the shot tips down or up following the flight, in degrees.")]
        [SerializeField, Range(0f, 80f)] float maxTilt = 50f;
        [Tooltip("Below this speed (m/s) it looks straight ahead: standing, running.")]
        [SerializeField, Range(0f, 10f)] float minSpeed = 1f;
        [Tooltip("Seconds to catch up. 0 = glued to the character.")]
        [SerializeField, Range(0f, 1f)] float smoothTime = 0.15f;
        [Tooltip("Seconds to travel to another character (the next one's turn).")]
        [SerializeField, Range(0.1f, 3f)] float switchTime = 0.8f;
        [Tooltip("If the followed point jumps more than this in one frame, it is another character: travel to it.")]
        [SerializeField, Range(1f, 50f)] float jumpDistance = 10f;

        System.Func<FlightState> source;
        Vector3 velocity;
        Vector3 lastTarget;
        bool snapped;
        float travelLeft;

        /// <summary>The character to chase: its feet, as the simulation tracks them.</summary>
        public void Bind(System.Func<FlightState> state) => source = state;

        // Switched on again: straight to the character, no flying in from where it was left.
        void OnEnable() => snapped = false;

        void LateUpdate()
        {
            if (source == null) return;
            var state = source();
            Vector3 centre = state.Position.ToUnity() + Vector3.up;
            Vector3 facing = state.Facing.ToUnity();

            // The way it flies, tilted as much as the flight but never more than maxTilt.
            Vector3 v = state.Velocity.ToUnity();
            float tilt = 0f;
            if (v.magnitude > minSpeed) tilt = Mathf.Atan2(-v.y, new Vector2(v.x, v.z).magnitude) * Mathf.Rad2Deg;
            tilt = Mathf.Clamp(tilt, -maxTilt, maxTilt);
            Vector3 path = Quaternion.AngleAxis(tilt, Vector3.Cross(Vector3.up, facing)) * facing;

            Vector3 position = centre - path * distance + Vector3.up * height;
            Quaternion rotation = Quaternion.LookRotation(centre + path * lookAhead - position);

            if (!snapped)
            {
                transform.SetPositionAndRotation(position, rotation);
                lastTarget = position;
                velocity = Vector3.zero;
                snapped = true;
                return;
            }

            // Another character's turn: a smooth trip there, easing back into normal chasing.
            if (Vector3.Distance(position, lastTarget) > jumpDistance) travelLeft = switchTime * 2f;
            lastTarget = position;
            travelLeft = Mathf.Max(0f, travelLeft - Time.deltaTime);
            float time = Mathf.Lerp(smoothTime, switchTime, travelLeft / (switchTime * 2f));

            transform.position = Vector3.SmoothDamp(transform.position, position, ref velocity, time);
            float t = time <= 0f ? 1f : 1f - Mathf.Exp(-Time.deltaTime * 4f / time);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, t);
        }
    }
}
