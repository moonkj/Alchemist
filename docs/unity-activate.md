# Unity License 활성화 — `UNITY_LICENSE` Secret 등록 가이드

GitHub Actions 러너에서 Unity 를 헤드리스로 구동하려면
Unity Personal 계정의 **ULF(Unity License File)** 전체 텍스트가 필요합니다.

## 개요

```
[로컬 mac] Unity 실행 → 수동 로그인 → ULF 파일 획득 → 레포 Secret 업로드
```

## 사전 준비

- Unity Hub 설치 (`/Applications/Unity Hub.app`)
- Unity 6000.0.32f1 LTS 설치 완료
- Unity ID 계정 (Personal/Free 도 가능)
- GitHub 레포 관리자 권한 (Secrets 등록 가능)

## 1단계 — ALF 파일 생성

로컬 터미널에서 다음 명령을 실행합니다. (이미 `build_ios.sh` 와 같은 Unity 경로 사용)

```bash
UNITY_APP="/Applications/Unity/Hub/Editor/6000.0.32f1/Unity.app/Contents/MacOS/Unity"

"$UNITY_APP" \
  -batchmode \
  -createManualActivationFile \
  -logFile -
```

실행 후 현재 폴더에 `Unity_vXXXX.X.X.alf` 파일이 생성됩니다.

## 2단계 — ULF 발급 (Unity 공식 사이트)

1. <https://license.unity3d.com/manual> 접속
2. Unity ID 로그인
3. `Unity_vXXXX.X.X.alf` 업로드
4. **Unity Personal → Individual use** 선택
5. `Unity_vXXXX.X.X.ulf` 파일 다운로드

> 주의: Pro/Plus 계정은 seat 제한이 있어 CI 에서 권장하지 않습니다.
> Personal 계정이면 기기/CI 동시 사용 가능.

## 3단계 — GitHub Secrets 등록

레포 `Settings → Secrets and variables → Actions → New repository secret`.

| Secret 이름 | 값 |
|---|---|
| `UNITY_LICENSE` | `Unity_vXXXX.X.X.ulf` 파일 **전체 텍스트** (`<?xml ...>` 부터 끝까지) |
| `UNITY_EMAIL` | Unity 계정 이메일 |
| `UNITY_PASSWORD` | Unity 계정 비밀번호 |

```bash
# 파일 내용 복사 (macOS)
pbcopy < Unity_vXXXX.X.X.ulf
```

붙여넣을 때 개행이 유지되도록 **Raw** 그대로 입력합니다.

## 4단계 — 동작 확인

`ci.yml` 을 수동 실행하거나 더미 PR 을 올려 다음을 확인합니다.

- `Run EditMode Tests` step 이 `Activating Unity license` 로그 출력 → 이후 테스트 진행
- 실패 시 로그에서 `No valid Unity Editor license found` 확인 후 Secret 재등록

## 5단계 — 추가 Secrets (TestFlight 빌드용)

`release-ios.yml` 에서 사용:

| Secret 이름 | 값 설명 |
|---|---|
| `APP_STORE_CONNECT_API_KEY_ID` | App Store Connect → Users & Access → Keys → Key ID (예: `ABCD123456`) |
| `APP_STORE_CONNECT_API_ISSUER_ID` | 동 페이지 Issuer ID (UUID) |
| `APP_STORE_CONNECT_API_KEY_P8` | 다운받은 `.p8` 파일 전체 텍스트 (`-----BEGIN PRIVATE KEY-----` 포함) |

App Store Connect 키는 **한 번만 다운로드 가능**하므로 잃어버리면 재발급 필요.

## 문제 해결

| 증상 | 원인 | 해결 |
|---|---|---|
| `Failed to activate/update license` | ULF 개행 깨짐 | Secret 을 파일 그대로 재업로드 |
| `Another copy of this license is already in use` | 동일 ULF 가 다른 곳에서 사용 중 | 로컬 Unity 로그아웃 후 재시도, 혹은 별도 CI 전용 계정 운영 |
| `Unity version mismatch` | 런너에서 다른 버전 설치 시도 | `ci.yml` 의 `unityVersion` 이 `6000.0.32f1` 고정인지 확인 |

## 회수(Return) 절차

계정 전환/만료 시:

```bash
"$UNITY_APP" -batchmode -returnlicense -quit
```

또는 Unity ID 웹에서 수동 회수 후 새 ULF 재발급.
