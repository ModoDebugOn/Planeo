using System;

namespace Gliding.Domain.Flight
{
    /// <summary>
    /// The jump everybody shares: the same run-up, the same push up and the same gravity. That is
    /// what makes the comparison fair: until the glide opens, all the trajectories are identical.
    /// </summary>
    public readonly struct JumpSettings
    {
        /// <summary>Run-up speed towards the edge, in m/s. It is also the horizontal speed of the jump.</summary>
        public readonly float RunSpeed;

        /// <summary>Upward speed when leaving the edge, in m/s.</summary>
        public readonly float JumpSpeed;

        /// <summary>m/s², positive. Games use more than the real 9.8 so jumps feel snappy.</summary>
        public readonly float Gravity;

        public JumpSettings(float runSpeed, float jumpSpeed, float gravity)
        {
            if (runSpeed <= 0f) throw new ArgumentOutOfRangeException(nameof(runSpeed));
            if (jumpSpeed < 0f) throw new ArgumentOutOfRangeException(nameof(jumpSpeed));
            if (gravity <= 0f) throw new ArgumentOutOfRangeException(nameof(gravity));
            RunSpeed = runSpeed;
            JumpSpeed = jumpSpeed;
            Gravity = gravity;
        }
    }
}
