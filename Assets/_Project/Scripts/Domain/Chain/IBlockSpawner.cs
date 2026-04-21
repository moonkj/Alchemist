using Alchemist.Domain.Blocks;

namespace Alchemist.Domain.Chain
{
    /// <summary>
    /// Abstraction for block creation during refill. Implementations must be deterministic
    /// given a seed (replay / test reproducibility — Phase 3). Pool-backed impls should recycle
    /// Block POCOs; Domain assumes ownership of the returned reference for its lifetime.
    /// </summary>
    public interface IBlockSpawner
    {
        /// <summary>Produce a block with a valid (primary/secondary/tertiary) color at (row,col).
        /// Returned block must already have Row/Col fields set; State is Spawned or Idle.</summary>
        Block SpawnRandom(int row, int col);
    }
}
