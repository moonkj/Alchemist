# 컬러 믹스: 연금술사 — Performance & Game Feel v2 고도화 설계안

**작성자:** Performance Engineer + Game Feel Engineer 통합 Teammate
**작성일:** 2026-04-21
**대상 단계:** v1.0.0 완료 직후 → Phase 5 착수 전 성능/체감 재정비
**상위 문서:** `docs/performance.md` (Phase 0 계약), `docs/phase1_performance_review.md` (Phase 1 리뷰 Grade B+)

> v1.0.0까지 Phase 0 성능 계약 §5.3(프레임당 GC ≤ 1KB, 5단 연쇄 avg fps ≥ 55)은 형식적으로 충족 상태다. 그러나 Phase 4 Juice(Metaball/Jelly/Haptic/Audio/Theme/Onboarding) 추가로 **파티클·햅틱·감염 애니의 중첩 레이어**가 한 턴에 동시에 터지는 구조가 되었다. v2는 (1) Critical 3 수정 이후 남은 잠재 병목, (2) Job System/Addressables/다운그레이드 Hysteresis 도입, (3) 체감(Game Feel)과 저사양 양립 전략을 못 박는다.

---

## 1. Phase 1~4 성능 현황 재평가

### 1.1 Critical 3 수정 후 예상 효과 (Phase 1 리뷰 §8 기준)

| 수정 항목 | 턴당 절감 | 근거 |
|---|---|---|
| `BoardView.OnAnimStep` delegate 캐싱 | **-960B/턴** | 15회 × 64B (phase1_performance_review.md §2.4, §5.3) |
| `DeterministicBlockSpawner` pool-backed (`Recycle` + `Stack<Block>`) | **-2.6KB/턴** | 42 refills × ~64B (phase1_performance_review.md §2.8) |
| `MatchGroup.EnsureBuffers` 선할당 (ChainProcessor ctor pre-warm) | **-2KB 첫-실행 스파이크** | 32 × 2 × sbyte[16]+헤더 (phase1_performance_review.md §2.2) |
| **합계** | **steady-state 턴당 GC < 500B** | Phase 0 §5.3 "1KB/frame 매치 중" 여유 2배 |

현재 `ChainProcessor.cs` L63-66을 확인한 결과, `MatchGroup.CreatePooled()`로 **pre-warm이 이미 ctor에서 수행됨**(Critical-3 실질적 반영 완료). Critical-1/2 반영 여부는 `BoardView.cs` / `DeterministicBlockSpawner.cs` 재확인 필요.

### 1.2 프레임 타임/메모리 기대치 (저사양, 2GB RAM, A10급)

| 메트릭 | Phase 0 계약 | Phase 1 실측 목표 | Phase 4 현재 추정 | Phase 5 v2 목표 |
|---|---|---|---|---|
| 평균 frame time | 16.6ms | 14ms | 15~17ms (Metaball/Jelly 추가) | **13ms** |
| p95 frame time | 22ms | 20ms | 22~24ms (5단 연쇄+햅틱) | **20ms** |
| p99 frame time | 22ms | 25ms | 28~32ms (스파이크) | **24ms** |
| GC alloc/frame (매치 중) | ≤ 1KB | ≤ 500B | ~1.2KB (Coroutine+TCS 잔재) | **≤ 300B** |
| Managed heap peak | 80MB | 60MB | ~75MB | **65MB** |
| RSS (총 메모리) | ≤ 450MB | 350MB | ~380MB | **≤ 400MB** |

### 1.3 남은 잠재 병목 TOP 5 (코드 경로 인용)

**① `BlockView.StartCoroutine(ExplosionRoutine/InfectionRoutine/SpawnRoutine)`**
- **경로:** `Assets/_Project/Scripts/View/BlockView.cs` (ExplosionRoutine / InfectionRoutine / SpawnRoutine)
- **비용:** IEnumerator state machine 인스턴스 약 **80B/회**. 3단 연쇄 × 15블록 = **1.2KB/턴** (phase1_performance_review.md §6.5).
- **리스크:** Phase 4 Jelly Deformer까지 Coroutine 기반이면 **2.4KB/턴**으로 증폭 가능.

**② `ChainProcessor.ProcessTurnAsync` async state machine + `IChainAnimationHub.Play*Async` TCS**
- **경로:** `Assets/_Project/Scripts/Domain/Chain/ChainProcessor.cs` L78, L101/L139/L155/L158
- **비용:** state machine ~100B/턴 + TCS 인스턴스 `BoardView.BeginBatch`에서 매 배치 new (phase1_performance_review.md §5.1). 연쇄 depth 5 × 4 Phase = 20 TCS/턴 가능.
- **대안:** `ValueTask<ChainResult>` + `ManualResetValueTaskSourceCore<bool>` 전환. 매치 없는 턴(대다수)에서 `ValueTask.CompletedTask` 반환.

**③ `QualityManager.ComputeP95()` 매 프레임 임시 배열 할당**
- **경로:** `Assets/_Project/Scripts/View/Effects/QualityManager.cs` L73 `var copy = new float[_ringFill];`
- **비용:** ring이 가득 차고 쿨다운 경과 시마다 **float[120] = 480B** 할당. 쿨다운 3초이므로 지속적은 아니지만, 강등 평가 타이밍에 확정 GC 발생.
- **수정:** 고정 `_sortScratch = new float[SampleWindow]` 필드로 선할당. ctor에서 1회만 `new`.

**④ `MatchDetector.FindMatches` 9x9+ 보드 확장 시 CPU 스파이크 예측**
- **경로:** `Assets/_Project/Scripts/Domain/Chain/MatchDetector.cs` L19-86
- **비용(6x7 현재):** H+V 이중 패스 O(rows*cols) = 84회 `BlockAt` + 색 비교. **~0.08ms @ A10**.
- **비용(9x9 Phase 5 확장):** 162회 × 2 패스 + 보드 캐시 미스 증가 → **0.25~0.4ms @ A10**. 저사양 Android에서는 Burst 없이는 5단 연쇄 한계.

**⑤ `UnityHapticService` Handheld.Vibrate() iOS/Android 일원화 — 저사양 배터리 드레인**
- **경로:** `Assets/_Project/Scripts/Services/Haptic/UnityHapticService.cs` L48-51
- **비용:** 세션 80회 상한은 있으나, 5단 연쇄에서 Mix1/Chain2 각 단계별 트리거 누적 시 실제 pulse 횟수가 단말 진동 코일 발열/배터리에 영향. iOS Core Haptics(Phase 5 예정)로 전환 시 정교한 패턴 + 전력 효율 개선 가능.

### 1.4 에셋 크기 예산 (현재 비어있음 — 미래 배정)

| 카테고리 | Phase 5 할당 | Phase 6 여유 | 비고 |
|---|---|---|---|
| 블록 아틀라스 (7색 × 상태 4종) | 4MB (1024² ASTC 6x6) | 6MB | 색약 모드 패턴 오버레이 포함 |
| UI 아틀라스 (HUD/팔레트/상점) | 3MB | 5MB | TMP SDF 아틀라스 별도 2MB |
| 파티클 텍스처 | 1.5MB | 2MB | 단일 sheet 512²×12프레임 |
| Metaball SDF 텍스처 | 2MB (고사양 전용) | 3MB | 중사양 이하는 sprite sheet 대체 |
| BGM (스테이지 1) | 2.5MB | 8MB (3스테이지) | Vorbis 96kbps 22kHz mono |
| SFX (30종) | 1.5MB | 4MB (60종) | 짧은 wav 메모리 상주 |
| **합계 (Phase 5)** | **14.5MB** | **28MB** | 앱 바이너리 + 에셋 총 ≤ 150MB 목표 |

**결정:** Addressables 마이그레이션 후 **스테이지별 번들 분리**, 초기 다운로드는 스테이지 1만 (앱 번들 < 80MB 유지).

---

## 2. 고도화 항목 (우선순위)

### P1. Burst + Job System 도입 — MatchDetector / ChainProcessor.ApplyGravity

**대상 경로:**
- `Assets/_Project/Scripts/Domain/Chain/MatchDetector.cs` (이중 pass → `IJobParallelFor` 열 단위 분할)
- `Assets/_Project/Scripts/Domain/Chain/ChainProcessor.cs` L237-272 `ApplyGravity` (열 독립 → 병렬화)

**수치 목표 (9x9 보드 기준, A10 급):**
| 항목 | Mono 현재 | Burst+Job 예상 | 감소율 |
|---|---|---|---|
| MatchDetector.FindMatches | 0.35ms | 0.08ms | **77%** |
| ApplyGravity | 0.22ms | 0.06ms | 73% |
| 합산 Logic 예산 (P0 §1.1 = 3.0ms) | 0.57ms | 0.14ms | Logic 여유 +14% |

**전환 조건:**
- 보드 크기 ≥ 8x8 **또는** 저사양 기기(p95 > 22ms 지속) 감지 시 자동 활성.
- Burst 컴파일러 의존성 추가 시 IL2CPP 빌드 시간 +60초 예상 → CI 별도 매트릭스.

**주의:**
- `Block` 클래스 참조를 Job에서 쓸 수 없음 → **`NativeArray<BlockSoA>`** 변환 레이어 필요. Phase 5-A에서 `Block` SoA 분해 선행 작업.
- `ColorMixCache._table` (65536 byte)은 `NativeArray<byte>` 변환 후 `[ReadOnly]` 전달.

### P2. 파티클 풀 사전할당 상세 수치

Phase 0 §2.1 "블록 풀 120 / 파티클 풀 20 / Trail 풀 8"의 **Quality 단계별 세분화**:

| Pool | Low (A10 이하) | Mid (A12 ~ A14) | High (A15+ / M-series) |
|---|---|---|---|
| BlockView pool | 120 | 150 | 180 |
| Explosion burst PS (재사용) | 1 (Emit(count)) | 2 (좌/우 스플릿) | 4 (방향별) |
| Infection spark pool | 8 | 16 | 24 |
| Trail renderer | **0 (비활성)** | 4 | 8 |
| Floating score text pool | 6 | 10 | 16 |
| Jelly deformer instances | 12 (단순 sin) | 24 | 42 (전체 보드) |
| Metaball quad pool | **0 (SpriteSheet)** | 8 (SDF) | 16 (full shader) |
| **총 파티클 하드캡** | **400** | **800** | **1500** |

**부팅 시 1회 사전할당:** `GameRoot.Awake`에서 위 테이블 참조해 풀 생성. 런타임 grow-on-demand는 **경고 로그** 강제 (phase1_performance_review.md §3.1 참조).

### P3. Addressables 도입 (Resources → 비동기 전환)

**현재 문제:**
- `UnityAudioService.cs` L51 `Resources.Load<AudioClip>("Audio/Bgm/" + trackId)` — **Resources 폴더 사용** (Phase 0 §5.1에서 "Resources 사용 → CI fail" 결정과 모순).
- 스테이지 로드 시 모든 Resources 에셋이 앱 번들에 포함되어 초기 다운로드 비대화.

**마이그레이션 경로:**
1. `Assets/_Project/Resources/Audio/Bgm/` → `Assets/_Project/Addressables/Audio/Bgm/` 이동 + Addressable 그룹 지정.
2. `IAudioService.PlayBgm(string trackId)` → `PlayBgmAsync(AssetReference clipRef, CancellationToken)` 로 변경. 로딩 중 fade-in 0.3초로 체감 커버.
3. 스테이지별 번들 분리: `stage_1`, `stage_2`, ... 각 번들에 BGM/스테이지 전용 SFX/아틀라스 포함.
4. CI에 `AddressableAssetSettingsDefaultObject` 존재 여부 + Resources 폴더 금지 검증.

**기대 효과:**
- 초기 앱 크기 -30~40MB (3스테이지 분량 디퍼럴)
- 스테이지 전환 시 비활성 아틀라스 언로드 → RSS 피크 감소

### P4. Audio voice 상한 + Mixer Ducking

**현재 문제:**
- `UnityAudioService.cs` L43 `_sfxSource.PlayOneShot(clip)` — 단일 AudioSource 재사용이지만, PlayOneShot은 내부적으로 voice pool을 사용해 **voice 상한이 AudioSettings.GetConfiguration().numRealVoices** (기본 32)에 의존.
- 5단 연쇄 시 Mix1(8회) + Chain2(5회) + Explosion(15회) ≈ **28 voice 동시 재생** 가능 → Phase 0 §1.1 "SFX 채널 Voice Limit 16" 초과.

**결정:**
- `AudioMixer` 의 `SfxGroup` 에 **MaxVoiceCount = 16** 명시. 초과 voice는 priority 기반 drop.
- **Ducking**: BGM 채널에 SideChainCompressor 삽입, SFX polyphonic 피크 시 BGM -6dB 자동 감쇠 (0.15s attack / 0.4s release).
- **콤보 SFX 병합**: Chain depth 3+ 시 개별 폭발 SFX → 단일 `combo_burst_3.wav` 로 교체. voice 15 → 1.

**구현 경로 (Phase 5-B):**
- `AudioLibrary.cs` 에 `SfxId.ComboBurst{3,5,7,10}` 추가
- `UnityAudioService.PlaySfx` 에 voice count 추적 + priority 파라미터 확장

### P5. UI Canvas 분리 (Static/Dynamic/Overlay) 실측

**Phase 0 §2.5 결정 실측:**

| Canvas | 포함 요소 | 리빌드 빈도 예상 | 조치 |
|---|---|---|---|
| StaticCanvas | 배경 프레임, 팔레트 슬롯 outline, 진행바 프레임 | 스테이지 전환 시만 | `RenderMode.ScreenSpaceOverlay` 분리 |
| DynamicCanvas | 점수, 콤보, 프롬프트 진행바 fill, 플로팅 숫자 | 턴당 3~5회 | 독립 Canvas — Static 재빌드 차단 |
| OverlayCanvas | 팝업, 일시정지, 튜토리얼 배너 | 세션당 수회 | 평상시 비활성 |

**실측 계획:**
- `UnityEngine.UI.CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild` 훅으로 Canvas별 rebuild 횟수 카운터.
- Deep Profile + `Profiler.enabled = true` 로 VertexHelper.FillMesh 비용 측정.
- **목표:** DynamicCanvas rebuild ≤ 8ms/턴, StaticCanvas rebuild 0회/턴 (스테이지 전환 제외).

### P6. 다운그레이드 Hysteresis (역업그레이드)

**현재 문제:**
- `QualityManager.cs` L59-66 단방향 강등만 구현. High → Mid → Low 로 내려가면 **복귀 불가**.
- 열 throttling이 해제된 뒤(폰이 식은 뒤)에도 계속 Low 유지 → Juice 감소 체감 지속.

**결정:**
- **역업그레이드 조건:** p95 < 22ms가 **연속 30초 지속** + Current != High → 1단계 승급.
- **Hysteresis gap:** 강등 임계 25ms, 승급 임계 22ms (3ms gap으로 oscillation 방지).
- **쿨다운:** 승급 후 60초 재평가 금지 (강등보다 긴 보수적 간격).

**구현 패치 (`QualityManager.cs` 확장):**
```csharp
private const float P95UpgradeThresholdMs = 22f;
private const float UpgradeCooldownSec = 60f;
private const float UpgradeDwellSec = 30f;
private float _upgradeCandidateSince = -1f;

// RecordFrame 내 추가:
if (p95 < P95UpgradeThresholdMs && Current != GraphicsQualityLevel.High) {
    if (_upgradeCandidateSince < 0) _upgradeCandidateSince = _timeSec();
    if (_timeSec() - _upgradeCandidateSince >= UpgradeDwellSec
        && _timeSec() - _lastDowngradeAt >= UpgradeCooldownSec) {
        var next = Current == GraphicsQualityLevel.Low ? GraphicsQualityLevel.Mid : GraphicsQualityLevel.High;
        Current = next; _lastDowngradeAt = _timeSec(); _upgradeCandidateSince = -1f;
        ClearRing(); OnLevelChanged?.Invoke(next);
    }
} else {
    _upgradeCandidateSince = -1f;
}
```

---

## 3. Game Feel — 햅틱/오디오/비주얼 LOD 에스컬레이션

### 3.1 연쇄 깊이별 피드백 강도 스케일 테이블 (depth 1~10)

**설계 원칙:** 깊이 1~3은 **선형 증가**(학습), 4~6은 **완만 증가**(절제), 7~10은 **완전 과잉 금지**(피로 방지). 10 초과는 `_onDepthExceeded` 콜백으로 경고 배너.

| Depth | 햅틱 스케일 | SFX volume dB | 화면 흔들림 (amp px) | 파티클 배율 | Time.timeScale |
|---|---|---|---|---|---|
| 1 | 0.55 (Mix1) | 0 dB | 0 | 1.0x | 1.0 |
| 2 | 0.80 (Chain2) | +0 dB | 2 | 1.2x | 1.0 |
| 3 | 0.85 | +1 dB | 4 | 1.5x | 0.95 (50ms) |
| 4 | 0.90 | +1 dB | 6 | 1.8x | 0.92 (80ms) |
| 5 | 0.95 | +2 dB | 9 | 2.2x | 0.88 (120ms) |
| 6 | 1.00 (cap) | +2 dB | 12 | 2.5x | 0.85 |
| 7 | 1.00 | +2 dB | 14 | 2.7x | 0.82 |
| 8 | 1.00 | +3 dB | 16 | 2.9x | 0.80 |
| 9 | 1.00 | +3 dB | 18 | 3.0x (cap) | 0.78 |
| 10 | 1.00 | +3 dB | 20 (cap) | 3.0x | 0.75 (200ms) |

**제약:** Phase 0 §2.1 "파티클 총량 하드캡 저사양 400" — depth 10 × 3.0x 배율 = 가상 피크가 캡 초과하면 **자동 50% 감축 후 스폰**.

### 3.2 Quality Level Low에서 Juice 축소 but 감정 유지 전략

**핵심:** Juice는 "입력 → 반응까지 인지된 시간과 임팩트 밀도"로 체감되므로, **비주얼을 줄이더라도 피드백 **0.1초 내** 트리거만 유지하면 감정은 보존**된다.

| 요소 | High | Mid | Low | 감정 보존 로직 |
|---|---|---|---|---|
| Metaball 셰이더 | Full (SDF + 굴절) | SDF only | SpriteSheet 12프레임 | 형태 인식 유지 |
| Jelly squash-stretch | 0.25 scale + 0.35 rebound | 0.18 + 0.25 | **0.12 + 단일 쉐이크** | 관성 느낌만 |
| 화면 흔들림 | 12~20px | 6~10px | **0~4px** | Time.timeScale로 대체 |
| 파티클 (depth 5) | 2.2x × 480 | 1.5x × 320 | **1.0x × 200** | 색·밝기 유지 |
| Trail | 8개 | 4개 | **0 (9-patch line)** | 선 궤적은 유지 |
| Time.timeScale 슬로우 | depth≥3부터 | depth≥4부터 | **depth≥5부터 (강화 사용)** | "Low일수록 슬로우로 커버" |
| Post-FX (bloom/vignette) | On | 일부 (bloom만) | **Off** | Chain2+SFX 강조로 보완 |
| Onboarding 배경 애니 | 전체 | 부분 | **정적 이미지 + subtle fade** | 첫인상 로드 타임 단축 |

**핵심 치트 코드:** Low에서 **Time.timeScale=0.75의 200ms 슬로우**는 CPU/GPU 비용은 오히려 감소(업데이트 수 감소)하지만 체감 임팩트는 High의 슬로우와 동등 이상. **저사양일수록 슬로우를 적극 사용**.

### 3.3 저사양 기기 햅틱 축소 시 오디오/비주얼 보상안

**배터리 온도 60°C 초과 또는 thermalState ≥ Serious 시 햅틱 50% 감축** 결정. 감축된 햅틱의 "빈자리"를 다음으로 보상:

| 원래 햅틱 | 보상 오디오 | 보상 비주얼 |
|---|---|---|
| BlockPickup (0.35) | `sfx_pickup_tap.wav` +2dB | 블록 scale 1.0 → 1.08 (100ms) |
| Mix1 (0.55) | `sfx_mix_swirl.wav` (새) | 색 링 팽창 이펙트 (128px) |
| Chain2 (0.80) | `sfx_chain_rise_02.wav` pitch +1 반음 | 플로팅 숫자 +크기 20% |
| StageSuccess (1.00) | `sfx_fanfare.wav` 0.5초 확장 | 풀스크린 fade-in white 150ms |
| Invalid (0.25) | `sfx_invalid_thud.wav` | 슬롯 red flash 80ms |

**결정:** 보상 오디오는 `AudioLibrary` 에 `SfxId.CompensationTier` enum 추가, `HapticProfile.Intensity == Off` 시 `UnityAudioService` 가 자동으로 보상 사운드 재생. 플레이어 설정(햅틱 Off)과 시스템 자동 감축(thermal) 양쪽 커버.

---

## 4. 측정 플랜

### 4.1 CI 회귀 시나리오 5종 (스크립팅 가능한 체인 시뮬레이션)

고정 seed로 재현 가능, `Unity.PerformanceTesting` API로 측정:

| # | 시나리오 | Fixed Seed | Expected 측정치 | Fail 기준 |
|---|---|---|---|---|
| S1 | **단일 3-match** (뒤섞기 없음) | 0xA1 | frame time ≤ 6ms, GC ≤ 200B | > 8ms OR > 500B |
| S2 | **3단 연쇄** (2-chain 유도) | 0xB7 | ProcessTurn ≤ 12ms, GC ≤ 800B | > 15ms OR > 1.2KB |
| S3 | **5단 연쇄** (고난도) | 0xC9 | ProcessTurn ≤ 22ms, GC ≤ 1.5KB, p95 frame ≤ 25ms | > 28ms OR > 2KB |
| S4 | **10단 연쇄 (MaxDepth)** | 0xD3 | exceeded=true, onDepthExceeded 호출, p95 ≤ 30ms | 크래시 OR p95 > 35ms |
| S5 | **풀 리필 (보드 전체 42 교체)** | 0xE5 | BlockSpawner 0B alloc (pool-backed), Refill ≤ 8ms | alloc > 100B OR > 12ms |

**실행 경로:**
```
Assets/Tests/Performance/ChainBenchmarks.cs
  → Measure.Method(() => processor.ProcessTurnAsync(...).Wait())
     .SetUp(() => BoardFactory.FromSeed(0xB7))
     .SampleGroup("GCAllocatedKB", SampleUnit.Kilobyte)
     .Run();
```

**CI 연동:** GameCI GitHub Action + Performance Testing Extension. PR 머지 차단: S3 기준 미달 시 fail. 결과는 artifact로 업로드하여 그래프 회귀 추적.

### 4.2 Firebase Performance trace 이름 표준화

| Trace 이름 | 커스텀 Attribute | 측정 지점 |
|---|---|---|
| `app_cold_start` | device_tier, os_version | Awake → 첫 playable frame |
| `stage_load_N` (N=1..M) | stage_id, quality_level | Addressables 로드 시작 ~ 첫 render |
| `match_resolve` | match_count, color_id | MatchDetector.FindMatches 호출 구간 |
| `chain_explosion` | depth, total_exploded, quality_level | ChainProcessor.ProcessTurnAsync 전체 |
| `board_rebuild` | rebuild_reason | BoardView.Bind 또는 full resync |
| `ui_canvas_rebuild` | canvas_name | CanvasUpdateRegistry 훅 |
| `audio_voice_peak` | voice_count, had_duck | Audio voice count > 12 시 |
| `quality_downgrade` | from_level, to_level, p95_ms | QualityManager.RecordFrame 강등 시 |
| `quality_upgrade` | from_level, to_level, dwell_sec | 신규 승급 경로 (§2.P6) |
| `thermal_event` | thermal_state | iOS NSProcessInfo thermalStateDidChange |

### 4.3 원격 KPI 대시보드 필드 리스트

**일 1회 샘플링 + 세션 종료 시 집계 전송:**
- `device_model`, `os_version`, `total_ram_mb`, `gpu_family`
- `avg_fps_session`, `p95_frame_ms_session`, `p99_frame_ms_session`
- `quality_level_start`, `quality_level_end`, `downgrade_count`, `upgrade_count`
- `gc_total_kb_session`, `managed_heap_peak_mb`
- `battery_pct_start`, `battery_pct_end`, `session_duration_min` → **배터리 드레인률 = (start-end)/min**
- `thermal_state_max_reached` (0/1/2/3 = Nominal/Fair/Serious/Critical)
- `chain_depth_max_reached`, `total_stages_cleared`
- `audio_voice_peak`, `haptic_trigger_count`
- `crash_count_session`, `asset_load_fail_count`

**KPI 목표:** 사용자 90%에서 `avg_fps_session ≥ 55`, `p95 ≤ 25ms`, 배터리 드레인 ≤ **15%/15분**.

---

## 5. 배터리 / 발열

### 5.1 30fps 배터리 세이버 모드 실측 가이드

**Phase 0 §1.1 결정 구체화:**

| 조건 | targetFrameRate | vSyncCount | 파티클 하드캡 | Juice depth 표 적용 |
|---|---|---|---|---|
| 기본 (60fps) | 60 | 0 | 1500/800/400 (Q별) | 전체 |
| 사용자 토글 세이버 | **30** | 2 | 400 (Q 무관) | depth 1~6까지만 (7+는 6 수준 유지) |
| thermal=Serious 자동 | 30 | 2 | 300 | depth 1~5까지만 |
| thermal=Critical | **24** (비상) | 2 | 200 | depth 1~3만 |

**실측 벤치 기기:** iPhone SE2(A13), Galaxy A52, Redmi Note 8 — 15분 연속 5단 연쇄 반복 플레이.

**배터리 드레인 목표:**
- 60fps 고사양: 12%/15분
- 60fps 저사양(Q=Low): 14%/15분
- 30fps 세이버: **8%/15분** (-40% 드레인)

### 5.2 iOS thermalState 대응 (Nominal/Fair/Serious/Critical)

`NSProcessInfo.thermalState` 변경 이벤트를 네이티브 플러그인으로 구독:

| thermalState | 상태 | 자동 조치 | 사용자 알림 |
|---|---|---|---|
| Nominal (0) | 정상 | 없음 | - |
| Fair (1) | 약간 따뜻 | QualityManager 강등 금지 억제 해제 | - |
| Serious (2) | 뜨거움 | 강제 `SetLevel(Mid if High)` + 30fps 전환 + 햅틱 50% 감축 | "배터리 보호 모드로 전환" 토스트 3초 |
| Critical (3) | 과열 | 강제 `SetLevel(Low)` + **24fps** + 햅틱/파티클 Off + BGM -6dB | "기기 온도가 높습니다. 잠시 후 재시작을 권장합니다." 모달 |

**복귀:** Fair 이하로 돌아오고 **2분 지속** 시 한 단계 회복 (§2.P6 hysteresis와 연동).

### 5.3 백그라운드/일시정지 진입 시 CPU 풀링 정리

**OnApplicationPause(true) / OnApplicationFocus(false) 진입 시:**

1. `Time.timeScale = 0` + `Application.targetFrameRate = 10` 강제. (Unity는 백그라운드에서도 일정 프레임 업데이트함)
2. `ChainProcessor` 진행 중 턴은 `CancellationToken` 발화 → 안전 중단. 보드 상태 스냅샷 저장.
3. 활성 `AudioSource.Pause()` 일괄. `_bgmSource`/`_sfxSource` 모두.
4. `ParticleSystem.Pause()` 일괄 (pool 내 시스템 전부).
5. `StartCoroutine` 으로 실행 중인 `BlockView` 애니 `StopAllCoroutines` + 포즈 상태 스냅샷.
6. Addressables 진행 중 로드는 `AsyncOperationHandle.WaitForCompletion` 없이 그대로 대기(백그라운드 IO 허용).
7. **OnApplicationPause(false)** 복귀: 스냅샷 복구 + `Time.timeScale=1` + 1프레임 warmup 후 `targetFrameRate` 원복.

**메모리 조치:**
- 백그라운드 3분 이상 지속 시 비활성 스테이지 아틀라스 `Addressables.Release` — iOS jetsam 회피 (Phase 0 §6 리스크 테이블).

---

## 6. Phase 5 로드맵 기여

### 6.1 Metaball 실제 셰이더 성능 예산 (Phase 4 스텁 상태 해소)

**현재:** `Assets/_Project/Scripts/View/Effects/MetaballRenderer.cs` 스텁.

**Phase 5 실셰이더 예산:**
- **픽셀 셰이더 제약:** 픽셀당 texture sample **≤ 4회** (Phase 0 §1.3). distance field lookup 2회 + alpha mask 1회 + noise 1회 = **4회 정확히 cap**.
- **화면 점유율:** Metaball 영역 **≤ 25%** (Phase 0 §2.2). ScriptableRenderFeature로 해당 쿼드만 별도 패스.
- **GPU 예산 (iPhone SE2 A13 기준):** Metaball 셰이더 총 **≤ 2ms/frame**. 초과 시 `QualityManager` 가 SpriteSheet fallback 강제.
- **해상도 스케일 적응:** p95 GPU time > 8ms 시 RenderScale 1.0 → 0.85 → 0.75. 해상도 축소는 Metaball이 가장 큰 수혜.

**실증 계획:**
- A10/A13/A15 3단 기기에서 `FrameTimingManager` 로 GPU time 측정. 중사양(A13) 2ms 달성 시 Mid 레벨 허용.

### 6.2 사운드 에셋 추가 시 메모리 재분배

**Phase 0 §1.2 오디오 캡 25MB. Phase 5 SFX 30종 → 60종 확장 시 재분배:**

| 카테고리 | Phase 1 | Phase 5 | Phase 5+ 기타 |
|---|---|---|---|
| BGM streaming (단일) | 1MB | 1MB | 동시 재생 없음 (ducking) |
| SFX short (<0.5s, 상주) | 3MB | **8MB** | voice 16 상한 |
| SFX long (≥0.5s, 스트리밍) | 0 | 3MB | Stage success 등 |
| UI sfx (상주) | 1MB | 2MB | |
| Voice-over (선택) | 0 | 6MB | Addressables 지연 로드 |
| 총 | 5MB | **20MB** | 5MB 여유 |

**결정:** Voice-over (튜토리얼 내레이션 등)는 **Addressables 별도 그룹** + 다운로드 선택 옵션. 기본 번들에 포함 금지.

### 6.3 스테이지 2+ (더 큰 보드 9x9) 확장 시 Job System 전환점

**트리거 조건 (자동 전환):**
- `Board.Rows * Board.Cols > 56` (현재 42의 1.33배 이상)
- **또는** 저사양 기기에서 p95 > 25ms 지속

**전환 작업 순서 (Phase 5-A → 5-B):**

1. **Phase 5-A (블로커):** `Block` 클래스 → `struct BlockData` + `Block[]` 중앙 풀 로 분리 (hot/cold separation). FSM 상태·색·id만 struct에 유지, 시각 참조는 `BlockView` 가 소유.
2. **Phase 5-B:** `MatchDetector.FindMatchesJob : IJobParallelFor` 구현. 열 단위 분할, `NativeArray<BlockData>` 입력.
3. **Phase 5-C:** `ChainProcessor.ApplyGravity` 의 열 루프 → `GravityJob`. 의존성: MatchDetector 완료 후 순차 schedule.
4. **Phase 5-D:** `ColorMixCache._table` → `NativeArray<byte>` + `[ReadOnly]` Job 전달. Burst compile 활성.

**측정 Gate:**
- 전환 후 9x9 보드에서도 5단 연쇄 ProcessTurn ≤ 22ms (현재 6x7 기준 동일 예산).
- 저사양 기기(A10)에서 Burst 비활성 경로도 Mono로 여전히 작동 (fallback 보장).

---

## 7. v2 구현 체크리스트

- [ ] Critical-1: `BoardView.OnAnimStep` delegate 캐싱 (1줄)
- [ ] Critical-2: `PooledBlockSpawner` 구현 + `IBlockSpawner.Recycle` 추가
- [ ] Critical-3: `MatchGroup.EnsureBuffers` pre-warm (이미 `CreatePooled()` 로 반영됨 — 재확인)
- [ ] `QualityManager.ComputeP95` 임시 배열 → 필드 재사용 (§1.3 ③)
- [ ] `QualityManager` 역업그레이드 hysteresis (§2.P6)
- [ ] `IChainAnimationHub` ValueTask 전환 (§1.3 ②)
- [ ] Addressables 마이그레이션 + Resources 폴더 CI 검증 (§2.P3)
- [ ] AudioMixer voice 상한 16 + Ducking (§2.P4)
- [ ] Canvas 3분할 + rebuild 카운터 (§2.P5)
- [ ] CI 성능 회귀 S1~S5 (§4.1)
- [ ] Firebase trace 표준화 (§4.2)
- [ ] thermalState 네이티브 플러그인 + 자동 모드 전환 (§5.2)
- [ ] Phase 5-A Block SoA 분리 (§6.3) — Job System 블로커 해소

---

## 부록 A: 계측 툴 요약

| 도구 | 용도 | 사용 시점 |
|---|---|---|
| Unity Profiler (Deep Profile) | 함수별 정밀 측정 | 주 1회 |
| Unity Memory Profiler | heap 스냅샷 diff | PR 머지 전 |
| Unity Frame Debugger | Draw Call 분석 | Canvas 변경 시 |
| Unity Test Framework + PerformanceTesting | CI 회귀 | PR마다 자동 |
| Xcode Instruments (Time Profiler, Energy) | iOS 네이티브 분석 | 릴리스 전 |
| Android GPU Inspector | Adreno/Mali 분석 | 저사양 검증 |
| Firebase Performance | 프로덕션 원격 | 상시 |
| Sentry | 크래시·성능 이벤트 | 상시 |

## 부록 B: 본 문서 밖 후속 과제

- Phase 5에서 **햅틱 네이티브 플러그인**(iOS Core Haptics AHAP, Android VibrationEffect composition)으로 `UnityHapticService` 교체. `HapticProfile` 의 `_basicScale[7]` 값이 AHAP 패턴 id와 매핑되는 새 ProfileV2 필요.
- Phase 6에서 **멀티플레이/리플레이** 도입 시 결정론적 시뮬레이션 요구 → 현재 `DeterministicBlockSpawner` 의 seed 전략이 Job System 전환 후에도 유지되는지 재검증.
- 색약 모드 패턴 오버레이가 **블록 아틀라스 2배 증가**를 유발할 경우, 텍스처 압축 포맷 재협상 (ASTC 6x6 → 8x8 with pattern fallback).
