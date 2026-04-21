using System.Threading;
using System.Threading.Tasks;

namespace Alchemist.Domain.Chain
{
    /// <summary>
    /// Test/headless implementation of IChainAnimationHub: every Play*Async returns
    /// immediately-completed Task. Allows ChainProcessor to be driven in unit tests
    /// without Unity or DOTween. Also used by CI perf/alloc probes.
    /// </summary>
    public sealed class NoOpAnimationHub : IChainAnimationHub
    {
        public Task PlayExplosionAsync(MatchGroup[] groups, int count, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task PlayInfectionAsync(sbyte[] rows, sbyte[] cols, int count, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task PlayGravityAsync(sbyte[] fromRows, sbyte[] toRows, sbyte[] cols, int count, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task PlayRefillAsync(sbyte[] rows, sbyte[] cols, int count, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
