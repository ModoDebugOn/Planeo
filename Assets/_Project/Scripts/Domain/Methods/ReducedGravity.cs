using System;
using System.Numerics;
using Gliding.Domain.Flight;

namespace Gliding.Domain.Methods
{
    /// <summary>
    /// Method 2, slowed fall (Super Mario 3D Land / 3D World, tanuki suit): while gliding, gravity
    /// still pulls, only weaker. Nothing else changes.
    ///
    /// The opposite of the fixed descent: the speed is never set, it is the result of a force. So the
    /// character keeps the horizontal speed it jumped with and keeps falling faster and faster, only
    /// more slowly than without the suit. The path is a curve that gets steeper as it goes down.
    /// </summary>
    public sealed class ReducedGravity : IGlideModel
    {
        /// <summary>Fraction of the world's gravity that still pulls: 1 = no suit, 0 = floating.</summary>
        public float GravityScale { get; }

        public ReducedGravity(float gravityScale)
        {
            if (gravityScale < 0f || gravityScale > 1f) throw new ArgumentOutOfRangeException(nameof(gravityScale));
            GravityScale = gravityScale;
        }

        public FlightState Step(FlightState state, float gravity, float dt)
        {
            // Plain gravity, just weaker. The velocity it came in with is kept and only bent downwards.
            Vector3 velocity = state.Velocity - Vector3.UnitY * (gravity * GravityScale * dt);
            return new FlightState(state.Position + velocity * dt, velocity, state.Facing);
        }
    }
}
