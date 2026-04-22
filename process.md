# process.md — 컬러 믹스: 연금술사 구현 진행 기록

> 팀 리더(Architect)가 구현 단계마다 업데이트합니다.

---

## 2026-04-21 — Phase 0 Kickoff

### 수행 항목
- 팀 에이전트 역할 메모리 저장 (아키텍트 + Teammate 1~4)
- 협업 프로토콜 메모리 저장 (병렬처리 / 과학적 토론 / 교차레이어 조정)
- 프로젝트 스캐폴드 생성: `Tasklist.md`, `process.md`
- 병렬 Agent 가동:
  - UX Designer → 유저 플로우 & 와이어프레임
  - Architect → 기술 스택 & 시스템 아키텍처
  - Performance → 모바일 성능 고려사항
  - Doc Writer → README 초안

### 결정 사항
- 엔진 결정은 Phase 0 통합 리뷰 후 유저와 확정 (Unity 2D 유력, Flutter 옵션)
- tmux 미설치 → Claude Code 네이티브 병렬 Agent로 대체

### 다음 단계
- 4개 병렬 에이전트 산출물 수령 → 아키텍트 통합 리뷰
- Phase 1 MVP 설계 착수 여부 유저 확인

---

## 2026-04-21 — Phase 0 통합 리뷰 완료

### 수행 항목
- 4개 Teammate 산출물 수령 완료
  - `docs/ux_design.md` (UX Designer)
  - `docs/architecture.md` (Architect, Plan모드 결과를 리더가 파일화)
  - `docs/performance.md` (Performance Engineer)
  - `README.md`, `docs/glossary.md` (Doc Writer)
- 교차레이어 충돌 3건 과학적 토론으로 해소 → `docs/phase0_integration_review.md`
- Tasklist P0-03~07 🟩, P0-08 유저 확정 게이트 오픈

### 확정 결정 (15건)
D1 Unity 2D / D2 저사양 A10·2GB / D3 보드 6×7 / D4 팔레트 3슬롯 / D5 Drag&Drop 하이브리드 / D6 Metaball SpriteSheet 기본 / D7 파티클 400/800/1500 / D8 비트플래그+2차캐시 / D9 1D 보드 배열 / D10 프리즘 결정론적 / D11 팔레트 연쇄 불참 / D12 연쇄 깊이 10캡 / D13 hover 기반 프리뷰 / D14 빌드 200MB / D15 로컬우선 세이브

### 리더 요청 사항 (유저 게이트)
1. Unity 2D 엔진 승인 여부
2. Phase 1 착수 범위 (전체 vs P1-01~04 핵심 4건)
3. GitHub 리모트 연동 — 리포 URL 필요

---

## 2026-04-21 — Phase 1 Wave 1 완료

### 유저 결정
- 엔진: Unity 2D 승인
- 범위: P1-01 ~ P1-11 전체
- 리포: https://github.com/moonkj/Alchemist.git (푸시 완료, root-commit `3994e0c`)

### Wave 1 산출물 (4개 Coder 병렬)
- Coder-1 P1-01 ColorMixer: 5 files (Colors/)
- Coder-2 P1-02 Block FSM: 7 files (Blocks/) — 15 전이 허용
- Coder-3 P1-06 Prompts: 9 files (Prompts/) — 박싱 허용(Phase 1 한도)
- Coder-4 P1-07 Scorer: 5 files (Scoring/) — BeginStage(par) API
- 합계 **26 files**, 전부 Domain POCO, GC alloc 0 경로 확보

### 교차레이어 결정 5건 (`docs/phase1_wave1_addendum.md`)
- C1 Gray 표현: Kind=Gray + Color=None
- C2 GetColorsCreated: exact match
- C3 OnBlocksExploded(count): 블록 수 곱
- C4 Mix 엣지: White/Black/Prism/Gray 케이스 확정
- C5 MessagePipe: Phase 2 이연, Phase 1은 델리게이트 필드

---

## 2026-04-21 — Phase 1 Wave 2 완료 (Board/Chain/View)

### 산출물
- Domain Board/Chain (Coder-5, 13 files — 1D 42셀 배열, 웨이브 큐)
- View/UI/Bootstrap (Coder-6 + 리더 보완, 12 files)
- 두 코더가 병렬 작업하여 `IChainAnimationHub` 시그니처 자동 정합

### 교차레이어 결정 (Wave2 addendum)
- 보드 6×7 고정 / 감염 4방향 / depth 10 하드캡

## 2026-04-21 — Phase 1 Wave 3 완료 (Debug/Test/Perf)

### 산출물
- Debug Report (Critical 5 / High 12 / Medium 9 / Low 10)
- Test Engineer 104 케이스 EditMode
- Performance Review (Grade B+, Critical 3)

### 아키텍트 결정 7건 (D16~D22)
- D16 Mix(Black, X) = Black 오염 전파 재확정
- D17 Prism 우선순위 (Gray/Black > Prism)
- D18 MatchDetector H/V 분리 + ChainProcessor dedupe
- D19 Scorer.OnColorCreated 신설 (생성 ≠ 폭발)
- D20 infectedMask bitset (첫 감염만)
- D21 감염 블록은 다음 depth 폭발
- D22 Phase 1 par=movesLimit 허용

### Wave 3 Fix (F1~F11) — Critical 8건 + 핵심 High 3건
- F1 ColorMixer 재작성 / F2~F4 BoardView null 방어 + Scorer 주입
- F5 GameRoot 초기 Refill / F6 infectedMask / F7 GameContext POCO
- F8 NotifyMoveCommitted 전파 / F9 delegate 캐싱
- F10 Spawner Block 풀 / F11 MatchGroup 사전 할당

### Reviewer 라운드 (R1 FAIL → R2 PASS-with-minor)
- R2-1 Chain asmdef에 Scoring 참조 누락 → 수정
- R2-2 ChainProcessorTests ctor 인자 정렬 → 수정

## Phase 1 완료 선언
- GitHub 태그 `v0.1.0-phase1` (커밋 a9268f4)
- Phase 2 백로그: H/M/L 잔여 이슈 + UX Q 유보 7건 (`docs/phase1_wave3_decisions.md` §2 참조)

**다음 단계:** Phase 2 — 특수 블록(필터/회색/프리즘) + 팔레트 슬롯 + 프롬프트 확장

---

## 2026-04-21 — Phase 2 완료

### 산출물
- Domain/Chain: GrayReleaseTracker, PrismAbsorbProcessor (+ ChainProcessor filter transit)
- Domain/Palette: Palette/PaletteSlot/IPaletteEvents (3슬롯)
- Domain/Prompts: FilterTransit/UsePaletteSlot Condition + DailyPuzzle
- Domain/Stages: StageData(SO) + StageLoader (D22 parMoves/maxMoves)
- View/PaletteView, UI/LocalizerService
- Bootstrap/GameRoot Phase 2 확장 (InputController.OnSwap 구독, OnFilterTransit 콜백)
- Tests: 6종 추가

### 아키텍트 결정 (D23~D25)
- D23 Gray: 2회 누적 해제 (턴 리셋)
- D24 Prism 승격은 턴 종료
- D25 필터 벽 = 낙하 경로 색 변환만

### Phase 3 이연
- Palette→Board 적용 로직 (PlayerAction)
- 실시간 프롬프트 UI 갱신 세부화

**태그:** `v0.2.0-phase2`

---

## 2026-04-21 — Phase 3 완료

### 산출물 (64 files)
- Domain/Ranking: IRankingService + RankingBoard (4 category enum, Top N)
- Domain/Badges: 16 배지 (조합 6 / 스타일 5 / 히든 5) + 12 조건 구현
- Domain/Replay: 순환 버퍼 기반 ReplayRecorder
- Services/Ranking: LocalRankingService (JSON persistence)
- Domain/Economy: InkEnergy(max5/300s), Inventory, ItemEffectProcessor (Brush/Eraser/Prism)
- Domain/Meta: Artwork(챕터 1 = 12조각), GalleryProgress
- Domain/Player: PlayerProfile + SaveService (atomic tmp→bak→rename) + MiniJson
- UI/InkEnergyDisplay, ItemButton, GalleryScreen
- Bootstrap/MetaRoot, GameRoot PendingProfile 훅
- Tests 10종 추가

**태그:** `v0.3.0-phase3`

---

## 2026-04-21 — Phase 4 완료 (최종)

### 산출물 (32 files)
- View/Effects: GraphicsQualityLevel, QualityManager (120프레임 p95 25ms 자동 다운그레이드)
- Shaders: Metaball2D.shader, JellyDeform.shader
- Services/Haptic: 7 이벤트 매핑 + Rich/Basic/Off 3단계
- Services/Audio: 7 SfxId + AudioMixer 3채널
- Services/Theme: Light/Dark 스위치
- UI/Onboarding: TutorialStage0 (빨+파=보라 4스텝)
- UI/Debug: DebugHud (FPS/Drawcalls/GC alloc), FrameHitchLogger
- Bootstrap/AppBootstrap (서비스 싱글톤 등록) + GameRoot Swap 시 Haptic/Audio 트리거

**태그:** `v1.0.0` (최종)

---

## 프로젝트 완료 선언 🎯

- **릴리스 태그**: v0.1.0-phase1 → v0.2.0-phase2 → v0.3.0-phase3 → **v1.0.0**
- **총 코드**: 170+ files / 16 어셈블리 / 120+ 테스트 케이스
- **아키텍트 결정**: 25건 (D1~D25)
- **과학적 토론**: 5건 충돌 해소
- **전체 요약**: `docs/project_summary.md`
- **후속 작업**: Phase 5 백로그 참고

---

## 2026-04-21 — iPhone 무선 설치 점검

### 유저 요청
"아이폰에 설치해줘" → 무선 기기 설치 실행 요청.

### 현재 시스템 점검 결과
- ✅ **Xcode 26.3 설치됨** (Build 17C529)
- ✅ **iPhone Air `Moon` 무선 연결 상태** (udid `835A5E84-05B4-520C-B52C-E69BBEE38FED`, state: connected)
- ✅ **서명 인증서 유효**: Apple Development (imurmkj@naver.com / R3K972V8DA) + Apple Distribution (kyeongju Moon / QN975MTM7H)
- ❌ **Unity Editor 미설치** — `/Applications/Unity*`, `/Applications/Unity/Hub/Editor/` 둘 다 부재
- ❌ **ios-deploy 미설치** (단 `xcrun devicectl`은 Xcode 26 번들)
- ❌ **.app 번들 없음** (빌드된 적 없음)
- ❌ **Scene/Prefab 부재** — 프로젝트는 순수 C# 스크립트 스캐폴드

### 진짜 블로커
```
[C# 소스] ──❌Unity 미설치──> [Xcode 프로젝트] ──✅archive──> [.app] ──✅devicectl 무선──> iPhone
         Scene/Prefab도 없음
```
무선 설치 단계는 가능하나, 그 앞단(Unity 빌드)이 불가능.

### 설치 가이드 산출물
- `Packages/manifest.json` — Unity 6 LTS + URP 2D + TMP + Test Framework
- `ProjectSettings/ProjectVersion.txt` — Unity 6000.0.32f1 LTS 마커
- `INSTALL_iOS.md` — 7단계 수동 설치 가이드 (환경 준비 → Scene 제작 → 서명 → 기기 설치)

### 진행 선택지 (유저 결정 대기)
- **A. Unity 설치 후 정상 경로** (`brew install --cask unity-hub` → Editor 설치 → Scene 제작 → 빌드 → devicectl 무선 설치, 총 4~10시간)
- **B. Swift/SpriteKit 네이티브 프로토타입** (Unity 우회, 2~4시간, 별도 코드베이스)
- **C. Unity Cloud Build + TestFlight** (GitHub push → 자동 빌드 → TestFlight 무선 배포, Unity Personal 계정 필요)

**결정: A** — Unity Hub brew 설치 → Editor 6000.0.32f1 + iOS Build Support 직접 pkg 다운로드
(`~/UnityDownloads/Unity-6000.0.32f1.pkg` 4.97GB, `UnitySetup-iOS-Support-*.pkg` 368MB) → sudo installer.

---

## 2026-04-21 — Unity 설치 + MinimalGameScene 초기 빌드

### 설치 체인
- Unity Hub 3.17.2 (brew cask) → Editor 6000.0.32f1 LTS + iOS Support 모듈 설치
- Unity 라이선스 활성화 (Unity Personal, GUI 사인인)
- Unity.app 경로: `/Applications/Unity/Unity.app` (Hub 관행 경로 아님 → build_ios.sh 수정)
- `Assets/_Project/Editor/BuildScript.cs` 에 `Alchemist.EditorTools.BuildScript.BuildIOS()` 메서드 구현
  - 배치 모드 Scene 자동 생성 (Camera + Canvas + EventSystem)
  - PlayerSettings: IL2CPP / ARM64 / iOS 13+ / 팀 ID QN975MTM7H
- `scripts/build_ios.sh` 4단계 파이프라인:
  1. Unity batch build → Xcode 프로젝트 생성
  2. xcodebuild archive (Automatic Signing)
  3. xcodebuild exportArchive → `.ipa`
  4. `xcrun devicectl device install app --device 835A5E84-... <ipa>` 무선 설치

### 버그 수정 체인
- 초기 TMP 렌더링 실패 (TMP_Essential_Resources import 안 됨, batchmode 에서 자동 import 불가) → **DebugSplashLabel** (IMGUI OnGUI 기반 내장 폰트) 로 대체
- PRODUCT_BUNDLE_IDENTIFIER 오버라이드가 framework 에도 전파 → UnityFramework / 앱 bundle ID 충돌 → xcodebuild 오버라이드 제거, Unity PlayerSettings 만으로 서명

### 최초 iPhone 설치 성공
- Bundle ID: `com.moonkj.colormixalchemist`
- 설치 완료 후 화면에 Unity 로고 → 검은 화면 (TMP 미작동 표시)
- **MinimalGameScene 전환**: 수동 Scene 없이 6×7 컬러 블록 보드 + 탭 반응 프로시저얼 씬으로 교체

---

## 2026-04-22 — 게임 플레이 구현 + UX v3 전체 리뷰

### Drag-to-Mix + 매치-3 전체 루프
- 인접 셀 드래그 드롭 → `ColorMixer.Mix()` 적용 (D16/D17/D26 오버사트 규칙 반영)
- **ColorMixer D26**: combinedPopCount == max(aPop, bPop) 면 과포화 → Black
  (Purple+Red=Black, Orange+Yellow=Black 등)
- 2차 이상 3연결 매치 → 폭발 → 중력 → 리필 Cascade (depth 10 캡)
- 연쇄 깊이 점수 보너스, OnGUI 점수/턴 HUD

### Polish 라운드 (유저 피드백 반영)
- **낙하 애니** + 폭발 확대/페이드 + Coroutine 기반 단계 지연
- **물감 블록 룩** — SDF rounded-square + 그라디언트 + 하이라이트 하이비트
- **화면 진동** (Handheld.Vibrate) — 폭발 전용
- **스플래시 파티클** 파편 방사

### 5 스테이지 + 로비 + 결과 화면 (M2+M3)
- ScreenState enum (Lobby/Playing/Result)
- StageConfig 배열 — Orange/Green/Purple/Green/White 목표
- 별점 PlayerPrefs 영속 (`stars_{stageId}`)
- 다음 스테이지 잠금 해제 (전 스테이지 ★1 이상)
- 프로시저얼 사운드 — AudioClip.Create (mix/explode/clear/fail)

### 전체 UX/UI v3 팀 리뷰 (3 병렬 리뷰어)
| 팀원 | 등급 | 산출물 |
|------|:---:|--------|
| UX/UI Senior | B- | `docs/review_uxui_v3_uxui.md` — 정보 위계/타이포/컬러/레이아웃 갭 |
| Architect | C | `docs/review_uxui_v3_arch.md` — 881 line 단일 MB + 섀도우 트랙 |
| Game Feel | C+ | `docs/review_uxui_v3_juice.md` — Chain/Clear/Fail D급 |

리더 취합 (`docs/review_uxui_v3_leader_aggregation.md`) → **P1~P10 일괄 반영**:
- P1 결과 버튼 오버플로 수정 (동적 폭)
- P2 진행 바 색 GoalColor 동적 바인딩
- P3 Toast 시맨틱 4분기 (Success/Warn/Danger/Neutral) + 0.35s 페이드
- P4 타이포 스케일 재배열 (40/28/16/12)
- P5 Screen.safeArea 자동 대응
- P6 입력 잠금 인디케이터 (캐스케이드 중 dim + "연쇄 처리 중…")
- P7 Chain depth 에스컬레이션 (쉐이크/파티클/SFX pitch)
- P8 결과 시퀀스 (별 순차 점등 + 점수 count-up)
- P9 OnDestroy + Texture2D 해제
- P10 결과 오버레이 페이드 인 + 보드 0.7x scale

---

## 2026-04-22 — Jelly 공격적 패치 + M4 갤러리

### Game Feel 공격적 패치 (`docs/jelly_aggressive_patch.md`)
유저 피드백 "블록 벽돌 같음, 진동 과함":
- Idle 브리딩 ±2.8% → ±7~8.5% XY 반위상 + 회전 wobble
- Drag: teleport → Vector3.SmoothDamp(0.08s) + velocity stretch + rotation
- Gravity: Lerp → EaseOutBounce 320ms + 착지 Y squash + 열 스태거
- Refill: 0.30 → 1.15 → 1.00 스프링 오버슈트 + fade-in
- MixBounceAnim 에코 잔향

### M4 갤러리 복원
- ScreenState.Gallery + 로비 우상단 🎨 버튼
- 챕터 1 "잃어버린 노을" 4×4 픽셀 캔버스 (총 15 조각 = 5 × 3별)
- 중앙→외곽 언락 순서 + 노을 그라디언트

---

## 2026-04-22 — Feel 수정 (유저 불만 대응) + 팔레트 + 튜토리얼 + 설정

### 유저 피드백 수정 체인
1. **"블록 딱딱 + 진동 과함"** → 진동을 폭발 전용으로 한정, idle amplitude 증폭
2. **"가만히 있을 때 움직임 과함"** → idle ±7% → ±1% 대폭 축소, 회전 wobble 제거
3. **"합칠 때 벽돌깨지듯, 화면 왜 어두워짐"** →
   - `_inputLocked` dim overlay 제거 (단일 믹스에도 뜨던 원인)
   - MixBounceAnim → MixPaintFlow 로 재작성 (소스 ghost 분리 + soft pulse 1.10x + smoothstep 색 블렌드)
4. **"합친 후 낙하 블록 검정 변함"** — **Critical 버그**
   - 원인: MixPaintFlow Phase 1(0.22초) 이 소스 블록 SR 점유 중 gravity 가 0.10초에 실행 → 코루틴 충돌, MixPaintFlow 가 끝나며 gravity 세팅 색을 None(회색) 으로 덮어씀
   - 수정: 소스 블록 SR 을 즉시 None 시각화 + 별도 Ghost GameObject 로 slide 연출

### M3 팔레트 슬롯 (핵심 차별화 D4)
- 보드 하단 월드 스페이스 3 슬롯 신설
- DragSource enum (None/Board/Palette) — 4 조합 (보드↔보드 mix / 보드→슬롯 저장 / 슬롯→보드 혼합 / 스냅백)
- MixFromSlotFlow 코루틴: 슬롯 → 타깃 ghost 흐름

### M3 튜토리얼 Stage 0
- ScreenState.Tutorial 신규, 첫 실행 시 자동 진입
- 3페이지 스텝 인디케이터 (환영 / 색 조합 규칙 / 3연결+팔레트)
- 로비 좌상단 "?" 버튼으로 재진입, PlayerPrefs 영속

### M5 설정 화면
- ScreenState.Settings 신규, 로비 "⚙" 버튼
- 효과음 / 햅틱 토글 (ON/OFF 스위치 UI)
- 음량 슬라이더 0~100%
- 튜토리얼 다시보기 · 모든 데이터 초기화 (2단계 확인)
- 연결: `PlaySfx` → SettingSfxOn 체크 + 볼륨 스케일, `Handheld.Vibrate` → `TryVibrate()` 감싸기

---

## 2026-04-22 — 현재 상태 요약

### 구현 완료
- 5 스테이지 드래그-혼합 매치-3 + 중력/리필/폭발/연쇄
- 팔레트 슬롯 3칸 (D4 핵심 차별화)
- 갤러리 복원 챕터 1 (15 조각)
- 튜토리얼 3페이지 (첫 실행 자동)
- 설정 화면 (효과음/햅틱/음량/초기화)
- 결과 오버레이 (별점 순차 + 점수 count-up)
- Jelly 물감 연출 (idle 미세 + mix ghost flow + gravity bounce + refill 스프링)

### 다음 (리더 추천 5번째)
- **스테이지 5 → 12 확장**: 챕터 1 (5) + 챕터 2 "바다의 기억" (7) + 갤러리 2챕터로 확장
- **Phase 5 이연**: Canvas/UGUI 이주, Metaball 실셰이더, 네이티브 햅틱 7-이벤트, 색맹 모드

### 주요 커밋 체인
- `fb58b37` 팔레트 슬롯 D4
- `9a3df9e` 튜토리얼 Stage 0
- `2b71abc` 설정 화면 M5
- `8970859` Mix 버그 수정 + 부드러움 강화
- `83c2875` Feel 수정 (idle 정적 + 물감 흐름 + dim 제거)
- `27aa545` 공격적 jelly 패치 + 갤러리
- `5548dec` UX v3 리뷰 취합 P1~P10
