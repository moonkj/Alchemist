# Color Mix: Alchemist — Juice v3 리뷰 (Game Feel Engineer)

> 작성자: Game Feel Engineer Teammate
> 작성일: 2026-04-22
> 대상: `Assets/_Project/Scripts/Bootstrap/MinimalGameScene.cs` (현재 플레이 빌드)
> 근거 스펙: `docs/motion_design.md`, `docs/performance_v2.md` §3 GameFeel LOD
> 유저 피드백: "벽돌같이 딱딱", "물감 섞이는 효과 부족", "옮길때마다 진동 과함"(이미 완화)

---

## 1. 5단계 체감 평가 (A/B/C/D)

| 단계 | 체감 등급 | 근거 (현재 구현 요약) | 주요 결함 |
|---|:---:|---|---|
| **Mix** (혼합 성공) | **B** | squash 3-stage(0.7→1.45→1.0) 0.45s + EaseInOutCubic 색 보간 + 양색 파편 10×2 + ScreenShake 0.12s/0.05 + Mix SFX | Metaball 블렌드 부재(스펙 §4 전체 미구현). 두 원본 블록이 "중점으로 수렴"하지 않고 한쪽만 사라짐. 플래시 알파 레이어 없음 |
| **Explode** (매치 해소) | **C+** | 1.55× 확대 + 알파 페이드(u²) 0.35s + 스플래시 14개 + 블록수 기반 쉐이크(0.05+cnt×0.012, cap 0.22) + Handheld.Vibrate 1회 | 깊이별 에스컬레이션 없음(스펙 §2 레벨1~5 모두 동일 수치). Black(오염) vs 일반 차별화 0. 파티클 링 구조 없이 난수 분사뿐 |
| **Chain** (연쇄) | **D** | depth≥1 공통 경로, 점수만 +50×(depth-1) 가산, 토스트 텍스트만 변함 | **치명:** timeScale 슬로우모 없음, 햅틱 단계 구분 없음, SFX 피치/레이어 고정, 비주얼 링/프리즘 없음. 스펙 §2 과 가장 멀리 떨어진 영역 |
| **Clear** (스테이지 클리어) | **C-** | SFX(아르페지오) + "STAGE CLEAR" 토스트 + 0.8s 후 Result 전환 + 별점 문자열 표시 | Result 오버레이 즉시 표시(페이드 없음), 별 순차 점등 없음, 점수 count-up 없음, StageSuccess 햅틱 없음, 풀스크린 fanfare 플래시 없음 |
| **Fail** (실패) | **D+** | Fail 아르페지오 SFX + "STAGE FAILED" 토스트 + 0.6s 후 Result | 낙담 모션(블록 색빠짐/sag) 전무, 진동(미세) 없음, 보드 grayscale 전환 없음, 전환 연출 부재 |

**종합:** 평균 C~C+. Mix/Explode는 "시늉은 난다" 수준, Chain/Clear/Fail은 "기능은 돌지만 체감 비어있음".

---

## 2. 10개 체크리스트 결과

| # | 항목 | 현재 | 스펙 대비 | 판정 | 핵심 갭 |
|---|---|---|---|:---:|---|
| 1 | Block Idle 브리딩 | sin(t×2.2+phase)×0.028 XY | §1 row2 `0.96↔1.00, 2.4s linear` | **B** | 진폭은 적절, 위상은 랜덤하나 주기(2.2rad/s≈2.85s)가 스펙(2.4s)과 12% 오차, Y 별도 stretch(2.4+1.1) 덕분에 동기화는 회피됨. 알파 brid 없음(스케일만) |
| 2 | Drag 입력 반응 | 1.15× 스케일만, 즉시 손가락 추적, stretch/trail/rotate 없음 | §3 jelly_drag 수식, §1 row4~5 lift/shadow/sway | **D** | 방향 stretch 수식(`scale.x=1+c×|v.x|`) 부재, 트레일 없음, rotation 없음, lift/shadow 없음 |
| 3 | Mix Bounce | 0.7→1.45→1.0 / Y반대 / 0.45s / Lerp 색보간 | §1 MergeResult `0→1.2→1.0 spring_soft 350ms + flash` + §4 Metaball 650ms | **C+** | 바운스 타이밍/강도는 양호. 단 Metaball 완전 부재, 플래시(#F3FAF2 0.9α→0 linear) 누락, 두 블록 수렴 연출 없음 |
| 4 | Explosion | 1.55×+알파 페이드 0.35s / 스플래시 14 / 쉐이크 cnt기반 | §2 level_1~5 에스컬레이션, §1 row10 `1.0→1.3→0 300ms + 12 burst` | **C** | 깊이별 분기 0, Black 폭발 차별화 0, 파티클 링(2nd ring/prism trail) 없음, rotational shake 없음 |
| 5 | Gravity 낙하 | position warp → `Lerp(pos, base, dt×12)` 연속 감쇠 | §1 Spawn 240ms spring_soft, 일반적 ease-out-bounce 관례 | **C-** | Ease-Out-Bounce 없음, 스태거링(열별 지연 20~40ms) 없음, 낙하 후 미세 squash(Y 0.92→1.0) 없음 |
| 6 | Refill 등장 | 위 r+1칸 spawnY → 공통 Lerp, 초기 scale 0.9×CellSize | §1 Spawn `scale 0→1.05→1.0 spring 240ms` | **D+** | 0.9→1.0 선형 수렴은 스프링/오버슈트 없음. 알파 페이드인 부재, 행별 staggering 부재 |
| 7 | 결과 오버레이 | IMGUI 즉시 `_overlayTex` 전면 도포 | §5.1 `board scale 1.0→0.7 + fade + card slide-up 500ms` | **D** | 페이드인 없음, 카드 slide-up 없음, 별점 순차 점등 없음, 점수 count-up 없음 |
| 8 | Stage 전환 | Lobby↔Playing↔Result 즉시 `_screen =` 대입 | §5.1 `붓 쓸기 셔터 600ms ease_in_out_cubic + swipe SFX` | **D** | 커튼/셔터/페이드 0. `SetBoardVisible(on)` 토글이 전부 |
| 9 | 햅틱 매트릭스 | 폭발 1건 `Handheld.Vibrate()` 무차별 | §9 7 이벤트 × 강도/패턴 구분 | **D-** | BlockPickup/Hover/Mix1/Chain2/StageSuccess/Invalid/TurnsLow 전부 미구현. 감도 구분 없음 |
| 10 | 사운드 레이어링 | `PlayOneShot(clip, 0.6)` 단일 소스, Chain depth 무관 고정 피치/볼륨 | §2 audio_layers(MixPlop+ChainChord+ExplodeWhoosh+StageFanfare 누적), §9 오프셋 | **D** | 피치 업(반음씩) 없음, 레이어 누적 없음, Ducking 없음, voice 상한 없음 |

---

## 3. 개선 TOP 8 (우선순위 정렬, 구체 수치 포함)

### ★ P0 — 즉시 체감 전환

**① Chain Depth 에스컬레이션 테이블 도입** — `ResolveCascadeCoroutine` 내부에 depth 분기
- 쉐이크 magnitude: `0.05 + depth × 0.035` (cap 0.22, rotational 옵션)
- `Time.timeScale`: depth 2 → 0.95(50ms), 3 → 0.92(80ms), 4 → 0.88(120ms), 5+ → 0.85(150ms) → 1.0 ease_out_quart 복귀
- 파티클 count: `14 × (1 + 0.25 × (depth-1))` cap 48
- SFX 피치: `AudioSource.pitch = 1.0 + (depth-1) × 0.06` (5단에서 +15반음 상당 체감 상승)
- Chain depth 3+ 시 풀스크린 radial flash(#FFFFFF 0.5α, 180ms)

**② Mix Metaball 간이 구현 (Low-fallback)** — 셰이더 없이 2-sprite cross-fade로 대체
- `TryMix` → Merging 200ms 동안 `_blocks[sr,sc]`을 `(sr+tr)/2, (sc+tc)/2` 중점으로 `ease_in_out_cubic` 이동 + scale 1→0.85
- 중점 도달 시 두 색 원형 sprite 2장을 0.35s 동안 radius 0.5→0.9 팽창 후 교체 cross-fade, 최종 블록 pop(spring 0→1.2→1.0)
- 총 예산 420ms (motion_design §4 Low 경로)

**③ 햅틱 7-이벤트 분리 (iOS/Android 네이티브 플러그인 전까지 스케일링)**
- `Handheld.Vibrate()` 대체: 이벤트별 `_hapticScale` 테이블 적용 후 Android는 `AndroidJavaClass("android.os.VibrationEffect")` 로 amplitude + duration 구분
- Pickup 0.35×30ms / Mix1 0.55×40ms / Chain2 depth×(0.80+0.04n)×(60+20n)ms / StageSuccess 1.00×220ms / Invalid 0.25×50ms
- `performance_v2.md §3.1` 깊이 테이블 준수 (cap 1.00 @ depth 6)

### ★ P1 — 학습된 기대치 충족

**④ Result 오버레이 시퀀스** — `DrawResult` 앞단에 float `_resultEnterT` 추가
- 보드 전체 scale 1.0 → 0.7 + alpha 1 → 0.4 (500ms ease_out_quart)
- `_overlayTex` 알파 0 → 0.82 lerp (500ms)
- "STAGE CLEAR" 라벨 slide-up from +40px + fade (300ms ease_out_quart, 200ms 지연)
- 별점 순차 점등: 별 i 점등 시각 = 0.6s + i × 0.25s, 각 별 scale 0 → 1.3 → 1.0 spring_soft (350ms) + 개별 햅틱 `Mix1`
- 점수 count-up: 0 → `_score` over 0.9s (`Mathf.Lerp` + 정수 반올림 표시), 틱당 짧은 Tick SFX (piano note 6k)

**⑤ Refill/Gravity 스태거링 + 바운스**
- Gravity: 열 c의 낙하 시작 지연 `= (c % 3) × 20ms`, Refill 행 r 지연 `= r × 15ms`
- 착지 직후 Y-squash 0.82, 120ms ease_out_bounce로 1.0 복귀 (motion_design §1 Spawn spring_soft 표준 준용)
- 현재 `Lerp(dt×12)` → `ease_out_bounce` 커스텀 함수로 교체 (0.32s, overshoot 1.08)
- Refill 진입 시 `scale 0 → 1.1 → 1.0` 스프링 (280ms), 알파 0 → 1 (180ms)

**⑥ Black(오염) 폭발 차별화**
- 일반 폭발: 1.55× + 컬러 스플래시 + Mid-high SFX
- Black 폭발: 0.6× 수축 → 1.8× 팽창 2단(0.18s+0.22s), 어두운 smoke 파티클 20개 (중력 -0.3, alpha 0.7) + Low-pass 럼블 SFX, 쉐이크 +30%
- 스코어 패널티 `-20`도 floating text로 빨간색 표시

### ★ P2 — 장기 완성도

**⑦ Stage 전환 붓 쓸기 셔터** (Lobby→Playing, Playing→Result)
- 스크린 절반 폭의 neutral.900 rect 2개를 좌/우에서 중앙으로 슬라이드 (300ms ease_in_out_cubic) → 만난 순간 씬 전환 → 동일 rect 반대 방향 회수 (300ms)
- 총 600ms, 교차 시점에 `_screen =` 갱신
- Selection 햅틱 + `swipe` SFX (90Hz descending sweep 80ms)

**⑧ Drag 방향 stretch + rotation**
- `OnDragMove` 시 `Vector2 v = (world - lastWorld) / dt` 계산, EMA 스무딩(α=0.25)
- `scale.x = clamp(1.0 + 0.35 × |v.x| / 8, 0.82, 1.22)`, `scale.y` 부피 보존
- `rotation = atan2(v.y, v.x) × Mathf.Rad2Deg × 0.15` (최대 ±9°)
- 멈춤 0.2s 이상 → 1.08로 수렴 (exit easing)

---

## 4. 연쇄 깊이별 Juice 에스컬레이션 표 (depth 1~10)

> `performance_v2.md §3.1` 표를 MinimalGameScene 구현 단위로 재작성. cap 준수.

| Depth | ScreenShake mag (dur) | Shake Rot | Time.timeScale (window) | 파티클 count/cell | SFX pitch | SFX layer | Haptic (scale × ms) | Full-screen Flash |
|:---:|---|:---:|---|:---:|:---:|---|---|---|
| 1 | 0.05 (0.30s) | — | 1.00 | 14 | 1.00 | MixPlop | Mix1 (0.55 × 40) | — |
| 2 | 0.08 (0.30s) | — | 0.95 (50ms→1.0/250) | 18 | 1.06 | + ChainChord(+100ms) | Chain2 (0.80 × 80) | vignette 0.3α 150ms |
| 3 | 0.11 (0.35s) | ±0.5° | 0.92 (80ms→1.0/350) | 22 | 1.12 | + ExplodeWhoosh(-80ms) | Chain2 (0.85 × 100) | radial #FFF 0.5α 180ms |
| 4 | 0.14 (0.40s) | ±0.8° | 0.88 (120ms→1.0/450) | 26 | 1.18 | ChainChord×2(+5th) | Chain2 (0.90 × 120) | prism bloom 2px 경계 |
| 5 | 0.17 (0.50s) | ±1.2° | 0.85 (150ms→1.0/550) | 32 | 1.24 | + StageFanfare bed | Chain2 (0.95 × 140) | #FFE27A 0.7α 280ms + zoom 1.05 |
| 6 | 0.20 (0.55s) | ±1.5° | 0.85 | 38 | 1.30 | full orchestration | Chain2 (1.00 × 160) cap | EPIC flash + "대단해요!" label |
| 7 | 0.21 (0.60s) cap | ±1.7° | 0.82 | 42 | 1.35 | + duckLow BGM -6dB | Chain2 × 2 (80ms 간격) | label "경이로움" |
| 8 | 0.22 cap (0.65s) | ±1.9° | 0.80 | 46 | 1.40 | 동일 | Chain2 × 3 | + 카메라 zoom-punch 1.08 |
| 9 | 0.22 cap (0.70s) | ±2.0° | 0.78 | 48 cap | 1.45 | 동일 | Chain2 × 3 + StageSuccess pre-roll | 풀스크린 prism arc |
| 10 | 0.22 cap (0.75s) | ±2.0° cap | 0.75 (200ms→1.0/850) | 48 cap | 1.50 cap | 최대 | Chain2 × 3 (80ms) + StageSuccess 종결 | #FFFFFF 0.9α 320ms + slow-zoom |

**Low 사양 (`QualityManager.Low`) 보정:**
- 쉐이크 `mag × 0.6`, rotational 0
- 파티클 count × 0.5 (cap 24/cell)
- 풀스크린 flash는 유지(GPU 저렴), bloom/vignette는 off
- Time.timeScale 슬로우는 **depth ≥ 5부터만** 사용하되 강도는 동등 (`performance_v2.md §3.2` 치트 코드: "Low일수록 슬로우를 적극 사용")

**구현 힌트:**
- `ResolveCascadeCoroutine`의 `depth++` 직후 위 테이블 lookup → `TriggerExplosionFeedback(cells, depth)` 에 전달
- 테이블은 `static readonly float[]` 9개 필드로 가지고, index = `Mathf.Clamp(depth-1, 0, 9)`

---

## 5. 요약 로드맵

| Phase | 기간 | 핵심 | 예상 체감 급등 |
|---|---|---|---|
| **v3.1 (1일)** | 즉시 | TOP ①②③ (Chain escalation + Metaball cross-fade + Haptic 분리) | Chain D → B, Mix B → A- |
| **v3.2 (1일)** | 직후 | TOP ④⑤⑥ (Result 시퀀스 + Gravity 스태거 + Black 차별화) | Clear D → B+, Explode C → B |
| **v3.3 (2일)** | 차주 | TOP ⑦⑧ (씬 전환 셔터 + Drag stretch) | 전체 평균 C+ → B+ |

완전한 motion_design.md 준수는 Metaball 실셰이더(Phase 5 GPU budget) 도입 전엔 `Low/Mid-fallback` 경로만 달성 가능. 그러나 v3.1~v3.3 만으로도 "벽돌같다"는 피드백은 제거되고, Chain 만족감이 핵심 플레이 루프를 재정의할 것으로 예상.

---

문서 끝.
