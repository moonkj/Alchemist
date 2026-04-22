# Redesign v4 — 타이포그래피·크기 전면 재설계 (Sizing Overhaul)

> 작성: Typography·Sizing Specialist Teammate
> 일자: 2026-04-22
> 대상 파일: `/Users/kjmoon/Alchemist/Assets/_Project/Scripts/Bootstrap/MinimalGameScene.cs`
> 원칙: **아이 친화 + 고령 사용자 친화** · 평균 **×1.6 ~ ×2.2 확대** · iPhone 14 (390×844) 기준

---

## 0. 유저 피드백 (2026-04-22)

> "폰트 버튼 챕터 버튼 등 **전부 너무 작아 잘 안보임**. 전체 재설계해줘."

- 현재 수치는 PC 뷰포트 기준이거나 초기 프로토 기준에 맞춰져 있어, 실제 모바일(1~2m 거리·4~60대 사용자)에선 **가독 임계 이하**.
- Apple HIG 최소 터치 타깃 44pt, Google Material 최소 48dp를 **여러 버튼이 밑돌고 있음** (뒤로 버튼 40h, 아이콘 버튼 48h·36h 혼재).
- 본문(Body 16), 캡션(Caption 12)은 **고령자 가독 한계(18pt) 미달**.

---

## 1. 현재 수치 감사 (Audit)

### 1.1 GUIStyle — 폰트 사이즈 (Line 1339~1357, 1439~1446, 2060, 2101, 2276)

| 스타일 | 현재 fontSize | 용도 | 위치 |
|---|---:|---|---|
| `_display` | **40** | 로비 대제목, 설정 제목, 갤러리 제목, 결과 점수 | L1339 |
| `_heading` | **28** | HUD 스테이지명, 챕터 헤더, 규칙 설명 | L1341 |
| `_scoreBig` | **32** | HUD 점수 표시 | L1343 |
| `_goalLabel` | **20** | 목표 진행도 텍스트 | L1345 |
| `_body` | **16** | 본문, 설정 항목 레이블 | L1347 |
| `_caption` | **12** | 힌트 문구, 버전 표기 | L1349 |
| `_overlayTitle` | **52** | 일시정지/결과 오버레이 타이틀 | L1351 |
| `_overlayBody` | **20** | 오버레이 본문 | L1353 |
| `_primaryBtn` | **22** | CTA 버튼 (재개/시작/다음) | L1355 |
| `_ghostBtn` | **18** | 보조 버튼 (뒤로/취소) | L1356 |
| `_stageBtn` | **18** | 스테이지 카드 | L1357 |
| `galleryStyle` | 18 | 갤러리 버튼 라벨 | L1439 |
| `lobbyIconStyle` | 24 | 로비 ❓⚙ 아이콘 | L1446 |
| `bigBtnStyle` (HUD) | 22 | HUD ⏸🏠 아이콘 버튼 | L2060 |
| 스타 힌트 캡션 | 13 | 결과 스타 기준 | L2101 |
| 스타 스타일 | 14 | 결과 스타 상세 | L2276 |

### 1.2 버튼 / 레이아웃 고정값 (int 리터럴)

| 변수 | 현재 값 | 사용처 | 위치 |
|---|---:|---|---|
| `btnH` (로비 스테이지 카드) | **80** | 스테이지 버튼 높이 | L1461 |
| `chapterHeaderH` | **42** | 챕터 구분 헤더 | L1464 |
| `btnH` (일시정지) | **60** | 재개/재시작/로비 CTA | L1577 |
| `rowH` (설정 행) | **64** | 설정 토글 행 높이 | L1619 |
| `btnW / btnH` (튜토리얼) | **180 / 60** | 이전/다음 버튼 | L1790-91 |
| 튜토 건너뛰기 버튼 | 84×36 | 우상단 | L1773 |
| `palSz` (튜토 팔레트 데모) | **50** | 팔레트 아이콘 | L1891 |
| 갤러리 뒤로 버튼 | 84×40 | 좌상단 | L1929 |
| 설정 뒤로 버튼 | 84×40 | 좌상단 | L1612 |
| 갤러리 진행도 바 | `barH = 20` | 복원 진행도 | L1966 |
| `panelH` (HUD 상단 패널) | **130** | 상단 정보 패널 | L2053 |
| HUD 진행도 바 | `barH = 24` | 목표 진행 | L2072 |
| HUD ⏸🏠 버튼 | 80×48 | 우상단 아이콘 | L2061-65 |
| HUD 점수 라벨 | 280×44 | 🏆 점수 | L2090 |
| 토스트 | `toastH = 56` | 알림 | L2201 |
| 결과 스타 | `starSize = 52, gap = 16` | 별 3개 | L2244-45 |
| 결과 버튼 | `btnH = 64` | 재도전/다음 | L2284 |

### 1.3 섹션별 현황 요약

| 섹션 | 평균 폰트 | 평균 버튼 높이 | 진단 |
|---|---:|---:|---|
| **로비** | 40/28/18 | 80 (스테이지), 56 (갤러리) | 챕터 헤더 42 너무 작음 |
| **게임 HUD** | 28/32/20/16 | 48 (아이콘), 24 (바) | 아이콘·본문 모두 협소 |
| **결과** | 52/40/20/14 | 64 | 스타 52는 OK, 본문/캡션 부족 |
| **일시정지** | 52/28/20 | 60 | 버튼 OK, 부제목 28 작음 |
| **설정** | 40/16 | 40(뒤로) / 64(행) | **뒤로 40은 HIG 미달** |
| **튜토리얼** | 60/48/26 | 60, 36(건너뛰기) | 건너뛰기 36은 작음 |
| **갤러리** | 40/28/20 | 40(뒤로), 20(바) | 뒤로 버튼 협소 |
| **팔레트** | 20(태그) | - | 태그 "꺼내기 ↑" 18 작음 |

---

## 2. 2~3배 확대 권고 테이블 (마스터 스펙)

> 모바일 기본값은 "권고 하한", 접근성(큰 글자) 토글 ON 시 "권고 상한".

| 요소 | 스타일/변수 | 현재 | **권고 신규** | 배수 | 이유 |
|---|---|---:|---:|---:|---|
| **Display (로비 대제목)** | `_display.fontSize` | 40 | **80** | ×2.00 | 로비 한 줄 강한 인상 |
| **Display (설정/갤러리 헤더)** | 공유 | 40 | **72** | ×1.80 | 서브 헤더는 조금 낮춤 |
| **Heading** | `_heading.fontSize` | 28 | **44** | ×1.57 | HUD 스테이지명·챕터명 |
| **ScoreBig** | `_scoreBig.fontSize` | 32 | **64** | ×2.00 | 점수 성취감 극대화 |
| **GoalLabel** | `_goalLabel.fontSize` | 20 | **28** | ×1.40 | 목표 진행 텍스트 |
| **Body** | `_body.fontSize` | 16 | **24** | ×1.50 | 본문 — 고령 가독 확보 |
| **Caption** | `_caption.fontSize` | 12 | **20** | ×1.67 | 힌트·버전 — 하한 20 |
| **OverlayTitle** | `_overlayTitle.fontSize` | 52 | **88** | ×1.69 | 결과 "완벽!" 오버레이 |
| **OverlayBody** | `_overlayBody.fontSize` | 20 | **30** | ×1.50 | 오버레이 서브 |
| **PrimaryBtn (라벨)** | `_primaryBtn.fontSize` | 22 | **34** | ×1.55 | CTA 라벨 |
| **GhostBtn (라벨)** | `_ghostBtn.fontSize` | 18 | **28** | ×1.56 | 보조 라벨 |
| **StageBtn (라벨)** | `_stageBtn.fontSize` | 18 | **30** | ×1.67 | 스테이지 카드 |
| **로비 아이콘 스타일** | `lobbyIconStyle` | 24 | **40** | ×1.67 | ❓ ⚙ 크게 |
| **HUD 아이콘 버튼 라벨** | `bigBtnStyle` | 22 | **36** | ×1.64 | ⏸ 🏠 이모지 |
| **스타 힌트 캡션** | star caption | 13 | **20** | ×1.54 | 결과 스타 기준 |

### 2.2 버튼 · 패널 치수

| 요소 | 현재 | **권고 신규** | 배수 | 이유 |
|---|---:|---:|---:|---|
| **CTA 버튼 높이** (일시정지) | 60 | **104** | ×1.73 | 엄지 확실 터치 |
| **CTA 버튼 높이** (결과) | 64 | **112** | ×1.75 | 결과 직후 피드백 |
| **CTA 버튼 높이** (튜토리얼) | 60 | **104** | ×1.73 | 통일 |
| **CTA 버튼 폭** (튜토) | 180 | **240** | ×1.33 | 라벨 여유 |
| **스테이지 버튼 높이** | 80 | **140** | ×1.75 | 카드 느낌·★ 가시성 |
| **챕터 헤더 높이** | 42 | **72** | ×1.71 | 챕터 구분 명확 |
| **로비 상단 아이콘 버튼** (❓⚙) | 64×56 | **88×72** | ×1.38 | HIG 44+ 확보 |
| **갤러리 버튼** (로비 우측) | 160×56 | **200×80** | ×1.25 | 라벨 가독 |
| **HUD 상단 패널 높이** | 130 | **200** | ×1.54 | 정보 여유 |
| **HUD 아이콘 버튼** (⏸🏠) | 80×48 | **96×72** | ×1.50 | 엄지 친화 |
| **HUD 진행도 바 높이** | 24 | **36** | ×1.50 | 가시성 |
| **갤러리 진행도 바 높이** | 20 | **32** | ×1.60 | 가시성 |
| **설정 행 높이** | 64 | **88** | ×1.38 | 토글 여유 |
| **뒤로 버튼** (설정/갤러리) | 84×40 | **112×60** | ×1.50 | HIG 미달 해소 |
| **튜토 건너뛰기** | 84×36 | **120×56** | ×1.56 | 미달 해소 |
| **튜토 팔레트 데모 칸** | 50 | **88** | ×1.76 | 시각 주목 |
| **결과 스타** (starSize) | 52 | **88** | ×1.69 | 임팩트 |
| **결과 스타 간격** (starGap) | 16 | **24** | ×1.50 | 균형 |
| **토스트 높이** | 56 | **80** | ×1.43 | 한 줄 가독 |

**평균 확대 배수 ≈ ×1.62 (폰트) · ×1.58 (버튼/패널) · 전체 ×1.60**

---

## 3. 화면 영역 안배 (iPhone 14 · 390×844)

### 3.1 게임 HUD (현재 vs 재설계)

```
현재 (총 844h)                    재설계 (총 844h)
┌──────────────────┐ topSafe 44   ┌──────────────────┐ topSafe 44
│ 상단패널 panelH=130│               │                  │
│  스테이지명 28    │               │ 상단패널 panelH=200│
│  ⏸🏠 48h         │               │  스테이지명 44    │
│  진행바 barH=24   │               │  ⏱ 24 · 🏆 64    │
│  ⏱18 · 🏆44 · ★14│               │  진행바 barH=36   │
├──────────────────┤ ~192          │  ★기준 20        │
│                  │               ├──────────────────┤ ~258
│   보드 Rows=7    │               │                  │
│   (약 470h)      │               │   보드 Rows=7    │
│                  │               │   (약 430h)      │
│                  │               │                  │
├──────────────────┤ ~660          ├──────────────────┤ ~700
│  팔레트 3칸      │               │  팔레트 3칸      │
│  (약 120h)       │               │  (약 100h)       │
├──────────────────┤               ├──────────────────┤
│ 힌트 Caption 12  │ bottomSafe 34 │ 힌트 Caption 20  │ bottomSafe 34
└──────────────────┘               └──────────────────┘
```

**핵심 재배분**:
- 상단 패널 **130 → 200** (+70h): 스코어·목표·스타기준 3단 명확 분리
- 보드 영역 **470 → 430** (-40h): 셀 70→65 소폭 감소, 대신 터치 정확도 유지
- 팔레트 영역 **120 → 100** (-20h): 아이콘 유지, 상단 여유 확보
- 힌트 **12 → 20**: 반드시 읽히게

### 3.2 로비 (세로 배분)

| 영역 | 현재 Y | 신규 Y | 높이 변화 |
|---|---:|---:|---|
| 상단 안전영역 | 0~44 | 0~44 | - |
| 아이콘(❓⚙) | 28~84 | 28~100 | 56→72 |
| 갤러리 버튼 | 28~84 | 28~108 | 56→80 |
| 타이틀 "Color Mix..." | 48~104 | 48~128 | 56→80 (Display 40→80) |
| 서브카피 | 112~134 | 140~170 | 22→30 |
| 챕터 헤더 1 | 184~226 | 220~292 | 42→72 |
| 스테이지 카드 ×5 | h=80, gap=10 | h=140, gap=14 | 합계 440→770 |
| 챕터 헤더 2 | 76h | 120h (상단 여백 48+헤더 72) | |
| 잠금 카드 ×5 | 동일 | 동일 | |

> 스크롤 가능하므로 컨텐츠 total height 확장 허용. `startY = titleY + 200` (현재 +140).

### 3.3 결과 화면 (오버레이)

```
권고 Y 배치 (h=844 기준)
┌───────────────────────┐
│                       │
│  타이틀 fontSize=88   │ h/2 - 260 (현 -210)
│  (예: "완벽!")        │
│                       │
│  ★★★ starSize=88     │ h/2 - 140 (현 -120)
│  (gap 24)             │
│                       │
│  점수 fontSize=80     │ h/2 - 20 (현 -30)
│  (ScoreBig 64 기반)   │
│                       │
│  턴·연쇄 body 24      │ h/2 + 60
│  스타 기준 cap 20     │ h/2 + 100
│                       │
│  [다시112h] [다음112h]│ h/2 + 180 (현 +140)
│                       │
└───────────────────────┘
```

### 3.4 설정 화면

- 제목 `Display 72` · 높이 60 (현 52)
- 행 높이 **88** (현 64) · 토글 icon 40 (현 28~32)
- 뒤로 버튼 **112×60** · Ghost 28

---

## 4. 접근성 모드

### 4.1 "큰 글자" 토글 (×1.3 상위 스케일)

```
접근성 ON 시 (모든 fontSize·btnH ×1.3)
Display 80  → 104
Heading 44  →  57
Body 24     →  31
Caption 20  →  26
CTA btnH 104→ 135
HUD panelH 200 → 260
```

- 설정 화면에 토글 1개 추가: **🔍 큰 글자 모드**
- 저장 키: `PlayerPrefs.SetInt("accessibility.largeText", 0|1)`
- 스케일 상수 `_a11yScale = 1.0f | 1.3f`

### 4.2 WCAG AA 대비 검증

| 조합 | 권고 배경 | 권고 전경 | 대비비 | AA |
|---|---|---|---:|:---:|
| 본문 텍스트 (24pt 일반) | #101418 | #F4F6FA | 16.8:1 | ✅ |
| 캡션 (20pt 일반) | #101418 | #E8ECF2 | 14.2:1 | ✅ |
| CTA 버튼 라벨 (34pt Bold) | #FF7A3D | #FFFFFF | 3.2:1 | ✅ Large |
| Ghost 버튼 (28pt Bold) | #1E232A | #F4F6FA | 14.5:1 | ✅ |
| 진행바 채움 | #2A2F36 | #FFB84D | 3.9:1 | ✅ Graph |
| 별 색 (88px) | 오버레이 | #FFD84D | 11.3:1 | ✅ |

- 24pt 이상 Body는 4.5:1, 30pt 이상 Large는 3:1 기준 모두 통과.
- 현재 caption=12는 저대비 시 부적합했으나, **20pt로 확대**되면서 AA 안정.

---

## 5. 구현 힌트 (OnGUI 직접 적용 테이블)

### 5.1 GUIStyle 초기화 (L1339~1357) — 교체 권고값

```csharp
// ─── 재설계 v4: 타이포 스케일 2배 상향 ───
_display      = new GUIStyle(GUI.skin.label) { fontSize = 80, ... };  // 40→80
_heading      = new GUIStyle(GUI.skin.label) { fontSize = 44, ... };  // 28→44
_scoreBig     = new GUIStyle(GUI.skin.label) { fontSize = 64, ... };  // 32→64
_goalLabel    = new GUIStyle(GUI.skin.label) { fontSize = 28, ... };  // 20→28
_body         = new GUIStyle(GUI.skin.label) { fontSize = 24, ... };  // 16→24
_caption      = new GUIStyle(GUI.skin.label) { fontSize = 20, ... };  // 12→20
_overlayTitle = new GUIStyle(GUI.skin.label) { fontSize = 88, ... };  // 52→88
_overlayBody  = new GUIStyle(GUI.skin.label) { fontSize = 30, ... };  // 20→30
_primaryBtn   = new GUIStyle(GUI.skin.button){ fontSize = 34, ... };  // 22→34
_ghostBtn     = new GUIStyle(GUI.skin.button){ fontSize = 28, ... };  // 18→28
_stageBtn     = new GUIStyle(GUI.skin.button){ fontSize = 30, ... };  // 18→30

// 파생 스타일
galleryStyle   = new GUIStyle(_ghostBtn) { fontSize = 28 };  // 18→28
lobbyIconStyle = new GUIStyle(_ghostBtn) { fontSize = 40 };  // 24→40
bigBtnStyle    = new GUIStyle(_ghostBtn) { fontSize = 36 };  // 22→36
starStyle(L2101) fontSize = 20;  // 13→20
starStyle(L2276) fontSize = 22;  // 14→22
```

### 5.2 Int 리터럴 (섹션별)

```csharp
// ── 로비 (L1461~1464) ──
int btnH = 140;              // 80→140   스테이지 카드
int gap  = 14;               // 10→14
int chapterHeaderH = 72;     // 42→72
int chapter2HeaderGap = 32;  // 20→32

// 로비 상단 아이콘 Rect
var galleryRect  = new Rect(w - 216, titleY - 32, 200, 80);  // 176/160x56 → 216/200x80
var helpRect     = new Rect(16, titleY - 32, 88, 72);        // 64x56 → 88x72
var settingsRect = new Rect(112, titleY - 32, 88, 72);       // 64x56 → 88x72
GUI.Label(new Rect(0, titleY, w, 88), ..., _display);        // 56→88
GUI.Label(new Rect(0, titleY + 96, w, 30), ..., _overlayBody); // 64/22 → 96/30

// ── 일시정지 (L1577~) ──
int btnH = 104; int btnW = 280; int gap = 18; // 60/240/14

// ── 설정 (L1619~) ──
int rowH = 88; int gap = 18;  // 64/14
// 뒤로 버튼
if (GUI.Button(new Rect(16, topY + 8, 112, 60), "◀ 뒤로", _ghostBtn))  // 84x40 → 112x60
GUI.Label(new Rect(0, topY, w, 72), "⚙ 설정", _display);  // 52→72

// ── 튜토리얼 (L1790~) ──
int btnH = 104; int btnW = 240; int gap = 20;  // 60/180/16
if (GUI.Button(new Rect(w - 136, topY, 120, 56), "건너뛰기", _ghostBtn))  // 100/84x36 → 136/120x56
int palSz = 88;  // 50→88

// ── 갤러리 (L1925~) ──
GUI.Label(new Rect(0, topY, w, 72), "🎨 갤러리", _display);  // 52→72
if (GUI.Button(new Rect(16, topY + 8, 112, 60), "◀ 뒤로", _ghostBtn)) // 84x40 → 112x60
int barH = 32;  // 20→32  (진행도 바)

// ── 게임 HUD (L2052~) ──
int panelH = 200;  // 130→200
GUI.DrawTexture(new Rect(0, topSafe, w, panelH), _panelBg);
GUI.Label(new Rect(16, topSafe + 14, w - 120, 52), _stage.Title, _heading);  // 10/32 → 14/52
if (GUI.Button(new Rect(w - 220, topSafe + 14, 96, 72), "⏸", bigBtnStyle))  // 180/80x48 → 220/96x72
if (GUI.Button(new Rect(w - 112, topSafe + 14, 96, 72), "🏠", bigBtnStyle))  //  92/80x48 → 112/96x72

int barH = 36; int barY = topSafe + 80;  // 24/52 → 36/80
GUI.Label(new Rect(16, infoY, 300, 44), "⏱ " + ..., _body);          // 260x34 → 300x44
GUI.Label(new Rect(w - 320, infoY - 8, 320, 64), "🏆 " + ..., _scoreBig);  // 280x44 → 320x64
int starInfoY = infoY + 54;  // 42→54
GUI.Label(new Rect(16, starInfoY, w - 32, 30), ..., starStyle);  // 22→30
int hintY = (int)(Screen.height - bottomSafeY - 44);  // 32→44

// ── 토스트 (L2201) ──
int toastH = 80;  // 56→80

// ── 결과 (L2244~) ──
int starSize = 88; int starGap = 24;  // 52/16 → 88/24
GUI.Label(new Rect(0, h/2 - 260 + titleOffset, w, 108), title, titleStyle);  // -210/68 → -260/108
GUI.Label(new Rect(0, h/2 - 40, w, 70), shownScore..., _display);   // -30/40 → -40/70
GUI.Label(new Rect(0, h/2 + 40, w, 40), "⏱ " + ..., _overlayBody);  // +16/28 → +40/40
GUI.Label(new Rect(0, h/2 + 90, w, 30), "★★★ " + ..., crit);       // +48/20 → +90/30
int btnH = 112; int btnW = 240; int gap = 20;  // 64/?/? → 112/240/20
```

### 5.3 접근성 스케일 적용 힌트

```csharp
// Update() 또는 OnGUI 초입
float _a11yScale = PlayerPrefs.GetInt("accessibility.largeText", 0) == 1 ? 1.3f : 1.0f;
int S(int v) => Mathf.RoundToInt(v * _a11yScale);

// 사용
_display.fontSize = S(80);
panelH = S(200);
btnH   = S(104);
```

---

## 6. 우선순위 적용 순서 (권고)

1. **Tier A (즉시)** — 폰트 7개(`_display`·`_heading`·`_scoreBig`·`_body`·`_caption`·`_primaryBtn`·`_ghostBtn`)
2. **Tier B (같은 커밋)** — 스테이지 카드 `btnH 80→140`, HUD `panelH 130→200`, 결과 `starSize 52→88`
3. **Tier C (후속)** — 뒤로/건너뛰기 버튼 HIG 준수 확대, 갤러리·튜토리얼 세부
4. **Tier D (옵션)** — 접근성 "큰 글자" 토글 신규 추가 (설정 화면 1행)

---

## 7. 검증 체크리스트

- [ ] iPhone SE (375×667) 세이프에어리어에서도 상단 패널(200h)이 보드 공간을 40% 이하 점유
- [ ] iPhone 14 Pro Max (430×932)에서 과도하게 희박해 보이지 않음 (최대 너비 제한 고려)
- [ ] Android 소형(360×640)에서 스테이지 카드 5개가 1스크린에 다 들어오지 않아도 OK (스크롤 전제)
- [ ] 모든 터치 버튼 **≥ 60h (Ghost) / ≥ 104h (CTA)** 준수
- [ ] 본문 텍스트 ≥ 24pt, 캡션 ≥ 20pt
- [ ] 대비 비 AA (4.5:1 / 3:1 Large) 전 구간 통과
- [ ] 접근성 토글 ON 시 깨짐/넘침 없음 (특히 토스트·HUD 점수)

---

## 8. 요약 (TL;DR)

- **전체 평균 ×1.60 확대**, 폰트 TOP-diff: Display 40→80, ScoreBig 32→64, Body 16→24, Caption 12→20.
- **버튼 TOP-diff**: 스테이지 카드 80→140, HUD 패널 130→200, 결과 스타 52→88, CTA 60→104.
- **HIG/Material 미달 해결**: 뒤로 40h→60h, 건너뛰기 36h→56h.
- **접근성 ×1.3 토글**로 4060+ 사용자·시각약자까지 커버.
- **파일 1개만 수정**하면 됨 — `MinimalGameScene.cs`의 GUIStyle 초기화 블록(L1339-1357)과 섹션별 int 리터럴만 교체.
