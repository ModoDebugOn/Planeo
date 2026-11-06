namespace Gliding.Domain.Terrain
{
    /// <summary>Something you can land on, seen from above: a rectangle at some height (the top of
    /// the ledge, of a platform, the ground…). Solid all the way down from Top.</summary>
    public readonly struct Block
    {
        public readonly float MinX, MaxX, MinZ, MaxZ;
        public readonly float Top;

        public Block(float minX, float maxX, float minZ, float maxZ, float top)
        {
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
            Top = top;
        }

        public bool Covers(float x, float z) => x >= MinX && x <= MaxX && z >= MinZ && z <= MaxZ;
    }
}
