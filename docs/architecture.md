# architecture.md — 컬러 믹스: 연금술사 시스템 설계안

> 작성: Architect Teammate | Phase 0 | 2026-04-21

---

## 1. 기술 스택 결정 (과학적 토론)

### 1.1 후보 비교: Unity 2D vs Flutter(Flame)

| 기준 | Unity 2D | Flutter + Flame |
|------|----------|-----------------|
| **매치-3 퍼즐 적합성** | 상용 매치-3 90% 이상이 Unity. 2D Tilemap, DOTween, 풍부한 레퍼런스 | Flame 엔진 성숙도 보통. 매치-3 상용 사례 희소 |
| **Metaball/Jelly 렌더링** | Shader Graph + SpriteShape + 서드파티(Jelly Sprite, Liquid2D, LiquidFun) 풍부 | Custom Painter + SDF 수동 구현. Shader 도구 부족 |
| **60fps 안정성** | IL2CPP AOT 컴파일, Burst/Jobs로 연쇄 계산 최적화 가능 | Skia 기반 Impeller 60fps 가능하나 파티클 100+시 저사양 기기 드랍 위험 |
| **크로스플랫폼(iOS/Android)** | 완전 지원, 빌드 파이프라인 성숙 | 완전 지원, 빌드 경량 |
| **개발 속도(Solo/소규모)** | Inspector/Prefab 작업 빠름. 단 C# + Unity 관행 학습곡선 | Dart Hot Reload 빠름. 위젯 UI는 매우 빠르나 게임 로직은 수동 |
| **에셋 생태계** | Asset Store 독보적 (파티클/쉐이더/효과음/툴) | Pub.dev 게임 에셋 제한적 |
| **빌드 크기** | 최소 20~30MB 오버헤드 | 15~20MB 수준 |
| **메타/네트워크(랭킹/BM)** | Firebase/PlayFab/Unity Gaming Services 통합 | Firebase 통합 우수 |

### 1.2 반대 논거 검토
- Flutter 지지 논거: "UI가 많은 메타 화면(갤러리/상점/랭킹)은 Flutter 위젯이 압도적으로 빠름"
- 반론: Unity에서도 UI Toolkit(UXML/USS)로 커버 가능. 게임 본체와 UI 레이어 분리 비용이 이중 엔진보다 낮음.
- Flame 지지 논거: "빌드 크기/개발 속도 이점"
- 반론: Metaball/Jelly/Shader 기반 Juice 요구가 **핵심 차별화 포인트**인 본 프로젝트에서, 쉐이더 생태계 부재는 Phase 4에서 치명적 병목.

### 1.3 결론
**결정: Unity 2D (Unity 6 LTS, URP 2D Renderer) 채택**

핵심 근거 3가지:
1. Metaball/Jelly/Liquid 쉐이더 에셋·레퍼런스가 Unity에 집중되어 Phase 4 리스크 최저
2. 매치-3 연쇄 로직을 위한 DOTween/UniTask 비동기 패턴이 성숙
3. PlayFab/Firebase/UGS 통합으로 Phase 3 메타 시스템 구현 비용 최소

### 1.4 선택 스택의 핵심 에셋·라이브러리

| 용도 | 라이브러리 | 비고 |
|------|-----------|------|
| 비동기/코루틴 | **UniTask** | async/await, 연쇄 큐 처리의 기반 |
| 트윈 | **DOTween Pro** | 블록 이동/폭발/Jelly 탄성 |
| 젤리 질감 | **Jelly Sprite** (Ali El Saleh) 또는 자체 Spring Mesh | 블록 Deform |
| Metaball | **Liquid2D** 또는 Shader Graph + SDF 커스텀 | 2차 색 Blob 효과 |
| 상태머신 | **Stateless**(C# 라이브러리) 또는 자체 enum FSM | 블록 단위 경량 FSM |
| DI | **VContainer** | 테스트 가능성, Architect 레이어 분리 |
| 이벤트 버스 | **MessagePipe** | UX ↔ GameLogic 디커플링 |
| 직렬화/세이브 | **MemoryPack** | 로컬 저장, 성능 우수 |
| 네트워크/메타 | **Firebase (Auth/Firestore/Remote Config) + PlayFab(랭킹)** | 유보 가능 |
| 사운드 | **FMOD** 또는 Unity 내장 | Phase 4 결정 |
| 햅틱 | **Lofelt Nice Vibrations** | iOS/Android 햅틱 추상화 |
| 오브젝트 풀 | **Unity Pool API** (2021+) | 블록/파티클 풀링 |

**유보:** FMOD vs Unity 내장 오디오는 사운드 디렉터 합류 후 결정.

---

## 2. 핵심 시스템 설계

### 2.1 색 조합 엔진

#### 조합 규칙 계층
- **1차 색(Primary):** Red(R), Yellow(Y), Blue(B)
- **2차 색(Secondary):** Orange(O=R+Y), Green(G=Y+B), Purple(P=R+B)
- **3차 색(Tertiary):** White(W=R+Y+B 또는 O+G+P 균형), Black(K=과포화/오염)
- **프리즘(Prism):** 인접 1차 색을 흡수하여 2차 색 생성. 인접 2차 2개 흡수 시 3차 승격.

#### 조합표 자료구조

```csharp
// ColorId: 비트플래그로 혼합 연산 O(1)
[Flags]
public enum ColorId : ushort {
    None   = 0,
    Red    = 1 << 0,
    Yellow = 1 << 1,
    Blue   = 1 << 2,
    // 파생색은 비트 합으로 자동 계산
    Orange = Red | Yellow,     // 0b011
    Green  = Yellow | Blue,    // 0b110
    Purple = Red | Blue,       // 0b101
    White  = Red | Yellow | Blue, // 0b111
    Black  = 1 << 3,           // 특수: 오염 플래그
    Prism  = 1 << 4,           // 특수: 만능 매처
    Gray   = 1 << 5,           // 특수: 흡수 후 비활성
    FilterMask = 1 << 6        // 필터 벽 (통과 시 색 변환)
}

public static class ColorMixer {
    public static ColorId Mix(ColorId a, ColorId b) {
        if (a == ColorId.Prism) return b;
        if (b == ColorId.Prism) return a;
        var merged = a | b;
        return IsValidRecipe(merged) ? merged : ColorId.Black; // 과포화 → 오염
    }

    static readonly HashSet<ColorId> ValidSet = new() {
        ColorId.Red, ColorId.Yellow, ColorId.Blue,
        ColorId.Orange, ColorId.Green, ColorId.Purple,
        ColorId.White
    };
    public static bool IsValidRecipe(ColorId c) => ValidSet.Contains(c);
}
```

**결정:** 비트플래그 방식 채택 — Dictionary lookup 없이 단일 OR 연산으로 조합 결정. 3차/특수는 예외 분기로 처리.

### 2.2 블록 상태 머신

```
[Spawned] → [Idle] ⇄ [Selected]
    ↓                    ↓
    ↓                [Merging] → [Exploding] → [Cleared]
    ↓                                ↓
    ↓                          [Infecting] (주변 1차 감염)
    ↓
 [Infected] → [Idle](색 변경)
 [Absorbed] → [Gray] (특수: 비활성화)
 [FilterTransit] (필터 벽 통과 중)
 [PrismCharging] → [Prism Burst]
```

경량 FSM 구현: 블록당 `enum BlockState` 필드 + `ITransition` 인터페이스. 전이 시 `OnEnter/OnExit` 훅으로 애니메이션/사운드 트리거.

### 2.3 연쇄 처리 큐

#### 순서 보장 파이프라인

```
1. 입력 → MatchDetector (2차 이상 3연결 탐지)
2. ExplosionQueue.Enqueue(matches)
3. Loop while queue not empty:
   a. Dequeue batch → 동시 폭발 애니메이션 (UniTask.WhenAll)
   b. InfectionPass: 폭발 반경의 1차 색 감염 계산
   c. Re-scan: 감염으로 새 매치 생성 시 → Enqueue
   d. 모든 폭발 애니 완료 대기
4. Gravity: 빈 셀 위 블록 낙하 (DOTween)
5. Refill: 상단 리스폰
6. Re-scan: 낙하 후 매치 발생 시 → step 2로 복귀 (연쇄)
7. 큐 비면 턴 종료, 점수 정산
```

#### 동시성 전략
- **직렬 큐 + 배치 병렬 애니메이션**: 폭발 웨이브 단위는 순차, 같은 웨이브 내 블록들은 병렬 애니메이션.
- **AnimationBarrier**: `await UniTask.WhenAll(animTasks)`로 웨이브 동기화.
- **입력 잠금**: 큐 처리 중 `InputGuard.IsLocked = true`.

**결정:** 웨이브 기반 BFS 큐잉. 우선순위 큐(PriorityQueue)는 유보 — 초기엔 FIFO로 충분, 필요 시 연쇄 깊이 기반 우선순위 도입.

### 2.4 보드 모델

```
Board (8x8 기본, 난이도별 6~10 확장)
├── Cell[row, col]
│   ├── Block? (nullable)
│   ├── Layer: Ground | Filter | Wall
│   └── FilterColor? (통과 시 변환)
└── EffectLayer (파티클/트레일 — 논리 분리)
```

- **좌표계:** `(row, col)` 정수 그리드. 월드 좌표 변환은 `BoardView`가 담당.
- **레이어 분리:** 논리(Model) ↔ 렌더(View) 완전 분리. Model은 POCO, View는 MonoBehaviour.

### 2.5 프롬프트 시스템

#### 조건 평가기

```csharp
public interface IPromptCondition {
    bool Evaluate(GameContext ctx);
    float Progress(GameContext ctx); // 0~1
}

public class CreateColorCondition : IPromptCondition {
    public ColorId Target;
    public int Count;
    // ctx.ColorsCreated[Target] >= Count
}

public class ChainCondition : IPromptCondition { /* 연쇄 N회 */ }
public class MoveLimitCondition : IPromptCondition { /* 이동 M 이하 */ }
public class FilterTransitCondition : IPromptCondition { /* 필터 경유 K회 */ }

public class PromptGoal {
    public List<IPromptCondition> All; // AND
    public List<IPromptCondition> Any; // OR
}
```

이벤트 리스너 패턴: `GameContext`가 MessagePipe로 이벤트 발행 → 각 Condition이 구독 → 카운터 누적.

### 2.6 점수 계산

#### 공식 초안

```
Score = BaseColorValue × ChainMultiplier × EfficiencyBonus + ResidualBonus

BaseColorValue:
  1차: 10, 2차: 30, 3차(W): 100, 3차(K): -20 (오염 패널티)

ChainMultiplier:
  1연쇄: 1.0, 2연쇄: 1.5, 3연쇄: 2.0, 4+: 2.5 + 0.3*(n-4)

EfficiencyBonus:
  (목표달성이동수 / 실제이동수) × 200

ResidualBonus:
  남은 이동 × 50 + 남은 팔레트 슬롯 × 30
```

**유보:** 밸런싱 계수는 Phase 1 플레이테스트 후 Remote Config로 조정.

---

## 3. 데이터 모델 (의사 코드)

```csharp
public enum ColorId : ushort { /* 2.1 참조 */ }

public class Block {
    public int Id;                  // 풀링 식별
    public ColorId Color;
    public BlockState State;
    public BlockKind Kind;          // Normal | Filter | Prism | Gray
    public int Row, Col;
    public float JellyAmplitude;    // 시각 파라미터
}

public class Cell {
    public int Row, Col;
    public Block? Block;
    public CellLayer Layer;         // Ground | Filter | Wall
    public ColorId? FilterColor;
}

public class Board {
    public int Rows, Cols;
    public Cell[,] Grid;
    public List<PaletteSlot> Palette;
    public int MovesRemaining;
}

public class Recipe {
    public ColorId InputA, InputB;
    public ColorId Output;
    public bool RequiresPrism;
}

public class Prompt {
    public string Id;
    public string LocalizedTitle;
    public PromptGoal Goal;
    public int MoveLimit;
    public PromptReward Reward;
}

public class Score {
    public int Total;
    public int ChainDepth;
    public int MovesUsed;
    public Dictionary<ColorId, int> ColorsCreated;
}

public class PaletteSlot {
    public int Index;
    public ColorId? Stored;
    public bool IsLocked;
}

public class GameContext {
    public Board Board;
    public Score Score;
    public Prompt? ActivePrompt;
    public IEventBus Events;
}
```

---

## 4. 아키텍처 레이어

### 4.1 레이어 정의

```
┌─────────────────────────────────────────┐
│  Presentation (View)                    │
│   BoardView, BlockView, UIHud, FXLayer  │
├─────────────────────────────────────────┤
│  Game Logic (Domain)                    │
│   MatchDetector, ChainProcessor,        │
│   ColorMixer, PromptEvaluator, Scorer   │
├─────────────────────────────────────────┤
│  Data                                   │
│   Board, Block, Recipe, Prompt,         │
│   SaveData, RemoteConfig                │
├─────────────────────────────────────────┤
│  Services                               │
│   Audio, Haptic, Persistence,           │
│   Network(Auth/Leaderboard), Analytics  │
└─────────────────────────────────────────┘
```

### 4.2 Unity MonoBehaviour 분해

| MonoBehaviour | 책임 | 순수 C# 협력자 |
|--------------|------|----------------|
| `GameRoot` | 씬 진입, DI 컨테이너 빌드 | VContainer LifetimeScope |
| `BoardView` | 그리드 렌더, 입력 → 이벤트 | `Board` (POCO) |
| `BlockView` | 개별 블록 비주얼/애니 | `Block` (POCO) + DOTween |
| `InputController` | 터치 드래그/스왑 감지 | `IInputEventBus` |
| `ChainAnimator` | 폭발/감염/낙하 시퀀스 | `ChainProcessor` (POCO) |
| `UIHud` | 점수/이동/프롬프트 표시 | `IScoreStream`, `IPromptStream` |
| `AudioService` | SFX/BGM 재생 | 싱글톤 |
| `HapticService` | Nice Vibrations 래퍼 | 싱글톤 |
| `PersistenceService` | MemoryPack 저장/로드 | 싱글톤 |
| `NetworkService` | Firebase/PlayFab | 싱글톤 |

**원칙:** MonoBehaviour는 "얇게", 도메인 로직은 POCO로 분리 → 테스트 가능성 확보.

---

## 5. 모듈 간 인터페이스 (교차레이어 조정)

### 5.1 UX ↔ GameLogic

```csharp
public interface IInputEventBus {
    IObservable<SwapEvent> OnSwap;
    IObservable<TapEvent> OnTap;
    IObservable<PaletteSelectEvent> OnPaletteSelect;
}

public interface IGameStateStream {
    IObservable<BoardStateSnapshot> BoardChanged;
    IObservable<ChainEvent> ChainOccurred;
    IObservable<ScoreUpdate> ScoreChanged;
    IObservable<PromptProgress> PromptProgressed;
}
```

UX가 정의한 와이어프레임 → 위 이벤트 스트림으로 바인딩.

### 5.2 Performance ↔ GameLogic

```csharp
public interface IObjectPool<T> where T : Component {
    T Rent();
    void Return(T obj);
}
// BlockPool, ParticlePool, FxTrailPool
```

- **오브젝트 풀링:** 블록/파티클은 씬 시작 시 사전 할당. Max 200 blocks + 500 particles.
- **이펙트 LOD:** `GraphicsQualityLevel` enum (Low/Mid/High) → Metaball/Jelly 샘플 수, 파티클 개수 스케일.
- **Burst/Jobs 후보:** MatchDetector (8x8 스캔)는 초기 PlainC#. 보드 12x12 이상 확장 시 Job System 전환 검토.

---

## 6. 구현 단계 (Phase 1 → Phase 4)

### Phase 1 — MVP
1. **P1-01:** `ColorMixer` + `Recipe` 구현 (2.1) — 유닛 테스트 20+
2. **P1-02:** `BlockState` FSM + `IStateTransition` (2.2)
3. **P1-03:** `ChainProcessor` 웨이브 큐 (2.3) — 모의 보드로 연쇄 검증
4. **P1-04:** `BoardView` + `BlockView` 연결, 3색 조합 플레이어블
5. **P1-05:** 기본 폭발 규칙 (2차 이상 3연결 → 폭발 + 1차 감염)
6. **P1-06:** 프롬프트 3종 — CreateColor, Chain, MoveLimit
7. **P1-07:** `Scorer` 공식 구현 + HUD 연결
8. **P1-08~11:** 디버깅 / 테스트 / 성능 리뷰 / 최종 리뷰

### Phase 2 — Systems
- 필터 벽 (Cell.Layer = Filter) + FilterTransit 상태
- 회색 블록 (Absorbed 상태 + 해제 조건: 인접 폭발 2회)
- 프리즘 블록 (Prism 승격 로직, 2차 흡수)
- 팔레트 슬롯 UI + Save/Use 로직
- 프롬프트 시스템 확장 (일일/고급)

### Phase 3 — Meta
- Firebase Auth + Firestore 프로필
- PlayFab 랭킹 (글로벌/친구/데일리)
- 배지 시스템 (조건 누적기)
- BM: 잉크 에너지 타이머, 브러시/지우개 인앱
- 갤러리 복원 (해금된 색별 픽셀 아트)

### Phase 4 — Juice & Polish
- Metaball 쉐이더 (2차 색 Blob 합성)
- Jelly Sprite 통합 (선택/폭발 시 탄성 변형)
- 햅틱 이벤트 매트릭스
- FMOD/Unity Audio 통합 (플롭/슈욱 레이어드 믹싱)
- 다크 모드 (URP 2D Global Light)

---

## 7. 리스크 & 검증 가설

| # | 리스크 | 가설/증상 | 대안 |
|---|--------|-----------|------|
| R1 | 연쇄 큐가 애니메이션 동기화에서 깨진다 | 웨이브 간 `UniTask.WhenAll` 누락 시 블록 중복 참조 | `AnimationBarrier` 명시적 클래스, Unit Test로 Mock Clock 주입 |
| R2 | 감염 전파가 무한 루프 생성 | Black(오염)이 다시 매치 조건 만족 시 | 웨이브 최대 깊이 10, 초과 시 강제 종료 + 로그 |
| R3 | Metaball 쉐이더 저사양 기기 드랍 | GPU fill-rate 병목 | LOD 3단계, Low에서는 일반 스프라이트 대체 |
| R4 | 비트플래그 조합이 3차 색(W/K) 표현 한계 | Black이 특수 플래그라 `Mix(O, G)` 결과 애매 | ColorMixer에 `SpecialRule` 우선 분기 추가 |
| R5 | 블록 풀 고갈 | 대연쇄 시 리필 요구량 폭증 | 사전 풀 200 + Grow-On-Demand (로그 경고) |
| R6 | 프롬프트 Condition 이벤트 누수 | 씬 전환 시 구독 미해제 | `IDisposable` + CancellationToken 강제 |
| R7 | Firebase/PlayFab 오프라인 상태 | 네트워크 불안정 시 점수 유실 | 로컬 MemoryPack 큐 + 재동기화 |
| R8 | Unity 6 LTS 미출시/불안정 | 현재 시점 릴리스 상태 확인 필요 | Unity 2022 LTS 폴백 경로 준비 |

---

## 8. UX / Perf / Doc에 던지는 질문 (교차레이어 조정)

### UX Designer께
1. 팔레트 슬롯 최대 개수는? (설계는 4 가정, UX 와이어프레임 확정 필요)
2. 프롬프트 실패 시 리트라이 UX — 즉시 재시작 vs 광고 보고 되돌리기?
3. 감염 애니메이션 길이 허용 범위 (60fps 기준 프레임 예산)?
4. 색상 접근성 — 색맹 모드에서 1차 색 구분 기호/패턴 요구사항?

### Performance Engineer께
1. 목표 기기 하한은? (iPhone SE 2 / Galaxy A32 수준으로 설정해도 되는지)
2. 블록 풀 사전 할당 200이 메모리 예산 내인지? (추정 블록 1개 = ~1KB 이미지 + mesh)
3. Metaball 쉐이더 저사양 대체 전략 — Pre-rendered sprite sheet vs 일반 색 스프라이트?
4. Burst/Jobs 적용 임계치 — 보드 몇 x 몇부터 필요?
5. Addressables vs Resources — 색/테마 에셋 로딩 전략?

### Doc Writer께
1. README에 엔진 결정(Unity 2D) 확정 반영 — OK?
2. `architecture.md` 요약을 README에 링크할지 별도 섹션으로 녹일지?
3. 퍼블릭 문서와 내부 설계 문서 구분 전략 (docs/public/ vs docs/internal/)?

---

## 부록 A — 디렉터리 제안 (Phase 1 착수 시)

```
Alchemist/
├── Assets/
│   ├── _Project/
│   │   ├── Scripts/
│   │   │   ├── Domain/        (POCO: Board, Block, ColorMixer, ChainProcessor)
│   │   │   ├── View/          (MonoBehaviour: BoardView, BlockView)
│   │   │   ├── Services/      (Audio, Haptic, Persistence, Network)
│   │   │   ├── UI/            (HUD, Palette, PromptPanel)
│   │   │   └── Bootstrap/     (GameRoot, DIContainer)
│   │   ├── Prefabs/
│   │   ├── Shaders/           (Metaball, JellyDeform)
│   │   └── Settings/          (ScriptableObject: Recipes, Prompts, Balance)
│   └── ThirdParty/            (UniTask, DOTween, VContainer...)
├── docs/
│   ├── architecture.md        (본 문서)
│   ├── ux_design.md           (UX 담당)
│   └── performance.md         (Perf 담당)
├── Tasklist.md
└── process.md
```
