# PR 요약

<!-- 무엇을 왜 바꿨는지 2~3 문장으로 요약 -->

## 변경 범위

- [ ] Domain (순수 C# 규칙 / 테스트 동반)
- [ ] View / Presentation (Unity 씬/프리팹)
- [ ] Infrastructure / Bootstrap
- [ ] CI / Build / Scripts
- [ ] Docs only

## 체크리스트

### 공통
- [ ] 커밋 메시지 컨벤션 준수 (`feat:`, `fix:`, `refactor:`, `docs:`, `chore:`, `build:` 등)
- [ ] `Tasklist.md` / `process.md` 갱신 필요 여부 확인
- [ ] 관련 Issue 번호 링크

### Domain 변경 시
- [ ] EditMode 테스트 추가/갱신 (커버리지 유지)
- [ ] `Debug.Log` 미사용 (Domain 규칙)
- [ ] 순수 C# — UnityEngine 의존 없음

### View 변경 시
- [ ] 씬/프리팹 변경은 스크린샷 첨부
- [ ] 60 FPS 유지 (iPhone 12 기준, 필요 시 Profiler 캡처)
- [ ] 접근성: 색각 이상 모드 테스트
- [ ] AppColors 컨벤션 준수 (매직 컬러 금지)

### 성능/빌드 영향
- [ ] 메모리 할당량 증가 없음 (GC.Alloc 0 목표)
- [ ] IL2CPP / ARM64 빌드 영향 검토
- [ ] 빌드 사이즈 변화 ±5% 이내

## 스크린샷 / 로그

<!-- UI/UX 변경은 이미지 첨부, 성능 변경은 Profiler 캡처 -->

## 테스트

- [ ] 로컬 EditMode 테스트 통과
- [ ] 실기기 설치 확인 (필요 시)

## 추가 노트

<!-- 리뷰어가 특히 봐야 할 지점, 알려진 한계, 후속 작업 -->
