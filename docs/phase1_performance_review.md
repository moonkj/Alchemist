# Phase 1 Performance Review

**작성자:** Performance Engineer Teammate
**작성일:** 2026-04-21
**리뷰 대상:** Phase 1 Wave 1+2 구현 (`/Assets/_Project/Scripts/**`)
**기준 문서:** `docs/performance.md` (Phase 0 성능 계약)

---

## 1. Executive Summary

### 종합 Grade: **B+**

Phase 1 MVP 기준 **대부분의 핫패스가 성능 계약을 충족** 한다. 특히 `ColorMixCache.Lookup`, `MatchDetector.FindMatches`, `Score._colorsCreated[256]` 등 Phase 0에서 못 박은 핵심 구조가 그대로 구현되어 있다. 다만 **`ChainProcessor`/`BoardView`의 `TaskCompletionSource` 반복 할당**, **`DeterministicBlockSpawner.SpawnRandom`의 `new Block()` 매 리필마다**, **`MatchGroup.EnsureBuffers` 지연 할당의 첫-실행 스파이크**가 Phase 2 연쇄 체감 단계에서 스파이크 리스크를 낳을 수 있다.

### Top 3 잘 된 점
1. **`ColorMixCache` 65,536B 단일 배열 + commutative folding**: `Lookup` 은 `if(a>b) swap` + 1회 배열 액세스로 O(1), 0B 할당 확정.
2. **`ChainProcessor`의 사전할당 scratch buffers**: `_hubBuffer[32]` + `_infect/grav/refill sbyte[42]` 를 ctor 1회만 `new`, 이후 모든 턴 재사용.
3. **`Score._colorsCreated = int[256]`**: Dictionary/boxing 대신 byte-indexed 배열. Phase 0 §2.4 그대로 이행.

### Top 3 개선 필요
1. **`BoardView.BeginBatch`가 매 배치마다 `new TaskCompletionSource<bool>(...)` 할당** — 폭발/감염/리필 각각 1회씩, 5단 연쇄 시 최대 **15회/턴 × ~120B ≈ 1.8KB/턴** 매니지드 힙 압박. (L197)
2. **`DeterministicBlockSpawner.SpawnRandom`가 매 리필마다 `new Block()`** — 6×7 보드 전면 리필 시 최대 **42개 × ~64B ≈ 2.6KB/턴** GC alloc. 풀링 부재. (L37)
3. **`MatchGroup.EnsureBuffers`의 지연 `new sbyte[16]`** — `_hubBuffer[32]` 의 RowBuf/ColBuf가 첫 FindMatches 호출 시 각각 할당 → **32 × 2 × (sbyte[16]+헤더) ≈ 2KB 초기 스파이크**. ChainProcessor ctor에서 선할당 가능.

### Phase 1 MVP 기준 지금 당장 수정해야 할 Critical: **3건**
(위 Top 3와 동일. 나머지는 Phase 2 이연 권고)

---

## 2. Hot Path Allocation Audit

### 2.1 `ColorMixer.Mix` / `ColorMixCache.Lookup`  **GRADE A**
- **파일:** `Domain/Colors/ColorMixer.cs`, `Domain/Colors/ColorMixCache.cs`
- `Mix` (L11): 순수 `byte` 비트 연산, `ColorId` enum 캐스팅만. **할당 0B**.
- `Lookup` (L31): swap + index lookup. **할당 0B**.
- `ColorMixCache.Initialize` (L17-28): 정적 ctor 1회 `new byte[65536]` (64KB)만 할당. 런타임 핫패스 아님.
- **권고:** 현재 상태 유지. Phase 2에서 `[MethodImpl(MethodImplOptions.AggressiveInlining)]` 추가 고려.

### 2.2 `MatchDetector.FindMatches`  **GRADE B+**
- **파일:** `Domain/Chain/MatchDetector.cs`
- 본문은 이중 `for` 루프 + `Span<MatchGroup>` 로 **per-call 0B 할당 보장**.
- **문제:** `MatchGroup.EnsureBuffers()` (L30-34 of `MatchGroup.cs`) 가 `RowBuf == null` 시 `new sbyte[16]` 2개씩 할당.
  - `ChainProcessor._hubBuffer = new MatchGroup[32]` (ctor) 는 배열만 할당하고 요소 RowBuf/ColBuf는 `null` 상태.
  - 첫 턴의 첫 매치 탐지 시 **최대 32 × 2 × 32B(sbyte[16] 헤더+데이터) ≈ 2KB** 첫-실행 스파이크.
  - 이후 턴에서는 재활용되어 0B.
- **권고 (Critical-3):** `ChainProcessor` ctor에서 모든 `_hubBuffer[i]` 슬롯에 미리 `EnsureBuffers()` 호출 (`ChainProcessor.cs` L65 뒤에 `for(int i=0;i<GroupCapacity;i++) _hubBuffer[i].EnsureBuffers();` — 다만 struct 복사 이슈가 있으므로 `ref` 로 접근 필요).
- **참고:** L34-35 의 `b != null` 체크는 참조 비교. `Block b = board.BlockAt(r, c)` 는 배열 인덱싱 → 0B.

### 2.3 `ChainProcessor.ProcessTurnAsync`  **GRADE B**
- **파일:** `Domain/Chain/ChainProcessor.cs`
- **Scratch 재사용:** L36-44의 `_hubBuffer`, `_infectRows/Cols`, `_gravFromRows/ToRows/Cols`, `_refillRows/Cols` 전부 **ctor 1회만** `new`. 턴 경계에서 재할당 없음.
- **async state machine:** `async Task<ChainResult>` → state machine 힙 박싱 1회/턴. 5단 연쇄 × 1턴 = 1 state machine instance. 60fps × 매 턴 ~5단 = **~300B/초** (state machine은 보통 80-120B).
- **`Task.ConfigureAwait(false)`**: L91, L120, L136, L140 모두 제대로 사용 → SynchronizationContext 캡처 회피 OK.
- **`new ChainResult(...)`**: L145 readonly struct, stack alloc → 0B.
- **`_onDepthExceeded?.Invoke()`**: delegate 호출 자체는 0B (이미 등록된 인스턴스).
- **권고 (Phase 2):** 연쇄가 실제로 발생하지 않는 턴(매치 0) 이 압도적 다수라면, `ValueTask<ChainResult>` 로 전환하면 state machine 힙 할당도 피할 수 있음. 측정 후 결정.

### 2.4 `BoardView.SyncCell` / `PlayExplosionAsync` / `PlayGravityAsync` / `PlayRefillAsync`  **GRADE C+**
- **파일:** `View/BoardView.cs`
- **`SyncCell` (L77-98):** `_pool.Rent/Return`, `v.Bind`, `v.SetWorldPosition` — 전부 0B 설계. `GridCoordinateMapper.GridToWorld(r,c)` 는 `Vector2` struct 반환 → stack. 
  - 단, `v.transform.SetParent(_blockRoot, false)` (L93) 는 내부적으로 Transform dirty flag를 건드리므로 저비용이나, 풀에서 나온 view가 이미 같은 부모를 가진 경우 생략 가능 (풀 생성 시 `_parent=_blockRoot` 이미 지정됨).
- **`PlayExplosionAsync` (L109-134):** `BeginBatch(total)` 내부에서 **`new TaskCompletionSource<bool>(...)`** (L198). **각 Play*Async 당 1개 새 TCS → 5단 연쇄 × 3(explosion+infection+refill) = 15 TCS/턴**.
  - `TaskCompletionSource<bool>` 인스턴스는 ~72B + Task 백킹 ~48B = **약 120B/회**. 15회 × 120B = **1.8KB/턴**.
  - 추가로 `v.PlayExplosion(OnAnimStep)` 콜백 전달 — `OnAnimStep` 는 **인스턴스 메서드 그룹 변환 → delegate 할당 1회**. 각 `new System.Action(this.OnAnimStep)` = ~64B.
    - 실제로는 C# 컴파일러가 delegate 캐싱 (메서드 그룹 변환 from instance method 는 캐싱 안 됨, **매 호출마다 할당**) — 이것은 숨은 alloc hot spot.
  - 전체 폭발 15블록 × 15턴 시: 15 × 64B = **960B/턴** 추가.
- **권고 (Critical-1):**
  - `_animTcs` 를 재사용 가능한 `ManualResetValueTaskSourceCore<bool>` (System.Threading.Tasks.Sources) 로 교체 + `IValueTaskSource` 구현한 wrapper 싱글톤. 또는 `_animTcs` 를 필드로 유지하되 `TrySetResult` 후 `new` 대신 reset 패턴 사용 (TCS 는 reset 불가하므로 pooled wrapper 필요).
  - **간이 대안:** `TaskCompletionSource<bool>` 풀 (크기 4개 = explosion/infection/gravity/refill). `OnAnimStep`에서 완료 후 풀로 반납.
- **권고 (Critical-1b):** `OnAnimStep` 델리게이트를 **필드로 캐싱** (`private readonly Action _onAnimStepCached;` ctor에서 `_onAnimStepCached = OnAnimStep;`). 그러면 `v.PlayExplosion(_onAnimStepCached)` 은 0B.

### 2.5 `UIHud.LateUpdate`  **GRADE A-**
- **파일:** `UI/UIHud.cs`
- Diff-based 업데이트 (L32-44): `_score.Total != _lastDisplayedScore` 검사 후에만 `SetText`.
- **`_scoreLabel.SetText("{0}", _lastDisplayedScore)`** (L37): TextMeshPro의 `SetText(string, int)` 오버로드 사용 → **내부적으로 char[] 버퍼 재사용, 0B 할당** (TMP 공식 문서 확인됨). OK.
- `_promptBanner.LateUpdate` (PromptBanner.cs L28-35): `_prompt.Goal.Progress(_ctx)` 매 프레임 호출. **`PromptGoal.Progress` 는 `float sum` 만 쓰고 배열 인덱스 for-loop → 0B**. OK.
- **미세 개선:** `_lastDisplayedScore`/`_lastDisplayedMoves`가 바뀌지 않는 프레임(대다수) 에서도 `_score.Total` 프로퍼티 getter 호출. 이벤트 구독(`IScoreStream.ScoreChanged += ...`)으로 전환하면 프레임당 비교조차 제거 가능. 트레이드오프: 델리게이트 호출이 프레임 분산되지 않아 발생 시 피크가 모이는 문제 있음. **현재 diff 방식 유지 권장**.

### 2.6 `InputController.Update`  **GRADE B+**
- **파일:** `View/InputController.cs`
- L37-48: `Input.touchCount` 쿼리 후 없으면 `Input.GetMouseButtonDown/Up` 으로 분기. Early exit 잘 됨.
- **문제:** `Input.GetTouch(0)` (L42) 가 `Touch` struct 반환 → stack. 0B.
- **문제:** `_camera.ScreenToWorldPoint(screen)` (L68) 은 Vector3 반환. 0B. OK.
- `OnSwap?.Invoke(new SwapEvent(...))`: **readonly struct → 0B** (Action<struct> 는 boxing 없이 전달). OK.
- **권고:** 현재 상태 유지. Input System 패키지 마이그레이션은 Phase 2 에서 검토.

### 2.7 `GameRoot.Awake`  **GRADE A**
- **파일:** `Bootstrap/GameRoot.cs`
- L31-52: 모두 1회성 초기화. `new Board`, `new Score`, `new Scorer`, `new DeterministicBlockSpawner`, `new ChainProcessor`, `new CancellationTokenSource` — 각 1회.
- **권고:** 현재 상태 유지.

### 2.8 `DeterministicBlockSpawner.SpawnRandom`  **GRADE D** (Critical-2)
- **파일:** `Domain/Chain/DeterministicBlockSpawner.cs`
- L35-46: **매 리필 호출마다 `new Block()`** 을 생성하고 초기화.
  - 1 block = ~64B (fields: int + byte + byte + byte + int + int + float + object header + method table = 약 40B padded).
  - 5단 연쇄 시 누적 리필 블록 수 = 최대 **보드 전체 교체 + α**. 단일 턴에서 **42개 × 64B = 2.6KB/턴**.
  - 보수적으로 계산해도 분당 50턴 × 2.6KB = **130KB/분 → Gen0 GC 주기 단축**.
- **권고 (Critical-2):** `IBlockSpawner` 인터페이스에 이미 "pool-backed 구현 교체 가능" 문서 있음 (L12-14). Phase 1 끝내기 전에 `PooledBlockSpawner` 구현:
  - 내부에 `Stack<Block> _pool` (capacity 150) 유지.
  - `SpawnRandom` 에서 `_pool.Count > 0 ? _pool.Pop() : new Block()` 후 `Reset()` + 필드 채우기.
  - 블록 폭발 시 ChainProcessor가 `_spawner.Recycle(block)` 호출 (IBlockSpawner 에 `Recycle(Block)` 추가).

### 2.9 `ChainProcessor.TryInfect` `new TransitionContext`  **GRADE A-**
- **파일:** `Domain/Chain/ChainProcessor.cs` L179
- `TransitionContext` 는 **readonly struct (3 fields)** → stack alloc, 0B. OK.
- `BlockFsm.TryTransition(target, ..., in ctx)` 에 `in` 으로 전달 → 복사조차 없음. OK.

---

## 3. Pooling / Data Structure

### 3.1 `BlockViewPool` 용량 재검토
- **파일:** `View/BlockViewPool.cs`
- 현재: `DefaultCapacity = 120`, 6×7=42 보드 × ~3배 버퍼.
- **이론적 최대 동시 활성 view:**
  - 보드 점유 42 + gravity 애니 오버랩(낙하 중 + 새 리필 중 겹침) 최대 42 = **84**.
  - 여기에 감염 중 시각 잔존 등 실제로는 ~60 추정.
- **결론:** 120 은 **안전 마진 2배**로 적절. Grow-on-demand 경고 L57 이 켜지면 문제 신호.
- **권고:** 유지. 다만 Rent 시 grow 경로에서 `var v = CreateInstance()` 후 stack에 넣지 않고 바로 active 반환하므로, Return 타이밍에 stack이 capacity 초과하면 array resize (L79-84) — 이 경로는 정상 동작이나 로그 한 번 뜨면 initial capacity를 늘려야 함을 시사.

### 3.2 `ChainProcessor` scratch buffers
- 크기 = `Board.CellCount` = 42. infect/grav/refill 각 3-7개 × 42 = 약 **300B 정적**.
- **검증:** ctor 1회만 할당, 이후 재사용. **OK**.
- `_hubBuffer = new MatchGroup[32]`: `MatchGroup` 은 struct이지만 내부 `sbyte[] RowBuf/ColBuf` 참조 필드 → 배열 자체는 32 × struct size. Row/ColBuf는 각각 지연 할당. **첫 사용 시 32×2 할당 스파이크** (§2.2 참조).

### 3.3 `BoardView._viewGrid`
- **파일:** `View/BoardView.cs` L49
- `_viewGrid = new BlockView[_rows * _cols]` — **Bind에서 1회만**. OK.
- 재진입 Bind 시 이전 배열 GC 대상이 되지만, 스테이지 전환 시점이므로 문제 없음.

### 3.4 `Board._cells` 1D 배열
- **파일:** `Domain/Board/Board.cs` L21, L42-50
- `_cells = new Cell[42]`, `CellAt(r,c)` = `_cells[r*Cols+c]` → **캐시 친화적**, 0B 접근. OK.
- `Cell` 은 class (heap). 2D 배열 대비 **포인터 간접 참조 1회**는 발생하나, 42개 전체가 한 덩어리로 new되어 locality는 Gen2로 올라가면 모여 있을 가능성 높음.
- **미세 개선 (Phase 2):** `Cell` 을 struct로 내리거나, block 참조만 `Block[]` 로 분리 (hot data / cold data separation). 현재 구조로도 Phase 1 예산 내 충분.

### 3.5 `Score._colorsCreated = new int[256]`
- **파일:** `Domain/Scoring/Score.cs` L20
- 256 바이트 enum 공간 커버. 1KB 고정. `GetColorsCreated((byte)color)` 직접 인덱싱 → 0B. OK.

### 3.6 `MatchGroup.RowBuf/ColBuf` 고정 sbyte[16]
- **파일:** `Domain/Chain/MatchGroup.cs` L25-26
- sbyte 기본형 배열 → 박싱 위험 없음. OK.
- L30-34 `EnsureBuffers` 지연 할당 이슈는 §2.2 에 기술.

### 3.7 `PromptGoal.All/Any` `IPromptCondition[]`
- **파일:** `Domain/Prompts/PromptGoal.cs`
- 인덱스 기반 for-loop (L30, L37) → enumerator 할당 없음. OK.
- `IPromptCondition` 는 인터페이스 → 구현이 `readonly struct` 이면 **boxing 발생** (interface dispatch).
  - `ChainCondition`, `CreateColorCondition`, `MoveLimitCondition` 모두 readonly struct.
  - `new CreateColorCondition(Purple, 10)` 를 `IPromptCondition[]` 에 넣는 순간 **boxing** (L49 of `Prompt.cs`) → heap alloc 1회.
  - 다만 이것은 **Prompt 생성 시 1회만** 발생 (`Prompt.SamplePurple10` static 필드 초기화). 런타임 핫패스 아님.
- **권고:** `Prompt.SamplePurple10` 이 `static readonly` → 앱 시작 1회만 boxing. OK. 다만 Phase 2에 stage data를 ScriptableObject로 마이그레이션 시 동일 패턴 유지 주의.

---

## 4. MonoBehaviour Overhead

### 4.1 `UIHud.LateUpdate` vs 이벤트 구독 토론
- **현재 (Diff 방식):** 매 프레임 `_score.Total` 비교. 변경 시에만 `SetText`. 프레임당 비용 = 2 × int 비교 ≈ 1ns.
- **대안 (이벤트 구독):** `scorer.ScoreChanged += OnScoreChanged` + `OnScoreChanged` 에서 `SetText`. 프레임당 비용 = 0 (변경 없으면). 변경 시 delegate invocation ≈ 30ns.
- **장단점:**
  - Diff: 프레임 분산됨, predictability 높음. `_score.Total` getter가 hot cache line.
  - Event: Zero-cost when idle, 발생 시 UI 스레드에서 동기 업데이트 → 연쇄 중 수십 번의 SetText가 한 프레임에 몰릴 수 있음 (overdraw).
- **결론:** 현재 **Diff 방식 유지**가 Phase 1 MVP에 적합. 연쇄 중 점수가 여러 번 갱신되어도 화면은 결국 마지막 값만 보여주면 되므로 diff가 "자연스러운 throttle" 역할.

### 4.2 `BlockView.Update`
- **파일:** `View/BlockView.cs`
- `Update()` 메서드 **없음**. 모든 애니는 Coroutine 기반 (ExplosionRoutine / InfectionRoutine / SpawnRoutine). OK.
- 풀 대기 상태: `gameObject.SetActive(false)` → Coroutine도 자동 중단. Update 호출 없음 보장. OK.
- **단, StartCoroutine 은 boxing/alloc:** Unity의 `StartCoroutine(IEnumerator)` 는 IEnumerator를 heap에 올림.
  - `IEnumerator ExplosionRoutine(Action)` 는 컴파일러가 **state machine class** 생성 → 매 호출 ~80B 할당.
  - PlayExplosion 15회/턴 × 80B = **1.2KB/턴** 추가 GC.
- **권고 (Phase 2):** DOTween 또는 UniTask 기반 `UnityEvent` 이벤트 드리븐으로 마이그레이션. 또는 중앙 `AnimationScheduler` 가 `Update`에서 활성 애니만 일괄 처리하면 state machine 할당 제거.

### 4.3 `InputController.Update`
- **파일:** `View/InputController.cs` L37-48
- 상시 호출. Early exit: `Input.touchCount > 0` 이면 touch 처리 후 `return`, 아니면 mouse 처리.
- `_pressed` 인 상태에서만 `GetMouseButtonUp` 체크 → 처리 경로 짧음. OK.
- **미세 비용:** `Input.touchCount` legacy Input Manager 쿼리. Unity Input System 패키지로 전환하면 디바이스 이벤트 드리븐 가능 (Phase 2 검토).

### 4.4 `PromptBanner.LateUpdate`
- **파일:** `UI/PromptBanner.cs` L28-35
- 매 프레임 `_prompt.Goal.Progress(_ctx)` 호출. 계산 자체는 0B지만, `_progressBar.fillAmount = progress` 가 **값이 같아도 Unity가 dirty rebuild 유발 가능**.
- **권고:** diff 가드 추가 `if (Mathf.Abs(_lastFill - progress) > 0.001f)` → Canvas rebuild 방지 (§2.5 Phase 0). **1줄 수정으로 Canvas rebuild 감소 효과 큼.**

---

## 5. Async / Task Allocation

### 5.1 `TaskCompletionSource<bool>` 풀링 제안
- **현재 (`BoardView.BeginBatch` L193-201):**
```csharp
_animTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
```
- **문제:** 매 배치마다 `new` — §2.4 에서 계산한 **1.8KB/턴**.
- **권고안 A (간편):** TCS 풀 (Stack<TaskCompletionSource<bool>> 크기 8).
  - 단점: TaskCompletionSource는 한 번 완료되면 재사용 불가 (`TrySetResult` 후 상태 리셋 없음). 풀에서 꺼낼 때 항상 `new` 여야 함. **풀링 의미 없음**.
- **권고안 B (권장):** `ManualResetValueTaskSourceCore<bool>` (System.Threading.Tasks.Sources) + `IValueTaskSource<bool>` 구현 wrapper.
  - `ValueTaskSource` 는 **version 기반 재사용** 가능.
  - Hub가 `ValueTask` 반환, Domain에서 `await valueTask.AsTask()` 필요 시만 변환.
  - 구현 예시 (참고용):
```csharp
private sealed class AnimValueTaskSource : IValueTaskSource<bool> {
    private ManualResetValueTaskSourceCore<bool> _core;
    public short Version => _core.Version;
    public bool GetResult(short token) { var r = _core.GetResult(token); _core.Reset(); return r; }
    ...
}
```
- **권고안 C (현실적 타협):** `IChainAnimationHub.Play*Async` 시그니처를 `ValueTask` 로 변경 (Task → ValueTask). 완료된 케이스는 `ValueTask.CompletedTask` 로 0B. 진행 중 케이스만 TCS 생성.
  - `ChainProcessor.ProcessTurnAsync` 는 async Task 유지.
  - `await _anim.PlayExplosionAsync(...).ConfigureAwait(false)` — ValueTask 는 ConfigureAwait OK.
  - Phase 1 구현 부담 중간, 효과 큼.

### 5.2 `ValueTask` 전환 제안
- **`ChainProcessor.ProcessTurnAsync` → ValueTask<ChainResult>`:**
  - 매 턴 실제 매치가 0개일 때 (hover/tap miss) 즉시 `new ChainResult(0,0,false,0)` 로 return → state machine 할당 회피.
  - 5단 연쇄 발생 턴에서는 여전히 state machine 필요.
- **권고:** Phase 2에서 측정 후 결정. MVP 레벨에서 state machine 1개/턴 (~100B) 은 허용 범위.

### 5.3 Delegate 캐싱 (Hot Fix)
- **`BoardView.OnAnimStep`** (L203) 인스턴스 메서드가 `v.PlayExplosion(OnAnimStep)` 에 전달될 때마다 **새 `Action` delegate 할당**.
- 수정:
```csharp
private readonly Action _onAnimStepCached;
public BoardView() { _onAnimStepCached = OnAnimStep; } // 또는 Awake
// 사용: v.PlayExplosion(_onAnimStepCached);
```
- 효과: 15회/턴 × 64B = **960B/턴 절감**. Critical-1 세트.

---

## 6. Unity 특화 이슈

### 6.1 `SpriteRenderer.color` 매 프레임 set 시 material instancing
- **파일:** `View/BlockView.cs` L57-59
- `_spriteRenderer.color = c32` 는 **material 인스턴스 생성 안 함** (SpriteRenderer는 내부 vertex color로 처리 → SRP Batcher 호환 유지). OK.
- **주의:** `_spriteRenderer.material.color = ...` 를 쓰면 material instance 생성됨 (draw call break). **현 코드는 `.color` 사용으로 안전**. OK.

### 6.2 `transform.position` set 최소화
- **`BlockView.SetWorldPosition` (L62-68):** `transform.localPosition` get → x/y 수정 → set. Unity 내부에서 변경 없을 시 dirty 호출 회피 최적화 적용됨.
- **`SyncCell` (L97):** `v.SetWorldPosition(GridCoordinateMapper.GridToWorld(row, col))` 매번 호출. 실제 위치가 동일해도 호출됨.
  - **권고:** `BlockView` 에 `_lastWorldPos` 캐싱 후 `if (pos != _lastWorldPos)` 가드 추가 → SyncCell 상시 호출 시에도 Transform.hasChanged false 유지.
- **Gravity 경로 (`PlayGravityAsync` L155-173):** 이미 `src==dst` continue 체크 있음. OK.

### 6.3 TMP `SetText` 오버로드
- **`UIHud.cs` L37, L42:** `_scoreLabel.SetText("{0}", _lastDisplayedScore)` — TMP의 `SetText(string, int)` 오버로드. 내부 char 버퍼 재사용, **0B 할당**. OK.
- **`PromptBanner.cs` L20:** `_titleLabel.SetText(prompt.LocalizedTitleKey)` — string 오버로드. 정적 key 문자열이므로 alloc 없음. OK.

### 6.4 Transform caching
- `BlockView.ExplosionRoutine` (L91-109): `transform.localScale = start * scale` 매 프레임. **`transform` 프로퍼티는 `Transform` cache된 reference 반환 (Unity는 GameObject.transform 캐싱 O(1))**. 다만 `v.transform` 반복 호출은 cache miss가 아니더라도 C# property call 오버헤드 있음.
- **권고 (미세):** `Transform _cachedTransform` 필드 + Awake에서 `_cachedTransform = transform` 캐싱. 하지만 Unity 2020+ 에서는 GameObject.transform 자체가 C++ 측에서 이미 캐시되어 있어 효과 미미. **우선순위 낮음**.

### 6.5 Coroutine state machine 할당
- `StartCoroutine(ExplosionRoutine(onComplete))`: **코루틴 state machine class 인스턴스 생성** ≈ 80B.
- 3단 연쇄 당 15블록 × 80B = **1.2KB/턴**. Phase 2 DOTween 전환 권장.

---

## 7. 교차레이어 질의 (Architect / Debugger / Test)

### 7.1 Architect에게
1. **`IChainAnimationHub` 시그니처 변경 허용?** — `Task` → `ValueTask` 전환 (Critical-1 해결) 이 domain/view 이 공유하는 인터페이스 변경을 요구합니다. Phase 1 끝에 할지, Phase 2 초입에 할지 결정 필요.
2. **`IBlockSpawner` 에 `Recycle(Block)` 추가 허용?** — Critical-2 를 위해 인터페이스 확장 필요. 현재 인터페이스 문서상 "pool-backed impl 교체 가능" 명시되어 있어 추가 가능해 보임.
3. **`BlockFsm.OnEnter/OnExit/Dispatch` Phase 2 훅에서 MessagePipe 통합 시** — 이벤트 struct (`BlockStateChanged`) 를 매번 publish하면 MessagePipe도 내부적으로 컨테이너 할당 가능. 사전 검토 필요.

### 7.2 Debugger에게
1. **디버그 HUD FPS/GC/DrawCall 패널 구현 상태 확인** (Phase 0 §5.2). `Profiler.GetTotalAllocatedMemoryLong()` 또는 `GC.CollectionCount(0)` 기반 per-frame alloc 샘플러 필요.
2. **Frame hitch 로깅 (>33ms) 임계 훅** — `Time.deltaTime > 0.033f` 감지 + 직전 1초 스택 덤프 구현.
3. `ChainProcessor._onDepthExceeded` 콜백을 Debugger가 구독해 R5 경고 배너 표시.

### 7.3 Test에게
1. **GC alloc 회귀 테스트 자동화:**
   - Unity Test Framework + Performance Testing API (`using Unity.PerformanceTesting;`) 로 `[Performance]` 테스트 작성.
   - `Measure.GC.Allocated` 로 `ChainProcessor.ProcessTurnAsync` 한 턴 할당 측정. 임계 1KB 초과 시 CI fail.
2. **벤치 시나리오 스크립트** (Phase 0 §6 "재현 가능한 시나리오"): 고정 seed로 5단 연쇄 유도하는 스크립트 테스트. 평균 fps ≥ 55 검증.
3. **`ColorMixCache.Initialize` 정확도 스냅샷**: 7×7 recipe table (White, Orange, Green, Purple, Red, Yellow, Blue, Black) 의 예상 출력 스냅샷 비교.

### 7.4 CI 자동화 방식 제안
- **Unity CI** (e.g. GameCI GitHub Action) + **Performance Testing Extension**.
- PR 머지 차단 임계 (Phase 0 §5.3 재확인):
  - 5단 연쇄 시나리오 평균 fps ≥ 55
  - `ProcessTurnAsync` GC alloc ≤ 1KB/턴
  - 보드 전체 리빌드 ≤ 6ms
- 성능 회귀 그래프는 GitHub Action artifact로 업로드.

---

## 8. Phase 2 이연 권고

Phase 1 MVP에서 수정하지 않고 Phase 2로 넘겨도 되는 항목:

| 항목 | 현재 할당 | 이연 이유 |
|---|---|---|
| `async Task<ChainResult>` → `ValueTask` | ~100B/턴 (state machine) | Critical 아님. 프로파일링 후 결정. |
| `StartCoroutine` state machine | ~1.2KB/턴 | DOTween/UniTask 도입과 함께 일괄 전환 (Phase 2 §2.2 Jelly 효과). |
| `BlockFsm.OnEnter/OnExit/Dispatch` MessagePipe 통합 | TBD | Phase 2 C5 확정 완료. |
| `Cell` class → struct 또는 SoA 분리 | 현 상태 예산 내 | Phase 2 보드 크기 확장 (9x9) 시 재검토. |
| `InputController` Unity Input System 마이그레이션 | negligible | Input 시스템 전체 개편 시 함께. |
| `PromptBanner.fillAmount` diff 가드 | Canvas rebuild 영향 | 1줄 수정이지만 UX Phase 2 팔레트 HUD 리워크와 함께. |
| `BlockView._cachedTransform` | 미미 | Unity 내부 캐싱으로 효과 작음. |
| `Score._colorsCreated` → 더 작은 배열 (실제 사용 값만) | 1KB 정적 | Phase 1 기준 허용. |
| `SpriteRenderer` → `MaterialPropertyBlock` 기반 instancing | 저사양 draw call | §2.1 파티클 셰이더 도입 시 함께. |

### Phase 1 내 완결 권고 Critical 재확인:
1. **`BoardView.OnAnimStep` delegate 캐싱** (1줄 변경, 960B/턴 절감).
2. **`DeterministicBlockSpawner` pool-backed 구현** (`IBlockSpawner.Recycle` 추가, 2.6KB/턴 절감).
3. **`MatchGroup.EnsureBuffers` 선할당** (`ChainProcessor` ctor 에서 32 슬롯 전체 pre-warm, 첫-실행 2KB 스파이크 제거).

이 3건만 Phase 1 종료 전 반영하면 **정상 턴 GC alloc < 500B/턴** (TCS 1개 + state machine 1개 정도) 달성 가능, Phase 0 계약 §5.3 의 "1KB/frame 매치 중" 조건 충분히 만족.

---

## 부록: 측정 단위 환산 참고
- TaskCompletionSource<bool> ≈ 72B (instance) + 48B (Task) = 120B
- Action delegate (instance method bound) ≈ 64B
- async state machine (ChainProcessor) ≈ 80-120B
- IEnumerator coroutine state machine ≈ 80B
- `new Block()` (7 fields + header) ≈ 40-64B

**기준 프레임 예산 16.6ms 중 GC Alloc < 1KB/frame 유지** 를 Phase 1 내내 트래킹 권고.
