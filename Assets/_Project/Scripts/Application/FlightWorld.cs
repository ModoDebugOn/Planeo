using System;
using System.Collections.Generic;
using System.Numerics;
using Gliding.Domain.Flight;
using Gliding.Domain.Terrain;

namespace Gliding.Application
{
    /// <summary>
    /// The demo as a whole: the ground, the shared jump and every glider, moved forward together.
    ///
    /// The simulation always advances in the same fixed steps, whatever the frame rate, and at the
    /// chosen speed (slow motion included). So the same jump with the same method always draws
    /// exactly the same path: on any PC, recording or not.
    /// </summary>
    public sealed class FlightWorld
    {
        /// <summary>Seconds of simulation per step (120 steps per simulated second).</summary>
        public const float StepSeconds = 1f / 120f;

        readonly List<Glider> gliders = new List<Glider>();
        float pending;

        public Ground Ground { get; }
        public JumpSettings Jump { get; set; }
        public IReadOnlyList<Glider> Gliders => gliders;

        /// <summary>1 = real time, 0.25 = slow motion…</summary>
        public float TimeScale { get; set; } = 1f;

        public bool Paused { get; set; }

        public FlightWorld(Ground ground, JumpSettings jump)
        {
            Ground = ground ?? throw new ArgumentNullException(nameof(ground));
            Jump = jump;
        }

        /// <summary>A new glider, standing where its feet are.</summary>
        public Glider Add(string name, IGlideModel model, Vector3 feet, Vector3 facing)
        {
            var glider = new Glider(name, model, feet, facing);
            glider.Reset(Ground);
            gliders.Add(glider);
            return glider;
        }

        /// <summary>Everybody back to the ledge and off they go, all at once.</summary>
        public void LaunchAll()
        {
            foreach (var glider in gliders)
            {
                glider.Reset(Ground);
                glider.Launch();
            }
        }

        public void ResetAll()
        {
            foreach (var glider in gliders) glider.Reset(Ground);
        }

        /// <summary>Real seconds have passed: advance as many fixed steps as they are worth.</summary>
        public void Advance(float realSeconds)
        {
            if (Paused) return;
            pending += realSeconds * TimeScale;
            while (pending >= StepSeconds)
            {
                StepOnce();
                pending -= StepSeconds;
            }
        }

        /// <summary>Exactly one step, even while paused (to advance frame by frame).</summary>
        public void StepOnce()
        {
            foreach (var glider in gliders) glider.Step(Jump, Ground, StepSeconds);
        }
    }
}
