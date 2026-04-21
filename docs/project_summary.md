# 컬러 믹스: 연금술사 — 프로젝트 총괄 요약

> 리더: Architect | 2026-04-21 | 최종 태그: v1.0.0

---

## 1. 프로젝트 개요
- **제목**: Color Mix: Alchemist (컬러 믹스: 연금술사)
- **장르**: 매치-3 진화형 색 조합 모바일 퍼즐
- **핵심 컨셉**: "색을 맞추는 게임이 아니라 색을 설계하는 게임"
- **엔진**: Unity 6 LTS + URP 2D
- **리포**: https://github.com/moonkj/Alchemist

## 2. Phase별 릴리스

| Phase | 태그 | 범위 | 파일 수 |
|-------|------|------|--------|
| 0 Kickoff | — | UX/Arch/Perf 설계 4 문서 | 설계 문서 5 |
| 1 MVP | v0.1.0-phase1 | 색 엔진/블록 FSM/보드/연쇄/프롬프트/점수 | ~35 |
| 2 Systems | v0.2.0-phase2 | 필터/회색/프리즘/팔레트/스테이지 SO | ~35 |
| 3 Meta | v0.3.0-phase3 | 랭킹/배지/리플레이/BM/갤러리/세이브 | 64 |
| 4 Polish | v1.0.0 | Metaball/Jelly/햅틱/사운드/다크/온보딩 | 32 |

**총 파일**: 약 170+ C# + asmdef + shader + test, 120+ EditMode 테스트 케이스.

## 3. 어셈블리 그래프 (16개)
```
Domain.Colors ─────────┐
Domain.Blocks ────────┐│
Domain.Board ────────┐││
Domain.Chain         ↓↓↓ (refs)
Domain.Palette       ← Colors, Blocks
Domain.Prompts       ← Colors
Domain.Scoring       ← Colors
Domain.Stages        ← Colors, Blocks, Prompts
Domain.Ranking       (POCO)
Domain.Badges        ← Colors, Prompts, Scoring
Domain.Replay        (POCO)
Domain.Economy       (POCO)
Domain.Meta          (POCO)
Domain.Player        ← UnityEngine
Services.Ranking     ← UnityEngine, Ranking
Services.Haptic      ← UnityEngine
Services.Audio       ← UnityEngine
Services.Theme       ← UnityEngine
View                 ← Domain(Colors/Blocks/Board/Chain/Palette)
UI                   ← TMP + Domain(Colors/Scoring/Prompts/Economy/Meta/Player)
Bootstrap            ← 모든 상위 레이어 (composition root)
```

## 4. 아키텍트 결정 총 25건
- **Phase 0 D1~D15**: 엔진 Unity 2D, 저사양 A10/2GB, 보드 6×7, 팔레트 3, Drag&Drop 하이브리드, Metaball SpriteSheet 기본 등
- **Phase 1 Wave1 C1~C5**: Gray 표현, exact match, 블록수 곱, Mix 엣지, MessagePipe 이연
- **Phase 1 Wave3 D16~D22**: Black 전파, Prism 우선순위, MatchDetector dedupe 분담, OnColorCreated 분리, infectedMask, 웨이브 시맨틱, par vs limit
- **Phase 2 D23~D25**: Gray 해제(2회), Prism 턴종료 승격, 필터 낙하경로

## 5. 핵심 성능 계약 준수
- 프레임당 GC alloc 0B 목표 (steady state) — MatchGroup/Scorer/ChainProcessor 풀/배열 사전할당
- 저사양 60fps: Quality Level 3단계 자동 다운그레이드 (p95 25ms 초과 시)
- Draw Call 저사양 <80, 메모리 ≤450MB
- 파티클 하드캡 Low/Mid/High = 400/800/1500

## 6. 팀 에이전트 협업 통계
- Phase 0: 4개 전문 에이전트 병렬 (UX/Arch/Perf/Doc)
- Phase 1: Wave1 4 Coder + Wave3 3 리뷰(Debug/Test/Perf) + Reviewer
- Phase 2: 2 Coder 병렬 (Domain + UI/Bootstrap)
- Phase 3: 2 Coder 병렬 (Ranking/Badges + Economy/Meta/Player)
- Phase 4: 1 Coder 집중 (Juice/Polish)
- 총 **17회 Agent 호출**, 리더는 통합/결정/커밋/태그/재시작 담당

## 7. 과학적 토론 기록
주요 충돌과 해소:
- 보드 크기 6×7 vs 8×8 → 6×7 (엄지 도달 + 파티클 버짓)
- 블록 자료구조 `Cell[,]` vs 1D `int[]` → 1D (캐시 친화) + 2D API 래퍼
- Metaball 셰이더 vs SpriteSheet → 저/중사양 Sprite, 고사양 Shader
- Mix(Black, X) None vs Black → **Black 전파** (게임 디자인 일관성)
- MatchDetector L-shape 병합 → H/V 분리 + ChainProcessor dedupe

## 8. Phase 5 백로그 (이연)
- iOS Core Haptics AHAP 패턴
- OS 다크모드 자동 추적
- URP Global Light 실제 색 전환
- Palette→Board 드래그 드롭 PlayerAction
- 스테이지 2+ 튜토리얼
- 광고 SDK 연동 (AdMob/Unity Ads)
- 서버 랭킹 Adapter (Firebase/PlayFab)
- 리플레이 재생 UI
- MessagePipe 이벤트 허브 통합
- Phase 1 백로그 잔여 H/M/L (debug report §2~4)

## 9. 다음 스텝 권장
1. Unity 에디터에서 프로젝트 빌드·실행 검증
2. Sprite/Audio/Shader 실 에셋 제작 (현재는 코드 스캐폴드)
3. Firebase/PlayFab 서버 랭킹 통합 (Phase 5)
4. 베타 테스트 → 밸런싱 (Remote Config로 ScoreConstants 주입)
5. 앱스토어 출시 준비 (빌드 ≤ 200MB, Privacy/Consent)
