using System;
using System.Numerics;
using Gliding.Domain.Flight;

namespace Gliding.Domain.Methods
{
    /// <summary>
    /// Method 1, fixed descent (Spyro the Dragon): while gliding, the character moves forward at a
    /// fixed speed and sinks at a fixed speed.
    ///
    /// The trick: there is no physics at all. Gravity is ignored and the speed is not the result of
    /// any force; it is simply set, every step, to the same value. That is why the path is a straight
    /// line and why every glide from the same height ends at the same distance.
    /// </summary>
    public sealed class FixedDescent : IGlideModel
    {
        /// <summary>Speed along the ground, in m/s.</summary>
        public float ForwardSpeed { get; }

        /// <summary>Speed going down, in m/s (positive).</summary>
        public float SinkSpeed { get; }

        /// <summary>Metres forward for each metre down: the slope of the straight line.</summary>
        public float GlideRatio => ForwardSpeed / SinkSpeed;

        public FixedDescent(float forwardSpeed, float sinkSpeed)
        {
            if (forwardSpeed < 0f) throw new ArgumentOutOfRangeException(nameof(forwardSpeed));
            if (sinkSpeed <= 0f) throw new ArgumentOutOfRangeException(nameof(sinkSpeed));
            ForwardSpeed = forwardSpeed;
            SinkSpeed = sinkSpeed;
        }

        public FlightState Step(FlightState state, float gravity, float dt)
        {
            // Gravity is not used on purpose. Neither is the velocity it came in with: from the very
            // first step of the glide, the fall stops dead and the speed is the fixed one.
            Vector3 velocity = state.Facing * ForwardSpeed - Vector3.UnitY * SinkSpeed;
            return new FlightState(state.Position + velocity * dt, velocity, state.Facing);
        }
    }
}
