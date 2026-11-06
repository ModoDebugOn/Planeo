using System.Collections.Generic;
using System.Numerics;

namespace Gliding.Domain.Flight
{
    /// <summary>One point of the path flown: when, where and in which phase.</summary>
    public readonly struct TrajectoryPoint
    {
        /// <summary>Seconds since the run-up started.</summary>
        public readonly float Time;
        public readonly Vector3 Position;
        public readonly FlightPhase Phase;

        public TrajectoryPoint(float time, Vector3 position, FlightPhase phase)
        {
            Time = time;
            Position = position;
            Phase = phase;
        }
    }

    /// <summary>
    /// The path a character has flown, sampled every few hundredths of a second. It is kept after
    /// landing, so the paths of every method can be laid on top of each other to compare them.
    /// </summary>
    public sealed class Trajectory
    {
        readonly List<TrajectoryPoint> points = new List<TrajectoryPoint>();

        public IReadOnlyList<TrajectoryPoint> Points => points;
        public int Count => points.Count;

        public void Add(float time, Vector3 position, FlightPhase phase) => points.Add(new TrajectoryPoint(time, position, phase));

        public void Clear() => points.Clear();
    }
}
