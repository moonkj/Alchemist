# UX/UI Review v3 — 실제 빌드 vs. 설계 시스템 갭 분석

> 작성자: UX/UI Senior Designer Teammate
> 작성일: 2026-04-22
> 대상 빌드: `MinimalGameScene.cs` (OnGUI 단일 파일) / iPhone 실기 플레이테스트 직후
> 참조: `design_system.md` v2 · `screen_designs_v2.md` · `motion_design.md` · `ux_design.md`
> 리뷰 범위: Read-only 코드 정적 리뷰 + 유저 보이스("너무 대충 되어있음") 원인 분석 + Write-only 산출물

---

## 1. 요약 (Exec Summary)

- **전체 등급: B-** (기능 완주 가능 · 시각적 완성도는 "내부 프로토타입" 수준)
- **근본 원인 1: 플랫폼 선택** — 모든 UI가 `OnGUI` 기반. design_system v2 의 Pretendard/Inter, Elevation, Radius, Semantic 토큰 **어느 하나도 렌더 불가**. GUI.skin default 폰트 + solid 2×2 텍스처만 사용.
- **근본 원인 2: 와이어프레임 미반영** — screen_designs_v2 §3.1(Prompt Banner yellow border, 팔레트·아이템 row, Preview Toast) 중 **어느 것도 없음**. HUD 는 단순 3행 레이아웃 + 2행 바.
- **근본 원인 3: 스테이지 색 컨텍스트 증발** — `StageConfig.GoalColor` 가 있음에도 진행 바 fill 이 항상 Purple(#9E4EDF) 단색. 스테이지 3·5 를 제외하면 **진행 바 색과 실제 목표 색이 다름 = 유저 혼란 직접 유발**.
- **긴급도**: 유저 피드백 1회차에 "대충"이 언급된 것은 심리적 한계 — 다음 라운드 안에 즉시 착수 TOP3 가 반드시 반영되지 않으면 리텐션 악화. Canvas(UGUI/UI Toolkit) 이주는 Phase 5 로 이연, **현 OnGUI 범위 안에서도 80% 개선 가능**.

---

## 2. 체크리스트 결과 (A~H)

### A. 정보 위계 — **Fail**
- `_title`(22px) ≈ `_goalLabel`(18px) ≈ `_hud`(16px) 로 **상대 배율이 1.1~1.4×** 에 그침. design_system 스케일은 display_xl 48 / heading_2 24 / body 16 로 **2~3× 차이** 필요.
- "Color Mix: Alchemist" 가 로비에서 `_overlayTitle`(48px) 로 정확히 표시되나, 인게임 HUD 의 `_stage.Title`(22px 굵게) 이 목표 프롬프트보다 시각적으로 우세 → "지금 무엇을 해야 하는가" 가 약함.
- 결과 화면: 별(48px) · 점수(22px) · 버튼(22px) 이 거의 동급 — screen_designs_v2 §4 에선 star = heading_1(32), title = heading_2(24), button = body_lg → **현재 버튼이 과도하게 큼**.

### B. 타이포그래피 — **Fail**
- 전부 Unity default font (GUI.skin). 한글 렌더가 기본 fallback 이라 **가중치 가변 불가** — FontStyle.Bold 만으로 Display 800 / Heading 700 / Body 500 을 표현하려 해 모든 글자가 "굵은 한 종류"처럼 보임.
- 점수 "8,420" 같은 숫자가 `_hud`(16) 로 표시 → 성공의 수치가 "턴 수" 와 동일 비중이라 **성취감이 전달되지 않음**.
- 한글 `★`, `🔒`, `▶` 등 이모지/기호가 Unity default font atlas 에 없어 **기기별로 다르게 렌더**될 위험 (이미 iPhone 에서 ☆/★ 모양 미세 어긋남 리포트 가능성).

### C. 컬러 — **Fail**
- `ColorToUnity()` 의 원색 값이 design_system.md §1.1~1.2 의 hex 와 **전부 불일치**.
  - Red: 구현 (0.90, 0.22, 0.27) = #E63845 / 토큰 = #E83B4C (차이 HSL S -2, L +1)
  - Purple: 구현 (0.62, 0.31, 0.87) = #9E4FDE / 토큰 = #9D4EDD (근사하나 v1 계열)
  - Orange/Green/White 도 각각 토큰 대비 L ±3 차이.
- 진행 바 `_barFill` 이 **항상 Purple 고정** — stage 1(Orange), 2/4(Green), 5(White) 에서 바 색 ≠ 목표 색.
- 배경 패널 `#14171F @ 0.92` 는 `surface.dark.canvas #0F1218` 와 유사하나 alpha 가 과해서 보드 상단이 **항상 어둡게 깔림** — 로비/플레이 공통 배경 연출 부재.
- Toast 가 `_overlayTitle` 색(아이보리 #FAF0D2) 만 씀 → **성공/실패/오류가 같은 색**. "STAGE FAILED" 와 "연쇄 3! +90" 이 같은 노란 대제목으로 뜨는 심각한 시맨틱 붕괴.

### D. 레이아웃 & 스페이싱 — **Warn**
- 상단 HUD `topSafe = 50px` 하드코딩 → iPhone 14 Pro Dynamic Island(상 59pt) 침범 가능, SE(상 20pt) 에선 과잉 여백. `Screen.safeArea` 활용 0.
- 하단 힌트 "인접 셀로 드래그 · …" 가 `Screen.height - 50` 고정 → iOS 홈 인디케이터(하 34pt) 와 14~16px 만 띄어져 있어 **홈바 제스처 오조작 위험**.
- 버튼 240×64px 는 44pt 최소 타깃은 만족하나, 결과 화면에서 **버튼 2개 총 폭 = 240+240+16 = 496px** → 390pt 디바이스에서 **화면 밖으로 넘침** (x0 = (390-496)/2 = -53). **실제 기기에서 일부 버튼 우측 잘림 버그**.
- 로비 스테이지 카드 너비 `Mathf.Min(w-60, 420)` → 420pt 상한이라 좁은 iPhone 에서 좌우 30pt, 넓은 iPad 에서 양쪽 여백 과다. gutter 16pt(design_system §3.1) 미준수.
- 결과 오버레이 수직 리듬: -200/-130/-50/-10/+60 → spacing 토큰(4의 배수) 미준수, 70→80→40→70 불규칙.

### E. 피드백/상태 — **Warn**
- Toast `_toastUntil = Time.time + 1.2f` — 페이드 없이 즉시 사라짐. motion_design duration.slow=400ms + ease_in 부재.
- `_inputLocked` 상태를 유저가 인지할 방법 **전무** — 캐스케이드 중 탭이 묵살되는데 시각 힌트 없음. 유저는 "앱이 멈췄나?" 의심.
- 목표 임계값(4/5 · 마지막 1개) 시각 강조 없음 — 바가 선형으로만 채워져 **"거의 다 왔다"** 의 감정적 보상이 빠짐.
- 잘못된 드롭 ("빈 칸"/"혼합 불가") 이 대제목(48px 아이보리) 으로 화면 중하단에 뜸 — 에러를 축하처럼 표시하는 꼴.

### F. 플로우 & 화면 전환 — **Fail**
- Lobby↔Play↔Result 전환 **애니메이션 0** — `_screen = ScreenState.Result` 한 줄로 순간 전환. motion_design §8 "carry-over" 원칙 미준수.
- 튜토리얼 스캐폴딩 없음 — ux_design §1.1(0~30초 Hook) 의 Tutorial Stage 0, 손가락 가이드 Lottie, "당신은 연금술사입니다" 타이틀 전무.
- 재도전/다음 버튼 — 성공 시 우선순위가 "다음" 이어야 하는데 현재 **"재도전"(왼쪽) 이 primary 스타일 없이 같은 굵기** 로 표시됨.

### G. 접근성 — **Fail**
- 색맹 모드 없음 — design_system §7.2 colorblind_patterns 미구현 (빨강/초록 매치에 의존하는 게임이므로 치명적).
- 고대비 모드 없음 — 다크 배경 + 중간 채도 블록으로 저시력 유저 대비 부족.
- 폰트 크기 스케일러 없음 — §7.3 5단계 토글 미구현.
- 한손 도달성: 결과 버튼이 `h/2 + 60` = 화면 세로 중앙 근처 → screen_designs_v2 §4(하단 Hot zone) 와 어긋남. 엄지 Warm 구역.

### H. 브랜딩 / 톤 — **Warn**
- 게임 중 블록 자체는 jelly breath + 스플래시 파티클 + 화면 쉐이크로 **"물감 연금술" 느낌 70% 달성**.
- 그러나 UI 레이어에는 장식 0 — 로비 배경이 완전 단색, 상단바 아이콘/로고 없음, 스테이지 카드도 평범한 사각버튼. 앱처럼 보이지 않음(웹 디버그 메뉴 같음).
- 스테이지명 "1. 노을의 주홍" 의 시적 톤이 `_title` 굵은 22px 로 **건조하게** 처리됨.

---

## 3. 개선 우선순위 TOP 10

| # | 문제 | 예상 개선 (구체) | 난이도 |
|---|---|---|---|
| 1 | 진행 바가 항상 Purple — 스테이지 목표 색 미반영 | `_barFill` 을 매 프레임 `MakeSolidTexture(ColorToUnity(_stage.GoalColor))` 로 교체. 하얀 목표는 neutral 강조. | **S** |
| 2 | 결과 버튼 화면 밖 넘침 (240+240+16 > 390) | 버튼 폭을 `(w-48)/2 clamped max 200` 로 동적 산출, gap 16 유지. | **S** |
| 3 | Toast 색이 성공/실패 모두 아이보리 | `_toast` 외 `_toastKind` (success/warn/info) 추가, 색을 `semantic.success #22C55E / warning #F59E0B / danger #EF4444` 로 분기. 페이드아웃 0.35s. | **S** |
| 4 | 텍스트 스케일이 평평함 (22/18/16) | display 40 / heading 24 / body 16 / caption 12 **4단계로 재배열**. 점수는 heading_1(32 볼드 + 숫자 tabular). | **S** |
| 5 | `topSafe=50` 고정 → 노치·DI 미대응 | `Screen.safeArea` 읽어 `topSafe = Screen.height - safeArea.yMax` 로 자동. 하단 힌트 Y 도 `safeArea.y + 8`. | **M** |
| 6 | 입력 잠금을 유저가 모름 | `_inputLocked` 중 보드 위 0.35α dim layer + 프롬프트 옆에 "연쇄 처리 중…" caption. | **M** |
| 7 | 결과 버튼 우선순위 없음 | 성공 시 "다음 ▶" 을 `semantic.success`/primary 스타일(더 큰 높이 64, 굵은 외곽), "재도전" 을 ghost(투명 + 테두리) 로 demote. | **M** |
| 8 | 화면 전환 전무 | Lobby↔Play↔Result 진입 시 180ms ease_out 알파 페이드 + 10px slide-up 을 `OnGUI` 내 `GUI.color.a` 보간으로 구현. | **M** |
| 9 | 로비 브랜딩 장식 0 | 타이틀 위에 세로 prism gradient 바(4×160) + 작은 "🎨" 장식, 배경에 매우 옅은 물감 텍스처(절차생성 splash 3~4개 정지 상태). | **M** |
| 10 | 색맹/고대비 접근성 부재 | 설정 진입점이 없으므로 로비 우상단 ⚙ 버튼 1개 추가 → 토글 2종(색맹 패턴 / 큰 글자). 패턴은 블록 sprite 위 체크/스트라이프 오버레이. | **L** |

---

## 4. 즉시 착수 권고 3건 (리더 요청)

### 1) 진행 바 색을 GoalColor 로 (#1, S난이도)
변경 파일: `MinimalGameScene.cs` `EnsureStyles()` 고정 `_barFill` 제거 → `DrawPlayingHud()` 내부에서 스테이지별 동적 생성/캐시. 유저가 "무엇을 만드는 중인가" 를 바 하나로 즉시 이해.

### 2) 결과 화면 버튼 오버플로 수정 + 우선순위 (#2, #7, S+M)
iPhone 에서 실제로 우측 잘림이 발생할 위험 — **버그성**. 동시에 "다음 ▶" 을 primary로 승격해 성공 감정 흐름을 살린다.

### 3) Toast 컬러 시맨틱 + 페이드 (#3, S)
"혼합 불가" 와 "연쇄 3! +90" 이 같은 노란 대제목인 건 **유저가 대충 만들었다 느끼는 가장 구체적 증거**. 3색 분기 + 0.35s 페이드로 즉각적 체감 개선.

> 위 3건은 **Canvas 이주 없이 OnGUI 범위 안에서 가능**, 작업량 합계 < 2h 예상.

---

## 5. Phase 5 이연 항목 (Scene 기반 Canvas 이주 필요)

다음은 OnGUI 로는 근본 해결 불가 — UGUI Canvas 또는 UI Toolkit 도입 후 착수:

- **Pretendard/Inter SDF 도입** (TMP 필수) — design_system §2 전 스케일, letter-spacing, Variable weight.
- **Elevation 시스템** — 9-slice soft-shadow 프리팹 또는 UI Toolkit box-shadow. 현재 OnGUI 로는 그림자 렌더 불가.
- **Pre-Game Loadout Modal** — 팔레트 슬롯 3 + 아이템 3, 포커스 트랩 필요.
- **Prompt Banner (yellow border radius lg)** — 현재는 라벨만 존재. 정식 배너는 radius·border·그라디언트 필요.
- **Onboarding 3단계** (Welcome / Tutorial Stage 0 / Nickname) — Lottie 가이드 포함.
- **갤러리 / 랭킹 / 상점 / 배지** — 현재 전혀 없음. 9 루트 화면 중 4 루트 미구현.
- **색맹 패턴 오버레이** (SpriteMask) — alpha-only R8 패턴 텍스처 8종.
- **Reduce Motion 토글** — OS 설정 감지 + duration × 0.3.
- **Safe Area 컴포넌트** — UGUI `SafeAreaFitter` 로 일괄 적용.
- **한손 도달성 맵** — 모든 CTA 를 Hot zone (화면 하 55~90%) 으로 재배치. OnGUI 에선 좌표 하드코딩이라 매번 수정 필요 → Canvas anchor 가 필수.

---

## 6. 결론

현 빌드는 **"게임 로직은 완성, UX 레이어는 와이어프레임 이전 상태"**. 유저의 "대충" 피드백은 시각적 완성도보다 **정보 전달 실패**(진행 색·성공/실패 구분·입력 상태·임계값)에서 기인. 위 TOP3 를 다음 라운드에 반영하면 B- → **B+** 로 즉시 상승. Canvas 이주 후 디자인 토큰 전면 적용 시 A- 진입 가능.

문서 끝.
