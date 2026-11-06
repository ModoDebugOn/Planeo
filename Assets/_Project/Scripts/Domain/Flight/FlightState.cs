using System.Numerics;

namespace Gliding.Domain.Flight
{
    /// <summary>Where a character is, how fast it moves and which way it faces, at one instant.</summary>
    public readonly struct FlightState
    {
        /// <summary>The character's feet, in metres (what touches the ground on landing).</summary>
        public readonly Vector3 Position;

        /// <summary>Metres per second. Y up.</summary>
        public readonly Vector3 Velocity;

        /// <summary>Unit direction it faces, flat (Y = 0). Turning while gliding turns this.</summary>
        public readonly Vector3 Facing;

        public FlightState(Vector3 position, Vector3 velocity, Vector3 facing)
        {
            Position = position;
            Velocity = velocity;
            Facing = facing;
        }

        /// <summary>Speed along the ground (what takes you far).</summary>
        public float HorizontalSpeed => new Vector2(Velocity.X, Velocity.Z).Length();

        /// <summary>Vertical speed: negative while falling.</summary>
        public float VerticalSpeed => Velocity.Y;

        public override string ToString() => $"at {Position}, moving {Velocity}";
    }
}
