using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Chain
{
    /// <summary>
    /// Phase 2 P2-03: 턴 종료 시점에 Prism 블록이 인접 1차/2차색 블록을 "흡수"하여
    /// 스스로 승격하는 순수 정적 프로세서.
    /// 규칙:
    ///   - 인접 1차색 2개 이상이면 Prism → Mix(p1,p2) 2차색(Normal)
    ///   - 인접 2차색 2개 이상이면 Prism → White(3차, Normal)
    ///   - 1차 1개 + 2차 1개: 승격 없음 (규칙 단순화)
    /// WHY static: 상태 보관이 불필요(보드만 순회); GC-free.
    /// WHY 턴 종료 훅: Gravity/Refill 이후 보드가 stable 한 상태에서 판정해야
    /// mid-cascade 에서의 flicker 승격을 방지.
    /// </summary>
    public static class PrismAbsorbProcessor
    {
        /// <summary>보드 전체를 스캔하여 Prism 블록을 승격. 반환: 승격 건수.</summary>
        public static int Process(Alchemist.Domain.Board.Board board)
        {
            if (board == null) return 0;

            int promoted = 0;
            int rows = Alchemist.Domain.Board.Board.Rows;
            int cols = Alchemist.Domain.Board.Board.Cols;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Block b = board.BlockAt(r, c);
                    if (b == null) continue;
                    if (b.Kind != BlockKind.Prism) continue;

                    if (TryPromote(board, b, r, c))
                    {
                        promoted++;
                    }
                }
            }
            return promoted;
        }

        private static bool TryPromote(Alchemist.Domain.Board.Board board, Block prism, int r, int c)
        {
            // 인접 4방 스캔 — 1차/2차 각 최대 2개까지만 수집.
            // WHY 수집 상한 2: 규칙상 2개만 매칭되면 즉시 승격 결정.
            ColorId p1 = ColorId.None;
            ColorId p2 = ColorId.None;
            int primaryCount = 0;
            ColorId s1 = ColorId.None;
            ColorId s2 = ColorId.None;
            int secondaryCount = 0;

            ScanNeighbor(board, r - 1, c, ref p1, ref p2, ref primaryCount, ref s1, ref s2, ref secondaryCount);
            ScanNeighbor(board, r + 1, c, ref p1, ref p2, ref primaryCount, ref s1, ref s2, ref secondaryCount);
            ScanNeighbor(board, r, c - 1, ref p1, ref p2, ref primaryCount, ref s1, ref s2, ref secondaryCount);
            ScanNeighbor(board, r, c + 1, ref p1, ref p2, ref primaryCount, ref s1, ref s2, ref secondaryCount);

            // 2차 우선 (White 승격이 상위 효과) — 인접 2차 2개면 White.
            if (secondaryCount >= 2)
            {
                prism.Kind = BlockKind.Normal;
                prism.Color = ColorId.White;
                board.MarkDirty(r, c);
                return true;
            }

            if (primaryCount >= 2)
            {
                ColorId mixed = ColorMixCache.Lookup(p1, p2);
                // WHY 두 primary 가 같은 색일 경우 Mix 는 자기 자신이 되어 2차가 아님 → 승격 보류.
                if (!ColorMixer.IsSecondary(mixed)) return false;
                prism.Kind = BlockKind.Normal;
                prism.Color = mixed;
                board.MarkDirty(r, c);
                return true;
            }

            return false;
        }

        private static void ScanNeighbor(
            Alchemist.Domain.Board.Board board,
            int r, int c,
            ref ColorId p1, ref ColorId p2, ref int primaryCount,
            ref ColorId s1, ref ColorId s2, ref int secondaryCount)
        {
            if (!Alchemist.Domain.Board.Board.InBounds(r, c)) return;
            Block nb = board.BlockAt(r, c);
            if (nb == null) return;
            // WHY Prism/Gray 는 흡수 대상 제외 — 본 규칙은 "순수 색상 블록" 에 한정.
            if (nb.Kind != BlockKind.Normal) return;

            ColorId col = nb.Color;
            if (ColorMixer.IsPrimary(col))
            {
                if (primaryCount == 0) p1 = col;
                else if (primaryCount == 1) p2 = col;
                if (primaryCount < 2) primaryCount++;
            }
            else if (ColorMixer.IsSecondary(col))
            {
                if (secondaryCount == 0) s1 = col;
                else if (secondaryCount == 1) s2 = col;
                if (secondaryCount < 2) secondaryCount++;
            }
        }
    }
}
