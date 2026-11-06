using System;

namespace Gliding.Domain.Flight
{
    /// <summary>
    /// A stamina bar: something that gets spent and runs out. It knows nothing about gliding (or
    /// climbing, or sprinting): whoever owns it decides what spends it and what happens when it is
    /// empty. Plain C# with no dependencies, so it can be taken to another project as it is.
    /// </summary>
    public sealed class Stamina
    {
        /// <summary>How much a full bar holds (here, seconds of glide).</summary>
        public float Capacity { get; }

        /// <summary>How much is left, from 0 to <see cref="Capacity"/>.</summary>
        public float Current { get; private set; }

        /// <summary>From 1 (full) to 0 (empty): what a stamina wheel draws.</summary>
        public float Fraction => Current / Capacity;

        public bool IsEmpty => Current <= 0f;

        public Stamina(float capacity)
        {
            if (capacity <= 0f) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            Current = capacity;
        }

        /// <summary>Uses up some of it (never below 0).</summary>
        public void Spend(float amount) => Current = Math.Max(0f, Current - Math.Max(0f, amount));

        /// <summary>Back to full.</summary>
        public void Refill() => Current = Capacity;
    }
}
