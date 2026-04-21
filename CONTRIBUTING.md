# Contributing Guide — 컬러 믹스: 연금술사

## 브랜치 전략

- `main`: 항상 배포 가능 상태. 직접 푸시 금지. PR + CI 통과 필수.
- `feat/*`, `fix/*`, `refactor/*`, `docs/*`, `chore/*`: 기능/수정 브랜치.
- 태그 `v*`: 릴리스 트리거 (`release-ios.yml` 가 자동 실행).

## 커밋 메시지

Conventional Commits + 한국어 본문 허용.

```
<type>(<scope>): <subject>

<body 선택>
```

- `feat`: 신규 기능
- `fix`: 버그 수정
- `refactor`: 동작 변경 없는 구조 개선
- `perf`: 성능 개선
- `test`: 테스트 추가/수정
- `docs`: 문서
- `build` / `chore` / `ci`: 인프라

예: `feat(domain): 보드 매치 판정 알고리즘 최적화`

## 아키텍처 레이어 규칙

| 레이어 | 디렉터리 | 규칙 |
|---|---|---|
| Domain | `Assets/_Project/Domain/` | 순수 C#. UnityEngine import 금지. Riverpod-free. 테스트 필수. |
| Presentation | `Assets/_Project/Presentation/` | Domain ↔ View 어댑터. |
| View | `Assets/_Project/View/` | MonoBehaviour, Unity 의존. AppColors 사용. |
| Bootstrap | `Assets/_Project/Bootstrap/` | 조립/초기화 진입점. |
| Infrastructure | `Assets/_Project/Infrastructure/` | IO, Persistence, 외부 SDK. |

## 코드 스타일

- C# 네임스페이스: `Alchemist.<Layer>.<Feature>`
- 필드: `_camelCase` (private), `camelCase` (public property)
- AppColors 컨벤션: `new Color(...)` 리터럴 금지 → `AppColors.Accent` 등 참조
- Domain 에서 `Debug.Log` 사용 금지 (CI 에서 차단)

## 테스트

- EditMode 120+ 테스트 유지. 신규 Domain 코드는 테스트 포함 PR만 병합.
- PlayMode Performance 테스트는 `Tests/Performance/` 에 `Category("Performance")` 로 등록.
- 실행: `Unity → Test Runner` 또는 `scripts/run_tests.sh` (후속 제공 예정).

## PR 체크리스트 (요약)

자세한 템플릿은 `.github/PULL_REQUEST_TEMPLATE.md` 참조.

1. 커밋 메시지 컨벤션 준수
2. EditMode 테스트 통과 (CI 자동)
3. Domain 변경은 단위 테스트 동반
4. View 변경은 스크린샷 첨부
5. `Tasklist.md` / `process.md` 갱신

## CI 파이프라인

| 워크플로우 | 트리거 | 목적 |
|---|---|---|
| `ci.yml` | PR → main | EditMode 테스트 + Lint |
| `release-ios.yml` | 태그 `v*` | Unity 빌드 + TestFlight 업로드 |
| `nightly.yml` | Cron 02:00 KST | 성능 회귀 감시 |

## Unity 라이선스 등록

최초 기여자는 `docs/unity-activate.md` 참고하여 `UNITY_LICENSE` secret 을 등록한다. (레포 관리자 권한 필요)

## 문의

- 리드: @moonkj
- 이메일: imurmkj@naver.com
