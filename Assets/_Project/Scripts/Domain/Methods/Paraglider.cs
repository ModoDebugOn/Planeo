using System;
using System.Numerics;
using Gliding.Domain.Air;
using Gliding.Domain.Flight;

namespace Gliding.Domain.Methods
{
    /// <summary>
    /// The Zelda extra (Breath of the Wild, paraglider): Spyro's fixed descent, plus the updrafts.
    /// Inside rising air it goes up instead of down. Like the fixed descent, it is a rule and not
    /// physics: the speed is set, never pushed.
    /// The stamina that runs out is a separate piece (<see cref="StaminaLimit"/>), so it can be
    /// flown with it or without it.
    /// </summary>
    public sealed class Paraglider : IGlideModel
    {
        readonly AirCurrents air;

        /// <summary>Speed along the ground, in m/s.</summary>
        public float ForwardSpeed { get; }

        /// <summary>Speed going down outside the updrafts, in m/s (positive).</summary>
        public float SinkSpeed { get; }

        /// <param name="air">The updrafts it can ride.</param>
        public Paraglider(float forwardSpeed, float sinkSpeed, AirCurrents air)
        {
            if (forwardSpeed < 0f) throw new ArgumentOutOfRangeException(nameof(forwardSpeed));
            if (sinkSpeed <= 0f) throw new ArgumentOutOfRangeException(nameof(sinkSpeed));
            ForwardSpeed = forwardSpeed;
            SinkSpeed = sinkSpeed;
            this.air = air ?? throw new ArgumentNullException(nameof(air));
        }

        public FlightState Step(FlightState state, float gravity, float dt)
        {
            // Like Spyro: the speed is set, not pushed. Only the vertical part depends on the air.
            float lift = air.LiftAt(state.Position);
            float vertical = lift > 0f ? lift : -SinkSpeed;
            Vector3 velocity = state.Facing * ForwardSpeed + Vector3.UnitY * vertical;
            return new FlightState(state.Position + velocity * dt, velocity, state.Facing);
        }
    }
}
