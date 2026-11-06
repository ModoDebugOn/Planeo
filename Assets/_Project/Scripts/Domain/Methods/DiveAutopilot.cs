using System;

namespace Gliding.Domain.Methods
{
    /// <summary>
    /// A pilot that flies the dive on its own, so a take always looks the same: it dives for a few
    /// seconds after the glide opens and then pulls back. As soon as the player touches the stick,
    /// the player flies instead.
    /// It is asked at every step of the simulation, so it follows simulated time exactly.
    /// </summary>
    public sealed class DiveAutopilot : IDiveControl
    {
        readonly IDiveControl hand;
        readonly Func<float> glideTime;

        /// <summary>Seconds diving fully after the glide opens.</summary>
        public float DiveSeconds { get; set; }

        /// <summary>The stick after the dive: -1 = pull back fully.</summary>
        public float Then { get; set; }

        /// <param name="hand">The player's stick: whenever it is not centred, it wins.</param>
        /// <param name="glideTime">Seconds since the glide opened.</param>
        public DiveAutopilot(IDiveControl hand, Func<float> glideTime, float diveSeconds = 1.5f, float then = -1f)
        {
            this.hand = hand ?? throw new ArgumentNullException(nameof(hand));
            this.glideTime = glideTime ?? throw new ArgumentNullException(nameof(glideTime));
            DiveSeconds = diveSeconds;
            Then = then;
        }

        public float Dive
        {
            get
            {
                if (hand.Dive != 0f) return hand.Dive;
                return glideTime() < DiveSeconds ? 1f : Then;
            }
        }
    }
}
