# architecture_v2.md — 컬러 믹스: 연금술사 v2 아키텍처 고도화 설계안

> 작성: Architect Teammate | v1.0.0 post-tag | 2026-04-21
> 대상: v1.1 ~ v2.0 마일스톤 (MessagePipe → PlayerAction → DI → Network)
> 전제: v1.0.0 태그 (Phase 0~2 완료, 16 asmdef, 170+ C# 파일, 120+ EditMode 테스트, D1~D25 결정 반영)

---

## 0. 요약 (TL;DR)

v1.0.0은 Domain POCO 격리 + 수동 DI(GameRoot/AppBootstrap) + delegate 기반 이벤트 전파로 Phase 2까지 확장을 버텨왔다. 본 설계안은 (1) **이벤트 버스(MessagePipe)**, (2) **PlayerAction/Command 패턴**, (3) **DI 컨테이너(VContainer)**, (4) **서비스 라이프사이클 정리**, (5) **Result 기반 에러 경계**, (6) **관측 레이어**, (7) **테스트 전략 확장**, (8) **v2.0 네트워크 통합 로드맵**을 **점진 도입** 순서로 제시한다. 전제는 "Domain 순수성 유지 + 결정론 유지 + 기존 120+ 테스트 무중단".

---

## 1. 현재 아키텍처 자가 진단

### 1.1 16 asmdef 의존 그래프 재검증

현재 구조 (asmdef 이름 기준):
- **Domain.*** (11개): Colors / Blocks / Board / Chain / Palette / Scoring / Prompts / Stages / Economy / Ranking / Meta / Badges / Player / Replay
- **Services.***: Audio / Haptic / Theme / Ranking
- **View / UI / Bootstrap**: 3개

**순환 리스크**: 현재까지 **순환 없음**. `Chain → Board, Blocks, Scoring` 단방향, `Bootstrap → 전 도메인` 은 composition root 로 허용.

**중복 리스크**:
- `View.IInputEventBus` 는 System.Action 기반 (Phase 1 C5로 MessagePipe 연기). v2에서 Domain 이벤트와 Input 이벤트 통합 게이트웨이 필요.
- `ChainProcessor.OnFilterTransit` / `Palette.SlotChanged` / `Palette.UnlockedCountChanged` / `InputController.OnSwap` 등 **네 곳에 흩어진 Action 필드**. Publisher/Subscriber 불일치 시 누수 위험.

**역주입 필요 지점 (DIP)**:
- `ChainProcessor` 가 향후 `IEventPublisher` 를 받게 되면 Domain.Chain → Infrastructure(MessagePipe) 경로가 생겨 **MessagePipe 어댑터 인터페이스를 Domain 에 둬야** 한다.

### 1.2 GameRoot 복잡도 한계점

`GameRoot.Awake()` 는 현재 12단계 수동 조립. 서비스 12개, 책임 혼재(로드·생성·바인딩·구독·초기배치), OnDestroy 역경로 수동 관리.
v1.3 에서 **VContainer 도입** 시 (1)~(10)을 `LifetimeScope.Configure` 로, (11)~(12) 만 `IStartable.Start()` 로 분리.

### 1.3 점대점 배선 산발성

현재 "이벤트" 는 delegate 이벤트 / Action 필드 / interface event 혼재:
- `InputController.OnSwap` (event Action<SwapEvent>)
- `Palette.SlotChanged` (event Action<int>)
- `ChainProcessor.OnFilterTransit` (public Action<int>, **event 아님 — 덮어쓰기 위험**)
- `Scorer` 는 별도 이벤트 없고 직접 메서드 호출

### 1.4 Domain 의존성 역주입 점검표

| Domain | 의존 방향 | 역전 필요? |
|---|---|---|
| Chain → IChainAnimationHub (View 추상) | 이미 역전 | No |
| Chain → IBlockSpawner | OK | No |
| Prompts → IPromptContext | 이미 역전 | No |
| Economy → PlayerProfile → IClock | 이미 역전 | No |
| Player → IPathProvider / IPlayerProfileStore | 이미 역전 | No |
| **Chain/Scoring/Palette → (미래) IEventPublisher** | **v1.1 신규** | **Yes** |

---

## 2. v2 개선 방향 (과학적 토론)

### 2.1 이벤트 버스: A. MessagePipe vs B. 경량 IEventBus 자작

**결정: A. MessagePipe 채택 (v1.1).**
근거: (1) v0 architecture.md §1.4에서 이미 선정. (2) 제로할당 퍼블리시가 ChainProcessor 루프에서 GC 프리 유지. (3) VContainer 상호운용이 v1.3 마이그레이션 비용 감소.

**안전장치**: MessagePipe 랩퍼 `IGameEventBus` 를 Domain 쪽에 둔다. 어댑터는 신규 `Infrastructure.Events` asmdef 에 둔다.

### 2.2 Reactive: A. UniRx vs B. System.Reactive

**결정: 둘 다 도입 보류.** MessagePipe IAsyncPublisher + ValueChanged 구독으로 충분. 학습비용 대비 이득 불명확.

### 2.3 DI: A. VContainer vs B. 수동 조립 유지

**결정: A. VContainer 채택 (v1.3).** v1.1 MessagePipe 가 안정화된 후 v1.3 에 도입. **한 번에 두 가지 변화 금지** 원칙.

### 2.4 Scene: A. 씬 분할 vs B. 단일 씬

**결정: A. 씬 분할 유지.** 단 `static PendingProfile` 은 **안티패턴**. v1.3 에서 `SceneLoader.LoadGameAsync(StageRequest)` 로 전환.

---

## 3. PlayerAction 패턴 (신규, v1.2)

### 3.1 개념

v1.0.0 에는 플레이어 의도 → 게임 상태 변경이 InputController.OnSwap → GameRoot.NotifyMoveCommitted 직결로 "턴 수만 증가" 하는 반쪽짜리.

### 3.2 인터페이스

```csharp
namespace Alchemist.Domain.Player.Actions
{
    public interface IPlayerAction
    {
        PlayerActionResult Execute(GameContext ctx, CancellationToken ct);
        PlayerActionRecord ToRecord();
    }

    public readonly struct PlayerActionResult
    {
        public bool Committed;
        public int ChainDepthTriggered;
        public ErrorCode Error; // None | Invalid | Blocked | OutOfMoves
    }

    public readonly struct PlayerActionRecord
    {
        public ActionKind Kind;
        public sbyte A0, A1, A2, A3;
        public int Param0;
    }
}
```

### 3.3 구현 대상

| Command | 책임 |
|---|---|
| `SwapCommand(fromR, fromC, toR, toC)` | Board 인접 스왑 + 매치 체크 + Chain 트리거 |
| `PaletteStoreCommand(slotIdx, srcR, srcC)` | Board → Palette.Store |
| `PaletteUseCommand(slotIdx, destR, destC)` | Palette.Use → Board 배치 |
| `UseItemCommand(itemId, params)` | ItemEffectProcessor 호출 |

### 3.4 결정론 보장

리플레이 재생 = `stage.BoardSeed → DeterministicBlockSpawner(seed)` + `List<PlayerActionRecord> → Execute()` 순차 재실행.
**Golden Replay Test**: 기록된 레코드 리스트를 재생 후 최종 Score/Board hash 가 일치하는지 검증.

### 3.5 Command Queue

```csharp
public sealed class PlayerActionQueue
{
    public bool TryEnqueue(IPlayerAction action);
    public ValueTask ProcessOneAsync(GameContext ctx, CancellationToken ct);
    public bool IsBusy { get; }
}
```

---

## 4. 서비스 라이프사이클 정리

### 4.1 범위 구분 (v1.3 VContainer Scope 매핑)

| 범위 | 서비스 | v1.3 Scope |
|---|---|---|
| App 전체 | ThemeService / Audio / Haptic / QualityManager / PlayerProfile / IGameEventBus / SceneLoader | `AppScope` (Singleton) |
| Stage | Board / Score / Scorer / ChainProcessor / Spawner / Palette / GameContext / Prompt / ItemEffectProcessor / PlayerActionQueue / ReplayRecorder | `GameScope` |

### 4.2 Dispose/구독 해제 패턴

**원칙**: v1.1 이후 **모든 구독은 `IDisposable` 반환**. `GameRoot.OnDestroy` 는 `CompositeDisposable.Dispose()` 한 줄로 축약.

---

## 5. 에러 경계 재설계

### 5.1 계층별 정책

| 계층 | v2 정책 |
|---|---|
| Domain 순수 | 조용히 None 반환 **유지** |
| Domain 상태 변경 | `Result<Unit, DomainError>` (Palette.Store, Ink.Consume 등 핵심만) |
| Services IO/Network | `Result<T, ServiceError>` + 재시도 정책 |
| Bootstrap | Debug.LogWarning 유지 |

### 5.2 Result<T,E> 도입 범위 (v1.2 부분)

**도입 대상**: SaveService.LoadAsync, PlayerAction.Execute, NetworkService(v2.0).
**비도입**: ColorMixer.Mix (핫패스), MatchDetector (성공/실패 개념 없음).

### 5.3 Network/IO 실패 정책 (v2.0)

- **재시도**: 지수 백오프 (2^n * 500ms, max 3회)
- **오프라인 큐**: `pending_scores.json`
- **충돌 정책**: "서버 우선 + 로컬 백업 보존"

---

## 6. 관측/디버깅 레이어

### 6.1 Log 카테고리 (v1.1)

```csharp
public static class LogCat
{
    public const string Domain   = "[Dom]";
    public const string Chain    = "[Chn]";
    public const string Scoring  = "[Scr]";
    public const string Input    = "[Inp]";
    public const string UI       = "[UI ]";
    public const string Audio    = "[Aud]";
    public const string Network  = "[Net]";
    public const string Save     = "[Sav]";
}
```

### 6.2 DebugHud 확장 (v1.1)

- 연쇄 깊이 분포 히스토그램
- 활성 폭발 파티클 수 / 풀 사용률
- Audio voice 사용률
- MessagePipe 구독자 수
- PlayerActionQueue 대기 개수

### 6.3 원격 로그 (v2.0)

- **Firebase Performance**: 연쇄 처리 시간 p95
- **Firebase Crashlytics**: IL2CPP 심볼
- **Remote Config**: BaseColorValue, ChainMultiplier 원격 조정

---

## 7. 테스트 전략 고도화

### 7.1 확장 포인트

| 대상 | 종류 | 우선순위 |
|---|---|---|
| `IPlayerAction` 각 구현 | EditMode | v1.2 필수 |
| MessagePipe Publisher/Subscriber 와이어링 | EditMode | v1.1 필수 |
| ReplayRecorder + 재생 결정론 (Golden) | EditMode | v1.2 필수 |
| Scene 통합 (Lobby→Game 전환) | PlayMode | v1.3 권장 |
| Firebase/PlayFab | 통합 테스트 (staging) | v2.0 필수 |

### 7.2 Golden Replay 테스트

```csharp
[Test]
public void Golden_Seed123_100Moves_FinalScoreMatches()
{
    var replay = LoadFixture("golden/seed123.json");
    var (scoreA, hashA) = Simulate(seed: 123, replay);
    var (scoreB, hashB) = Simulate(seed: 123, replay);
    Assert.AreEqual(scoreA, scoreB);
    Assert.AreEqual(hashA, hashB);
    Assert.AreEqual(12345, scoreA);
}
```

### 7.3 Coverage Target

- EditMode Domain 라인 **85%+** (현재 ~70%)
- PlayMode 스모크 **3본** (Lobby→Game, Game→Clear, Game→Fail)

---

## 8. 고도화 로드맵

### v1.1 — MessagePipe + IEventBus (2주)
- Domain `IGameEventBus` 정의
- 이벤트: BlockExplodedEvent, PaletteSlotChangedEvent, FilterTransitEvent, ChainCompletedEvent, SwapEvent 등
- 기존 delegate 대체
- DoD: 120+ 테스트 무회귀 + 와이어링 EditMode 테스트 10본

### v1.2 — PlayerAction + 리플레이 재생 (2주)
- IPlayerAction + 4개 Command
- PlayerActionQueue
- InputController → SwapCommand 전환
- Golden Replay 테스트 3본
- DoD: 재생 결정론 검증 + 디버그 메뉴로 재생 가능

### v1.3 — VContainer DI + SceneLoader (1주)
- AppScope / GameScope
- MessagePipe / VContainer 통합
- GameRoot.Awake → IStartable.Start 축소
- static PendingProfile 제거
- DoD: LifetimeScope 구성, PlayMode 스모크 3본

### v2.0 — Network: Firebase + PlayFab (3주)
- Firebase Auth (익명 + Apple/Google SIWA), Firestore sync
- PlayFab 리더보드
- Firebase Remote Config + Crashlytics + Performance
- 오프라인 큐 + 지수 백오프
- 광고 SDK (Unity Mediation 또는 AdMob)
- DoD: staging E2E 3시나리오

### 마일스톤 의존성

```
v1.1 (MessagePipe)
   └→ v1.2 (PlayerAction)
         └→ v1.3 (VContainer)
               └→ v2.0 (Network)
```

**불변 원칙**: 각 마일스톤은 **단일 축 변경**. 두 축 동시 리팩터 금지.

---

## 9. 리스크 & 완화

| # | 리스크 | 완화 |
|---|---|---|
| RV1 | MessagePipe 핫패스 GC 스파이크 | ValueType 이벤트 + Pooled Pipeline |
| RV2 | VContainer AOT iOS 빌드 실패 | v1.3 진입 전 iOS 빌드 검증 1주 |
| RV3 | PlayerAction 재생 결정론 깨짐 | Domain float 사용 금지 감사 + UnityEngine.Random 미사용 검증 |
| RV4 | 씬 전환 시 구독 누수 | CompositeDisposable 규칙 + 린트 |
| RV5 | Firebase SDK 버전 충돌 (iOS cocoapod) | INSTALL_iOS.md 업데이트 + 의존 고정 |
| RV6 | Remote Config vs Golden Replay 충돌 | Replay 재생 시 Config 기본값 강제 주입 |

---

## 10. 결정 로그 (v2 신규)

- **DV1**: Domain은 MessagePipe 직접 참조 금지. `IGameEventBus` 를 통해서만 발행.
- **DV2**: 모든 이벤트 payload 는 `readonly struct` (GC 회피).
- **DV3**: PlayerAction 은 Domain 레이어에 거주.
- **DV4**: `GameRoot.PendingProfile` static 은 v1.3 에서 제거 (deprecated 마킹 v1.2).
- **DV5**: Scorer 는 리팩터 제외. D16~D22 안정화.
- **DV6**: ChainProcessor 는 v1.1 에서 `IGameEventBus` 주입받되 기존 delegate 훅은 deprecated 로 한 버전 유지.

---

## 부록 A — 이벤트 카탈로그 (v1.1)

| Event | Publisher | Subscribers | Payload |
|---|---|---|---|
| SwapEvent | InputController | GameFacade | FromRow, FromCol, ToRow, ToCol |
| TapEvent | InputController | PaletteView 등 | Row, Col |
| PaletteSelectEvent | InputController | PaletteFacade | SlotIndex, Color |
| PaletteSlotChangedEvent | Palette | GameContext, PaletteView | SlotIndex, NewColor, WasStored |
| FilterTransitEvent | ChainProcessor | GameContext, DebugHud | Count |
| BlockExplodedEvent | ChainProcessor | Scorer, FX, Audio | Color, Count, Depth |
| ChainCompletedEvent | ChainProcessor | UIHud, Scorer | MaxDepth, TotalBlocks |
| MoveCommittedEvent | PlayerActionQueue | GameContext, UIHud | MovesUsed, MovesRemaining |
| StageClearedEvent | GameFacade | UI, Analytics | Score, ParRatio |

---

## 부록 B — 권장 신규 asmdef (v1.1+)

- `Alchemist.Infrastructure.Events` — MessagePipe 래퍼
- `Alchemist.Domain.Player.Actions` — PlayerAction 패밀리
- `Alchemist.Services.Network` (v2.0) — Firebase/PlayFab
- `Alchemist.Services.Analytics` (v2.0) — Firebase Performance/Crashlytics

현 16개 → v2.0 완료 시 약 20개 asmdef. DAG 유지.

---

## 3문장 요약

v1.0.0의 수동 DI·점대점 delegate·PlayerAction 부재 한계를 **MessagePipe(v1.1) → PlayerAction+Replay(v1.2) → VContainer+SceneLoader(v1.3) → Firebase/PlayFab(v2.0)** 의 4단 점진 마이그레이션으로 해소한다. 핵심 원칙은 "Domain 순수성 유지, 결정론 유지, 단일 축 변경, 120+ 테스트 무회귀" 이며 Golden Replay 테스트를 회귀 방어선으로 도입한다. 각 마일스톤은 2~3주 범위로 독립 검증 가능하게 설계되어, 리스크가 나타나면 해당 마일스톤 내에서 차단 가능하다.
