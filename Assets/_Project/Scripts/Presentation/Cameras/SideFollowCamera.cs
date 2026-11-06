using Gliding.Domain.Flight;
using UnityEngine;

namespace Gliding.Presentation.Cameras
{
    /// <summary>
    /// Side camera with two shots:
    /// - Follow: beside the character, close, keeping it in frame from the run-up to the landing.
    ///   It never turns (always looking the same way, across the lane), so the ground lines and the
    ///   distance signs slide past behind it: that is what shows the speed.
    /// - Overview: the wide shot of the whole lane, where it was placed in the scene. For comparing
    ///   the trajectories once everybody has landed.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class SideFollowCamera : MonoBehaviour
    {
        [SerializeField] bool follow = true;

        [Header("Follow")]
        [Tooltip("Metres to the side of the character (along +X, the camera looks towards -X).")]
        [SerializeField, Range(5f, 80f)] float distance = 18f;
        [Tooltip("Metres above the character's centre.")]
        [SerializeField, Range(-10f, 20f)] float height = 2f;
        [Tooltip("Metres ahead of the character (+Z): leaves room in front, where it is going.")]
        [SerializeField, Range(-20f, 20f)] float lead = 4f;
        [Tooltip("Degrees looking down.")]
        [SerializeField, Range(-30f, 45f)] float pitch = 5f;
        [Tooltip("Seconds to catch up. 0 = glued to the character.")]
        [SerializeField, Range(0f, 2f)] float smoothTime = 0.25f;

        [Header("Travelling")]
        [Tooltip("Seconds to travel between the two shots, or back to the ledge when the followed " +
                 "character changes (the next one's turn).")]
        [SerializeField, Range(0.1f, 3f)] float switchTime = 0.8f;
        [Tooltip("If the followed point jumps more than this in one frame, it is another character: travel to it.")]
        [SerializeField, Range(1f, 50f)] float jumpDistance = 10f;

        System.Func<FlightState> source;
        Vector3 overviewPosition;
        Quaternion overviewRotation;
        Vector3 velocity;
        Vector3 lastTarget;
        bool snapped;
        float travelLeft;   // seconds of trip left; while > 0 it moves with the slower switch time

        /// <summary>True: following the character. False: the overview.</summary>
        public bool Follow
        {
            get => follow;
            set => follow = value;
        }

        /// <summary>The character to follow: its feet, as the simulation tracks them.</summary>
        public void Bind(System.Func<FlightState> state) => source = state;

        void Awake()
        {
            // Where the scene placed it is the overview shot.
            overviewPosition = transform.position;
            overviewRotation = transform.rotation;
        }

        void LateUpdate()
        {
            Vector3 position;
            Quaternion rotation;
            float time;
            if (follow && source != null)
            {
                Vector3 centre = source().Position.ToUnity() + Vector3.up;
                position = centre + new Vector3(distance, height, lead);
                rotation = Quaternion.Euler(pitch, -90f, 0f);   // looking towards -X

                // Another character's turn: a smooth trip there instead of a jump cut. The trip
                // lasts a fixed time and eases back into normal following (the character may
                // already be moving, so "until it gets there" could never end).
                if (snapped && Vector3.Distance(position, lastTarget) > jumpDistance) travelLeft = switchTime * 2f;
                lastTarget = position;
                travelLeft = Mathf.Max(0f, travelLeft - Time.deltaTime);
                time = Mathf.Lerp(smoothTime, switchTime, travelLeft / (switchTime * 2f));
            }
            else
            {
                position = overviewPosition;
                rotation = overviewRotation;
                time = switchTime;
            }

            if (!snapped)
            {
                // First frame: straight there, no flying in from the overview.
                transform.SetPositionAndRotation(position, rotation);
                lastTarget = position;
                snapped = true;
                return;
            }
            transform.position = Vector3.SmoothDamp(transform.position, position, ref velocity, time);
            float t = time <= 0f ? 1f : 1f - Mathf.Exp(-Time.deltaTime * 4f / time);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, t);
        }
    }
}
