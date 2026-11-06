using System;
using Gliding.Domain.Flight;

namespace Gliding.Application
{
    /// <summary>
    /// The gliders jump one after another, in order, so each jump can be watched on its own:
    /// 1. The one whose turn it is jumps (the camera follows it).
    /// 2. It lands; a moment later the turn passes to the next one, still standing on the ledge
    ///    (the camera travels back to it).
    /// 3. A moment later, once the camera is there, the next one jumps.
    /// The trajectories of those who already jumped stay, to compare them.
    /// </summary>
    public sealed class TakeTurns
    {
        readonly FlightWorld world;
        int index = -1;
        float waited;
        bool launched;

        /// <summary>Seconds watching the landing before the turn passes to the next one.</summary>
        public float AfterLanding { get; set; } = 1f;

        /// <summary>Seconds between the turn passing and the next one jumping (the camera's trip back).</summary>
        public float BeforeJump { get; set; } = 1.5f;

        /// <summary>The glider whose turn it is: the one to look at. Null before the first turn.</summary>
        public Glider Current => index >= 0 && index < world.Gliders.Count ? world.Gliders[index] : null;

        /// <summary>True from the start of the first turn until the last one lands.</summary>
        public bool Running => Current != null && !(index == world.Gliders.Count - 1 && Current.Finished);

        public TakeTurns(FlightWorld world) => this.world = world ?? throw new ArgumentNullException(nameof(world));

        /// <summary>Everybody back to the ledge, and the first one jumps.</summary>
        public void Start()
        {
            world.ResetAll();
            if (world.Gliders.Count == 0) return;
            index = 0;
            Current.Launch();
            launched = true;
            waited = 0f;
        }

        /// <summary>Simulated seconds have passed.</summary>
        public void Advance(float seconds)
        {
            var current = Current;
            if (current == null) return;

            if (!launched)
            {
                waited += seconds;
                if (waited < BeforeJump) return;
                current.Launch();
                launched = true;
                waited = 0f;
                return;
            }

            if (!current.Finished || index == world.Gliders.Count - 1) return;
            waited += seconds;
            if (waited < AfterLanding) return;
            index++;
            launched = false;
            waited = 0f;
        }
    }
}
