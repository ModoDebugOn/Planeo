using System.Collections.Generic;
using System.Numerics;

namespace Gliding.Domain.Air
{
    /// <summary>
    /// The updrafts of the world, all switched on or off at once (to repeat the same jump with and
    /// without them). Only the methods that ride the air receive it.
    /// </summary>
    public sealed class AirCurrents
    {
        readonly List<Updraft> updrafts = new List<Updraft>();

        public IReadOnlyList<Updraft> Updrafts => updrafts;

        /// <summary>Off: as if there were no updrafts at all.</summary>
        public bool Enabled { get; set; } = true;

        public void Add(Updraft updraft) => updrafts.Add(updraft);

        /// <summary>How fast the air rises at that point, in m/s (0 outside every updraft).
        /// Where two overlap, the stronger one.</summary>
        public float LiftAt(Vector3 point)
        {
            if (!Enabled) return 0f;
            float lift = 0f;
            foreach (var updraft in updrafts)
                if (updraft.LiftSpeed > lift && updraft.Contains(point))
                    lift = updraft.LiftSpeed;
            return lift;
        }
    }
}
