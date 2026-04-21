using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Chain
{
    /// <summary>
    /// Phase 2 P2-02: 회색(Gray/Absorbed) 블록의 셀별 누적 인접 폭발 카운터.
    /// WHY: Gray 블록은 2회 이상 인접 폭발을 맞으면 Normal 로 복귀해야 하는데,
    /// 한 턴에 누적되는 폭발 수를 중앙에서 추적해야 FSM 전이와 색 할당이 일관된다.
    /// WHY 사전할당 int[42]: steady-state GC alloc 0 유지(런타임 신규 할당 금지).
    /// </summary>
    public sealed class GrayReleaseTracker
    {
        /// <summary>회색 해제에 필요한 최소 누적 폭발 수 (설계 문서 P2-02 기준값).</summary>
        public const int ReleaseThreshold = 2;

        private readonly int[] _hits;
        private readonly ColorId[] _lastColor;
        private readonly Alchemist.Domain.Board.Board _board;

        public GrayReleaseTracker(Alchemist.Domain.Board.Board board)
        {
            _board = board;
            int n = Alchemist.Domain.Board.Board.CellCount;
            _hits = new int[n];
            _lastColor = new ColorId[n];
        }

        /// <summary>턴 시작/종료 시 호출하여 카운터를 0으로 리셋. WHY: 턴을 넘어가는 누적을
        /// 허용하면 설계(턴 내 2회)가 아닌 영구 누적이 되어 밸런스가 무너진다.</summary>
        public void Reset()
        {
            for (int i = 0; i < _hits.Length; i++)
            {
                _hits[i] = 0;
                _lastColor[i] = ColorId.None;
            }
        }

        /// <summary>
        /// ChainProcessor 가 폭발을 처리한 좌표 (er, ec) 기준으로 4-인접 회색 블록에 1카운트 기입.
        /// WHY 폭발 셀이 아닌 인접 셀에 기입: 폭발 블록 자체는 제거되고, 회색은 "인접" 폭발에 반응.
        /// </summary>
        public void NotifyExplosionAt(int er, int ec, ColorId explosionColor)
        {
            Bump(er - 1, ec, explosionColor);
            Bump(er + 1, ec, explosionColor);
            Bump(er, ec - 1, explosionColor);
            Bump(er, ec + 1, explosionColor);
        }

        private void Bump(int r, int c, ColorId explosionColor)
        {
            if (!Alchemist.Domain.Board.Board.InBounds(r, c)) return;
            Block b = _board.BlockAt(r, c);
            if (b == null) return;
            // WHY: 회색 상태 판정은 Kind 기준 — State 는 Absorbed/Gray 둘 다 동일 의미.
            if (b.Kind != BlockKind.Gray) return;
            int idx = Alchemist.Domain.Board.Board.IndexOf(r, c);
            _hits[idx]++;
            if (explosionColor != ColorId.None)
            {
                // WHY "마지막 색" 저장: 해제 시 None 으로는 Mix 결과가 None 이라 사용 불가.
                // 마지막 인접 폭발 색을 polymorphic 색 할당 소스로 사용 (설계 문서 명시).
                _lastColor[idx] = explosionColor;
            }
        }

        /// <summary>
        /// 해당 셀의 누적 카운트가 임계값 이상이면 Gray 블록을 해제(Normal 복귀)하고 true.
        /// WHY outParam 없음: 호출부는 Board 에서 Block 을 다시 읽어 후속 처리를 수행한다.
        /// </summary>
        public bool TryRelease(int row, int col)
        {
            int idx = Alchemist.Domain.Board.Board.IndexOf(row, col);
            if (_hits[idx] < ReleaseThreshold) return false;

            Block b = _board.BlockAt(row, col);
            if (b == null) return false;
            if (b.Kind != BlockKind.Gray) return false;

            // FSM 전이: Absorbed -> Idle (Phase 2 신규). Gray 상태라면 경로가 없으므로 우회.
            var ctx = new TransitionContext(_lastColor[idx], b.Id, 0);
            if (b.State == BlockState.Absorbed)
            {
                BlockFsm.TryTransition(b, BlockState.Idle, ctx);
            }
            // Gray 상태에서 직접 전이 불가 — 의도적으로 Absorbed 만 복귀 허용.
            // WHY: Gray(완전 흡수) 블록은 Wave1 규칙상 영구 소비이며, 본 복귀는
            //      Absorbed 중간상태에서만 유효(설계 문서 재확인 필요 시 Coder-B 조정).

            b.Kind = BlockKind.Normal;
            // polymorphic 색 할당: 마지막 인접 폭발 색. 없으면 None 유지(상위에서 보정).
            b.Color = _lastColor[idx];

            // 카운터 소비
            _hits[idx] = 0;
            _lastColor[idx] = ColorId.None;
            _board.MarkDirty(row, col);
            return true;
        }

        /// <summary>해제 임계에 도달한 모든 셀을 일괄 처리. 반환값: 해제된 블록 수.</summary>
        public int SweepRelease()
        {
            int released = 0;
            int rows = Alchemist.Domain.Board.Board.Rows;
            int cols = Alchemist.Domain.Board.Board.Cols;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (TryRelease(r, c)) released++;
                }
            }
            return released;
        }

        /// <summary>디버그/테스트 용 카운트 조회.</summary>
        public int GetHitCount(int row, int col)
        {
            return _hits[Alchemist.Domain.Board.Board.IndexOf(row, col)];
        }
    }
}
