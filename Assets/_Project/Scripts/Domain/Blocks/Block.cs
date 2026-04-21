using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Blocks
{
    public sealed class Block
    {
        public int Id;
        public ColorId Color;
        public BlockState State;
        public BlockKind Kind;
        public int Row;
        public int Col;
        public float JellyAmplitude;

        public void Reset()
        {
            Id = 0;
            Color = ColorId.None;
            State = BlockState.Spawned;
            Kind = BlockKind.Normal;
            Row = 0;
            Col = 0;
            JellyAmplitude = 0f;
        }
    }
}
