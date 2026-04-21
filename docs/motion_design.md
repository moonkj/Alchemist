# Color Mix: Alchemist — Motion & Juice Specification

> 작성자: UX/UI Senior Designer Teammate
> 작성일: 2026-04-21
> 범위: 블록 상태 애니 / 연쇄 Juice 에스컬레이션 / Drag 반응 / Metaball 블렌딩 / UI 전환 / 프리뷰 / 프롬프트 / 시네마틱 / 햅틱-오디오-비주얼 동기화 매트릭스
> 참조: `design_system.md` §6 (Easing / Duration), `HapticEvent.cs`, `SfxId.cs`

---

## 1. 블록 상태별 애니메이션 표 (11 BlockState × Enter/Idle/Exit)

Duration 은 `design_system.md` §6.2 토큰 참조. Easing 동일.

| # | BlockState | Enter (duration / easing) | Idle (loop) | Exit (duration / easing) |
|---|---|---|---|---|
| 1 | `Spawn` | 240ms / spring_soft (scale 0→1.05→1.0) | — | — |
| 2 | `Idle` | — | 알파 브리드 0.96↔1.00, 2.4s, linear | — |
| 3 | `Hover` (hover by drag) | 150ms / ease_out_quart (scale 1.0→1.08) | 외곽 glow 2px pulse 0.8s | 120ms / ease_in_quad (scale 1.08→1.0) |
| 4 | `Picked` (드래그 시작) | 80ms / ease_out_quart (lift +4px, scale 1.12) | shadow elevation 3 유지, 약한 sway ±2deg, 1.6s | 120ms / ease_in_quad (원위치 복귀) |
| 5 | `DragGhost` (손가락 추적) | 즉시 | 손가락 오프셋 lerp 0.18, trail 파티클 3개/프레임 | 80ms / ease_in_quad |
| 6 | `PreviewTarget` (drop 후보) | 150ms / ease_out_quart (scale 1.0→1.06, tint +8% luminance) | 0.6s pulse loop | 150ms / ease_in_quad |
| 7 | `Merging` (혼합 직전) | 200ms / ease_in_out_cubic (두 블록 중점으로 수렴, 각각 scale 1→0.85) | Metaball 블렌드 활성 | — (MergeResult 로 이행) |
| 8 | `MergeResult` (2차색 등장) | 350ms / spring_soft (scale 0→1.2→1.0, flash #F3FAF2 알파 0.9→0 linear) | Idle 상태로 전환 | — |
| 9 | `ChainTrigger` (연쇄) | 400ms / spring_hard (scale 1.0→1.35→1.0, rotate ±8deg 감쇠) | — | — |
| 10 | `Match` (해소) | 300ms / ease_in_quad (scale 1.0→1.3→0, alpha 1→0, 파티클 12 burst) | — | — |
| 11 | `Locked` (이동 불가) | 200ms / ease_out_quart (saturate 1.0→0.4) | neutral.300 tint, 10s마다 1회 0.5s shake | 200ms / ease_out_quart (saturate 복귀) |

---

## 2. 연쇄 깊이별 Juice 에스컬레이션 (5단계)

연쇄 깊이 n 에 따라 **카메라 쉐이크 / 타임스케일 / 파티클 / 햅틱 / 사운드 레이어** 가 누적 강화.

```yaml
chain_escalation:
  level_1:    # n=1 — 단순 1차 혼합 성공 (빨+파=보라)
    camera_shake:   "amp 2px, 0.12s, decay 0.6"
    time_scale:     "1.0 (no slow-mo)"
    particles:      8 burst, radius 24px, gravity 0
    haptic:         "Mix1 (Medium Impact)"
    audio_layers:   ["MixPlop"]
    visual_extra:   "blockFlash #F3FAF2 0.9a → 0, 200ms"

  level_2:    # n=2 — 2차 연쇄 (보라+노랑=검정)
    camera_shake:   "amp 4px, 0.22s, decay 0.7"
    time_scale:     "0.7 slow-mo 0.15s → 1.0 over 0.25s"
    particles:      24 burst + 2nd ring 8, gravity -20
    haptic:         "Chain2 (Heavy Impact + rise)"
    audio_layers:   ["MixPlop", "ChainChord (CMaj7, +100ms delay)"]
    visual_extra:   "board-wide vignette 0→0.3α 150ms, label '연쇄!' (pop 250ms)"

  level_3:    # n=3 — 3차 연쇄 (검정/흰 대폭발)
    camera_shake:   "amp 6px, 0.35s, decay 0.65, rotational ±0.6deg"
    time_scale:     "0.55 slow-mo 0.25s → 1.0 over 0.35s"
    particles:      48 burst + prism trail 16, gravity -40, color-shift
    haptic:         "Chain2 (Heavy) + TurnsLow pulse 보강"
    audio_layers:   ["MixPlop", "ChainChord", "ExplodeWhoosh (-80ms)"]
    visual_extra:   "풀스크린 radial flash #FFFFFF 0.5α 180ms, Metaball threshold 0.85→0.3 펄스"

  level_4:    # n=4 — 대연쇄
    camera_shake:   "amp 9px, 0.50s, decay 0.55, rotational ±1.2deg"
    time_scale:     "0.45 slow-mo 0.40s → 1.0 over 0.55s"
    particles:      80 burst + 3 rings (48/24/16), prism hue 360 rotate
    haptic:         "Chain2 + StageSuccess (중첩 간격 120ms)"
    audio_layers:   ["ChainChord (x2 stacked +5th)", "ExplodeWhoosh", "StageFanfare bed (pre-roll)"]
    visual_extra:   "화면 경계 색 프리즘 bloom 2px, 보드 스케일 1.0→1.03 ease_out"

  level_5:    # n=5+ — 전설적 연쇄 (EPIC)
    camera_shake:   "amp 12px, 0.75s, decay 0.5, rotational ±2.0deg, zoom in 1.05"
    time_scale:     "0.30 slow-mo 0.60s → 1.0 over 0.85s"
    particles:      120+ (Low 품질: 60 캡), multi-layered, confetti, prism arcs
    haptic:         "Chain2 (Heavy) × 3회 (80ms 간격) + StageSuccess 종결"
    audio_layers:   ["ChainChord full orchestration", "StageFanfare", "ExplodeWhoosh duckLow"]
    visual_extra:   "풀스크린 #FFE27A flash 0.7α 280ms, 카메라 zoom-punch, '놀라워요!' 라벨 epic 1200ms"
```

> 저사양 기기 (`GraphicsQualityLevel.Low`): 파티클 40% 캡, 카메라 쉐이크 amp × 0.6, 풀스크린 flash 유지 (GPU 가벼움), Metaball blend 생략.

---

## 3. Drag 중 블록 반응 — Jelly Stretch 수식

기존 `JellyDeformer.cs` spring-damper 를 드래그 벡터로 구동.

### 3.1 물리 파라미터
```yaml
jelly_drag:
  stiffness_k:       180   # spring constant (JellyDeformer 기본값)
  damping_c:         16
  max_impulse:       0.5
  drag_coupling:     0.35  # 손가락 속도(px/s) → impulse 계수

stretch_formula:
  # 손가락 속도 벡터 v (normalized world units/s)
  # scale.x = 1.0 + coupling * |v.x|
  # scale.y = 1.0 - coupling * |v.x| * 0.6   # 부피 보존 흉내
  # clamp: [0.82, 1.22]
  axis_follow:
    rotation_deg: "atan2(v.y, v.x) * 0.15"   # 최대 ±9deg
    smoothing:    "lerp 0.25 per frame"
```

### 3.2 상태 전이
- 픽업 직후 80ms: scale 1.0 → 1.12 → 1.08 (overshoot).
- 드래그 중: 위 수식 실시간 적용, 손가락 멈춤 0.2s 이상 → 1.08로 수렴.
- Drop 유효: spring_soft 240ms 로 1.0 복귀.
- Drop 무효: `Invalid` 햅틱 + jelly squish (y축 0.82로 압축 120ms 후 복귀).

---

## 4. Metaball 블렌딩 타이밍

2차 색 생성 순간 두 원색 블록을 metaball shader 로 물리적으로 "녹여" 합침.

```yaml
metaball_sequence:
  T-0.00s: "Merging 상태 진입. 두 블록 Transform.position lerp (중점)"
  T-0.10s: "MetaballRenderer.SetRadius(0.5 → 0.9) — 두 원이 커져 겹침 시작"
  T-0.20s: "_MetaThreshold 0.6 → 0.3 (블렌드 증가)"
  T-0.35s: "블렌드 피크 — 두 색 sprite tint 각각 50% blend, 형태 유기적"
  T-0.50s: "snap — 두 블록 destroy, 결과색 블록 1개 Instantiate at midpoint, scale 0"
  T-0.50~0.65s: "MergeResult 애니 (spring_soft, 350ms), flash #F3FAF2"

total_budget: "650ms (0.35s 블렌드 + 0.15s snap + 0.15s pop)"
quality_fallback:
  Low: "blend 제거, 단순 cross-fade 180ms + pop 240ms = 420ms"
  Mid: "blend 유지, threshold 고정 0.85 (단순 cutoff)"
  High: "위 전체 시퀀스"
```

---

## 5. UI 전환 Motion 카탈로그

### 5.1 화면 간 전환
| From → To | 기법 | duration | easing | 추가 |
|---|---|---|---|---|
| Splash → Lobby | 흑백 → 컬러 번짐 radial wipe | 1500ms | ease_in_out_cubic | BGM fade-in 800ms |
| Lobby → Stage Select | 수평 슬라이드 + 카드 zoom 1.0→1.1 후 fade | 400ms | ease_out_quart | parallax 배경 -20% |
| Stage Select → Gameplay | 화면 중앙으로 수렴하는 "붓 쓸기" 셔터 | 600ms | ease_in_out_cubic | SFX "swipe", 햅틱 Selection |
| Gameplay → Result | 보드 전체 scale 1.0→0.7 + fade, result 카드 slide-up from bottom | 500ms | ease_out_quart | 햅틱 StageSuccess |
| Result → Gallery (복원 시네마틱) | §7 참조 | 1200ms | — | — |
| 모든 뒤로가기 | 반대방향 반복, duration × 0.85 | — | — | — |

### 5.2 모달
- Enter: overlay fade-in (200ms) + card slide-up 24px & scale 0.95→1.0 (base 240ms, spring_soft).
- Exit: card slide-down & scale 1.0→0.97 (fast 150ms, ease_in_quad) + overlay fade-out (150ms).
- Backdrop tap → 동일 exit.

### 5.3 토스트
- Enter: bottom 에서 slide-up 12px + fade (fast 150ms, ease_out_quart).
- 체류: 기본 2400ms, 중요도 높으면 3600ms.
- Exit: fade-out only (fast 150ms).
- 연속 토스트: 큐잉, 간격 200ms.

### 5.4 스켈레톤 로더
- 배경 neutral.100 (다크: neutral.700).
- 하이라이트 sweep: linear-gradient 90deg transparent → neutral.50 (30%) → transparent, width 120px.
- 이동: -100% → 100% over 1200ms, linear, loop.
- 대상: 갤러리 카드, 랭킹 리스트 행, 상점 상품 카드.

---

## 6. 미리보기 토스트 (Drag 중 실시간 프리뷰)

### 6.1 반응 속도 수치
```yaml
preview_toast:
  trigger:         "드래그 블록 hover on 후보 셀 진입 시"
  enter_anim:      "150ms fade-in + slide-up 8px, ease_out_quart"
  update_debounce: "80ms — hover 셀 변경 시 내부 텍스트/아이콘 cross-fade 120ms"
  exit_anim:       "120ms fade-out, ease_in_quad (드래그 종료 or hover 이탈 200ms 경과)"

  layout:
    height:        32px
    position:      "bottom safe + 8px"
    padding:       "6px 12px"
    typography:    body_sm (14/20, 500)
    background:    "neutral.800 alpha 0.90, radius md"
    content:       "🔴 + 🔵 = 🟣 보라  (아이콘 16px, 라벨 14px)"

  performance:
    recompute:     "hover 셀 변경 이벤트 시에만 (프레임마다 X)"
    text_pool:     "StringBuilder 재사용, TMP SetText(Span) 호출"
```

### 6.2 변형
- **무효 조합**: 동일 색 hover → `"같은 색은 섞이지 않아요"`, 배경 warning-dark 색, 햅틱 Invalid (단 드롭 전엔 발동 X, 텍스트만).
- **프리즘 hover**: 실시간 결과색 연산 후 예상 결과 표기 (`"🌈 + 🔴 = 🔴"`).

---

## 7. 프롬프트 배너 진행도 채움 애니메이션

상단 프롬프트 배너 (`PromptBanner.cs`) 에 "보라 3/5" 형태 서브 텍스트 + 진행 바 동반.

```yaml
prompt_progress:
  bar:
    height:         4px
    background:     neutral.200 (dark: neutral.700)
    fill:           yellow.base → secondary.purple (목표색 동적 매핑)
    radius:         pill
  fill_animation:
    trigger:        "서브 목표 1 tick 증가 시"
    duration:       400ms
    easing:         ease_out_quart
    stagger:        "동시 2 tick 증가면 180ms 간격으로 분리 재생 — 시각적 축적감"
  complete_flash:
    condition:      "목표 달성 (n/n 도달)"
    animation:      "bar glow 8px, yellow → white, 300ms, label '✓' scale 0→1.2→1.0 spring_soft"
    haptic:         "StageSuccess (미리 예고 펄스)"
    audio:          "PromptSuccessBell, 0.8s"
```

---

## 8. 갤러리 복원 시네마틱 (0.8 ~ 1.2s)

Result 화면에서 갤러리 진행도 증가 구간.

```yaml
gallery_restore_sequence:
  T-0.00s: "Result 카드 우측 '갤러리 복원' 섹션 하이라이트 ring 확장 (radius lg pulse)"
  T-0.10s: "썸네일 grayscale → color gradient reveal (세로 wipe, top → bottom, 400ms)"
  T-0.50s: "진행 바 old% → new% 채움 (ease_out_quart, 400ms) — §7 동일 규칙"
  T-0.75s: "새로 복원된 영역에 particle sparkle 12개 0.4s"
  T-0.90s: "% 숫자 count-up (증분 속도 160ms/1%), 도달 시 pop spring_soft"
  T-1.10s: "완료. 하단 '갤러리 보기' 링크 fade-in (200ms)"

total: "1100ms"

variant_100_percent:
  T-1.10s → T-2.30s: "100% 달성 시 EPIC: 풀스크린 prism 그라디언트 스윕 + StageFanfare + 배지 모달 큐잉"
```

---

## 9. 햅틱 × 오디오 × 비주얼 동기화 매트릭스

7 HapticEvent × 7 SfxId × 비주얼 트리거 — **지연 ±16ms 이내** 동기화 필수 (1 frame @ 60fps).

| # | 게임 이벤트 | HapticEvent | SfxId | Visual 트리거 | 동기화 오프셋 |
|---|---|---|---|---|---|
| 1 | 블록 픽업 | `BlockPickup` | — (무음) | scale 1.12 pop, shadow elevation 3 | 0ms (동시) |
| 2 | 인접 hover | `Hover` (최초 3회만) | — | tint luminance +8%, glow 2px pulse | 0ms |
| 3 | 1차 혼합 성공 | `Mix1` | `MixPlop` | Metaball blend 350ms + flash + §2.level_1 | Haptic 0ms / SFX +20ms / Flash +40ms |
| 4 | 2차 연쇄 | `Chain2` | `ChainChord` | §2.level_2 시퀀스 + slow-mo | Haptic 0ms / SFX +100ms (chord delay) / Shake +40ms |
| 5 | 3차+ 대연쇄 | `Chain2` × N | `ChainChord` + `ExplodeWhoosh` | §2.level_3~5 | Haptic 80ms stagger / SFX 레이어링 / Flash +40ms |
| 6 | 스테이지 성공 | `StageSuccess` | `StageFanfare` | Result 카드 slide-up, 별 spawn spring | Haptic 0ms / Fanfare 0ms / Card +200ms |
| 7 | 무효 드롭 | `Invalid` | `InvalidHiss` | block shake X축 ±4px 120ms, red outline 펄스 | Haptic 0ms / SFX 0ms / Shake 0ms |
| 8 | 턴 ≤ 3 | `TurnsLow` (1초 간격 loop) | `TurnsLowHeartbeat` (loop) | 이동 카운터 red pulse, vignette 0.15α | Haptic/SFX 동일 1Hz / Vignette on tick |
| 9 | 프롬프트 달성 | — (StageSuccess 준용) | `PromptSuccessBell` | 배너 진행 바 glow + ✓ pop | SFX 0ms / Visual +80ms |
| 10 | 폭발/매치 해소 | — (Mix1 or Chain2 준용) | `ExplodeWhoosh` | 파티클 burst, block fade-out | SFX -60ms (사전 예고) / Visual 0ms |

### 9.1 동기화 전략
- **이벤트 버스 단일 프레임 디스패치**: `FeedbackBus.Emit(eventId, pos)` → Haptic/Audio/VFX 3 subscriber 가 같은 LateUpdate 내에서 예약.
- **오프셋 테이블**은 `FeedbackProfile.asset` (ScriptableObject) 로 튜닝 가능.
- **Reduce Motion 모드**: Visual 변형 제거, Haptic/Audio 그대로. Slow-mo 무시.
- **사운드 끔 + 햅틱 켬**: Haptic 단독 발화 허용 — "진동만으로도 피드백 전달" 원칙.

---

## 10. 모션 체크리스트 (구현 검수용)

```yaml
checklist:
  - 모든 duration 토큰 사용 (하드코딩 값 금지)
  - 모든 easing AnimationCurve 재사용 (인라인 생성 금지)
  - 60fps 유지 @ 중간 사양 (iPhone 11 / Galaxy A54)
  - Reduce Motion 경로 테스트 (duration × 0.3, overshoot 0)
  - 색맹 모드에서 연쇄 구분 가능 (패턴 + 파티클 크기로 depth 식별)
  - 햅틱/오디오 off 시에도 Visual 단독으로 피드백 전달 가능
  - Jelly / Metaball Low-quality fallback 동작 확인
```

---

문서 끝.
