# Phase 0 통합 리뷰 — 아키텍트 최종 판단

> 리더: Architect | 2026-04-21 | 라운드: Phase 0 Final
> 입력: ux_design.md / architecture.md / performance.md / README.md / glossary.md
> 절차: 각 Teammate가 던진 교차레이어 질문에 **과학적 토론 방식**으로 결론 도출

---

## 1. 핵심 합의 사항 (결정 확정)

| # | 항목 | 결정 | 근거 (어느 문서 간 조정인가) |
|---|------|------|------------------------------|
| D1 | **게임 엔진** | Unity 2D (Unity 6 LTS / URP 2D) | Architect §1.3 - Metaball/매치-3 생태계 압승 |
| D2 | **저사양 타깃 기기** | iPhone SE 2 / Galaxy A32 (A10/Adreno 506, 2GB RAM) | UX Q6 ↔ Perf §1 — 동일 선에서 합의 |
| D3 | **보드 크기** | **6×7** (초기) | UX §3.1은 6×7 기준 와이어프레임, Architect 8×8 제안보다 UX 우선 — 엄지 도달 거리 + 파티클 하드캡 여유 |
| D4 | **팔레트 슬롯 최대** | **3개** (1→2→3 점진 해제) | Architect 가정 4 → UX §3.1 3개 확정안으로 **수정 채택** |
| D5 | **조작 UX** | Drag & Drop 하이브리드 (≤16px는 인접 스왑) | UX §4.1 권고 — Architect `IInputEventBus`가 두 이벤트 모두 수용하도록 확장 |
| D6 | **Metaball 렌더 전략** | 저/중사양 SpriteSheet / 고사양 셰이더 | Perf §2.2 — Architect "Liquid2D 셰이더" 기본을 **고사양 한정**으로 다운그레이드 |
| D7 | **파티클 하드캡** | Low 400 / Mid 800 / High 1500 | Perf §2.1 — UX Q5에 답으로 확정 |
| D8 | **색 조합 자료구조** | 비트플래그 `ColorId` (Architect) + 2차 캐시 배열 256×256 (Perf) 병행 | Architect §2.1 ↔ Perf §2.4 상호 보완 — 둘 다 채택 |
| D9 | **보드 내부 표현** | 1차원 `int[rows*cols]` | Perf §2.3 — Architect `Cell[,]` 의사 코드는 **렌더 레이어 전용**, 도메인은 1D |
| D10 | **프리즘 해석 규칙** | **결정론적** — `Mix(Prism, X) = X` | UX Q2 답 — 확률 요소 없음, 프리뷰 UI 신뢰 가능 |
| D11 | **팔레트 블록의 연쇄 참여** | **불참** (저장 전용, 보드 매치 판정에서 제외) | UX Q3 ↔ Architect 합의 |
| D12 | **연쇄 최대 깊이** | **10단계** 하드캡 + 이상 시 강제 종료 로그 | Architect R2 — UX Q1 답으로 확정 (시각 구별은 3차까지, 4차+는 일반 폭발 FX로 통일) |
| D13 | **프리뷰 연산 트리거** | hover 변경 시에만 (프레임마다 X) | Perf Q4 답 — UX 체감상 OK, CPU 부담 없음 |
| D14 | **빌드 바이너리 크기** | 셀룰러 다운로드 한도 200MB 이하 | Perf ↔ Doc 합의 |
| D15 | **세이브 동기화** | 로컬 MemoryPack 우선, Firestore 비동기 백그라운드 | Architect R7 + Perf §4 큐 전략 통합 |

---

## 2. 과학적 토론 결론 — 충돌 해소

### 토론 A: 보드 크기 (UX 6×7 vs Architect 8×8)
- **UX 논거**: 한손 엄지 도달 거리, 52px 셀 기준 세로 844px에 맞추려면 6×7 최적
- **Architect 논거**: 8×8이 매치-3 관습, 조합 다양성 확보
- **Perf 논거**: 6×7는 동시 블록 42개로 파티클 캡(저사양 400) 여유, 8×8은 64개로 5단 연쇄 시 1600개 초과 위험
- **결론**: **6×7 확정**. 관습 타파는 "색 조합 전략" 자체로 이미 달성, 보드 크기로 강화할 필요 없음.

### 토론 B: 블록 데이터 구조 (Architect `Cell[,]` vs Perf 1D 배열)
- 도메인 레이어는 **1D 배열**(캐시 친화), 렌더 레이어는 2D 좌표 래퍼로 **투영**
- `Board.Cells[r, c]` API는 유지하되 내부 저장은 `_cells[r * Cols + c]`
- 둘 다 만족. LINQ/foreach 금지는 Perf 규칙 채택.

### 토론 C: Metaball (Architect 셰이더 기본 vs Perf SpriteSheet 기본)
- 셰이더는 fill-rate 병목, 저사양 기기에서 GPU 8ms 초과 위험
- Phase 4 폴리시 단계에서 고사양만 셰이더 업그레이드, Phase 1~3는 SpriteSheet로 가시 효과 확보
- **Perf 전략 채택**. Architect 부록 A의 `Shaders/Metaball` 폴더는 유지하되 Phase 4 스코프로 이동.

---

## 3. 교차레이어 질문 답변 요약

### UX → Architect
- Q1 연쇄 깊이: D12 (10단, 4차 이상은 일반 FX로 통일)
- Q2 프리즘 결정성: D10 (결정론적)
- Q3 팔레트 연쇄 참여: D11 (불참)

### UX → Performance
- Q4 프리뷰 연산 주기: D13 (hover 변경시만)
- Q5 파티클 상한: D7 (400/800/1500)
- Q6 햅틱 빈도: 세션당 ≤80회, A10급까지 정상 동작 (UX §4.2 원칙 채택)

### Architect → UX
- 팔레트 슬롯 수: D4 (3개)
- 리트라이 UX: UX §5.4 "소프트 페일 +5" + 광고 시청 루트 (결정)
- 감염 애니 길이: 프레임 예산 2.5ms 내 (Perf §1.1에 맞춤)
- 색맹 모드: UX 유보 항목 1번 — Phase 2 접근성 리서치 후 (유보 유지)

### Architect → Performance
- 목표 기기: D2
- 블록 풀 200 → **120으로 축소** (6×7×2여유, Perf §2.1 반영)
- Metaball 대체: D6
- Burst/Jobs 임계치: 9×9 이상 시 (Perf §2.3)
- Addressables: 채택 (Perf §3.1)

### Performance → Architect
- ECS vs OOP: **OOP Manager + POCO Domain** (Architect §4 기반), Jobs는 부분 적용
- 세이브 동기화: D15
- 스테이지 데이터: **ScriptableObject**(정적) + **JSON**(원격 업데이트) 혼용
- 리플레이: Phase 3 스코프. 결정론 보장을 위해 Phase 1부터 **고정 시드 RNG** 인터페이스는 확보

### Performance → UX
- 팔레트 Trail: **없음** (Perf 부담, 드롭 스냅 애니로 대체)
- 연쇄 연출 동시 사용: 슬로우 모션 OR 화면 흔들림 **택일** (두 개 동시 사용 금지)
- 플로팅 숫자 최대: **8개** 풀
- Metaball 적용 레벨: 2차 색 이상만 (1차 원색은 일반 스프라이트)

---

## 4. 갱신된 리스크 레지스터

| # | 리스크 | 소유자 | Phase 0 → 1 전이 시 조기 작업 |
|---|--------|--------|--------------------------------|
| R1 (최우선) | 3단 연쇄 이상 프레임 스파이크 | Performance | 풀 사전할당 + 디버그 HUD를 Phase 1 선행 |
| R2 | 감염 전파 무한 루프 | Architect | 웨이브 depth 10 하드캡 테스트 시나리오 |
| R3 | Metaball fill-rate | Performance | SpriteSheet 기본화 (D6) |
| R4 | 비트플래그 Black/White 표현 | Architect | `SpecialRule` 분기 유닛 테스트 |
| R5 | 블록 풀 고갈 | Architect + Perf | 풀 120 + Grow-On-Demand 경고 |
| R6 | Prompt Condition 이벤트 누수 | Architect | `IDisposable` + CancellationToken 강제 |
| R7 | 오프라인 세이브 | Architect + Perf | D15 큐 전략 |
| R8 | Unity 6 LTS 안정성 | Architect | Unity 2022 LTS 폴백 경로 확보 |

---

## 5. 유보 항목 (Phase 1 이후 재논의)

- 색맹 모드 세부 규격 (UX 유보 1)
- 친구 시스템 정의 (UX 유보 2)
- 갤러리 공유 포맷 9:16 vs 1:1 (UX 유보 3)
- PVP 모드 여부 (UX 유보 4)
- 가로 모드 지원 (UX 유보 6)
- FMOD vs Unity Audio (Architect 1.4 유보)
- 리플레이 기능 정식 스코프 (Perf Q4)

---

## 6. Phase 1 진입 전 체크리스트

- [ ] Unity 프로젝트 초기화 (Unity 6 LTS, URP 2D)
- [ ] VContainer / UniTask / DOTween / MessagePipe / MemoryPack / Nice Vibrations 설치
- [ ] `Assets/_Project/Scripts/{Domain,View,Services,UI,Bootstrap}` 폴더 구조 생성
- [ ] Quality Level 3단계 스위치 + 디버그 HUD (Perf §5 선행)
- [ ] CI 에셋 감사 훅 (Perf §5.1)
- [ ] `ColorMixer` 유닛 테스트 20+ (Architect P1-01)
- [ ] 6×7 보드 프로토타입 (Dirty flag, 1D 배열)
- [ ] `ChainProcessor` 웨이브 큐 (depth 10 캡)
- [ ] Drag & Drop 하이브리드 인풋 컨트롤러

---

## 7. 리더 최종 판단

**Phase 0 KICKOFF 완료.** 4개 산출물이 교차레이어 조정을 통해 일관된 단일 설계로 수렴했습니다.
- 충돌은 **3건**(보드 크기 / 블록 자료구조 / Metaball 전략) 모두 과학적 토론으로 해소
- 유보 항목은 **7건**으로 명확히 분리, Phase 1~3 스코프로 이연
- Phase 1 진입 전제 조건 **9개**로 체크리스트화

**다음 게이트(유저 확정 필요):**
1. 엔진 결정 Unity 2D 승인하시겠습니까?
2. Phase 1 MVP 착수 범위는 Tasklist.md P1-01 ~ P1-11 전체 진행합니까, 아니면 핵심 4건(P1-01~04)으로 축소?
3. GitHub 리모트 커밋 연동(아키텍트 역할 정의 중 "GitHub 커밋")을 시작합니까? 리포 URL 필요.
