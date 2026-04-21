# Phase 2 Completion — 특수 블록 + 팔레트 + 프롬프트 확장

> 리더: Architect | 2026-04-21

## 구현 요약
- **특수 블록 3종**: Filter Wall (낙하 통과 색 변환) / Gray (2회 폭발 누적 해제) / Prism (턴 종료 승격)
- **Palette 슬롯**: 3개 고정 (StageData 제어), Store/Use 이벤트 → GameContext 기록
- **Prompt 확장**: FilterTransitCondition, UsePaletteSlotCondition, DailyPuzzle (date seed 결정론)
- **StageData SO**: parMoves/maxMoves 분리 (D22) + InitialPlacements + BoardSeed
- **LocalizerService**: BUG-H10 해소 (한국어 스텁)
- **M1 (InputController↔GameRoot)**: OnSwap → NotifyMoveCommitted 배선
- **ChainProcessor.OnFilterTransit**: delegate 훅으로 GameContext.RecordFilterTransit 연결

## 아키텍트 결정 추가 (D23~D25)
- **D23**: Gray 해제 조건 = 인접 4방향 폭발 2회 누적 (턴 경계 리셋). 해제 시 마지막 폭발 색으로 초기화.
- **D24**: Prism 승격은 **턴 종료 시** 단일 패스. 중간 캐스케이드 교란 방지.
- **D25**: 필터 벽은 **낙하 경로 상** 색 변환. 정지 블록은 변환되지 않음. 예외: 특수 블록(Prism/Gray/Filter) 통과 시 색 변환 안 함.

## Coder-B 질의 답변
1. **필터 통과 콜백**: `ChainProcessor.OnFilterTransit` delegate 훅 추가. GameRoot에서 `_promptCtx.RecordFilterTransit` 에 연결.
2. **Palette→Board 드래그**: Phase 2 범위 외. `PaletteView` 는 `Palette.Use()` 까지 수행, Board 적용은 Phase 3 `PlayerAction` 도입 시 처리.
3. **Block.Id 음수 충돌**: 없음. DeterministicBlockSpawner는 풀 반환 시 `Reset()` 호출 후 `_nextId++` 재할당. 음수 id는 초기 배치에만 존재하고 반환 시점에 positive로 덮어써짐.

## Phase 3 이연 항목
- Palette→Board 블록 적용 로직 (PlayerAction + SwapCommand)
- PromptBanner 실시간 필터/팔레트 진행도 UI 갱신 (현재 기반은 마련됨)
- 스테이지 XP/해금 시스템

## DoD 자가 검증
- [x] 특수 블록 3종 도메인 구현
- [x] Palette 도메인 + UI 바인딩
- [x] 프롬프트 확장 2종 + DailyPuzzle 결정론
- [x] StageData SO + Loader + Default fallback
- [x] LocalizerService 스텁
- [x] 6종 테스트 파일 추가 (FilterWall/Palette/DailyPuzzle/PrismAbsorb/StageLoader/GrayRelease)
- [x] D22 parMoves/maxMoves 분리 GameRoot 반영
- [x] M1 InputController.OnSwap 구독
- [x] Coder-B 3개 질의 해결

**Phase 2 완료 선언** → 태그 `v0.2.0-phase2` 대기.
