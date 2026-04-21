# Tasklist — 컬러 믹스: 연금술사 (Color Mix: Alchemist)

> 리더: Architect  |  최종 업데이트: 2026-04-21 (Phase 0 Kickoff)
> 상태 범례: 🟦 TODO | 🟨 IN-PROGRESS | 🟩 DONE | 🟥 BLOCKED | ⬜ SKIPPED

---

## Phase 0 — Kickoff & Planning (현재)

| ID | 작업 | 담당 | 상태 | 비고 |
|----|------|------|------|------|
| P0-01 | 팀 역할 메모리 저장 | Architect | 🟩 | team_agent_roles.md |
| P0-02 | Tasklist.md / process.md 스캐폴드 | Architect | 🟩 | 본 문서 |
| P0-03 | UX 유저 플로우 & 와이어프레임 초안 | UX Designer | 🟩 | docs/ux_design.md |
| P0-04 | 기술 스택·시스템 아키텍처 설계 | Architect | 🟩 | docs/architecture.md — Unity 2D 확정 |
| P0-05 | 모바일 성능 고려사항 초기 분석 | Performance | 🟩 | docs/performance.md |
| P0-06 | 프로젝트 초기 문서(README) 초안 | Doc Writer | 🟩 | README.md + docs/glossary.md |
| P0-07 | Phase 0 통합 리뷰 → Phase 1 계획 | Architect | 🟩 | docs/phase0_integration_review.md |
| P0-08 | 유저 확정 게이트 (엔진/범위/리포) | Architect | 🟨 | 3개 질문 대기 중 |

## Phase 1 — MVP Design

| ID | 작업 | 담당 | 상태 | 비고 |
|----|------|------|------|------|
| P1-01 | 색 조합 엔진 (ColorMixer, ColorId, Cache) | Coder-1 | 🟩 | 5 files, Wave 1 |
| P1-02 | 블록 상태 머신 (FSM, 15 전이) | Coder-2 | 🟩 | 7 files, Wave 1 |
| P1-03 | 연쇄 처리 큐 (ChainProcessor) | Coder-Wave2 | 🟨 | Wave 2 예정 |
| P1-04 | 보드 & View (Board POCO, BoardView Mono) | Coder-Wave2 | 🟨 | Wave 2 예정 |
| P1-05 | 매치 탐지 & 폭발 규칙 (MatchDetector) | Coder-Wave2 | 🟨 | Wave 2 예정 |
| P1-06 | 프롬프트 3종 (Condition/Goal/Prompt) | Coder-3 | 🟩 | 9 files, Wave 1 |
| P1-07 | 점수 계산 (Scorer, ScoreConstants) | Coder-4 | 🟩 | 5 files, Wave 1 |
| P1-08 | 디버깅 패스 (전 도메인) | Debugger | 🟦 | Wave 3 |
| P1-09 | 단위/통합 테스트 (EditMode) | Test Engineer | 🟦 | Wave 3 |
| P1-10 | 성능/최적화 리뷰 | Performance | 🟦 | Wave 3 |
| P1-11 | 최종 코드 리뷰 | Reviewer | 🟦 | Wave 3 |

## Phase 2 — Systems (게임 시스템 확장)
- 필터 벽 / 회색 블록 / 프리즘 블록
- 팔레트 슬롯
- 프롬프트 시스템 (기본/고급/일일)

## Phase 3 — Meta (랭킹·배지·BM·성장 루프)
- 랭킹 (글로벌/친구/데일리/프롬프트)
- 배지 (조합/스타일/숨겨진)
- 잉크 에너지, 매직 브러시/지우개
- 갤러리 복원, 색 해금

## Phase 4 — Juice & Polish
- Metaball/젤리 질감
- 햅틱 피드백
- 사운드 (플롭/슈욱)
- 다크 모드
