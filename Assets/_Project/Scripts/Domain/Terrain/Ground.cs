using System.Collections.Generic;
using System.Numerics;

namespace Gliding.Domain.Terrain
{
    /// <summary>
    /// The shape of the world for landing purposes: a set of blocks over a flat floor. All a glider
    /// needs to know is how high the ground is under a point.
    /// </summary>
    public sealed class Ground
    {
        readonly List<Block> blocks = new List<Block>();

        /// <summary>Height of the ground where there is no block.</summary>
        public float Floor { get; }

        public IReadOnlyList<Block> Blocks => blocks;

        public Ground(float floor = 0f) => Floor = floor;

        public void Add(Block block) => blocks.Add(block);

        /// <summary>The highest surface under that point (only X and Z matter).</summary>
        public float HeightAt(Vector3 point)
        {
            float height = Floor;
            foreach (var block in blocks)
                if (block.Top > height && block.Covers(point.X, point.Z))
                    height = block.Top;
            return height;
        }
    }
}
