using Gliding.Domain.Flight;
using Gliding.Domain.Methods;
using UnityEngine;

namespace Gliding.Infrastructure.Profiles
{
    /// <summary>Method 1, fixed descent (Spyro): a speed forward, a speed down, and that's it.</summary>
    [CreateAssetMenu(menuName = "Gliding/Fixed Descent (Spyro)", fileName = "FixedDescent")]
    public sealed class FixedDescentProfile : GlideProfile
    {
        [Tooltip("Speed along the ground while gliding, in m/s.")]
        [SerializeField, Range(0f, 30f)] float forwardSpeed = 10f;
        [Tooltip("Speed going down while gliding, in m/s. Forward / down = metres flown per metre lost.")]
        [SerializeField, Range(0.5f, 15f)] float sinkSpeed = 2.5f;

        public override IGlideModel CreateModel() => new FixedDescent(forwardSpeed, sinkSpeed);
    }
}
