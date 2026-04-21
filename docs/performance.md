# 컬러 믹스: 연금술사 - Phase 0 성능·최적화 가이드

**작성자:** Performance Engineer Teammate
**작성일:** 2026-04-21
**대상 단계:** Phase 0 (조기 성능 기반 확립)
**엔진 후보:** Unity 2D (유력) / Flutter + Flame (대안)

> 본 문서는 Phase 0에서 **엔진/아키텍처 결정 이전에 미리 못 박아야 하는** 성능 계약(contract)을 정의한다. 게임 로직보다 먼저 성능 예산·계측 인프라·풀링 구조를 넣어두지 않으면, 연쇄 폭발·Metaball 셰이더·UI 리빌드가 동시에 터지는 "3단 연쇄 이상" 구간에서 스파이크가 고착된다.

---

## 1. 성능 예산 (Budget)

### 1.1 프레임 예산 - 60fps = 16.6ms

**결정:** 저사양 타깃(2GB RAM, Adreno 506급 / Apple A10급)에서도 **평균 16.6ms / p99 22ms** 유지.

프레임 16.6ms 분해 (저사양 기준):

| 영역 | 예산(ms) | 비고 |
|---|---|---|
| Game Logic (매치 판정/보드 상태) | 3.0 | 연쇄 탐색·색 조합 룩업 포함 |
| Physics / Tween (Jelly 반응) | 2.0 | DOTween 또는 Flutter AnimationController |
| Particle / VFX 업데이트 | 2.5 | CPU 파티클 처리 |
| Rendering (Draw Call 제출) | 5.0 | SRP Batcher / Flame canvas |
| UI (HUD, 팔레트, 점수) | 2.0 | UGUI Canvas 분리 후 목표 |
| Audio + Haptic | 0.5 | 메인 스레드 비점유 원칙 |
| Reserve (GC/스파이크 흡수) | 1.6 | 여유 버퍼 |

**결정:** Draw Call 목표 **저사양 < 80 / 중사양 < 150 / 고사양 < 250**. SetPass Calls는 **저사양 < 40**.

**결정:** 30fps 모드(배터리 세이버) 스위치는 **Phase 1부터 제공**. Phase 0에서는 `Application.targetFrameRate` 토글 hook만 삽입.

### 1.2 메모리 예산 (2GB RAM 기기 기준)

**결정:** 앱 런타임 총 메모리 **≤ 450MB** (iOS jetsam / Android low-memory killer 여유 확보).

| 카테고리 | 캡(MB) | 비고 |
|---|---|---|
| 텍스처 (로드된 아틀라스) | 120 | ASTC 6x6 (Android) / ASTC 4x4 (iOS) |
| 오디오 (동시 디코딩) | 25 | BGM 1개 streaming + SFX 풀 |
| 메시/파티클 버퍼 | 15 | 풀 사전할당 포함 |
| 스크립트/매니지드 힙 | 80 | Unity Mono / Flutter Dart heap |
| 엔진 내부/셰이더 | 60 | SRP Batcher 버퍼 포함 |
| 네이티브 (UI, 플랫폼) | 80 | |
| Reserve | 70 | GC 피크 흡수 |

**결정:** 텍스처는 **2048x2048 아틀라스 최대 3장 동시 상주**. 그 이상은 Addressables/lazy load.
**결정:** 압축 포맷 **Android: ASTC 6x6 (fallback ETC2) / iOS: ASTC 4x4**. RGBA32 원본 배포 **금지**.
**권고:** 오디오 BGM은 **Vorbis / 96kbps / 22kHz mono**. SFX는 **PCM wav / 매우 짧은(<0.5s) 파일은 메모리 상주**, 그 외는 스트리밍.

### 1.3 배터리·발열

**결정:** **15분 연속 플레이 시 단말 표면 온도 상승 ≤ 8°C** 목표 (iPhone SE2 / 갤럭시 A 시리즈 벤치).
**결정:** 화면 정적 상태(일시정지·튜토리얼 대화)에서는 **targetFrameRate = 30** 으로 다운시프트.
**권고:** 셰이더 복잡도 지표 - Metaball 픽셀 셰이더의 texture sample은 **픽셀당 ≤ 4회**.
**유보:** "배터리 세이버 모드에서 파티클 수 자동 축소" - UX팀과 Phase 1에서 결정.

---

## 2. 핫스팟 예측 & 대응

### 2.1 연쇄 폭발 시 동시 파티클

**예측:** 3단 연쇄 = 동시 파괴 블록 ≥ 15개 × 블록당 파티클 30 = **450 파티클 동시**. 5단 연쇄 시 1000개 초과 가능.

**결정:** 파티클 총량 하드캡 **저사양 400 / 중사양 800 / 고사양 1500** (Quality 단계별). 초과분은 스폰 스킵.
**결정:** **오브젝트 풀링 사전할당**. 기동 시 블록 풀 120개, 파티클 시스템 풀 20개, Trail 풀 8개. `Instantiate/Destroy` 런타임 호출 **금지**.
**결정:** 파티클 시스템은 **Simulation Space = Local + GPU Instancing 가능한 머티리얼**만 사용. Soft Particles OFF.
**권고:** 대규모 연쇄는 **단일 "ExplosionBurst" ParticleSystem**을 위치 재사용으로 `Emit(count)` 호출 (여러 PS 생성 금지).
**권고:** Trail Renderer는 **저사양에서 disable**. 대체로 9-patch 라인 스프라이트 사용.

### 2.2 Metaball / Jelly 효과

**트레이드오프:**

| 방식 | 장점 | 단점 | 채택 기준 |
|---|---|---|---|
| 런타임 Metaball 셰이더 (distance field) | 해상도 독립, 유려 | 픽셀 셰이더 비용 높음 (모바일 GPU fill rate 부담) | 고사양 전용 |
| Sprite Sheet (12~16프레임) | 저비용, GPU 친화 | 메모리 사용, 해상도 의존 | 저/중사양 기본 |
| SDF(Signed Distance Field) 텍스처 + 간단 셰이더 | 메모리 절약 + 스케일 가능 | 구현 복잡도 | 중사양 |

**결정:** **기본 = Sprite Sheet (저/중사양) / 셰이더 업그레이드 = 고사양**. Quality Level 분기.
**결정:** Metaball 영역의 **최대 화면 점유율 ≤ 25%** (overdraw 제어).
**권고:** Jelly squash-stretch는 **버텍스 셰이더의 sin 기반 오프셋**으로 구현 (CPU tween 금지).

### 2.3 보드 리빌드

**결정:** 보드 전체 리빌드 **금지**. **Dirty flag 기반 증분 업데이트**: 변경된 셀만 `SetDirty()` → 다음 프레임에 해당 셀 렌더 갱신.
**결정:** 보드 내부 표현은 **1차원 `int[]` 배열 (rows*cols)**. 2D 배열·`List<List<Cell>>` 금지 (캐시 친화성).
**결정:** 중력 낙하/리필 로직은 **열 단위 `for` 루프, LINQ 금지**, 할당 0 목표.
**권고:** 보드 크기 ≥ 9x9 시 **열 단위 Job 분할** (Unity Job System 검토).

### 2.4 색 조합 연산

**결정:** 색 조합 테이블은 **`Dictionary<(ColorId, ColorId), ColorId>` + 사전 계산**, 런타임 O(1) 조회.
**결정:** 키는 **정렬된 튜플** (`(min, max)`), 교환 법칙 적용하여 항목 수 절반.
**권고:** `ColorId`는 **byte enum**. `string` 기반 키 금지 (해시/할당 비용).
**권고:** 자주 조회되는 상위 10개 조합은 **2차 배열 캐시** (256x256 byte 배열 = 64KB, L2 캐시 친화).

### 2.5 UI 리빌드

**Unity UGUI:**
- **결정:** Canvas 3단 분리 - `StaticCanvas` (배경/프레임) / `DynamicCanvas` (점수/콤보) / `OverlayCanvas` (팝업). 정적 Canvas 리빌드 방지.
- **결정:** TextMeshPro 사용 (Unity UI Text 금지), 점수 `SetText(int)` 오버로드로 할당 제거.
- **권고:** 리스트형 UI(도감, 상점)는 **UIToolkit 또는 ScrollRect 풀링**.

**Flutter/Flame:**
- **결정:** 고정 위젯은 모두 `const` 적용. `const MyWidget()` 리빌드 스킵.
- **결정:** 상태 분리는 `Selector<T, R>` (Provider) 또는 `ValueListenableBuilder`. `setState` 전체 리빌드 금지.
- **결정:** 스크롤 목록은 **반드시 `ListView.builder` + `itemExtent`** (레이아웃 계산 축소).
- **권고:** 파티클/보드는 **`RepaintBoundary`로 래핑**, 레이어 분리.

---

## 3. 엔진별 구체 가이드

### 3.1 Unity 2D

- **결정:** **URP 2D Renderer** + **SRP Batcher 활성화**. Built-in Pipeline 금지.
- **결정:** **Sprite Atlas V2**로 모든 2D 에셋 패킹, `Include in Build` 명시. 블록/파티클/UI 각각 분리.
- **결정:** **Addressables** 사용. Scene 직접 reference 금지. 스테이지별 번들 분리.
- **결정:** **Audio Mixer** - BGM / SFX / UI 3채널. SFX 채널은 Voice Limit **16**.
- **결정:** `Update()` 남발 금지. Manager 하나가 `IUpdatable` 리스트 순회. `Update` 메서드 총합 ≤ 50개.
- **결정:** GC 할당 감시 - 프레임당 매니지드 할당 **0B 목표**. `foreach`(IEnumerable), `string +`, LINQ, 람다 캡처 금지.
- **권고:** 벡터 연산 많은 로직(낙하·확산)은 **Burst + Job System** 검토 (Phase 1 측정 후 적용).
- **권고:** 물리 엔진(Physics2D) **사용 최소화**. 보드 로직은 전부 grid 기반, Physics 미사용.
- **권고:** Build 설정 - IL2CPP + ARM64 only (armv7 드롭). Managed Stripping Level = Medium 이상.

### 3.2 Flutter + Flame

- **결정:** Flame의 `Component` 트리에서 정적 배경은 **`PositionComponent` + 별도 레이어**, repaint 분리.
- **결정:** **`RepaintBoundary`** 로 보드·파티클·HUD 3분할.
- **결정:** 이미지 로딩은 **`Flame.images.loadAll` 부팅 시 일괄**, 런타임 lazy 로드 지양 (jank 원인).
- **결정:** 상태 관리는 **Riverpod + `Selector` 유사 패턴** 또는 `signals`. 전역 `setState` 금지.
- **결정:** 네트워크/파싱은 **`compute` 또는 `Isolate`** 로 분리. 메인 isolate blocking ≤ 8ms.
- **권고:** 파티클은 Flame의 `ParticleSystemComponent` 풀링 래퍼 자작.
- **권고:** `ListView.builder` + `itemExtent` 필수, `shrinkWrap: true` 금지.
- **유보:** Flame의 Rive 통합 vs Lottie - 라이선스/성능 재측정 필요.

---

## 4. 비동기 / 네트워크

**결정:** 랭킹·일일 퍼즐·배지는 **전부 비동기**, UI 블로킹 금지.
**결정:** **디바운싱 300ms** - 점수 업로드는 연쇄 종료 후 300ms 유휴 시 1회 전송.
**결정:** 랭킹 리스트 **페이지 크기 20**, 스크롤 하단 근접 시 다음 페이지 선로딩.
**결정:** 일일 퍼즐 메타데이터 **Cache TTL 1시간**. 배지 정의 TTL 24시간. HTTP `ETag` 사용.
**결정:** 재시도 정책 - **exponential backoff (1s, 2s, 4s, 최대 3회)**, jitter ±20%.
**결정:** 오프라인 큐 - 점수/진행도는 로컬 SQLite 큐 저장, 온라인 복귀 시 flush. **Phase 0에서 인터페이스만 잡고 구현은 Phase 1.**
**권고:** 이미지 썸네일(프로필/아바타)은 **lazy + 플레이스홀더** 필수, 다운로드 동시성 **4**.
**권고:** Payload는 **Protobuf 또는 MessagePack** 검토. JSON 유지 시 최소 gzip.

---

## 5. 측정·관측 계획

### 5.1 빌드 타임 체크

**결정:** CI에 **에셋 감사 태스크** 추가:
- Sprite 비아틀라스 검출 → fail
- 텍스처 2048 초과 검출 → fail
- Uncompressed 오디오 검출 → warn
- `Resources/` 폴더 사용 → fail (Addressables 강제)

### 5.2 런타임 계측

**결정:** **Phase 0에 디버그 HUD 필수 구현** (F12 또는 3지 탭):
- FPS (current / 1s avg / 10s min)
- Frame time breakdown (CPU/GPU)
- Draw Calls / SetPass
- GC Alloc / frame (byte)
- Managed Heap Size
- Particle 활성 수
- Active Pool usage

**결정:** **Frame hitch 로깅** - 프레임 시간 > 33ms 시 직전 1초 이벤트 스택 덤프.
**결정:** 프로덕션 빌드에 **Firebase Performance Monitoring + Sentry** 통합. Custom trace: `match_resolve`, `chain_explosion`, `board_rebuild`, `scene_load`.
**권고:** 일 1회 샘플링된 디바이스 성능 로그 수집 (기종/OS/평균 fps). 사용자 <50fps 비율을 Phase 1 KPI로.
**권고:** Unity Profiler는 **개발 빌드 + Deep Profile 주 1회** 기준.

### 5.3 성능 회귀 기준

**결정:** PR 머지 차단 지표 (CI 성능 테스트):
- 저사양 기준 5단 연쇄 시나리오 **평균 fps ≥ 55**
- 보드 리빌드 1회 **≤ 6ms**
- GC allocation per frame **≤ 1KB** (매치 중)

---

## 6. 리스크 & 조기 경고 지표

| 리스크 | 조기 경고 지표 | 대응(LOD/Degrade) |
|---|---|---|
| **[최우선] 3단 연쇄 이상에서 프레임 스파이크** | 연쇄 depth ≥ 3 구간 p95 frame > 25ms | 파티클 수 50% 감소, Trail 비활성, Metaball → SpriteSheet 강제 전환 |
| Metaball 셰이더 fill rate 폭주 | GPU time > 8ms | 해상도 렌더 스케일 0.85 → 0.75 다운, 셰이더 branch 단순화 |
| 메모리 피크 시 jetsam/LMK | RSS > 400MB | 비활성 스테이지 아틀라스 언로드, 오디오 캐시 flush |
| Audio voice 고갈로 SFX 소실 | Mixer voice > 14 | 낮은 우선순위 SFX drop, 콤보 SFX 병합 |
| Scene 로드 중 hitch | 로드 중 메인스레드 점유 > 50ms | Addressables `LoadAssetAsync` 분할, 프리로드 스테이지 진입 1단계 전 |
| 저사양 기기 열 throttling | 15분 후 avg fps 감소율 > 15% | 30fps 모드 권유 배너, 파티클 quality auto-down |
| 긴 문자열 가비지 (점수 포매팅) | GC alloc > 10KB/frame | StringBuilder 풀 + ZString 도입 |

**결정:** Quality Level은 **3단계 (Low/Mid/High)** + 런타임 동적 다운그레이드 경로 구현.
**결정:** 연쇄 단계별 예산 회귀 테스트를 Phase 0 말미에 **재현 가능한 시나리오 스크립트**로 저장.

---

## 7. 교차레이어 조정 - Architect / UX / Doc 에게 던지는 질문

**Architect에게:**
1. 게임 상태 머신은 ECS/Component 지향인가, OOP Manager 구조인가? → Job System 적용 가능성과 직결.
2. 멀티 디바이스 세이브 동기화 방식? (Firebase Firestore 실시간 vs REST 풀링) → 네트워크 예산·배터리 영향 결정.
3. 스테이지 데이터 포맷? (ScriptableObject / JSON / Protobuf) → 로드 타임·메모리에 영향.
4. 리플레이 기능 계획 유무? → 입력 기록·결정론적 시뮬레이션 설계가 Phase 0에 필요.

**UX에게:**
1. 팔레트 슬롯 드래그 궤적에 **Trail 이펙트** 요구되는가? → Trail 수·텍스처 크기 결정.
2. 연쇄 폭발 시 **화면 흔들림/후광/슬로우 모션** 동시 연출 여부? → Post-Processing 비용·Time.timeScale 영향.
3. 동시 표시 **플로팅 숫자(점수 팝업) 최대 수**? → 풀 크기 확정.
4. **햅틱 강도/빈도** 기대 수준? → iOS Core Haptics 비용, 배터리 영향.
5. Metaball을 **어느 블록 레벨부터** 강조 적용? → 전역 vs 조건부 셰이더 결정.
6. 튜토리얼/온보딩 씬에서 **배경 애니메이션 수준**? → 첫인상 로드 타임 타협 지점.

**Doc에게:**
1. 저사양 기기 정의(공식 지원 최저 스펙)를 **문서화 스펙 목록**으로 명시해 달라 - 현재 `2GB RAM / Adreno 506 / A10` 로 가정.
2. 빌드 바이너리 크기 목표(앱 스토어 cellular download 200MB 이하 권장) 확인.
3. 개인정보 관련 원격 로깅(Firebase Performance·Sentry) **수집 항목 문서화** 필요.

---

## 8. Phase 0 체크리스트 (완료 기준)

- [ ] Quality Level 3단계 정의 및 스크립트 스위치
- [ ] 블록/파티클 오브젝트 풀 사전할당 구현
- [ ] Sprite Atlas 규칙 + CI 감사 스크립트
- [ ] 디버그 HUD (FPS/Draw Call/GC)
- [ ] Frame hitch 로깅 훅
- [ ] 색 조합 Dictionary 룩업 기준 코드
- [ ] 보드 Dirty flag 증분 업데이트 프로토타입
- [ ] Audio Mixer 3채널 구성
- [ ] CI 성능 회귀 임계치 설정
- [ ] Addressables 부트스트랩
