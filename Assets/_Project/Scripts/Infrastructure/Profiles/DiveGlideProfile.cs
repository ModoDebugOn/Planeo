using System;
using Gliding.Domain.Flight;
using Gliding.Domain.Methods;
using UnityEngine;

namespace Gliding.Infrastructure.Profiles
{
    /// <summary>
    /// Method 3, dive physics (Batman): the player tilts the nose and height and speed are traded.
    /// It is the only method that listens to the player, so it is the only one that receives the
    /// dive control (the composition root passes it in). With nobody at the controls, its autopilot
    /// flies the textbook move: dive to gain speed, then pull back.
    /// </summary>
    [CreateAssetMenu(menuName = "Gliding/Dive Glide (Batman)", fileName = "DiveGlide")]
    public sealed class DiveGlideProfile : GlideProfile
    {
        [Header("Physics")]
        [Tooltip("Nose angle hands off, in degrees below the horizon.")]
        [SerializeField, Range(0f, 60f)] float neutralPitch = 20f;
        [Tooltip("Nose angle diving fully, in degrees below the horizon.")]
        [SerializeField, Range(30f, 90f)] float divePitch = 70f;
        [Tooltip("Nose angle pulling back fully, in degrees above the horizon.")]
        [SerializeField, Range(0f, 60f)] float climbPitch = 45f;
        [Tooltip("Air drag: the higher, the sooner speed runs out.")]
        [SerializeField, Range(0.005f, 0.1f)] float drag = 0.02f;
        [Tooltip("Below this speed (m/s) the nose cannot be flatter than hands off.")]
        [SerializeField, Range(0f, 20f)] float stallSpeed = 8f;
        [Tooltip("From this speed (m/s) on, the nose can go anywhere.")]
        [SerializeField, Range(5f, 40f)] float controlSpeed = 20f;

        [Header("Autopilot (when nobody touches W / S)")]
        [Tooltip("Seconds diving fully after the glide opens.")]
        [SerializeField, Range(0f, 5f)] float diveSeconds = 1.5f;
        [Tooltip("The stick after the dive: -1 = pull back fully, 0 = hands off.")]
        [SerializeField, Range(-1f, 1f)] float then = -1f;

        /// <summary>With nobody at the controls and no autopilot: always hands off.</summary>
        public override IGlideModel CreateModel() => Create(new DiveStick());

        /// <summary>Flown by the autopilot, unless the player's stick is touched.</summary>
        /// <param name="hand">The player's stick.</param>
        /// <param name="glideTime">Seconds since this glider's glide opened.</param>
        public IGlideModel CreateModel(IDiveControl hand, Func<float> glideTime) =>
            Create(new DiveAutopilot(hand, glideTime, diveSeconds, then));

        IGlideModel Create(IDiveControl control) =>
            new DiveGlide(control, neutralPitch, divePitch, climbPitch, drag, stallSpeed, Mathf.Max(controlSpeed, stallSpeed + 1f));
    }
}
