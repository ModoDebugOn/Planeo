using Gliding.Domain.Flight;
using Gliding.Domain.Methods;
using UnityEngine;

namespace Gliding.Infrastructure.Profiles
{
    /// <summary>Method 2, slowed fall (Mario tanuki): the same gravity as the jump, only weaker.</summary>
    [CreateAssetMenu(menuName = "Gliding/Reduced Gravity (Mario tanuki)", fileName = "ReducedGravity")]
    public sealed class ReducedGravityProfile : GlideProfile
    {
        [Tooltip("Fraction of the jump's gravity that still pulls while gliding. 1 = no suit, 0 = floating.")]
        [SerializeField, Range(0f, 1f)] float gravityScale = 0.15f;

        public override IGlideModel CreateModel() => new ReducedGravity(gravityScale);
    }
}
