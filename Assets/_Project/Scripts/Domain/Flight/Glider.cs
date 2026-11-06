using System;
using System.Numerics;
using Gliding.Domain.Terrain;

namespace Gliding.Domain.Flight
{
    /// <summary>
    /// A character that jumps off the ledge and glides. It runs the phases every method shares
    /// (run-up, jump, landing) and hands the glide itself to its <see cref="Model"/>, which can be
    /// swapped at any moment: same character, same jump, another way of gliding.
    /// </summary>
    public sealed class Glider
    {
        /// <summary>How far below the feet the ground may drop and still count as running on it
        /// (a step down). Further than that, the ground has ended: jump.</summary>
        public const float StepDown = 0.5f;

        /// <summary>Seconds between the points of the trajectory.</summary>
        public const float SampleEvery = 0.05f;

        readonly FlightState start;
        float nextSample;

        public string Name { get; }

        /// <summary>The way of gliding. Swap it to try another method from the same jump.</summary>
        public IGlideModel Model { get; set; }

        /// <summary>
        /// Optional: with it, gliding spends one unit per second and, when it runs out, the
        /// character lets go and falls with plain gravity (Zelda's paraglider). Null: it never tires.
        /// It belongs to the character, not to the method: any way of gliding can tire.
        /// </summary>
        public Stamina Stamina { get; set; }

        public FlightState State { get; private set; }
        public FlightPhase Phase { get; private set; }

        /// <summary>Seconds since the run-up started.</summary>
        public float Time { get; private set; }

        /// <summary>Seconds since the glide opened (0 before it opens).</summary>
        public float GlideTime => glideOpenedAt is float opened ? Time - opened : 0f;
        float? glideOpenedAt;

        /// <summary>The path flown in the last jump (kept after landing).</summary>
        public Trajectory Trajectory { get; } = new Trajectory();

        /// <summary>Where the glide opened, if it has.</summary>
        public Vector3? GlideStart { get; private set; }

        /// <summary>Where it touched down or hit something, if it has.</summary>
        public Vector3? End { get; private set; }

        /// <summary>
        /// How the velocity changed in the last step, in m/s²: the gravity the character really
        /// feels. During the jump it is the world's gravity; while gliding, whatever the method makes
        /// of it (none for a fixed descent, a fraction for the tanuki…). It is not updated on the
        /// step a phase changes (take-off, landing), where it would be a meaningless jolt.
        /// </summary>
        public Vector3 Acceleration { get; private set; }

        public bool InTheAir => Phase == FlightPhase.Jumping || Phase == FlightPhase.Gliding || Phase == FlightPhase.Falling;
        public bool Finished => Phase == FlightPhase.Landed || Phase == FlightPhase.Crashed;

        public Glider(string name, IGlideModel model, Vector3 feet, Vector3 facing)
        {
            Name = name;
            Model = model ?? throw new ArgumentNullException(nameof(model));
            var flat = new Vector3(facing.X, 0f, facing.Z);
            start = new FlightState(feet, Vector3.Zero, flat.LengthSquared() < 1e-8f ? Vector3.UnitZ : Vector3.Normalize(flat));
            State = start;
        }

        /// <summary>Back to the starting spot, standing on the ground, with the trajectory erased.</summary>
        public void Reset(Ground ground)
        {
            var feet = start.Position;
            State = new FlightState(new Vector3(feet.X, ground.HeightAt(feet), feet.Z), Vector3.Zero, start.Facing);
            Phase = FlightPhase.Standing;
            Time = 0f;
            nextSample = 0f;
            GlideStart = null;
            glideOpenedAt = null;
            End = null;
            Acceleration = Vector3.Zero;
            Stamina?.Refill();
            Trajectory.Clear();
        }

        /// <summary>Start the run-up (only from standing).</summary>
        public void Launch()
        {
            if (Phase == FlightPhase.Standing) Phase = FlightPhase.Running;
        }

        /// <summary>Moves the simulation dt seconds forward.</summary>
        public void Step(JumpSettings jump, Ground ground, float dt)
        {
            if (Phase == FlightPhase.Standing || Finished) return;

            var before = State;
            var phaseBefore = Phase;
            switch (Phase)
            {
                case FlightPhase.Running: Run(jump, ground, dt); break;
                case FlightPhase.Jumping: Jump(jump, dt); break;
                case FlightPhase.Gliding: Glide(jump, dt); break;
                case FlightPhase.Falling: State = PlainGravity(State, jump, dt); break;
            }
            if (InTheAir) CheckContact(before, ground);
            if (InTheAir && Phase == phaseBefore) Acceleration = (State.Velocity - before.Velocity) / dt;

            Time += dt;
            Record();
        }

        /// <summary>Runs straight ahead; the moment the ground ends, jumps.</summary>
        void Run(JumpSettings jump, Ground ground, float dt)
        {
            var velocity = State.Facing * jump.RunSpeed;
            var next = State.Position + velocity * dt;
            float height = ground.HeightAt(next);

            if (height < State.Position.Y - StepDown)
            {
                // The edge: same horizontal speed, plus the push up.
                State = new FlightState(next, velocity + Vector3.UnitY * jump.JumpSpeed, State.Facing);
                Phase = FlightPhase.Jumping;
            }
            else
            {
                State = new FlightState(new Vector3(next.X, height, next.Z), velocity, State.Facing);
            }
        }

        /// <summary>The jump: plain gravity. At the top of the jump (when it starts to fall), the glide opens.</summary>
        void Jump(JumpSettings jump, float dt)
        {
            State = PlainGravity(State, jump, dt);

            if (State.Velocity.Y <= 0f)
            {
                Phase = FlightPhase.Gliding;
                GlideStart = State.Position;
                glideOpenedAt = Time + dt;   // the end of this step
            }
        }

        /// <summary>The method glides. If the character tires, gliding spends its stamina, and
        /// when there is none left it lets go.</summary>
        void Glide(JumpSettings jump, float dt)
        {
            State = Model.Step(State, jump.Gravity, dt);
            if (Stamina == null) return;
            Stamina.Spend(dt);
            if (Stamina.IsEmpty) Phase = FlightPhase.Falling;
        }

        static FlightState PlainGravity(FlightState state, JumpSettings jump, float dt)
        {
            var velocity = state.Velocity - Vector3.UnitY * (jump.Gravity * dt);
            return new FlightState(state.Position + velocity * dt, velocity, state.Facing);
        }

        /// <summary>Below the ground? If it came from above that ground, it lands on it; if not, it
        /// went into the side of something (a platform it did not reach) and stops there.</summary>
        void CheckContact(FlightState before, Ground ground)
        {
            float height = ground.HeightAt(State.Position);
            if (State.Position.Y > height) return;

            if (before.Position.Y >= height)
            {
                var feet = State.Position;
                State = new FlightState(new Vector3(feet.X, height, feet.Z), Vector3.Zero, State.Facing);
                Phase = FlightPhase.Landed;
            }
            else
            {
                State = new FlightState(before.Position, Vector3.Zero, State.Facing);
                Phase = FlightPhase.Crashed;
            }
            End = State.Position;
        }

        void Record()
        {
            if (Phase == FlightPhase.Running) return;
            if (Time >= nextSample || Finished)
            {
                Trajectory.Add(Time, State.Position, Phase);
                nextSample = Time + SampleEvery;
            }
        }
    }
}
