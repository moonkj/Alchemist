# Leader Aggregation — UX/UI v3 Review 종합

> 2026-04-22 | Architect 리더 판단

## 1. 리뷰 요약

| 영역 | 등급 | 핵심 진단 |
|------|:---:|----------|
| UX/UI Senior | **B-** | 기능 완주 가능하나 시각적 완성도 "내부 프로토타입" 수준. 근본 원인: OnGUI 한계 + 와이어프레임 미반영 + 스테이지 색 컨텍스트 증발 |
| Architect | **C (Warn)** | 881 line MinimalGameScene 이 8 책임 결합. Domain 16 asmdef 자산·design_system 토큰·AppBootstrap 싱글톤 전부 우회하는 섀도우 트랙 |
| Game Feel | **C~C+** | Mix/Explode "시늉은 난다". Chain/Clear/Fail 은 "기능은 돌지만 체감 비어있음" (D급) |

## 2. 리더 판단 — 우선순위 매트릭스

| # | 이슈 | 리뷰 출처 | 영향 | 난이도 | 이번 라운드 |
|---|------|-----------|------|:---:|:---:|
| P1 | 결과 버튼 화면 오버플로 | UX#2 | Critical(버그) | S | ✅ |
| P2 | 진행 바 색 GoalColor 동적 | UX#1 | High | S | ✅ |
| P3 | Toast 시맨틱 컬러 + 페이드 | UX#3 | High | S | ✅ |
| P4 | 타이포 스케일 재배열 (40/28/16/12) | UX#4 | High | S | ✅ |
| P5 | Safe Area 자동 대응 | UX#5 | High | M | ✅ |
| P6 | 입력 잠금 인디케이터 | UX#6 | Medium | M | ✅ |
| P7 | Chain depth 에스컬레이션 | Juice#1 | High | M | ✅ |
| P8 | 결과 시퀀스 (별 순차 + 점수 count-up) | Juice#4 | High | M | ✅ |
| P9 | OnDestroy 정리 | Arch#1 | Medium | S | ✅ |
| P10 | 화면 전환 페이드 | UX#8 | Medium | M | ✅ |
| P11 | Canvas/UGUI 이주 | Arch#v1.2 | High | L | ⬜ (Phase 5) |
| P12 | Metaball 셰이더 | Juice#2 | High | L | ⬜ (Phase 5) |
| P13 | 햅틱 7-이벤트 네이티브 | Juice#3 | Medium | L | ⬜ (Phase 5) |
| P14 | 색맹 모드 + 접근성 | UX#G | High | L | ⬜ (Phase 5) |

## 3. 이번 라운드 범위 (P1~P10)

**제약**: MinimalGameScene.cs 내부 수정만. Domain/asmdef 무변경. 기존 테스트 120+ 무회귀 유지.

**예상 체감 변화**:
- 버튼 오버플로/잘림 **해결**
- 스테이지별 목표 색 명확히 인식
- 성공/실패/경고 즉각 구분
- 정보 위계 2~3× 간격 확보
- 노치/홈바 안전영역 자동
- 캐스케이드 중 "앱 멈춤" 오해 해소
- 연쇄 깊이 따른 쉐이크/피치/파티클 에스컬레이션
- 결과 화면 극적 표현 (별 순차 점등 + 점수 count-up)

**Phase 5 이연**: Canvas 이주, Metaball 실셰이더, 네이티브 햅틱, 색맹 패턴 오버레이, 파이프라인 Loc 연결.

## 4. 단일 축 변경 원칙 준수 확인

- MessagePipe(v1.1) / Canvas(v1.2) / Scene 분리(v1.3) / Network(v2.0) 중 **어느 것도 건드리지 않음**.
- 이번 라운드는 **"OnGUI 범위 안에서 UX/Juice 80% 개선"** 으로 한정 (UX/UI 리뷰 §1 결론 채택).
- v1.1 MessagePipe 진입 시점에 MinimalGameScene partial 분할 + AppBootstrap 싱글톤 소비 합류.
