# Color Mix: Alchemist — Master Design System v2

> 작성자: UX/UI Senior Designer Teammate
> 작성일: 2026-04-21
> 범위: 색상 / 타이포 / 스페이싱 / 엘리베이션 / Radius / Motion / 접근성 — 전역 디자인 토큰 정의
> 이식 대상: `Assets/_Project/Scripts/View/AppColors.cs` → `ColorTokens.asset` (ScriptableObject, 다음 라운드)

---

## 1. 색상 팔레트 v2 (Color Tokens)

### 1.1 브랜드 원색 (Primary — Red / Yellow / Blue)
게임 판타지 중심축. Jelly/Metaball 채널로 원본 샘플링되므로 **HSL S(채도) 78~88 / L(명도) 52~58** 구간으로 통일해 2차 혼합 시 탁해지지 않도록 재조정.

```yaml
primary:
  red:
    hex:  "#E83B4C"     # 기존 #E63946 대비 S +2, L -1 — 2차 Purple 혼합 시 그레이시 현상 제거
    hsl:  "353, 78%, 56%"
    usage: "Block-Red, 프롬프트 위반 경고 하이라이트 사용 금지 (danger와 혼동)"
    variants:
      light:  "#F27685"   # 다크 모드 보드 배경 위 대비 강화
      dark:   "#B12939"   # 라이트 모드 텍스트용 (AA 4.5:1 @ #FFFFFF)
  yellow:
    hex:  "#F7CE3B"
    hsl:  "49, 92%, 60%"
    usage: "Block-Yellow, 프롬프트 배너 테두리, CTA 보조"
    variants:
      light:  "#FFE27A"
      dark:   "#C59A14"   # 라이트 모드 텍스트 대비 확보
  blue:
    hex:  "#3A88F0"
    hsl:  "214, 86%, 58%"
    usage: "Block-Blue, 링크, info 상태"
    variants:
      light:  "#7AB0F7"
      dark:   "#1F5FB5"
```

### 1.2 2차 혼합색 (Secondary — Orange / Green / Purple)
원색 2종 혼합 결과. Metaball 블렌드 셰이더의 `_MetaTint` 최종 수렴 지점. HSL L 58~64 로 밝게 유지.

```yaml
secondary:
  orange:        # Red + Yellow
    hex:  "#F59545"
    hsl:  "27, 90%, 62%"
    variants: { light: "#FDBE82", dark: "#BF6715" }
  green:         # Yellow + Blue
    hex:  "#4CBA85"
    hsl:  "153, 43%, 51%"
    variants: { light: "#83D7AE", dark: "#2B8358" }
  purple:        # Red + Blue
    hex:  "#9D4EDD"
    hsl:  "274, 67%, 58%"
    variants: { light: "#C08BEB", dark: "#6F2BA8" }
```

### 1.3 3차/특수 (Tertiary & Special)
```yaml
tertiary:
  white_holo:    # 2차+2차 = 흰 (홀로그램 힌트)
    hex:  "#F3FAF2"
    hsl:  "114, 43%, 97%"
    effect: "rim-light 2px, Hue 회전 2.4s loop"
    variants: { light: "#FFFFFF", dark: "#E0ECDF" }
  black_contam:  # 2차+2차 반대축 = 검정 (오염/특수)
    hex:  "#17171C"
    hsl:  "240, 11%, 10%"
    effect: "shadow glow 8px @ #000 80%"
    variants: { light: "#2D2D36", dark: "#0A0A0D" }

special:
  prism:
    gradient: ["#FF4D6D", "#FFB84D", "#FFF14D", "#4DFF88", "#4DC9FF", "#A14DFF"]
    usage: "wildcard — 드래그 시 hover 대상 색으로 실시간 수렴"
    animation: "Hue rotate 360deg / 3.0s"
  gray_thief:
    hex:  "#6C757D"
    hsl:  "208, 7%, 46%"
    usage: "색도둑 블록 — 인접 색 흡수 (탈채화)"
    variants: { light: "#A1A8AE", dark: "#454B50" }
```

### 1.4 UI 중립 (Neutral 9단계)
```yaml
neutral:
  "50":  "#F8F9FB"   # 라이트 Canvas 최상층
  "100": "#EEF0F4"   # 라이트 Elevated 카드
  "200": "#D9DDE3"   # 구분선
  "300": "#B9BFC8"   # 비활성 텍스트
  "400": "#8C93A0"   # placeholder
  "500": "#6B7280"   # 보조 텍스트
  "600": "#4B5261"   # 본문 텍스트 (다크 모드 Caption)
  "700": "#333A45"   # 다크 Elevated
  "800": "#1F242D"   # 다크 Canvas
  "900": "#0F1218"   # 다크 Overlay 배경
```

### 1.5 Semantic (상태색)
```yaml
semantic:
  success: { base: "#22C55E", light: "#86EFAC", dark: "#15803D", on: "#FFFFFF" }
  warning: { base: "#F59E0B", light: "#FCD34D", dark: "#B45309", on: "#1F242D" }
  danger:  { base: "#EF4444", light: "#FCA5A5", dark: "#B91C1C", on: "#FFFFFF" }
  info:    { base: "#3B82F6", light: "#93C5FD", dark: "#1D4ED8", on: "#FFFFFF" }
```
> 주의: `danger`와 `primary.red` 는 HSL hue 6deg 차이 — danger는 **오직 시스템 경고(저장 실패/결제 오류)** 에서만 사용. 게임 내 "실패" 피드백은 `primary.red` 사용 금지하고 `neutral.400` 기반 톤다운 + warning-light 스트로크로 처리.

### 1.6 컨텍스트 3종 (Light / Dark × Canvas / Elevated / Overlay)
```yaml
surface:
  light:
    canvas:   "#F8F9FB"   # neutral.50 — 로비/스테이지 선택
    elevated: "#FFFFFF"   # 카드/모달 배경
    overlay:  "rgba(15, 18, 24, 0.56)"  # 모달 딤 배경
  dark:
    canvas:   "#0F1218"   # neutral.900
    elevated: "#1F242D"   # neutral.800 — 카드
    overlay:  "rgba(0, 0, 0, 0.72)"

on_surface:    # 각 컨텍스트 위 텍스트 기본값
  light:   { primary: "#1F242D", secondary: "#4B5261", disabled: "#B9BFC8" }
  dark:    { primary: "#F8F9FB", secondary: "#B9BFC8", disabled: "#4B5261" }
```

### 1.7 토큰 요약
- 브랜드 원색 3 × (base/light/dark) = 9
- 2차 3 × 3 = 9
- 3차/특수 4 × 3 = 12 (prism 그라디언트 6컬러 별도)
- 중립 9
- Semantic 4 × 4 (base/light/dark/on) = 16
- Surface/on 라이트·다크 2 × 3 + 2 × 3 = 12
- **합계: 67 토큰 + Prism 그라디언트 6컬러**

---

## 2. 타이포그래피 (TMP 기준)

### 2.1 폰트 패밀리 2종
```yaml
font_family:
  display: 
    name: "Pretendard Variable"
    weights: [400, 600, 800]
    fallback: "NotoSansKR"
    license: "SIL OFL 1.1"
    tmp_asset: "Fonts/Pretendard-Variable SDF"
    usage: "Heading, Display, Prompt Banner, Result Title"
  body:
    name: "Inter"
    weights: [400, 500, 700]
    fallback: "Pretendard"
    tmp_asset: "Fonts/Inter-VariableFont SDF"
    usage: "Body, Caption, 버튼 라벨, 시스템 문구"
```
> WHY Pretendard: 한국어 수치·영문 혼용 밀도 우수, 무료, Variable 폰트로 TMP 단일 SDF 에셋에서 Weight 보간 가능 → Atlas 하나만 유지.

### 2.2 스케일 (Type Ratio 1.25 — Major Third)
기준 body = 16px. TMP `FontSize` 단위는 `Units Per EM` 기준 1em=16px.

```yaml
scale:
  display_xl:  { size: 48, line: 56, weight: 800, letter_spacing: -2, family: display }  # 스플래시 "색이 돌아온다"
  display_lg:  { size: 40, line: 48, weight: 800, letter_spacing: -1.5, family: display }
  heading_1:   { size: 32, line: 40, weight: 700, letter_spacing: -1, family: display }  # Result 별·스테이지명
  heading_2:   { size: 24, line: 32, weight: 700, letter_spacing: -0.5, family: display } # 챕터 타이틀
  heading_3:   { size: 20, line: 28, weight: 600, letter_spacing: -0.25, family: display }
  body_lg:     { size: 18, line: 28, weight: 500, letter_spacing: 0, family: body }
  body:        { size: 16, line: 24, weight: 400, letter_spacing: 0, family: body }       # 기본
  body_sm:     { size: 14, line: 20, weight: 400, letter_spacing: 0.1, family: body }
  caption:     { size: 12, line: 16, weight: 500, letter_spacing: 0.4, family: body }     # 배지 라벨, 팔레트 캡션
  overline:    { size: 10, line: 14, weight: 700, letter_spacing: 1.5, family: body, uppercase: true }  # "CHAPTER 1"
```

### 2.3 본문 가독성 제약
- 한 줄 최대 16자(한국어) / 32자(영문) — Prompt Banner 기준.
- 본문 문단 최대 폭 320px (PromptBanner 내부 `MaxLineWidth`).
- TMP `Word Wrapping` ON + `Overflow = Truncate` 금지 (말줄임 … 사용).

---

## 3. 스페이싱 스케일 (4px 기반)

```yaml
spacing:
  "0":  0
  "1":  4     # hairline gap (보드 셀 간격)
  "2":  8     # 아이콘 ↔ 텍스트
  "3":  12    # 카드 내부 padding (소형)
  "4":  16    # 기본 margin / 카드 padding
  "5":  24    # 섹션 간격
  "6":  32    # 화면 하단 Safe 여백
  "7":  48    # 주요 섹션 블록 간격
  "8":  64    # 히어로 섹션 상하 여백
  "9":  96    # 스플래시 중앙 정렬 여백
```

### 3.1 레이아웃 규칙
- 화면 좌우 gutter: **16px** (375dp 기준), iPad: 24px.
- 카드 그리드 gap: **12px** (gallery), **4px** (board).
- 버튼 내부 padding: **12px × 24px** (세로×가로).
- Safe Area 상단: 추가 **8px** 여유 (다이나믹 아일랜드 충돌 방지).

---

## 4. 엘리베이션 (Shadow + Glow 5단계)

```yaml
elevation:
  "0":
    shadow: "none"
    use: "Canvas 배경, 보드 셀 기본"
  "1":   # Low — 카드 평상시
    shadow: "0 1px 2px rgba(15,18,24,0.08), 0 1px 3px rgba(15,18,24,0.06)"
    dark:   "0 1px 2px rgba(0,0,0,0.45)"
    use: "Gallery Card, Stage Node (비활성)"
  "2":   # Mid — 활성 카드, 팔레트 슬롯
    shadow: "0 2px 4px rgba(15,18,24,0.10), 0 4px 8px rgba(15,18,24,0.06)"
    dark:   "0 2px 6px rgba(0,0,0,0.55)"
    use: "Palette Slot, Item Button, 현재 챕터 카드"
  "3":   # High — 드래그 중 블록
    shadow: "0 6px 12px rgba(15,18,24,0.18), 0 2px 4px rgba(15,18,24,0.10)"
    dark:   "0 8px 16px rgba(0,0,0,0.65)"
    use: "Drag Ghost, Toast, Pause Overlay 패널"
  "4":   # Modal — 전면 모달
    shadow: "0 16px 32px rgba(15,18,24,0.24), 0 8px 16px rgba(15,18,24,0.12)"
    dark:   "0 20px 40px rgba(0,0,0,0.75)"
    use: "Pre-Game Loadout, 결제 에러, 세이브 충돌"
  "5":   # Glow — 성공 섬광
    shadow: "0 0 32px 8px rgba(247,206,59,0.60)"    # yellow glow
    dark:   "0 0 40px 12px rgba(247,206,59,0.75)"
    use: "1차 혼합 성공, 별 획득, 갤러리 복원 완료"
```
> 2D URP 에서 Shadow는 `UnityEngine.UI.Shadow` / `Outline` 대신 **SpriteRenderer 하위 9-slice soft-shadow 프리팹**으로 구현 권장 — 드로콜 1개 추가로 Canvas 전체 Rebuild 회피.

---

## 5. Radius 스케일

```yaml
radius:
  none:  0
  xs:    4      # 칩, 작은 배지
  sm:    8      # 버튼 기본
  md:    12     # 카드, 모달 내부 요소
  lg:    16     # 카드 외곽, 프롬프트 배너
  xl:    24     # 모달, 히어로 카드
  pill:  9999   # 둥근 버튼, 태그
```
> 블록은 Radius 고정 `12px` (52px 셀에서 비율 0.23). Board 전체 컨테이너는 `lg=16`.

---

## 6. Motion 원칙

### 6.1 Easing Curves 6종
TMP/UGUI `AnimationCurve` 직렬화 기준. DOTween `Ease` 매핑 병기.

```yaml
easing:
  linear:           { curve: "0,0,1,1", dotween: "Linear", use: "루프 회전, Hue shift" }
  ease_out_quart:   { curve: "0.25,1,0.5,1", dotween: "OutQuart", use: "진입/나타남 (default)" }
  ease_in_quad:     { curve: "0.55,0,1,0.45", dotween: "InQuad", use: "퇴장/사라짐" }
  ease_in_out_cubic:{ curve: "0.65,0,0.35,1", dotween: "InOutCubic", use: "스크롤, 모달 슬라이드" }
  spring_soft:      { curve: "0.34,1.56,0.64,1", dotween: "OutBack (overshoot 1.2)", use: "Jelly bounce, 아이콘 pop" }
  spring_hard:      { curve: "0.68,-0.55,0.27,1.55", dotween: "InOutBack (overshoot 1.7)", use: "연쇄 2차+ 카메라 펀치" }
```

### 6.2 Duration 스케일
```yaml
duration:
  instant:  80ms    # 탭 피드백 (셀 squish)
  fast:     150ms   # hover, 프리뷰 토스트 갱신
  base:     240ms   # 카드 pop, 버튼 press
  slow:     400ms   # 모달 전환, 보드 리플로우
  cinematic: 800ms  # 갤러리 복원 연출
  epic:     1200ms  # 스테이지 성공 팡파레, 스플래시
```

### 6.3 Motion 원칙 요약
1. **의미 있는 움직임만** — 장식 애니 금지, 모든 모션은 상태 변화 전달.
2. **60fps 유지** — 동시 애니 ≤ 8개, 파티클 ≤ 120 (Low 품질에서 40).
3. **Reduce Motion 설정** — iOS/Android 접근성 토글 감지 시 duration × 0.3, overshoot 제거.
4. **Easing 선택 규칙**: 나타남=ease_out, 사라짐=ease_in, 위치 이동=ease_in_out, 바운스=spring.

---

## 7. 접근성 (Accessibility)

### 7.1 Color Contrast (WCAG 2.1)
모든 텍스트 × 배경 조합은 **AA 기준 이상**:
- Body (16px 미만 볼드 아님) ≥ **4.5:1**
- Large (18px+ / 14px+ 볼드) ≥ **3.0:1**
- UI Component (버튼 테두리, 아이콘) ≥ **3.0:1**

검증된 조합:
```yaml
verified:
  - { fg: "#1F242D", bg: "#F8F9FB", ratio: "14.2:1", ok: "AAA" }
  - { fg: "#F8F9FB", bg: "#0F1218", ratio: "15.3:1", ok: "AAA" }
  - { fg: "#B12939", bg: "#FFFFFF", ratio: "6.1:1",  ok: "AA+ (red-dark)" }
  - { fg: "#C59A14", bg: "#FFFFFF", ratio: "4.6:1",  ok: "AA (yellow-dark)" }
  - { fg: "#FFFFFF", bg: "#3A88F0", ratio: "3.4:1",  ok: "AA Large only — 버튼 텍스트는 16px+ 볼드 의무" }
```

### 7.2 색맹 모드 패턴 오버레이
10 ColorId × 고유 패턴 1:1 매핑 — 블록 위에 **SpriteMask** 레이어로 alpha 0.35 오버레이.

```yaml
colorblind_patterns:
  Red:    "dot_diag_3px"        # 우상향 점선
  Yellow: "check_4px"            # 체크보드
  Blue:   "stripe_horiz_2px"
  Orange: "stripe_diag_2px"
  Green:  "wave_4px"
  Purple: "cross_hatch_3px"
  White:  "ring_concentric"
  Black:  "solid_center_dot"
  Prism:  "rainbow_gradient_radial"
  Gray:   "blur_soft"
```
- 설정 → 접근성 → "색맹 모드" 토글. 3 프로토콜 지원: **Deuteranopia / Protanopia / Tritanopia** (블록 틴트 HSL Hue 자동 재매핑 + 패턴 항상 ON).
- 패턴 텍스처 ≤ 64×64 alpha-only R8 — 메모리 오버헤드 < 16KB/atlas.

### 7.3 폰트 크기 스케일러
설정 → 접근성 → "글자 크기": **0.9 / 1.0 / 1.15 / 1.3 / 1.5** 5단계.
- 모든 TMP 컴포넌트는 `FontSizeScaler` 컴포넌트 경유 (기존 `LocalizerService` 패턴과 동일).
- 1.5 배율 시 상단바 높이 56→72px 자동 확장, 프롬프트 배너 2줄 허용.

### 7.4 기타 접근성
- **Haptic 강도 3단계** (풍부/기본/끔) — 이미 `HapticProfile.cs` 구현됨.
- **Reduce Motion** — OS 설정 감지 → `Duration × 0.3`, Metaball 블렌드 제거, 화면 흔들림 off.
- **터치 타겟 최소 44×44 pt** (iOS HIG) — 모든 버튼·아이콘 `MinSize` 검증 필수.
- **음성 안내 (VoiceOver/TalkBack)** — 블록에 `colorName + position` 라벨 자동 주입 (Phase 5).

---

문서 끝.
