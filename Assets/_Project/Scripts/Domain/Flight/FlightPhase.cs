namespace Gliding.Domain.Flight
{
    /// <summary>The moments of a jump, in order. Every method shares all of them except Gliding.</summary>
    public enum FlightPhase
    {
        /// <summary>Waiting on the ledge.</summary>
        Standing,
        /// <summary>Run-up towards the edge; the jump happens on its own when the ground ends.</summary>
        Running,
        /// <summary>The jump itself: plain gravity, the same for everybody, up to the top of the jump.</summary>
        Jumping,
        /// <summary>The glide is open: the method decides how it moves.</summary>
        Gliding,
        /// <summary>Out of stamina, it let go of the glide: plain gravity again, until it lands.</summary>
        Falling,
        /// <summary>Touched down on top of something.</summary>
        Landed,
        /// <summary>Hit the side of something (fell short of a platform).</summary>
        Crashed,
    }
}
