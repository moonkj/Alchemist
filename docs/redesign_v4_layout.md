# Redesign v4 — Layout & Touch UX (아이 친화 프리미엄 확대안)

- 작성일: 2026-04-22
- 작성자: Layout / Touch UX Specialist Teammate
- 기준 파일: `Assets/_Project/Scripts/Bootstrap/MinimalGameScene.cs` (2319 LoC, IMGUI)
- 유저 VOC: **"버튼, 챕터 버튼 등 너무 작음"**
- 목표: Apple HIG 44pt → 아이 친화 프리미엄 기준 **60~96pt**. 어린이(3~10세) 엄지 평균 타격 반경(~16mm, ≈ 72px @3x) 기준 확대.
- 제약: 본 문서는 권고안이며 **코드 수정 없음**. 수치는 `Rect(x,y,w,h)` 기반 그대로 반영 가능한 값.

---

## 0. 요약 (TL;DR)

| 영역 | 현재 | 권고 | 사유 |
|---|---|---|---|
| 로비 스테이지 버튼 | 80h, gap 10 | **120h, gap 16, 여백 24** | 엄지 중앙 정렬·연속 탭 여유 |
| HUD ⏸/🏠 | 80×48 | **80×72** (필요시 80×80) | 48h는 HIG 최소선 미달 |
| 팔레트 슬롯 Gap | `0.06` | **`0.20`** (×3.3) | 슬롯 오탐 방지, 드래그 타깃 분리 |
| 결과 버튼 btnH | 64 | **96**, gap 16→28 | 하단 Hot zone 주 CTA 확대 |
| 설정 rowH | 64 | **84** | 행 전체 터치 가능, 슬라이더 20→28h |
| 튜토리얼 이전/다음 | 180×60 | **220×84** | Hot zone 주 네비 확대 |
| 갤러리 진행바 | 20h, 캔버스 360 | **32h, 440** | 진척 가시성 + 복원 감각 |

**핵심 결론**: 현재 레이아웃은 성인 기준 HIG 최소선(44pt)에 근접하지만 **아이용 프리미엄에는 전반 1.3~1.5× 확대**가 필요하며, 특히 **팔레트 슬롯·HUD 보조 버튼·설정 행·결과 CTA** 4곳이 최우선 교정 대상이다.

---

## 1. 현 MinimalGameScene 레이아웃 스캔 (Read-only)

### 1.1 로비 (스테이지 선택) — `DrawLobby` L1432~1509
- 타이틀: `(0, titleY, w, 56)`, 서브 `+64, 22h`
- 우상단 갤러리 버튼: `(w-176, titleY-20, 160×56)`
- 좌상단 ❓ 도움말: `(16, titleY-20, 64×56)` — 엄지 도달은 OK, **64×56은 정사각 아님**
- ⚙ 설정: `(86, titleY-20, 64×56)` — ❓와 겹쳐 엄지 오탐 위험
- 스테이지 리스트:
  - `btnW = min(w-40, 420)`, **`btnH = 80`**
  - `gap = 10`, `chapterHeaderH = 36`, `chapter2HeaderGap = 20`
- 세로 스크롤 `scrollBottomMargin` 적용

### 1.2 게임 HUD — `DrawHud` L2050~2130
- 상단 패널: `(0, topSafe, w, panelH)` 반투명
- 스테이지 제목: `(16, topSafe+10, w-100, 32)`
- ⏸ 버튼: **`(w-180, topSafe+10, 80×48)`**
- 🏠 버튼: **`(w-92, topSafe+10, 80×48)`** — 폭 80 OK, 높이 48h는 HIG 최소선
- 목표 진행 바: 높이 barH (게이지), 라벨 `_goalLabel`
- 점수: `(w-296, infoY-6, 280×44)` — 44h
- 이동 표시: `(16, infoY, 260×34)`
- 팔레트 오버레이: `DrawPaletteOverlay` — 월드 좌표 기반, `Gap = 0.06f` (L19)
- 힌트 캡션: `(0, hintY, w, 20)`

### 1.3 결과 화면 — `DrawResult` L2210~2310
- 타이틀 `(0, h/2-210, w, 68)`
- 별점: starSize (기본 ~72), starGap **16**
- 점수 `(0, h/2-30, w, 40)`
- 서브 라인 `(0, h/2+16, w, 28)`
- **btn: `btnH=64`, `gap=16`, btnW = min(200, (avail-gap)/2)**

### 1.4 튜토리얼 — `DrawTutorial*` L1757~1916
- 페이지 도트: dotW 14, dotGap 8 (상단)
- 건너뛰기: `(w-100, topY, 84×36)`
- 하단 네비: **`btnY`, `btnH=60`, `btnW=180`, `gap=16`** (L1790~1816)
- 페이지 0: 데모 블록 72sz, gap 24
- 페이지 1: 조합 규칙 3행, sz=60, rowH=90

### 1.5 설정 — `DrawSettings` L1602~1693
- 타이틀 `(0, topY, w, 52)`
- 뒤로: **`(16, topY+8, 84×40)`** — 40h 문제
- 행: **`rowH=64`, rowW=min(w-40,420)`, gap=14**
- 슬라이더: `(x+180, y+(h-20)/2, w-260, 20)` — **슬라이더 트랙 20h 얇음**
- 초기화 확인 버튼: `rowH-6`

### 1.6 갤러리 — `DrawGallery` L1921~1985
- 캔버스: `canvasSize = min(w-40, 360)`
- 진행 바: **`barH = 20`**, fill = 선형 보간 색
- 섹션 높이: 30+20+canvasSize+22+32+48

---

## 2. 현재 문제점 진단

1. **스테이지 버튼 80h·gap 10**: 세로 연속 탭 시 엄지가 위/아래 이웃을 터치(fat-finger). 아이 손가락은 접촉 반경이 성인보다 크다. 80h → **120h** 권고.
2. **HUD ⏸(80×48), 🏠(80×48)**: 높이 48h가 HIG 최소선(44pt)과 근접하며, **두 버튼이 88px(w-180 vs w-92) 거리로 붙어 오탐**. 아이 시선으로 아이콘 구분이 어려움.
3. **팔레트 Gap = 0.06**: 월드 `CellSize` 대비 6%는 드래그 타깃 경계가 거의 붙은 수준. 드롭 인식이 인접 슬롯으로 튐. 권고 **0.20**.
4. **갤러리 진행바 20h**: 얇아서 "복원 진척감"이 약함. 프리미엄 연출에서 **32h + 그라데이션 필**로 확장 필요.
5. **결과 btnH 64**: 하단 Hot zone에 배치된 "재도전/다음"은 플레이 루프에서 가장 자주 누르는 버튼. **96h**가 "한손으로 확실히 다음 판" 감각을 준다.
6. **설정 rowH 64 / 슬라이더 20h**: 토글 행은 전체가 tappable이어야 하는데 64h는 공간 부족. 슬라이더 트랙 20h는 드래그 정밀도 저하.
7. **튜토리얼 이전/다음 180×60**: 첫인상 화면에 60h는 왜소. **220×84**로 CTA 확대.
8. **뒤로 버튼 84×40**: 설정·갤러리 뒤로(40h)는 HIG 미달. **96×56** 권고.
9. **설정 토글/슬라이더 간 gap 14**: 행 사이 공간이 좁아 스크롤 시 오탐 가능. **20** 권고.

---

## 3. 재배치 권고 (구체 좌표·비율)

### 3.1 로비 (DrawLobby)
```
titleY           : 상동
title label      : (0, titleY, w, 64)      // 56 → 64
subtitle         : (0, titleY+72, w, 26)   // 22→26, y offset 64→72
galleryBtn       : (w-200, titleY-24, 184, 72)   // 160×56 → 184×72
helpBtn (❓)     : (16, titleY-24, 72, 72)       // 64×56 → 72×72 정사각
settingsBtn (⚙) : (104, titleY-24, 72, 72)      // 86+72 = 158 → 104 기준
listStartY       : titleY + 116                  // 여백 96 → 116
btnW             : min(w-48, 440)                // 여백 40→48
btnH             : 120                           // 80 → 120 (+50%)
gap              : 16                            // 10 → 16
chapterHeaderH   : 44                            // 36 → 44
chapter2HeaderGap: 28                            // 20 → 28
stage label font : +2pt
```
Rect 예:
```
DrawStageButton(i, (w-btnW)/2, cy, btnW, 120);
cy += 120 + 16;
```

### 3.2 HUD (DrawHud)
```
panelH         : +12 (점수·제목 행간 확보)
⏸ btn          : (w-200, topSafe+12, 80, 80)   // 80×48 → 80×80
🏠 btn         : (w-100, topSafe+12, 80, 80)
(버튼 사이 거리: 20px → 오탐 감소)
❓ helpBtn     : 우측 상단 HUD와 분리, (16, topSafe+12, 72, 72) 또는 추가 미제공 유지
목표 진행 바   : barH 24 → 32
점수 표시      : (w-300, infoY-10, 280, 52)     // 44h → 52h
이동 표시      : (16, infoY, 260, 40)
힌트 캡션       : (0, hintY-4, w, 24)
```

### 3.3 팔레트 (DrawPaletteOverlay + `Gap` 상수)
```csharp
// L19
private const float Gap = 0.06f;   // → 0.20f 권고
```
슬롯 scale 권고: 월드 유닛 기준 **1.0 → 1.05~1.10**. `BuildPaletteSlots`의 `step = CellSize + Gap`이므로 Gap 변경만으로 간격 확대 가능. 단 보드 총폭은 그대로이므로 팔레트 표시 영역만 별도 오프셋.

기존:
```
step = CellSize + 0.06
totalW = 3*step - 0.06
```
권고:
```
step = CellSize + 0.20
totalW = 3*step - 0.20
slotScale = 1.05  // 시각적 확대 (스프라이트 transform.localScale)
```
`+` 아이콘 표시 라벨 박스 `(s.x-40, sy-40, 80×80)` → `(s.x-48, sy-48, 96×96)`.

### 3.4 결과 (DrawResult)
```
btnH      : 64 → 96
gap       : 16 → 28
btnW      : min(240, (avail - 28)/2)      // 200 → 240
by        : 결과 박스 하단 Hot zone 56% 라인 정렬
재도전 btn: (x0,            by, btnW, 96)  _ghostBtn
다음 btn  : (x0+btnW+28,    by, btnW, 96)  _primaryBtn
starSize  : +8 (72→80), starGap 16 → 20
```

### 3.5 설정 (DrawSettings)
```
뒤로     : (16, topY+8, 96, 56)             // 84×40 → 96×56
rowH     : 64 → 84
gap      : 14 → 20
rowW     : min(w-40, 440)                   // 420 → 440
토글 knob: sz += 8, trackH 40 → 56
슬라이더 : (x+200, y+(h-28)/2, w-280, 28)   // 20h → 28h
라벨     : (x+24, y, w-160, h), font +2pt
버전     : (0, y+8, w, 24)
```

### 3.6 튜토리얼 (DrawTutorial)
```
이전/다음 : 180×60 → 220×84
gap       : 16 → 24
btnY      : h - bottomSafeY - 132           // 100 → 132 (하단 여유)
dotW      : 14 → 18
dotGap    : 8 → 12
건너뛰기  : (w-120, topY, 100, 52)          // 84×36 → 100×52
페이지 0 블록: sz 72→88, gap 24→32
페이지 1 규칙행: rowH 90→110, sz 60→72
```

### 3.7 갤러리 (DrawGallery)
```
뒤로       : (16, topY+8, 96, 56)
canvasSize : min(w-40, 360) → min(w-48, 440)
barH       : 20 → 32
fill 그라데: 3-stop gradient (top→mid→bottom 색)
복원 라벨  : +2pt, (cx, cy-4, canvasSize, barH+8)
sectionH   : 30+20+canvasSize+22+32+48 → 36+24+canvasSize+36+40+56
```

---

## 4. 한손 도달성 맵 (iPhone 14 Pro 6.1" 세로 기준, 852 × 393pt)

화면을 세로 4구간으로 분할:

```
 0% ┌───────────────────────┐
    │ COLD ZONE (0~40%)     │  ← 정보 전용: 제목, 점수, 스테이지명, 진행바
40% ├───────────────────────┤
    │ WARM ZONE (40~55%)    │  ← 보드(블록 6×6), 프롬프트, 규칙 아트
55% ├───────────────────────┤
    │ HOT ZONE (55~90%)     │  ← 주 CTA: 재도전/다음, 시작, 재시작, 팔레트, HUD 보조
90% ├───────────────────────┤
    │ THUMB EDGE (90~100%)  │  ← 회피: 홈 인디케이터 충돌 위험
100%└───────────────────────┘
```

| 요소 | Zone | 이유 |
|---|---|---|
| 로비 타이틀·서브 | Cold | 정보 |
| ❓⚙ (상단) | Cold | 드물게 사용 |
| 갤러리 진행 뱃지 (우상단) | Cold | 정보 |
| 스테이지 리스트 | Warm~Hot (스크롤) | 중앙 기준 |
| **로비 "시작" / 튜토리얼 "시작하기"** | **Hot 62~78%** | 주 CTA |
| HUD ⏸🏠 | Cold(0~10%) | 빠른 중단은 상단이 직관 |
| 팔레트 (보드 하단) | Hot 65~80% | 드래그·탭 주 영역 |
| 결과 재도전/다음 | Hot 60~75% | 주 CTA |
| 설정 행 | Warm~Hot | 세로 스크롤 |
| 튜토리얼 이전/다음 | Hot 75~85% | 엄지 자연 위치 |

---

## 5. 사파리 존 여백 (Safe Area Insets)

### 5.1 기기별 기본 인셋 (iPhone 14 Pro 기준)
- 상단 Dynamic Island: **+59pt** (Portrait)
- 하단 Home Indicator: **+34pt**
- 좌우: 0

### 5.2 `GetSafeArea()` (L1375) 확장 권고
```
sa.y      : Dynamic Island 고려 topSafe = sa.y + 8
sa.height : 하단 끝 - 34pt
실제 하단 btnY 계산시 bottomSafeY + 추가 16pt 여유
```

### 5.3 화면별 인셋 적용
| 화면 | 상단 여유 | 하단 여유 |
|---|---|---|
| Lobby  | sa.y + 24 | +34 (리스트 끝) |
| HUD    | sa.y + 12 (Dynamic Island 회피) | +34 (팔레트 아래) |
| Result | 센터 정렬 유지, btn은 `h - 34 - 16 - btnH` | - |
| Tutorial | sa.y + 40 | `btnY = h - bottomSafeY - 132` |
| Settings | sa.y + 40 | +34 (버전 라벨 위) |
| Gallery  | sa.y + 48 | `listH = h - listY - bottomSafeY - 28` |

iPad(11") / Pro Max 대응: `Screen.safeArea` 사용 중이므로 그대로 유효. 단 rowW max를 **440 → 520** (큰 기기 여유).

---

## 6. 구현 힌트 (Rect 직접 반영 안)

### 6.1 L19 상수 변경
```csharp
private const float Gap = 0.20f;   // 기존 0.06f
```

### 6.2 L1460~1488 로비 수치 교체
```csharp
int btnW = Mathf.Min(w - 48, 440);
int btnH = 120;                         // 80 → 120
int chapterHeaderH = 44;                // 36 → 44
int gap = 16;                           // 10 → 16
int chapter2HeaderGap = 28;             // 20 → 28
```

### 6.3 L2061~2065 HUD 버튼
```csharp
if (GUI.Button(new Rect(w - 200, topSafe + 12, 80, 80), "⏸", bigBtnStyle)) ...
if (GUI.Button(new Rect(w - 100, topSafe + 12, 80, 80), "🏠", bigBtnStyle)) ...
```

### 6.4 L1612 설정 뒤로
```csharp
if (GUI.Button(new Rect(16, topY + 8, 96, 56), "◀ 뒤로", _ghostBtn))
```

### 6.5 L1619~1623 설정 행
```csharp
int rowH = 84;                          // 64 → 84
int rowW = Mathf.Min(w - 40, 440);      // 420 → 440
int gap = 20;                           // 14 → 20
```

### 6.6 L1723 설정 슬라이더 트랙
```csharp
var sliderRect = new Rect(x + 200, y + (h - 28) / 2, w - 280, 28);  // 20 → 28
```

### 6.7 L1790~1792 튜토리얼 네비
```csharp
int btnY = (int)(h - bottomSafeY - 132);
int btnH = 84;                          // 60 → 84
int btnW = 220;                         // 180 → 220
int gap  = 24;                          // 16 → 24
```

### 6.8 L2283~2288 결과 버튼
```csharp
int gap  = 28;                          // 16 → 28
int btnH = 96;                          // 64 → 96
int btnW = Mathf.Min(240, (avail - gap) / 2);
```

### 6.9 L1966 갤러리 진행바
```csharp
int barH = 32;                          // 20 → 32
int canvasSize = Mathf.Min(w - 48, 440);// 360 → 440
```

### 6.10 L2073 HUD 목표 진행바
```csharp
int barH = 32;   // 기존 24에서 확대 (게이지 가독성)
```

---

## 7. 교차 레이어 고려 (참고)

- **모션 팀**: 버튼 확대 시 탭 피드백 scale pulse (0.96→1.0 in 120ms) 유지 강도 +10%로 재튜닝 권고.
- **디자인 시스템**: `design_system.md` 토큰 추가: `touchTarget.minKid = 72`, `touchTarget.primaryKid = 96`, `touchTarget.heroCTA = 120`.
- **성능**: GUI.DrawTexture 횟수 동일 — 렌더 비용 영향 무시 가능(<0.1ms).
- **접근성**: 버튼 확대로 Dynamic Type 대응 여유도 확보, Voice Over focus ring 48→56 자연 맞물림.

---

## 8. 검증 체크리스트 (QA)

- [ ] 로비 스테이지 버튼 12개 세로 스크롤 시 엄지 오탐 0건
- [ ] HUD ⏸/🏠 각기 200ms 연속 탭 테스트 — 교차 탭 실패율 < 2%
- [ ] 팔레트 3슬롯 간 드래그 드롭 — 인접 슬롯 오인식 < 1%
- [ ] 결과 재도전/다음 엄지 한손(오른손/왼손 모두) 도달 100%
- [ ] 설정 토글/슬라이더 행 전체 tappable 확인
- [ ] 튜토리얼 3페이지 한손 왕복 30초 이내
- [ ] iPhone SE(2세대, 667h) ~ iPhone 15 Pro Max(932h) 전 기종 safe area 통과

---

## 9. 우선순위 (Sprint 반영 순서)

1. **P0**: `Gap 0.06 → 0.20` (1줄), 로비 btnH 80→120, HUD 버튼 48→80, 결과 btnH 64→96
2. **P1**: 설정 rowH 64→84, 슬라이더 20→28, 튜토리얼 220×84
3. **P2**: 갤러리 barH 32·canvas 440, 뒤로 버튼 96×56 통일
4. **P3**: 디자인 시스템 토큰화, 토큰 기반 리팩터

---

## 10. 결론

**한 줄 요약**: 전 화면 주요 터치 타겟을 **1.3~1.5× 확대**하고 팔레트 Gap을 0.06 → 0.20으로 늘려, 아이 엄지의 평균 타격 반경(72px)과 프리미엄 감각(96px CTA)을 동시에 만족시키는 것이 본 v4 리디자인의 핵심이다.
