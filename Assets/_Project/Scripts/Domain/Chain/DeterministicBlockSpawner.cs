using System;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Chain
{
    /// <summary>
    /// Seed-deterministic refill spawner. Uses System.Random (not UnityEngine.Random) for
    /// Domain purity and cross-run reproducibility (replay / save-state verification in Phase 3).
    ///
    /// Allocates new Block instances by default (Phase 1 MVP). A pool-backed implementation
    /// can be swapped in later by implementing IBlockSpawner with the same seed contract.
    /// Palette is restricted to the three primaries (R/Y/B) — secondaries/tertiaries arise
    /// exclusively from player-driven mixes (design intent from architecture.md §2.3).
    /// </summary>
    public sealed class DeterministicBlockSpawner : IBlockSpawner
    {
        // Palette: only primaries spawn; mixing is the player's job.
        private static readonly ColorId[] Palette =
        {
            ColorId.Red,
            ColorId.Yellow,
            ColorId.Blue,
        };

        private readonly Random _rng;
        private int _nextId;

        public DeterministicBlockSpawner(int seed)
        {
            _rng = new Random(seed);
            _nextId = 1;
        }

        public Block SpawnRandom(int row, int col)
        {
            var b = new Block();
            b.Reset();
            b.Id = _nextId++;
            b.Color = Palette[_rng.Next(Palette.Length)];
            b.Row = row;
            b.Col = col;
            b.State = BlockState.Spawned;
            b.Kind = BlockKind.Normal;
            return b;
        }
    }
}
