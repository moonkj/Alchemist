using System.Threading;
using System.Threading.Tasks;

namespace Alchemist.Domain.Chain
{
    /// <summary>
    /// View-facing bridge for animation. Implementations live in the View assembly
    /// (DOTween sequences etc.). Domain only awaits a completion Task.
    ///
    /// Span-passing limitation: async methods cannot capture ref-struct (Span) state across awaits.
    /// Phase 1 workaround: the Hub receives *materialized* MatchGroup[] payloads (not Span).
    /// ChainProcessor performs a single shallow copy out of its stackalloc buffer into a
    /// pre-allocated MatchGroup[] owned by the Processor — no per-turn `new` on the hot path.
    /// </summary>
    public interface IChainAnimationHub
    {
        /// <summary>Play explosion visuals for all groups. count is the active slice length.</summary>
        Task PlayExplosionAsync(MatchGroup[] groups, int count, CancellationToken ct);

        /// <summary>Play infection propagation for the cells mutated in the last wave.
        /// Row/Col buffers describe source primary blocks that were color-shifted.</summary>
        Task PlayInfectionAsync(sbyte[] rows, sbyte[] cols, int count, CancellationToken ct);

        /// <summary>Play gravity fall animation; drops is a parallel (fromRow, toRow, col) triplet list.</summary>
        Task PlayGravityAsync(sbyte[] fromRows, sbyte[] toRows, sbyte[] cols, int count, CancellationToken ct);

        /// <summary>Play refill pop-in for newly spawned blocks (topmost rows).</summary>
        Task PlayRefillAsync(sbyte[] rows, sbyte[] cols, int count, CancellationToken ct);
    }
}
