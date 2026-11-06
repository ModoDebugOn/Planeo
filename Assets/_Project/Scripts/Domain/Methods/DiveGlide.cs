using System;
using System.Numerics;
using Gliding.Domain.Flight;

namespace Gliding.Domain.Methods
{
    /// <summary>
    /// Method 3, dive physics (Batman: Arkham City / Knight): the player chooses the angle of the
    /// nose, and the character flies where the nose points. Along that line, height and speed are
    /// traded like on a slide:
    ///
    ///     acceleration = gravity · sin(angle) − drag · speed²
    ///
    /// - Nose down (diving): gravity pushes along the path, speed grows. Height becomes speed.
    /// - Nose level or up: gravity holds it back, speed is spent. Speed becomes distance or height.
    /// - The cape needs speed to hold a flat nose: the slower it goes, the closer the nose is forced
    ///   to the normal glide. At the top of the jump there is no speed yet, so it starts steep.
    /// So hands off it glides steeply, diving all the way crashes, and pulling back (while keeping
    /// speed) goes far: the same jump ends very far or very short depending on how it is flown.
    /// </summary>
    public sealed class DiveGlide : IGlideModel
    {
        readonly IDiveControl control;

        /// <summary>Nose angle with no input, in degrees below the horizon.</summary>
        public float NeutralPitch { get; }

        /// <summary>Nose angle diving fully, in degrees below the horizon.</summary>
        public float DivePitch { get; }

        /// <summary>Nose angle pulling up fully, in degrees above the horizon.</summary>
        public float ClimbPitch { get; }

        /// <summary>Air drag: the higher, the sooner speed runs out (per metre).</summary>
        public float Drag { get; }

        /// <summary>Below this speed (m/s) the nose cannot be flatter than the normal glide.</summary>
        public float StallSpeed { get; }

        /// <summary>From this speed (m/s) on, the nose can go anywhere up to the full climb.</summary>
        public float ControlSpeed { get; }

        public DiveGlide(IDiveControl control, float neutralPitch = 20f, float divePitch = 70f, float climbPitch = 45f,
                         float drag = 0.02f, float stallSpeed = 8f, float controlSpeed = 20f)
        {
            this.control = control ?? throw new ArgumentNullException(nameof(control));
            if (drag < 0f) throw new ArgumentOutOfRangeException(nameof(drag));
            if (controlSpeed <= stallSpeed) throw new ArgumentOutOfRangeException(nameof(controlSpeed));
            NeutralPitch = neutralPitch;
            DivePitch = divePitch;
            ClimbPitch = climbPitch;
            Drag = drag;
            StallSpeed = stallSpeed;
            ControlSpeed = controlSpeed;
        }

        /// <summary>The nose angle the player asks for, in degrees (positive below the horizon).</summary>
        public float PitchFor(float dive)
        {
            dive = Math.Clamp(dive, -1f, 1f);
            return dive >= 0f
                ? NeutralPitch + dive * (DivePitch - NeutralPitch)
                : NeutralPitch + dive * (NeutralPitch + ClimbPitch);
        }

        /// <summary>The flattest (or highest) nose the cape can hold at this speed, in degrees.</summary>
        public float FlattestPitch(float speed)
        {
            float control01 = Math.Clamp((speed - StallSpeed) / (ControlSpeed - StallSpeed), 0f, 1f);
            return NeutralPitch - control01 * (NeutralPitch + ClimbPitch);
        }

        public FlightState Step(FlightState state, float gravity, float dt)
        {
            float speed = state.Velocity.Length();
            float pitch = Math.Max(PitchFor(control.Dive), FlattestPitch(speed));

            double radians = pitch * Math.PI / 180.0;
            float sin = (float)Math.Sin(radians), cos = (float)Math.Cos(radians);

            // The slide: gravity along the path, minus drag.
            speed += (gravity * sin - Drag * speed * speed) * dt;
            speed = Math.Max(speed, 0f);

            // It flies where the nose points.
            Vector3 velocity = (state.Facing * cos - Vector3.UnitY * sin) * speed;
            return new FlightState(state.Position + velocity * dt, velocity, state.Facing);
        }
    }
}
