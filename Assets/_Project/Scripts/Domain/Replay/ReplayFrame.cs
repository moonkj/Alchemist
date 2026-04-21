namespace Alchemist.Domain.Replay
{
    /// <summary>
    /// 리플레이 1 턴 스냅샷. from→to swap 과 결과 연쇄 깊이.
    /// WHY readonly struct: 순차 append 시 할당 없이 순환 버퍼에 저장.
    /// 좌표는 sbyte — 보드 폭/높이 &lt; 128 가정(현재 Board 6x7).
    /// </summary>
    public readonly struct ReplayFrame
    {
        public readonly int Turn;
        public readonly sbyte FromRow;
        public readonly sbyte FromCol;
        public readonly sbyte ToRow;
        public readonly sbyte ToCol;
        public readonly int ChainDepth;

        public ReplayFrame(int turn, sbyte fromRow, sbyte fromCol, sbyte toRow, sbyte toCol, int chainDepth)
        {
            Turn = turn;
            FromRow = fromRow;
            FromCol = fromCol;
            ToRow = toRow;
            ToCol = toCol;
            ChainDepth = chainDepth;
        }
    }
}
