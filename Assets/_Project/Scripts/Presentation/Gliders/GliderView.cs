using Gliding.Domain.Flight;
using UnityEngine;

namespace Gliding.Presentation.Gliders
{
    /// <summary>
    /// Puts a character where its flight state says, facing where it faces, and leaning into the
    /// direction it flies: forward when it goes down, back when it goes up (Batman's dive and
    /// pull-up read at a glance). It only reads: whoever binds it decides which glider it shows.
    /// </summary>
    public sealed class GliderView : MonoBehaviour
    {
        [Tooltip("From the feet (what the simulation tracks) to the pivot of the model. 1 for Unity's capsule.")]
        [SerializeField] float feetToPivot = 1f;
        [Tooltip("How much of the flight angle the body follows: 1 = all of it, 0 = always upright.")]
        [SerializeField, Range(0f, 1f)] float lean = 1f;
        [Tooltip("Most it leans, in degrees (forward or back).")]
        [SerializeField, Range(0f, 90f)] float maxLean = 75f;
        [Tooltip("How quickly the lean follows the flight (higher = snappier).")]
        [SerializeField, Range(1f, 30f)] float leanSpeed = 8f;
        [Tooltip("Below this speed (m/s) it stands upright: standing, landed.")]
        [SerializeField, Range(0f, 5f)] float minSpeed = 0.5f;

        System.Func<FlightState> source;
        float currentLean;

        public void Bind(System.Func<FlightState> state) => source = state;

        void LateUpdate()
        {
            if (source == null) return;
            var state = source();
            var velocity = state.Velocity.ToUnity();

            // The angle of the flight below the horizon (positive going down).
            float flight = 0f;
            float horizontal = new Vector2(velocity.x, velocity.z).magnitude;
            if (velocity.magnitude > minSpeed) flight = Mathf.Atan2(-velocity.y, horizontal) * Mathf.Rad2Deg;
            float target = Mathf.Clamp(flight * lean, -maxLean, maxLean);
            currentLean = Mathf.Lerp(currentLean, target, 1f - Mathf.Exp(-leanSpeed * Time.deltaTime));

            // Positive X rotation tips the top of the capsule forward.
            var rotation = Quaternion.LookRotation(state.Facing.ToUnity()) * Quaternion.Euler(currentLean, 0f, 0f);
            transform.SetPositionAndRotation(state.Position.ToUnity() + Vector3.up * feetToPivot, rotation);
        }
    }
}
