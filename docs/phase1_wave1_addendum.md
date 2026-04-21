# Phase 1 Wave 1 — 교차레이어 결정 애드덤

> 리더: Architect | 2026-04-21 | 라운드: Phase 1 Wave 1 Complete
> 트리거: Coder 1~4가 구현 중 발견한 규격 모호점에 대한 리더 최종 판단

---

## 합의된 결정 (Phase 1 Wave 2/3에 적용)

### C1. Gray 블록의 데이터 표현 (from Coder-2 질의)
**결정:** 흡수된 블록은 `BlockKind = Gray` + `Color = ColorId.None` + `State = Absorbed` (영구 상태).
- 권위 식별자는 **`BlockKind`** (타입). `ColorId.Gray` 플래그는 도메인에서 사용하지 않음 (타입 혼동 방지).
- `ColorId.Gray` enum 상수는 향후 필터 벽 색상 표시 등 시각 전용 용도로만 남기거나, Phase 2에서 제거 검토.
- **Why:** Kind=타입, Color=현재 띠고 있는 색. 흡수된 블록은 "색 없음"이 의미상 정확.

### C1b. Gray 재활성 경로 (Gray → Idle)
**결정:** Phase 1 FSM에 포함하지 않음. Phase 2 "색 도둑(회색 블록) 해제" 메커닉에서 FSM 표에 추가.
- 현재 전이 허용 15건은 그대로 유지.

### C2. `GetColorsCreated(ColorId.Purple)` 의미론 (from Coder-3 질의)
**결정:** **Exact match** (정확히 Purple 플래그인 블록만 카운트).
- `ctx.GetColorsCreated(Purple) = (byte)Purple 인덱스의 카운터`
- White 블록은 별도 카운터. 상위 집합 매칭 아님.
- **Why:** "보라 10개 생성" 프롬프트 = 유저가 직접 보라를 10개 만든다는 **성취 의미**. 포함 매칭은 보라 1개 + 흰 10개로 달성되어 의도 훼손.

### C3. `OnBlocksExploded(color, count, depth)` 시맨틱 (from Coder-4 질의)
**결정:** `count`는 **같은 색으로 한 번에 폭발하는 블록 수**. 점수는 `Base × Multiplier × count` (블록당).
- `BeginStage(goalMoveCount)` API 채택 — Efficiency 계산용 par 값 주입.
- 스테이지 데이터(ScriptableObject)에 `PerfectMoveCount` 필드 추가 예정 (Phase 2).

### C4. 색 조합 엣지 케이스 (from Coder-1 유보)
**결정:**
- `Mix(White, *)` = **Black** (과포화 오염). White는 "완성색"이므로 더해지면 오염.
- `Mix(White, White)` = **Black**. 동일.
- `Mix(Prism, Prism)` = **Prism** (프리즘끼리 만나면 프리즘 유지). 희귀 케이스지만 결정론 보장.
- `Mix(Gray, *)` = None (Gray는 흡수 비활성 상태, 도메인 Mix 비참여)
- `Mix(Black, *)` = Black (오염은 전파)

### C5. MessagePipe 이벤트 허브 (Coder-2 보류)
**결정:** Phase 1에서는 `StateChangedCallback` 델리게이트 필드로 더미 유지. Phase 2에서 MessagePipe 통합.
- Wave 3 Debugger/Test는 직접 델리게이트 등록으로 검증.

---

## Wave 2 Coder들에게 전달할 사전 합의

1. **Board POCO는 1D 배열** `int[] _cells = new int[Rows * Cols]` (실제로는 `Cell[]` 또는 `Block?[]`). Perf §2.3 준수.
2. **6×7 보드 고정** (리더 결정 D3). 난이도 확장은 Phase 2.
3. **MatchDetector는 2차 이상 색만 폭발 트리거** (1차 단독 3연결은 매치 NO). 예: 보라 3개 → 폭발, 빨강 3개 → 폭발 X (빨강이 다른 색과 만나 2차로 승격해야 함).
4. **감염 반경** = 폭발 중심에서 체비셰프 거리 1(인접 8방향 또는 4방향). Phase 1은 **4방향(상하좌우)** 로 확정. 감염 대상은 1차 색 블록만.
5. **연쇄 웨이브 depth 하드캡 = 10**. 초과 시 강제 종료 + 로그.
6. **View 레이어는 UnityEngine 의존 허용**. UniTask, DOTween은 ThirdParty로 가정, Phase 1 MVP는 `Coroutine` 또는 `async Task`로 대체 구현 가능 (UniTask 미설치 상태면 async Task 사용).

---

## Tasklist 갱신
- P1-01 `ColorMixer` 🟩
- P1-02 `BlockState FSM` 🟩
- P1-06 `Prompts` 🟩
- P1-07 `Scorer` 🟩
- P1-03 `ChainProcessor` 🟨 (Wave 2 투입)
- P1-04 `BoardView` 🟨 (Wave 2 투입)
- P1-05 `Explosion rules (MatchDetector)` 🟨 (Wave 2 투입)
