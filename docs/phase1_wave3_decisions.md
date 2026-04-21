# Phase 1 Wave 3 — 아키텍트 결정 및 수정 라운드 계획

> 리더: Architect | 2026-04-21 | 라운드: Wave 3 Integration
> 입력: Debug Report (Critical 5 + H 12 + M 9 + L 10), Perf Review (Grade B+, Critical 3), Test Report (104 cases + 2 Architect Qs)

---

## 1. 아키텍트 결정 (Scientific Debate 결론)

### D16 · `Mix(Black, X) = Black` (오염 전파) — C4 원안 재확정
**토론:** 현 `ColorMixer.Mix` 구현은 Black 비트가 SpecialMask에 걸려 None 반환. Test Engineer가 "C4에서는 Black 전파"로 기록된 것과 충돌을 발견.
- **가설 A (현구현):** Black은 죽은 블록, 매칭 참여 안 함 → None
- **가설 B (C4 원안):** Black은 오염 전파자, 만나는 모든 블록을 Black으로
- **결론:** **가설 B 채택.** 이유: 게임 디자인상 Black은 "오염"이라는 negative 메커닉을 강화해야 플레이어가 회피 동기를 가진다. Gray는 비활성이라 None 유지.

### D17 · `Mix(Prism, Gray) = None`, `Mix(Prism, Black) = Black`
**결정:** Prism 와일드카드는 **정상 색만 투과**. Gray/Black 같은 특수 상태와 만나면 특수 상태가 우선.
- Mix(Prism, Prism) = Prism
- Mix(Prism, None) = None
- Mix(Prism, Gray) = None
- Mix(Prism, Black) = Black (오염 전파 D16)
- Mix(Prism, White) = White
- Mix(Prism, 1차/2차) = 그 색

### D18 · `MatchDetector`는 H/V 분리 방출, `ChainProcessor`가 dedupe
**토론:** L-shape 매치 시 같은 셀이 H와 V 두 그룹에 포함. 병합 책임은 누구?
- **결정:** 현 구현 유지 — MatchDetector는 단순 스캐너, ChainProcessor의 `visitedLo` bitset이 dedupe. Scorer에는 **dedupe 후 unique count** 전달.

### D19 · `Scorer`는 폭발 ≠ 생성 분리 (`OnColorCreated` 신설)
**토론:** C2 "정확 매칭" 결정과 연계. "보라 10개 생성" 프롬프트는 merge/infection 시점에 증가해야 의미 정확.
- **결정:** `Scorer.OnColorCreated(color, count)` 훅 신설. ChainProcessor가 감염 시 호출.
- `OnBlocksExploded`는 점수 + 폭발 카운트만 담당 (생성 카운트는 여기서 증가 X).

### D20 · 감염 대상 dedupe — 첫 감염만 (BUG-H05)
**결정:** `infectedMask` bitset 도입. 같은 웨이브 내 한 primary 블록은 **최초 감염 색**만 적용.
- 이유: 결정론적, 리플레이 재현 가능.

### D21 · 감염된 블록은 **다음 depth**에서 폭발 (BUG-H07)
**결정:** 현 구현 유지. 웨이브 단위 시뮬레이션 명확성 + chain multiplier 의도 일치.

### D22 · `BeginStage(par)` vs `OnStageEnded(movesLimit)` Phase 1 처리
**결정:** Phase 1 MVP는 **par = movesLimit** (동일) 허용 → Efficiency 항상 1.0 반환. Phase 2 스테이지 데이터에 `parMoves` 필드 추가 시 분리.

---

## 2. 수정 라운드 범위 (Phase 1 Wave 3 Fix)

리더 판단: Critical 전부 + Perf Critical 전부 + High 중 Scorer/UX 파이프라인 핵심만 수정.
나머지 H/M/L은 **Phase 2 백로그**로 이관.

### 즉시 수정 (Wave 3 Fix)
| ID | 분류 | 파일 | 요약 |
|----|------|------|------|
| F1 | BUG-C02 + D16/D17 | `ColorMixer.cs` | Prism 분기 → None/Gray 뒤, Black 전파 |
| F2 | BUG-C03 | `BoardView.cs PlayGravityAsync` | `_viewGrid == null` 방어 |
| F3 | BUG-C04 | `BoardView.SyncCell` | `_pool == null` 방어 |
| F4 | BUG-C05 + D19 | `ChainProcessor`, `Scorer` | Scorer 주입 + OnBlocksExploded/OnColorCreated 연결 |
| F5 | BUG-C01 | `GameRoot.cs` | 초기 보드 Refill (Awake에서 42셀 스폰) |
| F6 | BUG-H05 + D20 | `ChainProcessor` | infectedMask bitset |
| F7 | BUG-H09 | `GameRoot.cs` + GameContext | GameContext 생성 + PromptBanner.SetContext |
| F8 | BUG-H11 | `GameRoot.cs` | 매 turn 후 UIHud.SetMovesRemaining |
| F9 | PERF-1 | `BoardView.cs` | `OnAnimStep` delegate 필드 캐싱 |
| F10 | PERF-2 | `DeterministicBlockSpawner.cs` | Block 풀링 추가 (Rent/Return) |
| F11 | PERF-3 | `MatchGroup.cs` | RowBuf/ColBuf ctor 사전 할당 |

### Phase 2 백로그 (이연)
- BUG-H01/H02 Prism 교환법칙 엣지 (D17에서 일부 해소)
- BUG-H03 Selected→Selected 전이 (UX 재조준)
- BUG-H04 생성/폭발 분리 (D19 구현 후 재검토)
- BUG-H07 감염 후 즉시 재스캔 옵션 (D21 유지)
- BUG-H08 par vs limit 분리 (D22)
- BUG-H10 로컬라이저 서비스
- BUG-H12 → F10으로 이관
- BUG-M01~M09 (리뷰어 재판단)
- BUG-L01~L10 (코드 리뷰 후 선별)

---

## 3. 수정 후 DoD (Phase 1 완료 조건)

- [ ] Critical 5 + Perf Critical 3 = **8건 전부 수정**
- [ ] `Mix(Black, X) = Black`, `Mix(Prism, Gray) = None` 테스트 추가 통과
- [ ] `Score.Total`이 ChainProcessor 폭발로 변경되는지 통합 테스트 통과
- [ ] GameRoot.Awake 후 보드가 42개 블록으로 채워짐 검증
- [ ] Reviewer 최종 라운드 (Phase 2 백로그 승인 포함)
- [ ] process.md 업데이트 + GitHub 태그 `v0.1.0-phase1`
