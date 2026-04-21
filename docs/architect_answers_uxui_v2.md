# 리더(Architect) → UX/UI 질의 답변 (v2)

> 2026-04-21 | UX/UI v2 산출물(design_system/motion_design/screen_designs_v2) 수령 후

## Q1. Theme 스위칭 구조 — IPaletteService 신설 vs AppColors 정적 확장

**결정: `IPaletteService` 인터페이스 신설 (v1.2에서 도입)**

근거 (과학적 토론):
- **A. 정적 클래스 유지**: 기존 `AppColors.TryGet(ColorId)` 시그니처 보존 쉽지만 Theme 파라미터가 늘어나며 호출부 전파 필요 → **호출 100+ 곳 수정 비용 높음**
- **B. `IPaletteService` 신설**: Light/Dark × Contrast × Accessibility 변형을 런타임 주입 가능. VContainer(v1.3) 도입 시 Scope 교체만으로 테마 변경 가능 → **확장성 우수**
- **결론**: B 채택. 단 **점진 마이그레이션**:
  - v1.1: `AppColors.TryGet` 기존 API 유지 + 내부에서 `IPaletteService` 구현체 lookup
  - v1.2: 모든 호출부를 `IPaletteService` 주입으로 전환
  - v1.3: `AppColors` 정적 클래스 deprecated 마킹

인터페이스 시그니처:
```csharp
public interface IPaletteService
{
    bool TryGet(ColorId colorId, out Color32 color);
    bool TryGetPrismGradient(out Color32[] colors); // 6컬러 배열
    AppTheme CurrentTheme { get; }
    ContrastMode CurrentContrast { get; }
    event System.Action<ThemeChangedEvent> ThemeChanged;
}
```

## Q2. FeedbackBus 신규 서비스 도입

**결정: 도입 (v1.1 MessagePipe 마일스톤과 함께)**

근거:
- 현재 Haptic/Audio를 **호출부에서 개별 호출**하는 구조는 **3-way 동기화 버그**(비주얼/햅틱/오디오 타이밍 오프셋) 추적이 어려움
- 튜닝 편의성: ScriptableObject 기반 `FeedbackMapping` 테이블 도입 시 디자이너가 코드 수정 없이 오프셋 조정 가능
- **구조**:
  ```csharp
  public interface IFeedbackBus
  {
      // 단일 feedback trigger → 해당 FeedbackId 의 매핑(haptic+audio+visual offset) 일괄 실행
      void Fire(FeedbackId id, FeedbackContext ctx = default);
  }

  public enum FeedbackId
  {
      BlockPickup, BlockHoverValid, BlockHoverInvalid,
      Mix1Primary, Mix2Secondary, Mix3Tertiary,
      ChainDepth1, ChainDepth2, ChainDepth3Plus,
      PromptSuccess, PromptFail, TurnsLow,
      PaletteStore, PaletteUse, InkRefill,
  }

  [CreateAssetMenu]
  public class FeedbackMapping : ScriptableObject
  {
      public FeedbackId Id;
      public HapticEvent Haptic;
      public float HapticDelayMs;
      public SfxId Sfx;
      public float SfxDelayMs;
      public string VisualTriggerId;
      public float VisualDelayMs;
  }
  ```
- MessagePipe 의 `IPublisher<FeedbackEvent>` 로 구현 (v1.1)
- DebugHud 에 최근 10건 피드백 기록 패널 추가 (디자이너 튜닝 지원)

## 추가 UX/UI 결정 (리더 자체 승격)

- **DV7**: 색상 팔레트 67 토큰을 Phase 5 에서 `PaletteSO`(ScriptableObject) 로 승격. Light/Dark/HighContrast 3 variant 에셋 분리.
- **DV8**: Motion §연쇄 5단계 에스컬레이션은 `ChainFeedbackCurve` SO 로 데이터화. 연쇄 깊이 → HapticIntensity/CameraShake/TimeScale 매핑을 디자이너 조정 가능하게.
- **DV9**: 스크린 18뷰 중 **로비·게임플레이·결과** 3개만 v1.1 PlayMode 스모크 테스트 필수. 나머지는 v1.2 이후.

## 다음 라운드 작업
- v1.1 시작 시 `IPaletteService`, `IFeedbackBus`, `IGameEventBus` 3개 인터페이스 동시 도입 (같은 Infrastructure asmdef)
- UX/UI 팀은 Phase 5 에셋 제작 시 디자인 토큰을 실제 ScriptableObject 에 입력
