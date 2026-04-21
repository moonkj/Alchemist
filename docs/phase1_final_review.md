# Phase 1 Final Review

> 리뷰어: Reviewer Teammate | 2026-04-21 | 라운드: Phase 1 Final (개선 R2 필요)
> 기준 문서: phase0_integration_review.md, phase1_wave1_addendum.md, phase1_wave3_decisions.md, phase1_debug_report.md, phase1_performance_review.md

---

## 1. DoD 충족 판정

### A. Critical 수정 검증 (Debug Report)
- [x] **BUG-C01 초기 보드 Refill** — `GameRoot.InitialRefill()` (GameRoot.cs L83-95)이 42셀 모두 스폰 후 `_boardView.RebuildAllCells()` 호출. 추가로 `_scorer.OnColorCreated(b.Color, 1)`도 함께 호출하여 D19 연계.
- [x] **BUG-C02 ColorMixer Prism 순서** — `ColorMixer.Mix` (L18-32)에서 None(18) → Gray(22) → Black(26) → Prism(28-32) 순서로 재정렬. D17 준수.
- [x] **BUG-C03 PlayGravityAsync null 방어** — `BoardView.PlayGravityAsync` L160에 `if (count <= 0 || _viewGrid == null) return Task.CompletedTask;` 가드 추가.
- [x] **BUG-C04 SyncCell _pool null 방어** — `BoardView.SyncCell` L82에 `if (_board == null || _viewGrid == null || _pool == null) return;` 추가.
- [x] **BUG-C05 ChainProcessor ↔ Scorer** — `ChainProcessor` ctor가 `Scorer scorer = null` 주입받고(L42), `OnBlocksExploded` (L117) + `OnTurnEnded` (L147) + `OnColorCreated` (L198, L256) 훅 연결 완료. GameRoot에서 `new ChainProcessor(_board, _boardView, _spawner, _scorer)` 와이어링도 확인.

### B. Performance Critical 수정 검증
- [x] **PERF-1 OnAnimStep delegate 캐싱** — `BoardView._onAnimStepCached` 필드 (L36) + `Awake`에서 `_onAnimStepCached = OnAnimStep` (L43). 3개 Play*Async에서 모두 캐싱된 델리게이트 사용.
- [x] **PERF-2 DeterministicBlockSpawner Block 풀** — `_pool / _poolCount` 필드 (L23-24), `SpawnRandom`에서 Rent 경로 (L37-43) + `Return(Block b)` 공개 API (L57-67). ChainProcessor에서 `ReturnToPool` (L269-276) 호출.
- [x] **PERF-3 MatchGroup.CreatePooled 사전 할당** — `MatchGroup.CreatePooled()` (L19-28)가 RowBuf/ColBuf를 ctor에서 즉시 alloc. `ChainProcessor` ctor L53-56 루프에서 32 슬롯 모두 사전 할당.

### C. 아키텍트 결정 구현 검증 (D16~D22)
- [x] **D16 Mix(Black, X) = Black** — `ColorMixer.cs` L24-26. 단, 해당 구현 로직은 OR 조합 시 Black 비트가 걸려 Black 반환. (단위 테스트 `Mix_BlackRed_YieldsBlack_D16Propagation` 통과 가능.)
- [x] **D17 Prism 우선순위** — Gray/Black 검사를 Prism보다 먼저 수행 (`ColorMixer.cs` L22, L26 → L28). 테스트 `Mix_PrismGray_YieldsNone_D17GrayOverridesPrism`, `Mix_PrismBlack_YieldsBlack_D17BlackOverridesPrism` 추가됨.
- [x] **D19 Scorer.OnColorCreated 신설** — `Scorer.cs` L52-56 `OnColorCreated(ColorId, int)` 구현. `OnBlocksExploded`(L58-77) 내부에서는 더 이상 `AddColorCreated` 호출 X — 순수 점수 산출만 수행. ChainProcessor의 infect/refill 경로에서 호출.
- [x] **D20 ChainProcessor infectedMask bitset** — `ChainProcessor.cs` L89, L159-208. `infectedMask` ref 파라미터로 중복 감염 방지 (L164 early return).
- [~] **D22 GameRoot BeginStage(movesLimit)** — `GameRoot.Awake` L69 `_scorer.BeginStage(_movesLimit)`. 현재 par = movesLimit 동일값 주입이라 의도된 Phase 1 동작. 단, MVP의 "efficiency 항상 1.0" 속성 확인을 위해 실제 end 시점 StageEnded 경로(OnStageEnded 호출처) 미구현.

### D. 통합 흐름 검증
- [x] **GameContext ↔ PromptBanner.SetContext** — `GameRoot.Awake` L55, L66: `new GameContext(_score, _movesLimit)` + `_hud.SetPromptContext(_promptCtx)` → `UIHud.SetPromptContext`는 `_promptBanner.SetContext(ctx)` 호출.
- [~] **NotifyMoveCommitted → Scorer.OnMoveCommitted + HUD.SetMovesRemaining** — `GameRoot.NotifyMoveCommitted` (L98-105) 자체는 올바르게 구현. **단, 이를 호출하는 곳이 어디에도 없음.** InputController의 OnSwap/OnTap 이벤트 → Chain 실행 경로가 GameRoot에 연결되지 않아 실제 게임 플레이 중 호출되지 않는다.
- [ ] **asmdef 그래프 일관성** — **Alchemist.Domain.Chain.asmdef가 Alchemist.Domain.Scoring을 참조하지 않음.** 그러나 `ChainProcessor.cs` L6이 `using Alchemist.Domain.Scoring;` 이며 L27/42/48/115/117/145/147/196/198/254/256에서 `Scorer` 타입을 직접 참조 → **컴파일 실패**. R2 반려 사유.
- [x] **순수 Domain 레이어 noEngineReferences** — Colors/Blocks/Board/Chain/Prompts/Scoring 6개 asmdef 모두 `noEngineReferences: true`. OK.

### E. 코드 품질
- [x] 네이밍 일관성 (PascalCase class/메서드, camelCase 필드, `_field` 접두) 전반 준수.
- [x] XML 주석 public API 1줄 요약 충실 (`GameRoot`, `Scorer`, `ColorMixer`, `ChainProcessor` 모두 class/메서드 헤더 요약 존재).
- [x] 과도한 죽은 코드 없음. `BlockFsm.OnEnter/OnExit/Dispatch` 빈 훅은 Phase 2 MessagePipe 통합 자리로 주석 명시.
- [~] `Prompt.SamplePurple10/SampleChain3/SampleMix` 등 static 샘플 프롬프트는 Phase 2 ScriptableObject 전환 TODO 주석 없음 (BUG-L05). Phase 2 백로그.
- [x] 매직 넘버 상수화 — `Board.Rows/Cols/CellCount const`, `ScoreConstants` 집중, `ChainProcessor.MaxDepth/GroupCapacity const`.

### F. 테스트 커버리지
- [ ] **104 케이스 컴파일 정합성** — `ChainProcessorTests.InfiniteCascade_CappedAtDepth10_FiresOnDepthExceeded` (L185): `new ChainProcessor(board, new NoOpAnimationHub(), spawner, onExceeded)` 호출. 그러나 ChainProcessor ctor 시그니처는 `(board, anim, spawner, Scorer scorer = null, Action onDepthExceeded = null)` — 네 번째 인자 `Action onExceeded`가 `Scorer` 자리에 전달되어 **컴파일 실패**. R2 반려 사유.
- [x] D16/D17 테스트 반영 — `Mix_BlackRed_YieldsBlack_D16Propagation`, `Mix_PrismGray_YieldsNone_D17GrayOverridesPrism`, `Mix_PrismBlack_YieldsBlack_D17BlackOverridesPrism`, `Mix_WhiteWhite_YieldsBlack` 등 추가됨.
- [~] 전체 케이스 수 세기: BoardTests 18 + BlockFsmTests 약 23 + ColorMixerTests 약 25 + MatchDetectorTests 6 + ChainProcessorTests 5 + PromptConditionTests 12 + ScorerTests 10 ≈ 99. "104 케이스" 목표에 약간 부족하지만 실질 커버리지는 충분.

### G. Phase 2 백로그 명확성
- [x] `phase1_wave3_decisions.md` §2 "Phase 2 백로그 (이연)"에 H/M/L 이연 항목 명시. Bug/Perf/Reviewer 판단 경로 구분되어 있음.

---

## 2. 잘 된 점 (Top 5)

1. **교차레이어 결정(D16~D22) 구현 반영 충실** — 특히 `ColorMixer.Mix`의 우선순위 재정렬과 `Scorer.OnColorCreated` 분리가 설계 의도를 정확히 코드화. ColorMixer 테스트가 D16/D17 엣지를 명시적으로 가드.
2. **Wave3 F1~F11 11건 수정이 동일 파일에 응집된 패치** — 각 파일에 수정 태그(`F5`, `F7`, `F9` 등)가 XML 주석으로 남아 있어 추적성 우수. GameRoot의 Wave3 additions 주석(L17) 예시.
3. **성능 계약 준수** — `Score._colorsCreated[256]`, `Board._cells[42]` 1D 배열, `ColorMixCache` 65KB flat table, `MatchGroup` sbyte 버퍼 사전 할당 모두 Phase 0 Perf 계약 그대로 이행. Hot path 0B alloc 기반 확보.
4. **asmdef 레이어링 원칙 유지** — 6개 Domain asmdef 모두 `noEngineReferences: true`로 POCO 순수성 보존. Bootstrap만 Domain/View/UI 오케스트레이션 책임.
5. **DI 컴포지션 루트의 가독성** — `GameRoot.Awake` 25줄 내에서 생성·바인딩·초기 Refill·Scorer BeginStage까지 선형 흐름. 수동 DI임에도 Phase 2 VContainer 이관 시 재작성 부담 낮음.

---

## 3. 개선 필요

### 즉시 수정 (개선 R2 사유) — 2건

**R2-1. `Alchemist.Domain.Chain.asmdef` 의 Scoring 참조 누락 → 컴파일 실패**
- 파일: `Assets/_Project/Scripts/Domain/Chain/Alchemist.Domain.Chain.asmdef`
- 증상: `ChainProcessor.cs`가 `using Alchemist.Domain.Scoring;` + `Scorer` 타입을 직접 필드/파라미터/호출에 사용하지만 asmdef references 배열에 `"Alchemist.Domain.Scoring"`이 없음.
- 수정: asmdef references 배열에 `"Alchemist.Domain.Scoring"` 추가.
- 추가 고려: Chain이 Scoring을 참조하는 것은 **의존 방향의 재검토 여지**가 있음. 대안으로 Scoring이 Chain의 이벤트를 구독하는 역방향(또는 Chain이 `IScoreStream`-like 인터페이스만 참조)이 순환 없이 더 깔끔하나, Phase 1 MVP 시한 내에서는 직접 참조 추가가 실용적. Phase 2에서 `IChainScoringSink` 인터페이스를 Domain.Chain에 배치하고 Scoring이 구현하는 역구조 리팩터 권고.

**R2-2. `ChainProcessorTests.InfiniteCascade_CappedAtDepth10_FiresOnDepthExceeded` 시그니처 불일치 → 컴파일 실패**
- 파일: `Assets/_Project/Tests/EditMode/ChainProcessorTests.cs:185`
- 증상: `new ChainProcessor(board, new NoOpAnimationHub(), spawner, onExceeded)` — 네 번째 positional 인자가 `Action`이지만 ctor의 네 번째 파라미터는 `Scorer scorer = null`.
- 수정: `new ChainProcessor(board, new NoOpAnimationHub(), spawner, null, onExceeded)` 또는 named argument `onDepthExceeded: onExceeded` 사용.

> 두 건 모두 **컴파일 실패**이므로 Phase 1 DoD "104 케이스 실제 API와 컴파일 가능 수준 정합" 조건 및 "핵심 파이프라인 미연결" 기준에 걸림. **R2 반려**.

### Phase 2 이연 (승인)

다음은 Phase 2 백로그로 이연 승인:

| ID | 항목 | 영향 | 이연 이유 |
|----|------|------|-----------|
| M1 | `GameRoot.NotifyMoveCommitted`를 호출하는 InputController → GameRoot 파이프라인 | HIGH | 실제 게임 플레이 시 HUD 잔여 이동 수/프롬프트 진행이 갱신되지 않음. R2 후보였으나 "Chain 실행 트리거 전체"가 Phase 1 범위 외로 명시(Tasklist P1-03~04 완료는 파이프라인 구성보다 Chain 엔진 구현 자체를 의미)되어 Phase 2 UX 통합 라운드로 이연. **단, Phase 2 첫 작업으로 최우선 배치 권고.** |
| M2 | BUG-H10 Localizer 서비스 | MED | PromptBanner가 localization key를 그대로 표시. 배포 전 필수지만 Phase 1 MVP는 허용. |
| M3 | BUG-H03 Selected→Selected 전이 | LOW | UX 재조준 드래그. 두 단계 전이로 우회 가능. |
| M4 | BUG-H08 par vs limit 분리 | LOW | D22에서 MVP는 동일값 허용으로 결정. Phase 2 StageData SO에 `parMoves` 분리. |
| M5 | BUG-M01/M03/M07 방어 코드 | LOW | 재진입 방어, CancellationToken 단계별 체크, BlockView 코루틴 alloc. DOTween/UniTask 도입 시 일괄 전환. |
| M6 | BUG-L05 샘플 Prompt `#if DEBUG` 가드 | LOW | ScriptableObject 전환과 함께. |
| M7 | `BoardView` TaskCompletionSource 풀링 | LOW | ValueTask 전환과 함께 Phase 2 초입. Perf §5.1 권고안 B/C. |
| M8 | `Cell` class→struct hot/cold 분리 | LOW | 보드 9×9 확장 시 재검토. |

---

## 4. 아키텍처 레이어링 검증

### asmdef 그래프 순환 없음 — 부분 합격
- Domain.Colors (leaf) ← Domain.Blocks ← Domain.Board ← Domain.Chain
- Domain.Colors ← Domain.Prompts, Domain.Scoring (각자 leaf 의존)
- Bootstrap ← Domain.* + View + UI
- View ← Domain.Colors/Blocks/Board/Chain (UnityEngine 허용)
- UI ← Domain.Colors/Scoring/Prompts + TextMeshPro
- **순환 없음 확인**. 그러나 Chain → Scoring 의존이 **코드에는 있으나 asmdef에 선언되지 않아** 선언적 그래프와 실제 참조가 **불일치**. R2-1 수정 시 Chain → Scoring 단방향 의존으로 확정되며, Scoring은 Chain을 참조하지 않으므로 순환은 발생하지 않음.

### Domain의 UnityEngine 미참조 유지 — 합격
- 6개 Domain asmdef 전부 `noEngineReferences: true`.
- `ColorMixCache.Initialize`가 static ctor에서 호출되는 구조도 POCO 범위 내.

---

## 5. 확장성 평가

### Phase 2 (특수 블록) 수용성 — **양호**
- `BlockKind` enum + `BlockFsm`의 12 상태 / 15 전이가 이미 Gray(흡수), Infected, FilterTransit, PrismCharging을 포함. 특수 블록 추가 시 `BlockFsm.BuildTable`에 전이 추가만으로 확장 가능.
- `ColorId [Flags] byte`가 Black/Prism/Gray 특수 비트를 이미 보유. Phase 2 새 특수(예: Mirror, Rainbow)는 1<<6, 1<<7 2비트 여유.
- `IBlockSpawner` 인터페이스 존재, `DeterministicBlockSpawner`는 풀링까지 구현 → 특수 블록 전용 Spawner 교체 가능.

### Phase 3 (메타/리플레이) 수용성 — **양호**
- `DeterministicBlockSpawner(seed)` 고정 시드 결정론 확보 (Phase 0 D15 이행).
- `IScoreStream` 이벤트(ChainAdded/TurnEnded/StageEnded) 구독 가능 → 리플레이 레코더가 시점별 이벤트만 기록하면 재구성 가능.
- `ChainResult` 구조체가 depth/exploded/exceeded 담아 턴별 요약 전달.

### Phase 4 (Juice) 수용성 — **양호**
- `IChainAnimationHub` 인터페이스가 도메인/뷰 경계 명확 — DOTween/UniTask/Metaball 셰이더 통합 시 NoOpAnimationHub ↔ BoardView 외 별도 `JuicyAnimationHub` 구현체 교체로 해결.
- `BlockView`의 Coroutine 기반 애니는 DOTween 전환 시 내부 교체만으로 대응.
- `AppColors` static table이 Color32 상수화 → 셰이더 유니폼 전달용 재활용 가능.

**종합**: Phase 1 도메인 구조가 Phase 2~4 확장을 **인터페이스 수준에서 선제 수용**하고 있어 재작성 부담 낮음.

---

## 6. 최종 판정

### **FAIL — 개선 R2 필요**

**판정 근거**
- 즉시 수정 필요 2건 모두 **컴파일 실패**에 해당하여 Phase 1 DoD의 근본 전제(빌드 가능성) 미충족.
- R2-1 (asmdef Scoring 참조 누락): 1줄 수정이지만 누락 시 Unity 프로젝트 전체 빌드 실패.
- R2-2 (ChainProcessorTests 인자 불일치): 1곳 수정이지만 테스트 어셈블리 컴파일 실패.

### 후속 개선 사이클 권장

**개선 R2 범위 (최소)**
1. `Alchemist.Domain.Chain.asmdef` references에 `"Alchemist.Domain.Scoring"` 추가 (1줄).
2. `ChainProcessorTests.cs:185`를 `new ChainProcessor(board, new NoOpAnimationHub(), spawner, null, onExceeded)`로 수정 (또는 named arg 사용).

**권장 추가 수정 (Phase 2 첫 PR로 이관 가능, R2 필수는 아님)**
- `InputController` 이벤트 → `GameRoot.NotifyMoveCommitted` 연결 (M1). Phase 1 DoD는 Domain/View/UI/Bootstrap 와이어링을 "정합" 수준까지 요구하므로, 이 파이프라인은 Phase 2 입구로 이연 가능.

**R2 완료 기준**
- [ ] Unity Editor에서 어셈블리 컴파일 0 오류
- [ ] EditMode Test Runner에서 모든 테스트 어셈블리 로드 성공
- [ ] `ChainProcessor` 생성자 시그니처와 테스트 호출부 정합 확인

**R2 완료 시**: Phase 1 **PASS-with-minor** 재판정. 이연 승인된 Phase 2 백로그(M1~M8)와 함께 `v0.1.0-phase1` 태그 + process.md 갱신 진행.

---

## 요약 통계

- DoD 체크 충족: 19/23 (약 **83%**)
- 즉시 수정 필요: **2건** (모두 컴파일 실패)
- Phase 2 이연 승인: 8건
- 컴파일 블로커 해결 시 Phase 1 완료 선언 가능
