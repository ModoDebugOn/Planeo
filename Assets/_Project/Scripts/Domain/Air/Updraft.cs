using System.Numerics;

namespace Gliding.Domain.Air
{
    /// <summary>
    /// A column of rising air (a fire, a hot rock…): a box seen from above, from Bottom to Top. A
    /// paraglider inside it goes up instead of down.
    /// </summary>
    public readonly struct Updraft
    {
        public readonly float MinX, MaxX, MinZ, MaxZ;
        public readonly float Bottom, Top;

        /// <summary>Speed going up inside it, in m/s.</summary>
        public readonly float LiftSpeed;

        public Updraft(float minX, float maxX, float minZ, float maxZ, float bottom, float top, float liftSpeed)
        {
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
            Bottom = bottom;
            Top = top;
            LiftSpeed = liftSpeed;
        }

        public bool Contains(Vector3 point) =>
            point.X >= MinX && point.X <= MaxX &&
            point.Z >= MinZ && point.Z <= MaxZ &&
            point.Y >= Bottom && point.Y <= Top;
    }
}
