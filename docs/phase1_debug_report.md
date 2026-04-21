# Phase 1 Debug Report

> 작성: Debugger Teammate | 2026-04-21 | 대상 범위: Phase 1 Wave 1 + Wave 2 전체 구현

범위: Domain/Colors, Domain/Blocks, Domain/Board, Domain/Chain, Domain/Prompts, Domain/Scoring, View, UI, Bootstrap.
기준 문서: `docs/phase1_wave1_addendum.md` (C1~C5 결정), `docs/architecture.md`, `docs/performance.md`.

---

## 1. Critical (컴파일/크래시 → 즉시 수정 필요)

### BUG-C01 · 초기 보드 Refill 누락 — 첫 턴에 보드가 빈 채로 남음
- 파일: `Assets/_Project/Scripts/Bootstrap/GameRoot.cs:31-52`, `Assets/_Project/Scripts/Domain/Chain/ChainProcessor.cs:67-146`
- 증상: `GameRoot.Awake`가 `new Board()`만 수행 → `_board`는 42개 셀 모두 `Block == null`. `ChainProcessor.ProcessTurnAsync`를 호출해도 `ScanMatches() == 0`이라서 즉시 break. 즉 게임을 시작해도 **블록이 하나도 스폰되지 않음**.
- 근거: `ApplyRefill`은 루프의 **매치 발생 이후** 단계에서만 실행됨 (line 89 `if (n == 0) break;`).
- 수정 제안: `GameRoot.Awake` 말미 또는 별도 `StartStage()` 메서드에서 초기 Refill 1회 수행.
```csharp
// in GameRoot (after Bind)
for (int r = 0; r < Board.Rows; r++)
  for (int c = 0; c < Board.Cols; c++)
      _board.SetBlock(r, c, _spawner.SpawnRandom(r, c));
_boardView.RebuildAllCells();
```

### BUG-C02 · `ColorMixer.Mix(Prism, Gray)` 규격 위반 — Gray가 Mix에 참여해서는 안 됨
- 파일: `Assets/_Project/Scripts/Domain/Colors/ColorMixer.cs:14-15`
- 증상: `if (a == Prism) return b;`가 Gray/Black/None 특수 처리보다 **먼저** 평가됨. 결과: `Mix(Prism, Gray) = Gray`, `Mix(Gray, Prism) = Gray`. 이는 애드덤 C4 "Mix(Gray, *) = None"과 직접 충돌.
- 또한 `Mix(None, Prism) = Prism` (Prism 분기) vs `Mix(Prism, None) = None` (b==0이면 None) — `ColorMixCache.Lookup`이 `(min,max)` 정규화만 하므로 **캐시가 도메인 원본과 비대칭**하게 됨.
- 수정 제안: Prism 분기를 SpecialMask 검사 뒤로 이동.
```csharp
if (((ba | bb) & SpecialMask) != 0) {
    if (((ba | bb) & (byte)ColorId.Gray) != 0) return ColorId.None;
    // … 기존 Gray/Black 처리
}
// Prism 와일드카드는 Gray/Black 배제 이후에 평가
if (a == ColorId.Prism) return b == ColorId.None ? ColorId.None : b;
if (b == ColorId.Prism) return a == ColorId.None ? ColorId.None : a;
```

### BUG-C03 · `PlayGravityAsync`에서 `_viewGrid` null 접근 위험
- 파일: `Assets/_Project/Scripts/View/BoardView.cs:155-174`
- 증상: `Bind`가 호출되기 전에 `PlayGravityAsync`가 호출되면 `_viewGrid[src]` 접근이 NullReferenceException. 다른 Play*Async는 `count <= 0 return`·`ViewAt` null 체크가 있어서 안전하지만 Gravity는 없음.
- 근거: `Bind` 이전 `_viewGrid == null`, `_cols == 0`.
- 수정 제안:
```csharp
public Task PlayGravityAsync(...) {
    if (count <= 0 || _viewGrid == null) return Task.CompletedTask;
    // …
}
```

### BUG-C04 · `BoardView.Bind` 시 `_pool == null` 방어 누락 — prefab 미설정 시 SyncCell 실패 경로
- 파일: `Assets/_Project/Scripts/View/BoardView.cs:44-55, 77-98`
- 증상: `_blockPrefab`이 인스펙터에서 할당되지 않으면 `_pool == null`, `RebuildAllCells`는 조기 return 하지만 이후 `SyncCell → v = _pool.Rent()`에서 NullReferenceException.
- 근거: `SyncCell`이 `_pool` null 체크를 하지 않음.
- 수정 제안: `SyncCell` 진입부에 `if (_pool == null) return;` 추가 + `Bind`에서 prefab 미설정 시 LogError.

### BUG-C05 · `ChainProcessor`에서 Scorer 미연결 — 점수 이벤트 발생 안 함
- 파일: `Assets/_Project/Scripts/Bootstrap/GameRoot.cs:31-52`, `Assets/_Project/Scripts/Domain/Chain/ChainProcessor.cs`
- 증상: `ChainProcessor`는 폭발 시 `Scorer.OnBlocksExploded`를 호출하지 않음. GameRoot에서도 와이어링 없음. `_scorer`는 초기화되지만 이벤트 싱크에 연결되지 않아 **Total이 항상 0으로 표시됨**.
- 근거: `ChainProcessor.ProcessTurnAsync` 본문에 Scorer 참조 0개. `grep -n "Scorer" ChainProcessor.cs` = miss.
- 수정 제안 (2 가설):
  - (a) ChainProcessor가 `IScoreStream` 또는 `Scorer`를 생성자 주입받아 폭발 마다 `OnBlocksExploded(g.Color, g.Count, depth + 1)` 호출.
  - (b) ChainResult에 그룹별 payload를 담아 반환 후 GameRoot에서 채점. 검증 방법: 목업 테스트에서 Purple 3개 매치 → `Score.Total` 기대값 확인.
- 권고: (a) 방식. ProcessTurnAsync 내 explosion 단계 직후 Scorer 호출. Wave 2 Coder에게 인계.

---

## 2. High (논리 오류, 사양 불일치)

### BUG-H01 · `ColorMixer.Mix`의 Prism 분기가 교환법칙을 깸
- 파일: `Assets/_Project/Scripts/Domain/Colors/ColorMixer.cs:14-15, 20-21`
- 증상: `Mix(Prism, None) = None` (Prism 분기는 b 반환) / `Mix(None, Prism) = Prism` — 비대칭.
- `ColorMixCache.Lookup`은 `(min,max)` 정규화하므로 **어떤 순서로 호출해도 동일 값 반환 보장**하지만, 캐시 생성 시 `for (int a=0; a<256; a++) for (int b=a; b<256; b++)` 구조라 `a=0, b=Prism(16)` 쌍에서 `Mix(None, Prism) = Prism`이 기록됨 → Lookup 결과는 항상 Prism.
- 가설: (a) Prism 와일드카드 의도상 상대 색 그대로 반환, None과 섞이면 None으로 귀결되는 것이 자연. (b) None+Prism도 Prism을 유지하는 것이 의도일 수도. 확인 필요.
- 수정 제안: 교환법칙 보장 + None 배제를 명시적 정책으로.
```csharp
if (ba == 0 || bb == 0) return ColorId.None;   // None 우선 처리
if (a == ColorId.Prism) return b;
if (b == ColorId.Prism) return a;
```

### BUG-H02 · `ColorMixer.Mix`의 White+Primary 검사 순서로 Prism과 함께 쓰일 때 오진
- 파일: `Assets/_Project/Scripts/Domain/Colors/ColorMixer.cs:35-41`
- 증상: `Mix(Prism, White)`는 14행 분기로 `return White` 정상. 그러나 **Prism 분기를 특수 마스크 뒤로 옮기면** `White | Prism(16) = 23` — `& SpecialMask` 비교에서 Prism 비트가 걸려서 None으로 떨어질 수 있음. BUG-C02 수정 시 Prism을 SpecialMask에서 제외하거나 Gray 단독 체크로 범위를 좁혀야 함.
- 수정 제안: SpecialMask를 Black|Gray만 포함시키고 Prism은 별도 와일드카드로 취급.
```csharp
private const byte SpecialMask = (byte)(ColorId.Black | ColorId.Gray); // Prism 제외
```

### BUG-H03 · `BlockFsm` 전이 표에 `Selected -> Selected` 누락 (드래그 타겟 재조준)
- 파일: `Assets/_Project/Scripts/Domain/Blocks/BlockFsm.cs`
- 증상: UX §4.1 "드래그 중 스왑 상대 변경" 시나리오에서 Selected 재지정 경로가 필요할 수 있음. 현재 전이 표는 Selected→Idle→Selected 경유만 허용.
- 가설: (a) 두 단계 전이로 우회 가능(성능 영향 미미). (b) 애드덤 C1b "15건 그대로 유지"가 이 제약을 수용.
- 결론: Phase 1에서는 문제없음 (UX 매뉴얼 상 우회 경로 존재). **Low 이슈로 강등 가능**.

### BUG-H04 · `Scorer.OnBlocksExploded`가 "생성" 카운터에 폭발 카운트를 혼용
- 파일: `Assets/_Project/Scripts/Domain/Scoring/Scorer.cs:52`
- 증상: `_score.AddColorCreated(color, count)`를 폭발 시 호출. 애드덤 C2는 "유저가 직접 **만든** 블록 수"를 요구 — 생성(merge/infection 결과로 새 색 블록이 보드에 등장)과 폭발(그 블록이 3개 연결되어 사라짐)은 분리되어야 의미 상 정확.
- 가설: (a) Phase 1 디자인에서는 생성 = 곧 폭발 이벤트가 채점 타이밍이라 의도적. (b) 실제로는 `BlockInfected` / merge 완료 이벤트에서 집계해야 함.
- 검증: `Mix(Red, Blue) = Purple` 보드에서 Purple 블록이 **폭발 전에 존재만 해도 카운트되어야 하는가?** 현 구현은 폭발 시점에만 증가 → 만약 유저가 Purple을 만들고 폭발시키지 않으면 "생성 10" 목표 진행 안 됨.
- 수정 제안: 별도 `OnColorCreated(color, count)` 훅을 만들고 ChainProcessor 감염/머지 단계에서 호출. 또는 디자인 결정 재확인.

### BUG-H05 · `ChainProcessor.TryInfect`가 동일 웨이브 내 중복 감염 허용
- 파일: `Assets/_Project/Scripts/Domain/Chain/ChainProcessor.cs:99-118, 159-200`
- 증상: 같은 폭발 웨이브에서 한 primary 블록이 여러 이웃 폭발 셀의 감염 대상이 될 수 있음. 첫 감염으로 색이 secondary가 되면 `IsPrimary(target.Color)`가 false가 되어 2차 감염은 무시됨(안전). 단 동일 웨이브에서 **여러 폭발 셀이 동시에** 이 primary를 타깃 → for 루프 순서에 따라 어떤 색이 먼저 입혀지는지 비결정적이 될 수 있음.
- 실제로는 `visitedLo`가 폭발 셀만 dedupe하고 **감염 대상 셀은 dedupe 안 함**.
- 가설: (a) 기대 동작 — 마지막 감염 색이 남음. (b) 감염 대상도 visited 세트로 묶어야 함.
- 수정 제안: 감염된 셀을 별도 bitset에 기록하여 한 웨이브 내 재감염 방지.
```csharp
ulong infectedMask = 0UL;
// TryInfect 내:
if ((infectedMask & mask) != 0) return infectCount;
infectedMask |= mask;
```

### BUG-H06 · `ChainProcessor`가 감염 후 즉시 재매치 스캔 하지 않음 (웨이브 루프는 맞음, 주석과 체크리스트 요구사항 확인)
- 파일: `Assets/_Project/Scripts/Domain/Chain/ChainProcessor.cs:73-143`
- 증상: Explode → Infect → Remove → Gravity → Refill → (루프 재시작 시 재매치). 감염 후 **즉시** 재매치가 아니라 Gravity+Refill 후에 재매치. 체크리스트가 요구하는 "감염 전파 후 재매치 스캔"은 다음 depth iteration 진입으로 충족됨. ✓
- 판정: 통과. 체크리스트의 "재매치 스캔"은 중력 후 재스캔을 의미 → 정상.

### BUG-H07 · `ChainProcessor`가 "감염된 블록이 2차 색이 되면 같은 웨이브에 폭발 포함해야 하는가?" 규격 미정
- 파일: `Assets/_Project/Scripts/Domain/Chain/ChainProcessor.cs`
- 증상: 현재 구현에서는 감염 후 색 변경되더라도 같은 웨이브 내에서는 폭발 대상이 되지 않음 (ScanMatches는 루프 시작 시 1회). 다음 depth에서 3연결이면 비로소 폭발.
- 규격 모호: 애드덤에 해당 처리 미정. 다음 depth 진입하면 chain depth += 1 → depth 카운트 부풀림 가능.
- 수정 제안: 디자인 결정 필요 항목으로 이관 (§5).

### BUG-H08 · `Scorer.OnStageEnded`의 `movesLimit` vs `BeginStage(goalMoveCount)` 의미 중복
- 파일: `Assets/_Project/Scripts/Domain/Scoring/Scorer.cs:33-36, 82-112`, `Assets/_Project/Scripts/Bootstrap/GameRoot.cs:50`
- 증상: `BeginStage`는 par (efficiency 분모), `OnStageEnded(movesLimit, ...)`는 hard cap (residual 분모)로 분리. 하지만 GameRoot는 `BeginStage(_movesLimit)`으로 동일 값 주입 → efficiency ratio가 항상 1.0이 되어 최대 보너스(200점) 고정 발생.
- 가설: (a) Phase 1 MVP는 의도적. (b) Phase 2에서 Stage 데이터로 par 값 분리.
- 판정: 애드덤 C3 "PerfectMoveCount 필드 추가 예정" 참조. 현 단계는 허용 범위. 확인 요청 §5에 올림.

### BUG-H09 · `PromptBanner`가 IPromptContext 미연결 → 진행바 항상 0
- 파일: `Assets/_Project/Scripts/UI/PromptBanner.cs:23-35`, `Assets/_Project/Scripts/Bootstrap/GameRoot.cs`
- 증상: `SetContext(ctx)`가 어디서도 호출되지 않음. `_ctx == null` 체크로 진행바 업데이트 스킵 → 목표 진행 상태가 화면에 반영 X.
- 원인: `IPromptContext` 구현체(GameContext)가 Phase 1 Wave 2 범위에 부재. GameRoot도 미구현.
- 수정 제안: Wave 3에 `GameContext : IPromptContext` 추가 및 GameRoot에서 `_promptBanner.SetContext(gameContext)` 호출.

### BUG-H10 · `PromptBanner.Bind`가 로컬라이즈 키를 그대로 화면 표시
- 파일: `Assets/_Project/Scripts/UI/PromptBanner.cs:19-21`
- 증상: `_titleLabel.SetText(prompt.LocalizedTitleKey)` — "prompt.create_purple_10"이 UI에 그대로 나옴.
- 수정 제안: ILocalizer 인터페이스 주입. Phase 1 MVP는 임시로 `string.Concat("[key] ", key)` 등 placeholder 허용하나, 배포 전 반드시 L10n 서비스 연동.

### BUG-H11 · `UIHud.SetMovesRemaining` 호출 경로 부재 → 잔여 이동 수 표시 고정
- 파일: `Assets/_Project/Scripts/UI/UIHud.cs:27-30`, `Assets/_Project/Scripts/Bootstrap/GameRoot.cs`
- 증상: `SetMovesRemaining` API는 있지만 GameRoot나 InputController 어디서도 호출하지 않음. `OnMoveCommitted` 이후 HUD 업데이트 경로가 끊김 → 15로 고정 표시.
- 수정 제안: Scorer에 `event Action<int> MovesUsedChanged` 또는 GameRoot에서 매 move 후 `_hud.SetMovesRemaining(_movesLimit - _score.MovesUsed)` 직접 호출.

### BUG-H12 · `DeterministicBlockSpawner.SpawnRandom`에서 `new Block()` 매번 alloc — 초기 Refill로 42개 GC
- 파일: `Assets/_Project/Scripts/Domain/Chain/DeterministicBlockSpawner.cs:37`
- 증상: 매 스폰마다 `new Block()` → 매 턴 Refill 시 GC 할당. Perf §2.3 "GC alloc 0 steady-state" 위반.
- 수정 제안: `BlockPool`을 도입하여 SpawnRandom이 풀에서 Rent. Phase 1 MVP 주석 있으나 Wave 3에서 개선 필요.

---

## 3. Medium (성능/동시성 잠재 위험)

### BUG-M01 · `BoardView.BeginBatch`가 이전 배치 완료 전에 호출되면 `_animTcs` 덮어씀
- 파일: `Assets/_Project/Scripts/View/BoardView.cs:193-216`
- 증상: `PlayExplosionAsync → PlayInfectionAsync` 순차 await이면 안전하나, 병렬 호출(입력 중첩, 재진입) 시 `_animPending`과 `_animTcs`가 덮어써져 이전 배치 영원히 pending. 이전 Task를 await 중인 ChainProcessor는 deadlock 가능.
- 가설: (a) Phase 1 MVP는 ChainProcessor가 순차 await이라 발생 안 함. (b) 그럼에도 방어코드 권장.
- 수정 제안: `_animTcs != null`이면 이전 tcs를 `TrySetCanceled()` 또는 assert/throw.

### BUG-M02 · `BoardView.OnAnimStep`의 `_animPending` underflow 가능
- 파일: `Assets/_Project/Scripts/View/BoardView.cs:203-216`
- 증상: `OnAnimStep`이 예상 횟수보다 많이 호출되면 `_animPending`이 음수로 감소. 현재는 `<= 0` 체크라 첫 완료 시 TrySetResult 후 `_animTcs = null` → 이후 `TrySetResult`는 no-op. 안전.
- 판정: 방어 로직 정상. 통과.

### BUG-M03 · `ChainProcessor`가 `CancellationToken`을 각 단계마다 체크 안 함
- 파일: `Assets/_Project/Scripts/Domain/Chain/ChainProcessor.cs:67-146`
- 증상: `while` 루프 상단에서 `ct.IsCancellationRequested`만 체크. 각 Play*Async는 `ct`를 전달하지만 NoOpAnimationHub는 무시. 감염/Gravity/Refill 사이에는 ct 체크 없음.
- 가설: Phase 1 헤드리스 테스트 환경에서는 문제없음. 긴 체인에서 중간 취소 불가.
- 수정 제안: 각 `await` 후 `ct.ThrowIfCancellationRequested();` 또는 break.

### BUG-M04 · `ChainProcessor._hubBuffer`의 MatchGroup 재사용 — RowBuf/ColBuf stale data
- 파일: `Assets/_Project/Scripts/Domain/Chain/MatchGroup.cs`, `Assets/_Project/Scripts/Domain/Chain/MatchDetector.cs:100-107`
- 증상: `Reset(color)`는 Count만 0으로. 이전 배치가 Count=10이었다가 이번에 Count=3이면 index 3~9는 stale 값. 소비자(BoardView.PlayExplosionAsync)는 Count만 읽으므로 안전하나 디버깅 혼란 유발.
- 판정: 기능상 통과. 디버그 가독성 Low.

### BUG-M05 · `ColorMixCache.Initialize`의 이중 루프 65k 번 Mix 호출 → 정적 초기화 지연
- 파일: `Assets/_Project/Scripts/Domain/Colors/ColorMixCache.cs:17-28`
- 증상: 첫 접근 시 65,536회 Mix 호출. 모바일에서 수 ms 지연 가능. 단 1회뿐이라 게임 로드 중 수용 가능.
- 판정: 통과. Prewarm을 GameRoot.Awake에서 명시적으로 한 번 호출하는 것을 권장 (`ColorMixCache.Lookup(ColorId.Red, ColorId.Red);`).

### BUG-M06 · `ChainProcessor.visitedLo` 주석과 코드 불일치 (42셀 < 64 비트이면 OK)
- 파일: `Assets/_Project/Scripts/Domain/Chain/ChainProcessor.cs:23-24, 95-123`
- 증상: 주석은 "two ulongs = 128 bits"인데 실제 코드는 `ulong visitedLo` 단일. 42 cells < 64 bits이라 기능 OK.
- 수정 제안: 주석 업데이트 또는 Rows/Cols 확장 대비 2-ulong 버전 유지.

### BUG-M07 · `BlockView`의 Coroutine + `Action` 콜백 조합이 GC 할당 유발 (async state machine/closure)
- 파일: `Assets/_Project/Scripts/View/BlockView.cs:71-87`
- 증상: `PlayExplosion(OnAnimStep)`에서 `OnAnimStep`은 메서드 그룹 → delegate 생성 시 할당 (매 호출마다). `StartCoroutine`도 `IEnumerator` alloc.
- 가설: Unity Coroutine 자체가 reserve된 alloc 존재. 회피하려면 UniTask 등 pool-backed async 필요.
- 수정 제안: 메서드 그룹을 필드에 캐시: `private readonly Action _onAnimStepCached; ... _onAnimStepCached = OnAnimStep;` BoardView ctor에서 초기화.

### BUG-M08 · `InputController._camera.ScreenToWorldPoint(screen)`의 Vector2→Vector3 변환 boxing 없음이지만 Z축 누락
- 파일: `Assets/_Project/Scripts/View/InputController.cs:68`
- 증상: 2D orthographic 카메라라면 Z는 무관하지만 3D perspective 카메라라면 z=0 → near plane에서 world 좌표 왜곡.
- 수정 제안: `new Vector3(screen.x, screen.y, -_camera.transform.position.z)` 사용 또는 orthographic 가정 명시.

### BUG-M09 · `BoardView.PlayGravityAsync`의 dst 기존 뷰 존재 시 무조건 pool 반환 — silent 데이터 손실
- 파일: `Assets/_Project/Scripts/View/BoardView.cs:168-170`
- 증상: `_viewGrid[dst] != null`이면 `_pool.Return(_viewGrid[dst])` 후 새 뷰로 덮어씀. 이는 로직 버그(도메인과 뷰 동기화 불일치)의 증상일 수 있으나 경고 로그 없음.
- 수정 제안: `Debug.LogWarning("[BoardView] Gravity collision at dst")` 추가.

---

## 4. Low (코드 품질, 리팩터 후보)

### BUG-L01 · `GridCoordinateMapper`와 `Board`의 Rows/Cols 중복 상수
- 파일: `Assets/_Project/Scripts/View/GridCoordinateMapper.cs:16-17`, `Assets/_Project/Scripts/Domain/Board/Board.cs:13-14`
- 증상: 두 곳에 하드코딩. 한쪽을 변경하면 다른 쪽과 어긋남.
- 수정 제안: View asmdef가 Board를 참조하므로 `GridCoordinateMapper.Rows => Board.Rows` 또는 상수 파일 통합.

### BUG-L02 · `BlockFsm.OnEnter/OnExit/Dispatch` 빈 메서드 — JIT 인라인 OK이나 Phase 2 TODO
- 파일: `Assets/_Project/Scripts/Domain/Blocks/BlockFsm.cs:73-87`
- 증상: 세 빈 hook이 매 전이마다 호출됨. Phase 2에서 MessagePipe 연결 시 채움.
- 판정: 통과. TODO로 유지.

### BUG-L03 · `Scorer.OnBlocksExploded`의 `(int)(float)` 절삭 — 음수 점수(Black) 방향 버그 없음
- 파일: `Assets/_Project/Scripts/Domain/Scoring/Scorer.cs:60`
- 증상: `(int)(-20 * 1.5f * 3) = (int)(-90f) = -90`. `Score.AddPoints(-90)` → clamp at 0. OK.
- 판정: 통과. 명시적 truncation toward zero 문서화됨.

### BUG-L04 · `MatchDetector`의 Horizontal/Vertical overlap → ChainProcessor의 bitset으로 dedupe
- 파일: `Assets/_Project/Scripts/Domain/Chain/MatchDetector.cs`, `Assets/_Project/Scripts/Domain/Chain/ChainProcessor.cs:95-118`
- 증상: "Purple 3개 가로 + 3개 세로 교차(L자/T자)" 시 교차 셀이 두 group에 포함되지만 `visitedLo` bitset으로 dedupe. 점수 계산 시 `g.Count`를 사용하면 중복 카운트.
- 수정 제안: Scorer 연결 시 **per-group count**가 아니라 **wave-level unique exploded count**를 사용하거나 dedupe 후 Scorer 호출.

### BUG-L05 · `Prompt`의 sample prompts가 static readonly로 production 런타임까지 유지
- 파일: `Assets/_Project/Scripts/Domain/Prompts/Prompt.cs:44-83`
- 증상: 샘플 프롬프트가 assembly에 static으로 영구 pin. Phase 2에서 ScriptableObject 로딩으로 대체하면 제거해야.
- 수정 제안: `#if UNITY_EDITOR || DEBUG` 가드.

### BUG-L06 · `DeterministicBlockSpawner._nextId` overflow 방어 없음
- 파일: `Assets/_Project/Scripts/Domain/Chain/DeterministicBlockSpawner.cs:27, 39`
- 증상: int.MaxValue 도달 시 overflow → 0. 실무에서는 수십억 이전에 게임 종료되지만 기술적 결함.
- 판정: 통과. 현실성 부족.

### BUG-L07 · `Board.ClearDirtyFlags`가 현재 호출되는 곳 없음
- 파일: `Assets/_Project/Scripts/Domain/Board/Board.cs:72-78`
- 증상: IsDirty 플래그 세팅만 하고 clear 루틴 미사용 → 모든 셀이 영구히 Dirty. 현재 View가 IsDirty를 읽지 않고 전체 SyncCell 하므로 무해.
- 수정 제안: Phase 2에서 per-cell diff 구현 시 BoardView.LateUpdate에서 호출.

### BUG-L08 · `Cell.FilterColor`가 non-nullable ColorId — None 센티널로만 구분
- 파일: `Assets/_Project/Scripts/Domain/Board/Cell.cs:22-23`
- 증상: `ColorId?` (nullable) 대신 `ColorId.None`을 센티널로 사용. 도메인 단순화 OK. `Layer != Filter`일 때 값 무시 주석 명시됨.
- 판정: 통과.

### BUG-L09 · `ColorMixCache.Initialize`가 private이 아닌 public — 외부에서 재호출 가능
- 파일: `Assets/_Project/Scripts/Domain/Colors/ColorMixCache.cs:17`
- 증상: 공용 API로 노출. idempotent 하지만 실수로 호출되면 65k 루프 반복.
- 수정 제안: internal로 변경하거나 XML comment에 "테스트 전용" 명시.

### BUG-L10 · `ColorId`에 `[Flags]`이지만 `Black`/`Prism`/`Gray`가 단일 비트 — OR 조합 의미 없음
- 파일: `Assets/_Project/Scripts/Domain/Colors/ColorId.cs`
- 증상: White | Black = 15 같은 조합은 논리적으로 무의미하나 byte 비트로 표현 가능. 256 배열 인덱스가 이런 non-canonical 조합도 포함.
- 판정: Score._colorsCreated[256]은 모든 조합 safe. OK.

---

## 5. 규격 모호점 (Architect 확인 요청)

### Q1 · `Scorer.BeginStage(goalMoveCount)` vs `OnStageEnded(movesLimit, ...)` 의미론
- GameRoot는 둘 다 `_movesLimit=15`로 넘김 → efficiency ratio 항상 1.0.
- 애드덤 C3에서 "PerfectMoveCount 필드 Phase 2 추가 예정"인데 Phase 1 MVP에서 par ≠ limit 구분을 **어떻게 처리**할지 명시 필요.
- 검증: 스테이지 JSON/SO 데이터 샘플에 `parMoves` + `maxMoves` 두 필드 추가 및 GameRoot에서 분리 주입.

### Q2 · 감염으로 2차 색이 된 블록이 "현재 웨이브"에서 폭발 가능한가?
- 현 구현: 다음 depth 루프의 ScanMatches에서 감지 → 폭발. 이는 chain depth를 1단계 부풀림.
- 대안: 감염 후 즉시 재스캔 → 같은 depth에서 합산. 스코어 multiplier가 달라짐.
- 검증: 기대 플레이 패턴 (예: "3연결 → 감염 → 새 3연결" 시 유저 인식상 같은 chain인가 다른 chain인가) 디자인 결정.

### Q3 · `OnBlocksExploded`의 `AddColorCreated` 타이밍
- 현재: 폭발 = 생성 카운트 증가. 실제로 "Purple 생성"은 merge/infection 시점.
- "유저가 만들었지만 폭발시키지 않은 Purple"을 집계 대상으로 볼지 결정 필요.
- 검증: 프롬프트 "Purple 10 생성" 달성 정의 명시.

### Q4 · `ColorMixer`의 Prism 와일드카드와 None/Gray/Black 우선순위
- 애드덤 C4는 `Mix(Prism, Prism) = Prism`, `Mix(Gray, *) = None`, `Mix(Black, *) = Black`, `Mix(White, *) = Black` 명시.
- `Mix(Prism, Gray)`, `Mix(Prism, Black)`, `Mix(Prism, None)`, `Mix(Prism, White)`의 기대값은 각각 None, Black, None, White인가? 현 코드와 다름 (BUG-C02).
- 검증: 결정 표 (10×10 = 100 케이스) 스프레드시트로 확정 후 ColorMixCache 스팟체크 유닛테스트 추가.

### Q5 · 감염 대상 dedupe 정책
- 같은 웨이브에서 한 primary 블록이 여러 폭발 셀의 감염 대상이 될 때 결정적 규칙이 필요 (BUG-H05).
- 옵션: (a) 첫 감염만 적용, (b) 마지막 감염 색이 남음, (c) 여러 색의 조합(Mix)을 시도.
- 검증: 결정 후 `infectedMask` 도입.

---

## 6. 통과한 검증 항목 (긍정 리포트)

- **P1** `MatchDetector`가 1차 색(Red/Yellow/Blue) 단독 매치를 `IsMatchEligible` 필터로 정확히 억제 — 애드덤 C3 준수.
- **P2** `ChainProcessor.MaxDepth` 가드가 `while` 루프 **상단**에서 `depth >= 10` 체크 → 11회 이상 돌지 않음. `depth++`는 루프 말미에서만 수행되므로 off-by-one 없음.
- **P3** `Score.GetColorsCreated(Purple)` exact match — `_colorsCreated[(byte)Purple]` 인덱스만 참조. 애드덤 C2 준수.
- **P4** `BlockFsm` 전이표 15건이 Wave 1 Addendum C1b와 일치 (Gray → Idle 미포함, Absorbed → Gray 일방향).
- **P5** `MatchGroup.RowBuf/ColBuf`는 sbyte[16] lazy alloc — `EnsureBuffers` 최초 1회만 alloc. Hot path에서 zero-GC.
- **P6** `Score._colorsCreated`가 `int[256]`로 Dictionary 회피 — perf §2.3 준수.
- **P7** `Board._cells`가 1D `Cell[]` (row-major) — perf §2.3 직접 인덱싱 준수.
- **P8** `AppColors.TryGet` switch-on-byte (박싱 없음) + `out Color32` — alloc-free.
- **P9** asmdef 의존 그래프가 레이어링 준수: Domain은 UnityEngine 미참조 (`noEngineReferences: true`), View/UI/Bootstrap만 Unity 의존. `Alchemist.Bootstrap`이 Chain/View/UI를 모두 참조 — Wave 2 integration 정합.
- **P10** `NoOpAnimationHub`가 모든 Play*Async에서 `Task.CompletedTask` 반환 — 헤드리스 테스트 가능 인터페이스 유지.
- **P11** `MatchGroup.Capacity=16`이 최대 런 길이(가로 6, 세로 7) 수용 — overflow 방어 `if (Count >= Capacity) return;` 존재.
- **P12** `ChainProcessor._hubBuffer` 32 capacity가 이론 최대 매치 그룹(H 7 + V 6 = 13) 여유 포함.

---

## 요약 통계

- Critical: 5건
- High: 12건
- Medium: 9건
- Low: 10건
- 규격 모호점: 5건
- 통과 항목: 12건

## 즉시 수정 권고 Top 3

1. **BUG-C01**: GameRoot 초기 보드 Refill 수행 — 없으면 게임 시작 시 보드가 비어 있음.
2. **BUG-C02**: `ColorMixer.Mix`의 Prism 분기 순서 재정렬 — `Mix(Prism, Gray) = Gray`가 애드덤 C4를 위반.
3. **BUG-C05**: `ChainProcessor ↔ Scorer` 와이어링 — 현재 폭발 시 점수 이벤트가 발생하지 않아 `Score.Total` 항상 0.
