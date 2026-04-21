using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Board
{
    /// <summary>
    /// Phase 2 P2-01: 필터 벽 레이아웃 디버그/테스트 픽스처.
    /// WHY static helpers: 테스트/에디터 전용 상수 세팅이므로 인스턴스 상태 불요;
    /// Domain 레이어에 두어 UnityEngine 의존 없이 EditMode 테스트가 재사용 가능.
    /// </summary>
    public static class BoardTestFixtures
    {
        /// <summary>가운데 가로줄(row=3) 전체에 빨강 필터. 낙하 블록이 통과하며 Red 와 Mix.</summary>
        public static void ApplyHorizontalRedFilter(Board board)
        {
            if (board == null) return;
            int mid = Board.Rows / 2;
            for (int c = 0; c < Board.Cols; c++)
            {
                board.SetCellLayer(mid, c, CellLayer.Filter, ColorId.Red);
            }
        }

        /// <summary>좌측 세로줄(col=0)에 파랑 필터. 사이드 낙하 패턴 테스트용.</summary>
        public static void ApplyVerticalBlueFilter(Board board)
        {
            if (board == null) return;
            for (int r = 0; r < Board.Rows; r++)
            {
                board.SetCellLayer(r, 0, CellLayer.Filter, ColorId.Blue);
            }
        }

        /// <summary>체커보드 패턴의 노랑 필터. 복수 통과 누적 변환 케이스.</summary>
        public static void ApplyCheckerYellowFilter(Board board)
        {
            if (board == null) return;
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Cols; c++)
                {
                    // WHY (r+c)&1: 대각 인접을 피해 연속 통과가 드물도록 분산.
                    if (((r + c) & 1) == 0)
                    {
                        board.SetCellLayer(r, c, CellLayer.Filter, ColorId.Yellow);
                    }
                }
            }
        }

        /// <summary>단일 셀 필터 — 제일 단순한 유닛테스트용(행 5, 열 2에 Red).</summary>
        public static void ApplySingleCellRedFilter(Board board, int row = 5, int col = 2)
        {
            if (board == null) return;
            if (!Board.InBounds(row, col)) return;
            board.SetCellLayer(row, col, CellLayer.Filter, ColorId.Red);
        }

        /// <summary>모든 필터/Wall 레이어를 Ground 로 초기화. 테스트 간 격리.</summary>
        public static void ClearAllLayers(Board board)
        {
            if (board == null) return;
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Cols; c++)
                {
                    board.SetCellLayer(r, c, CellLayer.Ground, ColorId.None);
                }
            }
        }
    }
}
