namespace Alchemist.Domain.Blocks
{
    public static class BlockFsm
    {
        private const int StateCount = (int)BlockState.Count;
        private static readonly bool[,] Allowed = BuildTable();

        private static bool[,] BuildTable()
        {
            var t = new bool[StateCount, StateCount];

            // Spawned -> Idle
            t[(int)BlockState.Spawned, (int)BlockState.Idle] = true;

            // Idle <-> Selected
            t[(int)BlockState.Idle, (int)BlockState.Selected] = true;
            t[(int)BlockState.Selected, (int)BlockState.Idle] = true;

            // Selected -> Merging
            t[(int)BlockState.Selected, (int)BlockState.Merging] = true;

            // Merging -> Exploding
            t[(int)BlockState.Merging, (int)BlockState.Exploding] = true;

            // Exploding -> Cleared, Infecting
            t[(int)BlockState.Exploding, (int)BlockState.Cleared] = true;
            t[(int)BlockState.Exploding, (int)BlockState.Infecting] = true;

            // Infecting -> Idle (for source block after infection propagation)
            t[(int)BlockState.Infecting, (int)BlockState.Idle] = true;

            // Idle -> Infected -> Idle (infection target: color change)
            t[(int)BlockState.Idle, (int)BlockState.Infected] = true;
            t[(int)BlockState.Infected, (int)BlockState.Idle] = true;

            // Idle -> Absorbed -> Gray (one-way; Gray reactivation is separate)
            t[(int)BlockState.Idle, (int)BlockState.Absorbed] = true;
            t[(int)BlockState.Absorbed, (int)BlockState.Gray] = true;

            // Idle -> FilterTransit -> Idle
            t[(int)BlockState.Idle, (int)BlockState.FilterTransit] = true;
            t[(int)BlockState.FilterTransit, (int)BlockState.Idle] = true;

            // Idle -> PrismCharging -> Exploding (prism reuses generic explode)
            t[(int)BlockState.Idle, (int)BlockState.PrismCharging] = true;
            t[(int)BlockState.PrismCharging, (int)BlockState.Exploding] = true;

            return t;
        }

        public static bool CanTransition(BlockState from, BlockState to)
        {
            int f = (int)from;
            int x = (int)to;
            if ((uint)f >= StateCount) return false;
            if ((uint)x >= StateCount) return false;
            return Allowed[f, x];
        }

        public static bool TryTransition(Block block, BlockState next, in TransitionContext ctx)
        {
            if (block == null) return false;
            var prev = block.State;
            if (!CanTransition(prev, next)) return false;

            OnExit(block, prev, next, ctx);
            block.State = next;
            OnEnter(block, prev, next, ctx);
            Dispatch(block, prev, next, ctx);
            return true;
        }

        private static void OnExit(Block block, BlockState from, BlockState to, in TransitionContext ctx)
        {
            // Hook reserved for Phase 2 (MessagePipe wiring / view side-effects).
        }

        private static void OnEnter(Block block, BlockState from, BlockState to, in TransitionContext ctx)
        {
            // Hook reserved for Phase 2.
        }

        private static void Dispatch(Block block, BlockState from, BlockState to, in TransitionContext ctx)
        {
            // Placeholder: a future Coder wires MessagePipe or a StateChangedCallback registry here.
            // var evt = new BlockStateChanged(block.Id, from, to);
        }
    }
}
