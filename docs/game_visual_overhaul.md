# 게임 비주얼 오버홀 마스터 플랜 (Color Mix: Alchemist)

> **작성자**: 크리에이티브 디렉터 + 게임 UI Teammate
> **작성일**: 2026-04-22
> **목적**: "메모장 같다"는 유저 피드백 → "Candy Crush 급" 프로덕션 UX 로 전환
> **참조**: Candy Crush Saga / Block Puzzle / Royal Match / Toon Blast / Best Fiends

---

## 0. 한 줄 요약
**유니티 OnGUI 한계 안에서도, "프로시저얼 Texture2D 아이콘 + 카드/리본 프레임 + 그라디언트 팔레트 + 모듈화된 화면 레이아웃"으로 게임형 UX 를 전면 재구축한다.**

---

## 1. 근본 원인 — 왜 현재 UX 가 "엉망" 인가

### 1.1 이모지 미렌더 (🏠⚙❓⏸🎨🏆 → □)
- Unity 기본 폰트는 이모지 글리프 미포함 → 토푸(□) 박스로 표시.
- 이모지를 제거하지 않으면 **"텍스트가 짤린다"는 인상** 을 유발.

### 1.2 OnGUI 플랫 사각형
- `GUI.Box`, `GUI.Button` 기본 스타일은 회색 플랫 rect → 메모장 느낌.
- 그라디언트, 둥근 모서리, 내부 섀도우, 하이라이트가 전무 → 입체감 제로.

### 1.3 컬러 팔레트의 어두움·희소성
- 현재 배경: `#2E1A4F` 단색 → 밀도 부족.
- 카드/패널이 없어 **"화면이 비어 보임"**.
- 액센트(금색/민트) 가 거의 사용되지 않아 눈이 머물 곳이 없음.

### 1.4 MinimalGameScene 화면별 현상
| 화면 | 현재 문제 |
|---|---|
| Lobby | 12개 카드 세로 리스트 → 단조, 게임 맵 느낌 제로 |
| Playing | 상단 패널·보드·팔레트가 분리된 "창" 처럼 떠 있음 |
| Result | 블랙 오버레이 + 플랫 별점 → 축제감 없음 |
| Paused | 단순 반투명 오버레이 + 버튼 |
| Tutorial | 텍스트 위주, 일러스트 없음 |
| Settings | 플랫 토글, 온/오프 시각 구분 약함 |
| Gallery | 단순 grid, 포스터감 없음 |
| **첫 화면** | **없음** (튜토리얼이 첫 진입 = 몰입감 파괴) |

---

## 2. 비주얼 랭귀지 (Visual Language)

### 2.1 Mood
**"마법 조제실(Alchemist's Lab) + 색채 놀이터(Color Playground)"**
- 저녁 조명 아래 유리병이 반짝이는 느낌
- 블록은 말랑한 젤리/치즈 같은 양감

### 2.2 컬러 팔레트
| 역할 | HEX | 용도 |
|---|---|---|
| Bg Top | `#2E1A4F` | 배경 상단 (밤하늘) |
| Bg Bottom | `#6D3A6B` | 배경 하단 (마젠타 석양) |
| Card | `#8B6BC7` | 라벤더 카드 프레임 |
| Card Hi | `#B59BE2` | 카드 상단 하이라이트 스트라이프 |
| Play Field | `#2A1E52` | 어두운 보라 (도트 패턴 체커) |
| Play Dot | `#3A2E66` | 도트 패턴 명도차 |
| Gold | `#FFD93D` | 별/코인/왕관 |
| Mint | `#7EE8C6` | 성공/확인 |
| Coral | `#FF7A7A` | 경고/닫기 |
| Text Main | `#FFFFFF` | 흰 텍스트 |
| Text Sub | `#D8CCEE` | 연보라 서브 텍스트 |

### 2.3 타이포그래피
- **타이틀**: Unity 기본 `fontSize: 42, fontStyle: Bold` + `letterSpacing` 기본
- **HUD 숫자**: `fontSize: 32, Bold` (점수/턴)
- **본문**: `fontSize: 18, Normal`
- **버튼 라벨**: `fontSize: 20, Bold`
- 폰트 파일은 Unity TMP 기본. 한글 전용 웨이트는 Phase 2.

### 2.4 형태 랭귀지
- **카드**: 반경 24px 둥근 사각. 상단 8px 하이라이트 스트라이프.
- **버튼 Primary**: 라벤더→마젠타 세로 그라디언트, 반경 18px, 하단 2px 섀도우.
- **버튼 Ghost**: 투명 + 흰 테두리 2px.
- **리본 HUD**: 가로 리본, 좌우 끝 삼각 꼬리, 중앙 볼록.
- **블록**: 기존 SDF 둥근 사각 유지 + 상단 하이라이트 강화, 하단 미묘한 쉐도우.

---

## 3. 이모지 퇴출 전략 (P0 차단 항목)

### 3.1 원칙
1. **이모지 전면 제거**. Unicode 기본 한글/ASCII 만 사용.
2. 아이콘이 필요하면 **프로시저얼 `Texture2D` 32×32** 로 코드에서 그려 `GUI.DrawTexture` 렌더.
3. 텍스트 라벨로 충분한 경우 (예: "점수", "턴") 라벨만 사용.

### 3.2 매핑 테이블
| 기존 이모지 | 위치 | 대체안 |
|---|---|---|
| 🏠 | 좌상단 | 프로시저얼 `IconTex.Home` (삼각지붕+사각몸체) |
| ⚙ | 우상단 | 프로시저얼 `IconTex.Gear` (원+6톱니) |
| ❓ | 도움말 | `"?"` 글자 (기본 폰트 지원) |
| ⏸ | 일시정지 | 프로시저얼 `IconTex.Pause` (두 평행 수직바) |
| ▶ | 재생 | 프로시저얼 `IconTex.Play` (삼각형) |
| 🎨 | 갤러리 | 프로시저얼 `IconTex.Palette` (반원+점3) |
| 🏆 | 점수 라벨 | 라벨 `"점수"` + 숫자 (아이콘 생략) |
| ⏱ | 턴 라벨 | 라벨 `"턴"` + `"5/12"` |
| ★ | 별점 | 프로시저얼 `IconTex.Star` (5각 별, 금색) |
| 👑 | 크라운 | 프로시저얼 `IconTex.Crown` (사각+3스파이크) |
| 🎉🌊✨ | 결과/효과 | 한글 텍스트 `"완주!"`, `"복원!"`, 파티클 대체 |
| ✖ | 닫기 | 프로시저얼 `IconTex.Close` (X 두 선) |

### 3.3 구현 힌트 — `IconTex` 정적 캐시
```
public static class IconTex {
    public static readonly Texture2D Gear   = BuildGear(32);
    public static readonly Texture2D Home   = BuildHome(32);
    public static readonly Texture2D Pause  = BuildPause(32);
    public static readonly Texture2D Play   = BuildPlay(32);
    public static readonly Texture2D Palette= BuildPalette(32);
    public static readonly Texture2D Star   = BuildStar(32, gold);
    public static readonly Texture2D Crown  = BuildCrown(32, gold);
    public static readonly Texture2D Close  = BuildClose(32);
    // BuildXxx(size) → Color32[] pixels → SetPixels32 → Apply
}
```
- 앱 로드 시 1회 생성, 영구 캐시. GC 압력 없음.
- 각 Build 함수는 `Color32[size*size]` 를 0초기화 → 수학적으로 도형 픽셀만 색칠.

---

## 4. 화면별 완전 재설계

### A. 첫 화면 (Title Splash) — **신규 추가**
**현재**: 없음 (튜토리얼이 첫 진입)
**신규**:
```
┌──────────────────────┐
│  [별 반짝임 배경]      │
│                      │
│   ○ ○ ○  (RYB 물감)  │
│  Color Mix           │
│  Alchemist (볼드)    │
│                      │
│  [ 시작하기 ]        │  (하단 중앙, 라벤더 그라디언트)
│  [ 계속하기 ]        │  (저장 있을 때만, ghost)
└──────────────────────┘
```
- **배경**: 저녁 그라디언트 + 랜덤 5~8점 별 `sin(Time.time*0.8 + seed)` 맥동 alpha.
- **로고**: 프로시저얼 드롭 3개 (R `#FF5F5F` / Y `#FFD93D` / B `#5F8AFF`) + 타이틀 텍스트 42pt Bold.
- **CTA**: "시작하기" 320×64 그라디언트 버튼 + 하단 2px 섀도우.
- **계속하기**: `PlayerPrefs` 저장된 `lastStageId` 있을 때만 노출.
- **진입 플로우**: Splash → (최초) Tutorial → Lobby / (2회차+) Lobby.

**구현 힌트**:
- `MinimalGameScene` 에 `ScreenMode.TitleSplash` enum 추가.
- `Start()` 에서 `PlayerPrefs.GetInt("tutorial_done", 0)` 체크 후 초기 모드 결정.
- 별 반짝임: `for (int i=0; i<starCount; i++) { a = 0.3f + 0.7f*Mathf.Abs(Mathf.Sin(t+seeds[i])); GUI.color = ... }`.

---

### B. 로비 (Lobby) — **전면 재설계**
**현재**: 12 카드 세로 스크롤
**신규**: Candy Crush 식 챕터 포스터 + 가로 노드 맵
```
┌─────────────────────────────────┐
│ [로고小] [★ 12/36] [⚙]          │ 상단 HUD
├─────────────────────────────────┤
│ ┌─ 챕터 1: 입문 ─────────────┐ │
│ │ [큰 일러스트 그라디언트]      │ │
│ │ 진행 ████░░░░ 4/12          │ │
│ │          [ 시작 ]            │ │
│ └─────────────────────────────┘ │
│ ○━━●━━○━━○━━○━━○  (노드 맵)   │ 가로 스크롤
│                                 │
│ [< 챕터 1 / 챕터 2 >]           │ 토글 탭
└─────────────────────────────────┘
```
- **상단 HUD**: 로고(48×48) + "★ 12/36" 크라운 표기 + 설정 휠 버튼.
- **챕터 포스터 카드**: 360×200 라벤더 그라디언트 카드. 제목 + 진행 바 + 시작 버튼.
- **노드 맵**: 가로 스크롤. 각 노드 48×48 원형. 상태별 색:
  - 잠김: 회색 + 자물쇠 실루엣
  - 오픈: 흰 테두리
  - 완료: 금색 + 별 표시 1~3개 (프로시저얼 star 소형)
  - 현재: 라벤더 펄스 애니
- **챕터 토글**: 좌우 화살표 또는 탭 두 개로 챕터 1/2 전환.
- 기존 세로 리스트 완전 제거.

**구현 힌트**:
- `ChapterData[]` 정적 배열로 챕터 메타(id, title, stageIds[]).
- 노드 맵 렌더: `for (int i=0; i<stages.Length; i++) DrawNode(x0 + i*72, y, stage);`
- 연결선: 인접 노드 사이 `GUI.DrawTexture` 로 2px 가로 라인.

---

### C. 게임 플레이 (Playing)
**현재**: 상단 패널 + 보드 + 팔레트 (분리)
**신규**: **하나의 카드 프레임** 으로 감싸기
```
[⏸]              [X]    ← 원형 FAB 2개 (카드 바깥)
┌──────────────────────┐
│  ╭─ HUD 리본 ────╮    │  ← 카드 상단에 걸침
│  │턴 5/12  목표 점수│   │
│  ╰─────────────╯    │
│  ┌─플레이필드 ────┐ │
│  │ · · · · · · · │ │  ← 도트 체커
│  │  [블록][블록]  │ │
│  │   …            │ │
│  └───────────────┘ │
│  ╭─ 팔레트 ─────╮   │
│  │ [슬롯][슬롯][슬롯]│  │
│  ╰─────────────╯    │
└──────────────────────┘
```
- **게임 카드 프레임**: 화면 좌우 16px 여백, 라벤더 `#8B6BC7` + 상단 하이라이트.
- **HUD 리본**: 카드 상단에 걸치는 가로 리본 (카드 위로 12px 돌출).
  - 리본 형태: 중앙 rect + 좌우 삼각 꼬리 (3 rect 합성)
  - 좌: "턴 5/12", 중: "목표 : 1800", 우: "점수 : 1245"
  - 배경: 골드 그라디언트 `#FFD93D → #E8B82D`
- **일시정지**: 좌상단 원형 FAB 56px, 프로시저얼 pause 아이콘.
- **로비 복귀(X)**: 우상단 원형 FAB 56px, 코랄색, 프로시저얼 close 아이콘.
- **플레이필드**: `#2A1E52` 배경 + 8×8 도트 패턴 (체스보드 대체, 6px 도트).
- **팔레트 영역**: 카드 하단 리본 "팔레트" + 3 슬롯 기존 유지.

**구현 힌트**:
- 도트 패턴: `Texture2D` 16×16 타일 1회 생성 → `GUI.DrawTextureWithTexCoords` 로 반복.
- 리본: `DrawRibbon(rect, gold)` 헬퍼. 삼각 꼬리는 `GUI.matrix` 회전 or 삼각 텍스쳐.
- FAB: `DrawCircleButton(rect, icon, bgColor)` 헬퍼.

---

### D. 결과 화면 (Result)
**현재**: 블랙 오버레이 + 별점 + 점수
**신규**: 축제 카드
```
       [ 축제 카드 ]
       ╭────────╮
       │ 클리어!│  ← 42pt Bold
       │ ★ ★ ★  │  ← pop-in + 반짝임
       │  1,845  │  ← count-up
       │최고 기록!│  ← (옵션)
       │[다시][다음]│
       ╰────────╯
  [confetti 파티클]
```
- **카드**: 400×480 중앙, 라벤더 + 상단 "클리어!"/"아쉬워!" 타이틀.
- **별점 애니**: 순차 `0.0s → 0.3s → 0.6s` pop-in (scale 0→1.2→1.0 bounce). 별 주변 금색 스파클.
- **점수 count-up**: 0→final 까지 0.8초 linear interpolate.
- **CTA 2개**: "다시" ghost 160×56 / "다음" primary 160×56 (밝은 하이라이트 빛).
- **confetti**: 20개 파티클, 상단에서 낙하 + 회전. 색 랜덤 (팔레트 기본 12색).

**구현 힌트**:
- 별 pop-in: `float t = Mathf.Clamp01((Time.time - resultStartTime - i*0.3f)/0.3f); float s = BounceEase(t);`
- confetti: `struct Particle { Vector2 pos; Vector2 vel; float rot; Color c; }` 20개 배열 + `Update` 에서 `pos += vel*dt`.

---

### E. 일시정지 (Paused)
**신규**: 중앙 모달 카드
```
  [배경 어두운 블러]
    ╭─ 잠시 멈춤 ─╮  [X]
    │               │
    │ [ 재개 ] primary │
    │ [재시작] ghost  │
    │ [ 로비 ] ghost  │
    ╰───────────────╯
```
- 카드 400×500 중앙. 타이틀 "잠시 멈춤" 32pt.
- 세로 3 CTA, 각 280×56, 16px 간격.
- 우상단 X 원형 40px.
- 배경: 기존 반투명 검정 `#000 75%`.

---

### F. 튜토리얼 (Tutorial)
**현재**: 텍스트 위주 3페이지
**신규**:
- 배경 그라디언트 유지.
- **각 페이지 대형 일러스트**: 화면 상단 60% 영역에 프로시저얼 색 합성 데모 블록.
  - 페이지 1: 빨강+파랑 블록 나란히 + 화살표 → 보라 블록 등장
  - 페이지 2: 노랑+빨강 → 주황 / 팔레트 슬롯 강조
  - 페이지 3: 3단 목표 달성 데모
- 버튼 크기 증가: "다음" 200×56.
- 진행 도트: 3개 원형, 현재 페이지 라벤더 금테, 나머지 회색.

**구현 힌트**:
- 일러스트는 기존 블록 렌더러 재사용, 단순 배치만 튜토리얼용 데이터로.

---

### G. 설정 (Settings)
**신규**:
- 카드형 리스트. 각 항목: 라벨(좌) + 토글(우) 또는 슬라이더.
- 토글 on: 민트 `#7EE8C6` + 원형 노브 우측
- 토글 off: 회색 `#4A3A6B` + 원형 노브 좌측
- 슬라이더: 라벤더 트랙 + 금색 노브
- 하단 "데이터 초기화" 코랄 ghost 버튼.

---

### H. 갤러리 (Gallery)
**신규**:
- 챕터 포스터 카드 2개 세로 배치.
- 각 카드 380×180: 큰 프리뷰 영역(좌) + 제목/진행 바/스테이지 개수(우).
- 카드 탭 → 챕터별 갤러리 상세 (클리어한 스테이지 썸네일 grid).

---

## 5. 구현 우선순위

### P0 (차단) — 다음 라운드 즉시
1. **이모지 전면 제거 + 프로시저얼 아이콘 대체**
   - `IconTex` 정적 클래스 신설, 8개 아이콘(Home/Gear/Pause/Play/Palette/Star/Crown/Close) 빌드.
   - `MinimalGameScene` 의 이모지 문자열 전수 검색 후 `GUI.DrawTexture(rect, IconTex.Xxx)` 로 교체.
   - 텍스트 라벨로 충분한 곳(점수, 턴)은 라벨만.

2. **게임 플레이 카드 프레임 + HUD 리본**
   - `DrawCard(rect, cornerRadius, color, highlightColor)` 헬퍼.
   - `DrawRibbon(rect, goldGradient, leftText, centerText, rightText)` 헬퍼.
   - Playing 화면 렌더 순서: 배경 → 카드 프레임 → HUD 리본 → 플레이필드 → 팔레트 → FAB.

3. **결과 화면 카드 wrap + 별 애니**
   - 기존 오버레이 위에 중앙 400×480 카드 렌더.
   - 별 pop-in, 점수 count-up, CTA 2개.

### P1 (핵심) — P0 완료 후
4. **첫 화면 (Title Splash) 신규**
   - `ScreenMode.TitleSplash` enum 추가.
   - PlayerPrefs `tutorial_done` 분기.
   - 로고 + CTA + 별 반짝임.

5. **로비 챕터 포스터 + 노드 맵**
   - 세로 12카드 리스트 제거.
   - `ChapterData` 정적 배열.
   - 포스터 카드 + 가로 노드 맵 + 챕터 토글.

6. **프로시저얼 아이콘 6종 완성**
   - House, Gear, Star, Crown, Palette, Pause, Play, Close (P0 기본 + 세부).
   - 각 32×32, Color32[] 기반.

### P2 (완성도) — P1 완료 후
7. **축하 confetti + 타이틀 별 반짝임**
   - 20 파티클 + sin 맥동.

8. **일시정지 모달 카드화**
   - 400×500 카드 + 3 CTA.

9. **튜토리얼 큰 일러스트**
   - 페이지별 데모 블록 배치.

---

## 6. 프로시저얼 아이콘 그리는 법 (구현 힌트)

### 6.1 공통 패턴
```
static Texture2D BuildXxx(int size) {
    var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
    tex.filterMode = FilterMode.Bilinear;
    var px = new Color32[size*size];
    // 모든 픽셀 초기 투명
    for (int i = 0; i < px.Length; i++) px[i] = new Color32(0,0,0,0);
    // --- 도형별 픽셀 색칠 로직 ---
    tex.SetPixels32(px);
    tex.Apply();
    return tex;
}
```

### 6.2 House (32×32)
- 지붕: y ∈ [0, 12], x 는 중앙 기준 삼각형 `abs(x-16) <= y` 충족 시 금색
- 벽: y ∈ [12, 28], x ∈ [6, 26] 흰색
- 문: y ∈ [20, 28], x ∈ [14, 18] 라벤더

### 6.3 Gear (32×32)
- 외곽 원: `sqrt((x-16)^2+(y-16)^2) <= 14` 회색
- 6 tooth: 각도 `θ = i * 60°` (i=0..5) 방향으로 4×4 사각 픽셀 fill
- 내부 원: 반경 5 흰색 (중심 hole)

### 6.4 Star (32×32, 금색)
- 5각 별: 극좌표 `r = |sin(5θ/2)|` 또는
- 10 꼭지점 polygon: 바깥 5점(반경 14) + 안쪽 5점(반경 6) 교차 → scanline fill

### 6.5 Crown (32×32, 금색)
- 베이스 사각: y ∈ [20, 28], x ∈ [4, 28]
- 3 스파이크: 중앙+좌우 각 삼각형 꼭지
- 중앙 보석: y=22, x=16 민트색 2×2 사각

### 6.6 Palette (32×32)
- 반원: `(x-16)^2 + (y-24)^2 <= 12^2 && y <= 24` 라벤더
- 색 점 3개: R/Y/B, 반경 2, 위치 (10,18)(16,14)(22,18)

### 6.7 Pause (32×32)
- 좌 바: x ∈ [9, 13], y ∈ [6, 26] 흰색
- 우 바: x ∈ [19, 23], y ∈ [6, 26] 흰색

### 6.8 Play (32×32)
- 삼각형: 꼭지 (8,6)(8,26)(26,16) scanline fill 흰색

### 6.9 Close (32×32)
- 대각선 1: `abs((x-16) - (y-16)) <= 2` 흰색
- 대각선 2: `abs((x-16) + (y-16) - 0) <= 2` 흰색

### 6.10 버튼/카드 헬퍼도 프로시저얼
- `DrawCard(Rect r, float radius, Color bg, Color hi)`: 모서리 `Mathf.Min(dx, dy) < radius` 영역은 `dx²+dy² <= r²` 에 해당할 때만 채움 (둥근 사각).
- `DrawGradientBar(Rect r, Color top, Color bottom)`: 1×64 Texture2D 수직 그라디언트 1회 생성 → `GUI.DrawTexture` 스트레치.

---

## 7. 기대 결과물

### Before
- 플랫 회색 사각형들
- □ 박스 이모지
- 세로 리스트 뿐
- "메모장 느낌"

### After
- 라벤더 그라디언트 카드 프레임
- 금색 리본 HUD
- 프로시저얼 아이콘 선명
- 챕터 포스터 + 노드 맵
- 별 반짝임 + confetti
- **유저 반응 목표**: "드디어 게임 같다"

---

## 8. 다음 라운드 작업 배정 (리더 참고)

| 역할 | P0 작업 |
|---|---|
| Coder (Unity) | `IconTex` 정적 클래스 + 8 아이콘 빌드, `DrawCard`/`DrawRibbon`/`DrawCircleButton` 헬퍼 |
| Coder (Unity) | `MinimalGameScene` 이모지 전수 제거 + 아이콘 교체 |
| Coder (Unity) | Playing 화면 카드 프레임 + HUD 리본 레이아웃 적용 |
| Coder (Unity) | Result 화면 카드 wrap + 별 pop-in + 점수 count-up |
| Debugger | iPhone 15 Pro 시뮬 safe area 재검증 (카드 프레임이 notch 영역 침범 방지) |
| Test | 각 화면 스크린샷 비교 (Before/After) |
| Reviewer | 컬러 대비 WCAG AA 검증 (금색 리본 위 흰 텍스트) |
| Doc | 본 문서 + 실제 구현 스크린샷 링크 |

---

## 9. 주의 사항

1. **TMP 전환 금지** (P0 범위). OnGUI 유지하며 프로시저얼 아이콘만 추가.
2. **폰트 패키지 추가 금지** (P0). 기본 폰트 + 프로시저얼 아이콘으로 충분.
3. **블록 렌더 로직 수정 금지**. 기존 SDF rounded rect 유지. 배경/프레임만 개선.
4. **성능**: 프로시저얼 텍스쳐는 Start 시 1회 생성 후 캐시. 매 프레임 재생성 금지.
5. **Safe Area**: iPhone notch 영역은 카드 프레임 top 16px 여백으로 피할 것.
6. **모든 랜덤 시드 고정** (별 위치, confetti 색) → 테스트 재현성 확보.

---

## 10. 핵심 산출 체크리스트

### P0 완료 기준
- [ ] `IconTex` 8 아이콘 모두 렌더 확인
- [ ] Playing 화면 어디에도 □ 박스 없음
- [ ] Lobby/Result/Paused 전부 라벤더 카드 프레임 적용
- [ ] HUD 리본에 "턴 / 목표 / 점수" 표시
- [ ] Result 별 pop-in 애니 작동

### P1 완료 기준
- [ ] Title Splash 첫 진입 시 노출
- [ ] Lobby 챕터 포스터 + 가로 노드 맵 작동
- [ ] PlayerPrefs `tutorial_done` 분기 정상

### P2 완료 기준
- [ ] Result confetti 20 파티클 작동
- [ ] 튜토리얼 페이지별 대형 일러스트

---

**END — 본 문서는 단일 마스터 플랜. 코드 수정은 리더의 다음 라운드에서 일괄 적용.**
