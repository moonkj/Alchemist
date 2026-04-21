namespace Alchemist.Domain.Blocks
{
    public interface IStateTransition
    {
        bool CanTransition(BlockState from, BlockState to);
        bool TryTransition(Block block, BlockState next, in TransitionContext ctx);
    }
}
