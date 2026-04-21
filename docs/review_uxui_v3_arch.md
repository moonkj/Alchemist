# review_uxui_v3_arch.md — Architect UX/UI 구조 리뷰 (v3 라운드)

> 작성: Architect Teammate · 2026-04-22
> 대상: `Assets/_Project/Scripts/Bootstrap/MinimalGameScene.cs` (881 line) 및 Bootstrap 전반
> 배경: v1.0.0 태그 기반 프로토타이핑용 `MinimalGameScene` 이 Lobby/Play/Result 전 화면을 단일 OnGUI 로 처리. 유저 피드백 "전체 UX/UI 대충 돼있음 / 구조 리뷰 필요".

---

## 1. 현재 아키텍처 진단 — Health Grade: **C (Warn)**

MinimalGameScene 은 "1-session 플레이어블 프로토"라는 **의도된 임시 구조**로 기능 목표는 달성했으나, v1.0.0 축적 Domain 자산(16 asmdef, 120+ 테스트)과 **완전 분리**된 섀도우 트랙으로 성장 중이다. Lobby/Stage Select/Play/Result 4 기능 + Drag Input + Cascade Resolve + Feedback(Shake/Haptic/Splash) + 절차적 Audio + IMGUI 스타일 5종 + 프로시저얼 Sprite 생성까지 **8개 책임이 단일 MonoBehaviour** 에 결합되어 있고, architecture_v2.md §1.2 에서 경고한 "GameRoot 12단계 수동 조립" 보다도 결합도가 높다. screen_designs_v2.md 의 9 루트 화면·Design System 토큰·Safe Area·한손 도달성 규범 **어느 것도 반영되지 않음**(IMGUI 는 Design Token 매핑 불가). 긍정 측면: 결정론(DeterministicBlockSpawner Seed), PlayerPrefs 별점 영속, Cascade Loop cap=10, 드래그 상한 거리 등 **방어 코드는 견실**하며 `_bouncing`/`_jellyPhase` 분리로 애니 상태 격리는 깔끔하다. 결론: "playground 로는 A, 프로덕션 경로로는 D" — 교차 상태가 지속될수록 부채는 가속된다.

---

## 2. 항목별 상태표

| # | 항목 | 상태 | 한 줄 이유 |
|---|---|---|---|
| A1 | 800+ line 단일 MB | **Fail** | Lobby/Play/Result + Audio + Feedback + Sprite 생성까지 8 책임 결합 — SRP 위반 |
| A2 | Playground ↔ 실사용 혼재 | **Fail** | Bootstrap asmdef 안에 MinimalGameScene 과 GameRoot/MetaRoot 가 공존, 진입점 2 개 |
| A3 | Domain 레이어 분리 | **Fail** | Board/Chain/Scoring/Palette/Prompts/Stages/Player 전부 **미사용** — 섀도우 구현 |
| A4 | GameRoot 중복 | **Warn** | Scene 당 GameRoot 존재하나 MinimalGameScene 씬은 독자 경로 — 통합 미정 |
| B1 | ScreenState enum 단일 씬 | **Warn** | 프로토엔 OK, v1.1+ 에서 씬/Canvas 분리 필수 |
| B2 | Scene 분할 (Lobby/Game/Result) | **미착수** | 기존 `Lobby.unity` / `Game.unity` 존재하나 MinimalGameScene 은 우회 |
| B3 | UI Canvas 3 분리 대안 | **권장** | 단기 이주 비용 최저, 가시성 확보 유리 |
| C1 | OnGUI / IMGUI 사용 | **Fail** | design_system.md Radius/Elevation 토큰 매핑 불가, 폰트/애니 빈약, 프로덕션 불가급 |
| C2 | Canvas/UI Toolkit 이주 시점 | **Warn** | v1.1 (MessagePipe) 와 동시 진행 금지 — v1.2 또는 별 트랙 권고 |
| C3 | TMP Essential Resources | **Warn** | DebugSplashLabel 주석대로 TMP 미설치 → IMGUI fallback, batchmode 자동 import 스크립트 부재 |
| D1 | 블록 스프라이트 절차적 | **Warn** | BuildSquareSprite 128×128 round-SDF — 런타임 Texture2D.Create, 아트 리플레이스 경로 없음 |
| D2 | UI 패널 solid texture | **Fail** | MakeSolidTexture(2×2) — elevation/shadow/radius 토큰 미대응 |
| D3 | 실 스프라이트 교체 경로 | **미설계** | asset_production_plan.md 연결 부재 |
| E1 | PlayerPrefs 별점 | **Warn** | 간단하나 PlayerProfile.SaveService 와 **분리**된 2-트랙 저장 |
| E2 | PlayerProfile 통합 | **Fail** | MetaRoot/GameRoot 의 PendingProfile 경로와 무관하게 동작 |
| E3 | Replay / Badge 연결 | **미착수** | Phase 3 Domain(Replay/Badges) 완전 비활성 |
| F1 | 한국어 하드코딩 | **Fail** | Stage Title / Toast / 버튼 전부 string literal, LocalizerService 미호출 |
| G1 | Coroutine 누수 (OnDestroy 없음) | **Warn** | 씬 종료 시 SplashMotion/MixBounceAnim 활성 코루틴 정리 미보장 (Destroy(go)는 있음) |
| G2 | AppBootstrap 미연결 | **Fail** | Audio/Haptic/Theme/Quality 싱글톤 대신 자체 AudioSource+절차 사운드 중복 |
| G3 | Handheld.Vibrate() 직접 호출 | **Warn** | HapticService 우회 — Phase 4 정책 무시 |
| H1 | 점진 이주 계획 | **미문서화** | 이 문서가 첫 로드맵 — 아래 §3 참조 |

Pass 0 · Warn 9 · Fail 10 · 미착수/미설계 3 · 미문서화 1.

---

## 3. 점진 리팩터 로드맵

### v1.1 — 최소 안전화 (1주, MessagePipe 트랙과 병렬 가능)

1. **OnDestroy 추가**: `StopAllCoroutines()` + 생성한 Texture2D `Destroy` — 씬 재진입 누수 차단.
2. **로컬라이징 문자열 상수화**: Stage Title / Toast / 버튼 레이블 `const string` 모듈화 → v1.2 LocalizerService 주입 지점 확보.
3. **AppBootstrap 싱글톤 소비**: `AppBootstrap.Instance.Audio/Haptic` 존재 시 우선 사용, 절차 사운드는 fallback. `Handheld.Vibrate()` 제거.
4. **ScreenState 책임 경량화**: `DrawLobby / DrawPlayingHud / DrawResult` 를 **partial class** 3파일로 쪼개 파일당 300 line 이내 유지 (리팩터 비용 최소).
5. **PlayerPrefs 키 네임스페이스**: `"alchemist.stars.{id}"` 로 접두사 부여 → v1.2 PlayerProfile 마이그레이션 시 충돌 회피.

### v1.2 — UI Canvas 점진 이주 (2주)

1. **LobbyCanvas / GameHudCanvas / ResultCanvas** 3개 Prefab 신설, uGUI Button/Text(TMP) 기반. MinimalGameScene 은 Board 렌더/입력만 담당(≈400 line 목표).
2. **LobbyController / ResultController** 별도 MonoBehaviour 로 분리 — ScreenState 제거, Canvas.enabled 토글로 대체.
3. **Design System 토큰 매핑**: design_system.md Radius/Elevation/Color 팔레트를 ScriptableObject (`UiTheme.asset`) 로 외부화 → IMGUI 하드코드 색상 추방.
4. **TMP Essential Resources import 자동화**: Editor 스크립트로 batchmode 빌드 시 보장 (현 DebugSplashLabel 주석의 이슈 해결).
5. **PlayerProfile 통합**: 별점 `PlayerPrefs → PlayerProfile.StageProgress` 로 1회성 마이그레이션 후 SaveService 경유.

### v1.3 — Scene 분리 + DI 합류 (1주, architecture_v2.md v1.3 VContainer 트랙과 합병)

1. **Lobby.unity / Game.unity / Result.unity** 로 Scene 물리 분리. 기존 Lobby.unity/Game.unity 재활용, MinimalGameScene 은 **삭제 또는 Editor-only 로 격리**.
2. **SceneLoader.LoadGameAsync(StageRequest)** 로 `GameRoot.PendingProfile` static 제거 (DV4 준수).
3. **MinimalGameScene 의 Board 렌더링** → `View.BoardView` 와 통합, Domain `Board/ChainProcessor/Scorer/Palette` 활성. ColorMixer.Mix 섀도우 로직 제거.
4. **LocalizerService 활성**: 모든 문자열 `Loc.T("stage.s1.title")` 경유.

---

## 4. 즉시 착수 권고 5건 (다음 라운드 포함 요청)

1. **OnDestroy + StopAllCoroutines + Texture2D 해제 패치** — 리스크 낮고 즉시 G1 수습. (Coder 1시간)
2. **AppBootstrap.Audio/Haptic 우선 소비 라우팅** — G2/G3 동시 해결, Phase 4 정책 정합. (Coder 2시간)
3. **MinimalGameScene partial 3분할** (Lobby/Playing/Result) — A1 가독성 즉시 개선, 이후 Canvas 이주 기반. (Coder 2시간)
4. **UiTheme ScriptableObject 초안** — design_system.md Radius/Elevation/Color 토큰을 코드에 주입 가능한 포맷으로 외부화. (UX + Coder 반나절)
5. **LocalizerService 연결 포인트 식별 문서화** — F1 대비, v1.2 진입 전 string literal 위치 인벤토리. (Doc 1시간)

우선순위 근거: 1~2 는 **누수/싱글톤 무시**라는 v1.0 회귀 리스크, 3 은 **이후 모든 UI 이주의 전제**, 4~5 는 v1.2 Canvas 이주의 **가드레일**. 5건 모두 **MinimalGameScene 코드 수정 범위**이며 Domain 레이어 무변경 — 120+ 테스트 무회귀 조건 충족.

---

## 5. 원칙 재확인

- **단일 축 변경** (architecture_v2.md §2.4): MessagePipe(v1.1) / Canvas(v1.2) / Scene 분리(v1.3) / Network(v2.0) 순서 위배 금지.
- **Domain 순수성 유지**: MinimalGameScene 은 Bootstrap 책임. 이주 후에도 Domain 은 UI 기술(IMGUI/uGUI/UI Toolkit) 을 모른다.
- **결정론 유지**: Seed 기반 Board 구성 / PlayerAction 경로 / Replay 재생 가능성 유지 — Drag-to-Mix 를 **SwapCommand + PaletteUseCommand** 로 전환할 때도 동일.
- **playground 격리**: MinimalGameScene 계속 쓸 경우 `Alchemist.Bootstrap.Playground` 하위 asmdef 로 분리해 실사용 경로와 구분.

---

## 3문장 요약

MinimalGameScene 은 881 line 단일 MonoBehaviour 가 OnGUI 로 Lobby/Play/Result 를 동시에 처리하며 Domain 16 asmdef 자산·design_system 토큰·AppBootstrap 싱글톤 전부를 우회하는 **섀도우 트랙**이므로 현재 Health Grade 는 C(Warn) 이다. 해결책은 v1.1 최소 안전화(OnDestroy·AppBootstrap 소비·partial 분할) → v1.2 Canvas 3분할 + Design Token 외부화 → v1.3 Scene 분리 + Domain 합류의 **3단 점진 이주**이며, 각 단계는 architecture_v2.md 의 "단일 축 변경" 원칙을 준수한다. 다음 라운드 즉시 착수 권고 5 건은 모두 MinimalGameScene 국소 수정 범위로 120+ 기존 테스트 무회귀가 보장된다.
