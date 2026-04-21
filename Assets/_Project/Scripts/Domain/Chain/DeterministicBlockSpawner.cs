using System;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Chain
{
    /// <summary>
    /// Seed-deterministic refill spawner with internal Block pool (Wave3 F10).
    /// Call <see cref="Return"/> when a Block leaves the board so the next refill reuses it.
    /// </summary>
    public sealed class DeterministicBlockSpawner : IBlockSpawner
    {
        private static readonly ColorId[] Palette =
        {
            ColorId.Red,
            ColorId.Yellow,
            ColorId.Blue,
        };

        private readonly Random _rng;
        private int _nextId;

        private Block[] _pool;
        private int _poolCount;

        public DeterministicBlockSpawner(int seed, int poolCapacity = 64)
        {
            _rng = new Random(seed);
            _nextId = 1;
            _pool = new Block[poolCapacity > 0 ? poolCapacity : 64];
            _poolCount = 0;
        }

        public Block SpawnRandom(int row, int col)
        {
            Block b;
            if (_poolCount > 0)
            {
                _poolCount--;
                b = _pool[_poolCount];
                _pool[_poolCount] = null;
                b.Reset();
            }
            else
            {
                b = new Block();
            }
            b.Id = _nextId++;
            b.Color = Palette[_rng.Next(Palette.Length)];
            b.Row = row;
            b.Col = col;
            b.State = BlockState.Spawned;
            b.Kind = BlockKind.Normal;
            return b;
        }

        public void Return(Block b)
        {
            if (b == null) return;
            if (_poolCount >= _pool.Length)
            {
                var grown = new Block[_pool.Length * 2];
                for (int i = 0; i < _poolCount; i++) grown[i] = _pool[i];
                _pool = grown;
            }
            _pool[_poolCount++] = b;
        }
    }
}
