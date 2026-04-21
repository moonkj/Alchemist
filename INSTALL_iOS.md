# iPhone 설치 가이드 (v1.0.0)

> ⚠️ **제가 대신 못 하는 이유**: 원격 CLI에서는 Unity Editor 빌드, Xcode 서명, USB 기기 접근이 불가합니다. 아래 단계는 **사용자 로컬 Mac에서 직접** 수행해야 합니다.

## 현재 상태
- [Packages/manifest.json](Packages/manifest.json) — Unity 6 LTS + URP 2D + TMP + Test Framework 매니페스트 생성 완료
- [ProjectSettings/ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) — Unity 6000.0.32f1 LTS 마커
- 170+ C# 스크립트 + shader/asmdef 스캐폴드 (테스트 120+ 케이스)
- **누락**: Scene 파일, Prefab, 실제 Sprite/Audio/아이콘 에셋 (Unity 첫 실행 시 일부 자동 생성, 나머지는 수동 제작 필요)

---

## 1단계 — 개발 환경 준비 (30분 ~ 2시간)
| 항목 | 요구사항 |
|------|----------|
| macOS | 13 Ventura 이상 권장 |
| Xcode | 최신 정식 버전 (App Store) |
| Unity Hub | https://unity.com/download |
| Unity Editor | **Unity 6000.0.32f1 LTS** + **iOS Build Support** 모듈 함께 설치 |
| Apple ID | 무료도 가능 (7일 재서명 필요), **유료 Developer Program** ($99/년) 권장 |
| iPhone | iOS 최신, 케이블로 Mac 연결, "이 컴퓨터를 신뢰" 허용 |

## 2단계 — Unity 프로젝트 오픈
```bash
# 터미널에서:
cd /Users/kjmoon/Alchemist
# Unity Hub 열고 Add → 기존 프로젝트 추가 → /Users/kjmoon/Alchemist 선택
```
Unity가 처음 열 때:
- `Library/`, `Logs/`, `Temp/` 자동 생성 (이미 .gitignore에 포함)
- `.meta` 파일 자동 생성 (커밋 대상 — 생성 후 `git add Assets/` 실행 권장)
- **첫 컴파일**이 수십 초~수 분 걸림. 콘솔에 컴파일 에러 없는지 확인

> 컴파일 에러 가능 포인트:
> - `AppBootstrap`, `GameRoot` 등에서 Scene 레퍼런스 요구 → 3단계에서 Scene 생성
> - TMP Essential Resources 설치 프롬프트 → 수락

## 3단계 — Scene & Prefab 수동 제작
현재 코드는 **MonoBehaviour 스크립트**만 있고 Scene/Prefab은 없습니다. Unity Editor에서 최소:
1. **GameScene** 생성 → 빈 GameObject에 `GameRoot.cs` 부착
2. `BoardView`, `UIHud`, `PromptBanner`, `InputController`, `PaletteView`, `InkEnergyDisplay` GameObject 생성 + 각 컴포넌트 부착
3. `BlockView` 프리팹 1개 제작 (SpriteRenderer + BlockView 컴포넌트)
4. `BoardView._blockPrefab`에 드래그 연결
5. `Canvas` 생성 → TMP 라벨들 배치 → `UIHud._scoreLabel` 등 연결
6. Camera는 Orthographic, Size ≈ 5
7. File → Save Scene As → `Assets/_Project/Scenes/GameScene.unity`
8. File → Build Profiles → Scenes in Build에 추가

> ⏰ 예상 3~8시간. UX 와이어프레임(`docs/ux_design.md`) 참고.

## 4단계 — iOS Player Settings
`Edit → Project Settings`:
- **Player → iPhone/iPad**
  - Bundle Identifier: `com.moonkj.colormixalchemist`
  - Version: `1.0.0`
  - Target minimum iOS Version: **13.0**
  - Scripting Backend: **IL2CPP** (필수)
  - Architecture: **ARM64**
  - Camera Usage Description: 없음 (미사용)
  - Signing Team ID: Xcode 로그인한 Apple ID 팀 선택
- **Graphics**: URP 2D Renderer Asset 지정
- **Quality**: Low/Mid/High 3단계 프로파일 생성

## 5단계 — Xcode 프로젝트 빌드 (Unity)
```
File → Build Profiles → iOS → Switch Platform
Build → 경로 지정 (예: /Users/kjmoon/Alchemist/Builds/iOS)
```
Unity가 **Xcode 프로젝트(.xcodeproj)를 생성**합니다 (직접 IPA 생성 안 함).

## 6단계 — Xcode 서명 & 기기 설치
```bash
open /Users/kjmoon/Alchemist/Builds/iOS/Unity-iPhone.xcodeproj
```
Xcode에서:
1. `Unity-iPhone` 타겟 선택 → Signing & Capabilities
2. Team: Apple Developer 계정 선택
3. Bundle ID 충돌 시 변경
4. iPhone 선택 (USB 연결된 기기)
5. ▶️ Run 버튼 (빌드 & 설치 5~20분)
6. iPhone에서 **설정 → 일반 → VPN 및 기기 관리 → 개발자 앱 신뢰**

첫 실행 성공 시 앱 아이콘이 홈 화면에 표시됩니다.

## 7단계 — 무료 계정 주의
- Apple Developer Program 가입 안 한 경우: **7일마다 Xcode로 재설치** 필요
- 개인 가입($99/년): TestFlight로 90일 유효 테스트 빌드 배포 가능

---

## 트러블슈팅
| 증상 | 원인 / 해결 |
|------|-------------|
| Unity 컴파일 에러 (CS0246 등) | asmdef 참조 누락 → 에러 메시지의 어셈블리 참조 추가 |
| TMP 오류 | Window → TextMeshPro → Import TMP Essential Resources |
| IL2CPP 빌드 실패 | Xcode Command Line Tools 설치: `xcode-select --install` |
| "Unable to install" iPhone | 프로비저닝 프로파일 만료 / 기기 신뢰 해제 → Xcode에서 Devices and Simulators 재연결 |
| 검은 화면 | Scene의 Camera 설정 / URP 2D Renderer 미지정 |

---

## 현실적 다음 단계
이 프로젝트는 **설계·아키텍처 + 코드 베이스라인 완성** 상태이며, 실제 플레이 가능한 앱으로 만들려면 **에셋 제작(스프라이트·오디오·아이콘) + Scene/Prefab 세팅**이 필요합니다.

빠른 검증을 원하시면:
1. Unity에서 먼저 열어 컴파일 에러 유무 확인
2. EditMode 테스트 120+ 케이스 실행 (`Window → General → Test Runner`)
3. 그 다음 Scene 제작 착수

원격 설치 자동화는 CI/CD (Unity Cloud Build + TestFlight)로 구축하면 GitHub Push 시 자동 빌드·배포가 가능합니다. 필요하시면 별도로 설정 도와드리겠습니다.
