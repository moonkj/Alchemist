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
