using Gliding.Domain.Air;
using Gliding.Domain.Flight;
using Gliding.Domain.Methods;
using UnityEngine;

namespace Gliding.Infrastructure.Profiles
{
    /// <summary>
    /// The Zelda extra (Link's paraglider): Spyro's fixed descent with updrafts that lift. With the
    /// same speeds as Spyro's profile, the jump starts identical and only the limits make the
    /// difference. The stamina that runs out is the "Tires" option every profile has.
    /// </summary>
    [CreateAssetMenu(menuName = "Gliding/Paraglider (Zelda)", fileName = "Paraglider")]
    public sealed class ParagliderProfile : GlideProfile
    {
        [Header("Paraglider")]
        [Tooltip("Speed along the ground while gliding, in m/s (Spyro's, to compare).")]
        [SerializeField, Range(0f, 30f)] float forwardSpeed = 10f;
        [Tooltip("Speed going down outside the updrafts, in m/s.")]
        [SerializeField, Range(0.5f, 15f)] float sinkSpeed = 2.5f;

        /// <summary>Without the scene's air: no updrafts.</summary>
        public override IGlideModel CreateModel() => CreateModel(new AirCurrents());

        /// <param name="air">The updrafts of the scene.</param>
        public IGlideModel CreateModel(AirCurrents air) => new Paraglider(forwardSpeed, sinkSpeed, air);
    }
}
