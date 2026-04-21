using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;
using BoardType = Alchemist.Domain.Board.Board;

namespace Alchemist.Domain.Economy
{
    /// <summary>
    /// 아이템 효과를 Board 에 적용. Inventory 소모와 Board 편집을 한 트랜잭션으로 묶음.
    /// WHY: Domain 순수성 유지를 위해 View 갱신은 호출자가 담당(RebuildCell 등).
    /// WHY: using alias(BoardType) 로 네임스페이스와 동일명 클래스의 모호성을 회피.
    /// </summary>
    public sealed class ItemEffectProcessor
    {
        private readonly BoardType _board;
        private readonly Inventory _inventory;

        public ItemEffectProcessor(BoardType board, Inventory inventory)
        {
            _board = board;
            _inventory = inventory;
        }

        /// <summary>매직 브러시: 지정 좌표의 블록 색을 newColor 로 변경. 특수 블록/빈 셀은 실패.</summary>
        public bool ApplyBrush(int row, int col, ColorId newColor)
        {
            if (!BoardType.InBounds(row, col)) return false;
            var block = _board.BlockAt(row, col);
            if (block == null) return false;
            // WHY: Filter/Prism/Gray 는 브러시 대상에서 제외(기획: Normal 에만 적용).
            if (block.Kind != BlockKind.Normal) return false;
            if (newColor == ColorId.None) return false;
            if (!_inventory.Use(ItemId.MagicBrush)) return false;

            block.Color = newColor;
            _board.MarkDirty(row, col);
            return true;
        }

        /// <summary>지우개: 지정 좌표 블록 제거. 빈 셀은 실패.</summary>
        public bool ApplyEraser(int row, int col)
        {
            if (!BoardType.InBounds(row, col)) return false;
            var block = _board.BlockAt(row, col);
            if (block == null) return false;
            if (!_inventory.Use(ItemId.Eraser)) return false;

            _board.SetBlock(row, col, null);
            return true;
        }

        /// <summary>추가 프리즘: 지정 좌표 블록을 Prism 으로 변환. 이미 Prism/빈 셀은 실패.</summary>
        public bool ApplyExtraPrism(int row, int col)
        {
            if (!BoardType.InBounds(row, col)) return false;
            var block = _board.BlockAt(row, col);
            if (block == null) return false;
            if (block.Kind == BlockKind.Prism) return false;
            if (!_inventory.Use(ItemId.ExtraPrism)) return false;

            block.Kind = BlockKind.Prism;
            block.Color = ColorId.Prism;
            _board.MarkDirty(row, col);
            return true;
        }
    }
}
