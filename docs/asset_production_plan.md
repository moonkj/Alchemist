# Asset Production Plan — 컬러 믹스: 연금술사 v1.0.0

> **작성 역할**: Asset Production Lead Teammate  
> **작성일**: 2026-04-21  
> **상태**: v1.0.0 코드 완료 / 실제 에셋 0% — 본 문서는 에셋 공급 로드맵  
> **관련 문서**: [design_system.md](design_system.md) · [motion_design.md](motion_design.md) · [performance_v2.md](performance_v2.md) · [architecture_v2.md](architecture_v2.md)

---

## 0. Executive Summary

| 항목 | 수치 |
|------|------|
| 총 필요 에셋 개수 | **약 187개** (파생 해상도 제외 마스터 기준) |
| 권고 제작 방식 | **하이브리드 (D안)** — 무료 베이스 + AI 생성 + 부분 직접 제작 |
| 권고 예산 (크레딧) | **$280** (저비용 경로) / 시간 약 14 man-day |
| Phase 0 전략 | **코드 런타임 생성 플레이스홀더** (단색 `Texture2D` + `PrimitiveQuad`) 로 빌드 검증 먼저 |
| 목표 완료 | v1.1.0 출시 전 Phase 3까지 완료 |

---

## 1. 에셋 카탈로그

### 1.1 블록 스프라이트 (13종 × 3해상도 = 39 파일)

| # | 블록 ID | 분류 | Hex | 비고 |
|---|---------|------|-----|------|
| 1 | `block_red` | 1차색 | `#E53935` | Primary |
| 2 | `block_yellow` | 1차색 | `#FDD835` | Primary |
| 3 | `block_blue` | 1차색 | `#1E88E5` | Primary |
| 4 | `block_orange` | 2차색 | `#FB8C00` | Red+Yellow |
| 5 | `block_green` | 2차색 | `#43A047` | Yellow+Blue |
| 6 | `block_purple` | 2차색 | `#8E24AA` | Red+Blue |
| 7 | `block_brown` | 3차색 | `#6D4C41` | 갈색(3색 혼합) |
| 8 | `block_rainbow` | 3차색 | gradient | 무지개 |
| 9 | `block_prism` | 특수 | iridescent | 와일드카드 |
| 10 | `block_gray` | 방해 | `#9E9E9E` | 비활성 |
| 11 | `block_filter_red` | 장애물 | `#E53935` 테두리 | Filter Wall |
| 12 | `block_filter_yellow` | 장애물 | `#FDD835` 테두리 | Filter Wall |
| 13 | `block_filter_blue` | 장애물 | `#1E88E5` 테두리 | Filter Wall |

- **해상도**: 128×128 @1x / 256×256 @2x / 512×512 @3x (PNG, sRGB, 알파 포함)
- **Pivot**: Center (0.5, 0.5)
- **SpriteAtlas**: `Assets/_Project/Art/Blocks/BlocksAtlas.spriteatlas` (1024×1024, ASTC 6x6)

### 1.2 UI 스프라이트 (45종)

- **버튼**: primary / secondary / ghost / icon (각 normal/pressed/disabled) = 12
- **모달/패널**: card_bg, modal_bg, tooltip_bg, toast_bg (9-patch) = 4
- **진행바**: progress_frame, progress_fill, turns_badge, score_badge = 4
- **팔레트 슬롯**: palette_slot_empty, palette_slot_filled, palette_slot_highlight (9-patch) = 3
- **탭/내비**: tab_active, tab_inactive, nav_home, nav_gallery, nav_settings = 5
- **아이콘**: heart, star, coin, hint, shuffle, pause, close, back, arrow, check = 10
- **기타**: badge_new, badge_locked, divider, shadow, vignette = 5
- **특수**: chain_indicator, combo_banner = 2
- **SpriteAtlas**: `Assets/_Project/Art/UI/UIAtlas.spriteatlas` (2048×2048, ASTC 6x6)

### 1.3 오디오 SFX (7종, `SfxId` enum 일치)

| SfxId | 파일명 | 길이 | 용도 |
|-------|--------|------|------|
| `MixPlop` | `sfx_mix_plop.ogg` | 0.2s | 블록 혼합 순간 |
| `ExplodeWhoosh` | `sfx_explode_whoosh.ogg` | 0.4s | 매치 폭발 |
| `PromptSuccessBell` | `sfx_prompt_success_bell.ogg` | 0.5s | 프롬프트 성공 |
| `InvalidHiss` | `sfx_invalid_hiss.ogg` | 0.3s | 잘못된 조작 |
| `ChainChord` | `sfx_chain_chord.ogg` | 0.45s | 연쇄 트리거 |
| `StageFanfare` | `sfx_stage_fanfare.ogg` | 0.8s | 스테이지 클리어 |
| `TurnsLowHeartbeat` | `sfx_turns_low_heartbeat.ogg` | 0.6s (loopable) | 턴 부족 경고 |

- **포맷**: Vorbis 96kbps / 22kHz / Mono / `-14 LUFS` 정규화
- **Unity Import**: Force Mono ON, Load Type = `DecompressOnLoad`, Compression Quality = 70

### 1.4 BGM (2종)

| 파일명 | 길이 | 분위기 |
|--------|------|--------|
| `bgm_lobby.ogg` | 2:00 loop | 따뜻한 재즈피아노 (BPM 78) |
| `bgm_gameplay.ogg` | 2:00 loop | 경쾌한 신스팝 (BPM 112) |

- **포맷**: Vorbis 128kbps / 44.1kHz / Stereo
- **Unity Import**: Load Type = `Streaming`, Compression Quality = 80, Preload Audio Data = OFF

### 1.5 폰트 (2종, 한국어+영문)

| 용도 | 후보 | 라이선스 |
|------|------|---------|
| Display (제목) | **Pretendard Bold** + **Poppins ExtraBold** | OFL (무료 상업) |
| Body (본문) | **Pretendard Regular** + **Inter** | OFL (무료 상업) |

- **Unity**: TextMeshPro SDF Atlas (KR Subset 2350 + ASCII), Padding 5, Sampling 64
- 경로: `Assets/_Project/Fonts/`

### 1.6 앱 아이콘 & 스플래시

- **마스터**: `icon_master_1024.png` (iOS 1024×1024, PNG RGB, 알파 금지)
- **자동 생성 20 사이즈**: 20/29/40/58/60/76/80/87/120/152/167/180/1024 등 (Unity iOS Player 자동)
- **스플래시**: `splash_portrait_2732x2732.png` (Universal) — 가운데 로고 + 배경 `#FFF3E0`
- 도구: `xcrun actool` 또는 Unity Player Settings Splash Screen 기능

### 1.7 파티클 텍스처 (5종)

| 파일명 | 해상도 | 용도 |
|--------|--------|------|
| `particle_circle_soft.png` | 128×128 | 기본 원형 글로우 |
| `particle_star_5.png` | 128×128 | 매치 성공 별 |
| `particle_smoke_puff.png` | 256×256 | 폭발 연기 |
| `particle_spark_flame.png` | 128×128 | 연쇄 불꽃 |
| `particle_droplet.png` | 128×128 | Metaball 물방울 |

- **포맷**: PNG + 알파, Grayscale 채널로 저장하여 런타임 Color 틴트

### 1.8 갤러리 명화 (챕터 1 — 12조각 픽셀 그리드)

- 기반 작품: **"별이 빛나는 밤" (고흐, 퍼블릭 도메인)** — 12조각 (4×3) 픽셀화
- 각 조각 파일: `gallery_ch01_piece_01.png` ~ `gallery_ch01_piece_12.png` (512×512)
- 완성본: `gallery_ch01_complete.png` (2048×1536)
- 썸네일: `gallery_ch01_thumb.png` (256×192)
- **장기 확장**: 챕터 2~10 각 12조각 예정 (총 120조각) — 본 Plan은 챕터 1만

### 1.9 총 마스터 에셋 수량

| 분류 | 개수 |
|------|------|
| 블록 스프라이트 (@1x 기준) | 13 |
| UI 스프라이트 | 45 |
| SFX | 7 |
| BGM | 2 |
| 폰트 | 4 (weight 별) |
| 아이콘/스플래시 | 2 (마스터) |
| 파티클 텍스처 | 5 |
| 갤러리 조각 (챕터 1) | 14 (12조각 + 완성본 + 썸네일) |
| **합계 (마스터)** | **92** |
| 3해상도 파생 포함 | **약 187** |

---

## 2. 제작 방식 결정 — 과학적 토론

### 2.1 옵션 비교

| 옵션 | 장점 | 단점 | 예상 비용 | 예상 기간 |
|------|------|------|----------|----------|
| **A. Asset Store 구매** | 품질 안정, 시간 단축 | 타 프로젝트와 중복, 커스터마이즈 제한 | $300~800 | 1~2일 |
| **B. AI 생성** | 빠름, 대량 생성 가능 | 저작권 불투명, 후처리 필수, 스타일 일관성 | $50 (구독) | 3~5일 |
| **C. 직접 제작** | 완벽한 일관성, 고유 IP | 인력·시간 과다 | 인건비 $2,000+ | 10~15일 |
| **D. 하이브리드** | 균형, 리스크 분산 | 오케스트레이션 필요 | $200~500 | 7~14일 |

### 2.2 권고안: **D. 하이브리드**

| 자산 분류 | 선택 방식 | 근거 |
|-----------|----------|------|
| 블록 스프라이트 13종 | **C 직접 (Figma/Procreate)** | 게임 정체성 핵심, 색상 정확도 필수 |
| UI 스프라이트 45종 | **A 무료 팩 (Kenney UI Pack) + C 리터치** | Kenney 라이선스 CC0, 디자인 시스템 톤매칭만 |
| SFX 7종 | **A 무료 (Freesound CC0) + C 편집** | 짧고 교체가능, 음질만 정규화 |
| BGM 2종 | **A 무료 (Incompetech, Pixabay Music)** | Kevin MacLeod CC-BY 상업 허용 |
| 폰트 | **A 무료 (Pretendard OFL)** | 한국어 완벽 지원, 로열티 0 |
| 앱 아이콘 | **B AI (Midjourney) → C 리터치** | 시각적 임팩트, 단일 이미지 |
| 파티클 5종 | **C 직접 (Photoshop 노이즈 브러시)** | 단순 그레이스케일, 30분 작업 |
| 갤러리 12조각 | **퍼블릭 도메인 명화 + C 픽셀화 스크립트** | 저작권 안전, Python Pillow 자동화 |

### 2.3 예산 예상

| 시나리오 | 크레딧 | man-day |
|----------|--------|---------|
| **무료 경로** (권고) | **$0** | 14 MD (직접 제작분 포함) |
| **저비용 경로** | **$280** (Midjourney $30 + 아이콘 외주 $250) | 10 MD |
| **프리미엄 경로** | **$2,100** (Synty $800 + 오디오 외주 $800 + 아이콘 외주 $500) | 4 MD |

> 현재 단계(v1.0.0 출시 직후)에서는 **무료 경로** 권장. 유저 피드백 확보 후 유료 확장.

---

## 3. Unity Import 규격

### 3.1 Sprite Import Settings

| 항목 | 값 |
|------|---|
| Sprite Mode | `Multiple` (아틀라스) / `Single` (개별) |
| Pixels Per Unit | 100 |
| Mesh Type | `Tight` |
| Filter Mode | `Bilinear` |
| Compression | `ASTC 6x6` (iOS), `ETC2` (Android) |
| Max Size | 2048 |
| Generate Mip Maps | OFF (UI) |

### 3.2 Texture Import Settings

```yaml
# Assets/_Project/Art/Blocks/*.png
TextureType: Sprite (2D and UI)
SpriteMode: Multiple
AlphaIsTransparency: true
GenerateMipMaps: false
WrapMode: Clamp
```

### 3.3 AssetPostprocessor 자동화

`Assets/_Project/Editor/TexturePostprocessor.cs` 를 신설하여 경로 기반 자동 설정:

```csharp
public class TexturePostprocessor : AssetPostprocessor {
    void OnPreprocessTexture() {
        var ti = (TextureImporter)assetImporter;
        if (assetPath.Contains("/Art/Blocks/"))  ApplyBlockPreset(ti);
        if (assetPath.Contains("/Art/UI/"))      ApplyUIPreset(ti);
        if (assetPath.Contains("/Particles/"))   ApplyParticlePreset(ti);
    }
    void ApplyBlockPreset(TextureImporter ti) {
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Bilinear;
        var ios = ti.GetPlatformTextureSettings("iPhone");
        ios.overridden = true;
        ios.format = TextureImporterFormat.ASTC_6x6;
        ios.maxTextureSize = 2048;
        ti.SetPlatformTextureSettings(ios);
    }
}
```

### 3.4 Audio Import Settings

```yaml
# SFX
LoadType: DecompressOnLoad
CompressionFormat: Vorbis
Quality: 0.7
ForceToMono: true
PreloadAudioData: true

# BGM
LoadType: Streaming
CompressionFormat: Vorbis
Quality: 0.8
ForceToMono: false
PreloadAudioData: false
```

---

## 4. 파일 네이밍 컨벤션

### 4.1 규칙

- 소문자 snake_case
- 접두사: `block_` / `ui_` / `sfx_` / `bgm_` / `particle_` / `gallery_` / `icon_` / `splash_`
- 버전: 기본 없음, 리비전 시 `_v2` 접미
- 해상도: `@1x` 기본, `@2x`/`@3x` 파생

### 4.2 디렉토리 구조

```
Assets/_Project/
├── Art/
│   ├── Blocks/
│   │   ├── block_red_128.png
│   │   ├── block_red_256.png
│   │   ├── block_red_512.png
│   │   └── BlocksAtlas.spriteatlas
│   ├── UI/
│   │   ├── Buttons/
│   │   ├── Panels/
│   │   ├── Icons/
│   │   └── UIAtlas.spriteatlas
│   ├── Particles/
│   └── Gallery/
│       └── Ch01/
├── Audio/
│   ├── SFX/
│   │   ├── sfx_mix_plop.ogg
│   │   └── ...
│   └── BGM/
│       ├── bgm_lobby.ogg
│       └── bgm_gameplay.ogg
├── Fonts/
│   ├── Pretendard/
│   └── Inter/
└── Editor/
    └── TexturePostprocessor.cs
```

### 4.3 예시

| 파일 | 유효성 |
|------|-------|
| `block_red_128.png` | OK |
| `ui_button_primary_pressed.png` | OK |
| `sfx_mix_plop.ogg` | OK |
| `bgm_gameplay.ogg` | OK |
| `particle_star_5.png` | OK |
| `gallery_ch01_piece_07.png` | OK |
| `BlockRed.png` (PascalCase) | NG |
| `sfx-mix-plop.wav` (WAV/하이픈) | NG |

---

## 5. Phase 별 우선순위

### 5.1 Phase 0 — Unity 설치 직후 (플레이스홀더)

**목표**: `BuildScript.cs` 로 iOS 빌드가 에셋 없이도 성공하도록 보장.

- **런타임 단색 스프라이트 생성**: `PlaceholderSpriteFactory.cs` 신설
  ```csharp
  public static Sprite Solid(Color c, int size = 128) {
      var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
      var pixels = new Color[size * size];
      for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
      tex.SetPixels(pixels); tex.Apply();
      return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
  }
  ```
- **Editor 더미 아이콘**: Unity 기본 흰색 정사각형 + 텍스트 "CMA" (Player Settings)
- **SFX**: `AudioClip.Create` 로 0.1초 사인파 런타임 생성 → `SfxPlayer` 가 null 체크
- **BGM**: 공백 (Mute)
- **검증 성공 조건**: `BuildScript.cs` 실행 → Xcode 프로젝트 생성 → 시뮬레이터 실행 → 보드 렌더링 OK

### 5.2 Phase 1 — 블록 실 스프라이트 + 원자 SFX (2 MD)

- 블록 13종 Figma → PNG export → Unity Import
- `BlocksAtlas.spriteatlas` 생성 및 `BlockView` 레퍼런스 교체
- SFX 7종 Freesound 다운로드 → Audacity 정규화 → Vorbis 변환
- `SfxPlayer.cs` 의 `SfxId → AudioClip` 매핑 완성

### 5.3 Phase 2 — UI 컴포넌트 + 모션 파티클 (3 MD)

- Kenney UI Pack 다운로드 → 색상 리터치 (Hue Shift to `design_system.md` 팔레트)
- 9-patch 슬라이스 편집기 설정
- 파티클 5종 텍스처 제작 (Photoshop 클라우드 브러시)
- `ParticleSystem` 프리셋 적용

### 5.4 Phase 3 — 갤러리 + BGM (3 MD)

- 고흐 "별이 빛나는 밤" → Python Pillow 픽셀화 스크립트 → 12조각 분할
- 썸네일/완성본 export
- BGM 2종 Incompetech 선곡 → `AudioMixer` 라우팅 (`MusicGroup`)

### 5.5 Phase 4 — Metaball 쉐이더 최적화 실에셋 (2 MD)

- Metaball용 거리장(SDF) 텍스처 2048×2048 생성
- `JellyMesh` 쉐이더의 `_NoiseTex` 교체 (현재 Perlin → 수제 텍스처)
- GPU Profiler 검증 (iPhone 8 기준 60fps 유지)

### 5.6 간트 요약

```
Phase 0 [Day 0]       ██ 플레이스홀더 (반나절)
Phase 1 [Day 1-2]     ████ 블록+SFX
Phase 2 [Day 3-5]     ██████ UI+파티클
Phase 3 [Day 6-8]     ██████ 갤러리+BGM
Phase 4 [Day 9-10]    ████ Metaball 실에셋
버퍼   [Day 11-14]    ████████ QA/리비전
```

---

## 6. 제작자 역할 분담 제안

| 역할 | 담당 | 산출물 | 예상 MD |
|------|------|--------|--------|
| **그래픽 아티스트 A** | 블록 13종 + UI 45종 | PNG, SpriteAtlas | 5 MD |
| **그래픽 아티스트 B** | 갤러리 12조각 + 앱 아이콘 + 스플래시 + 파티클 5종 | PNG, ICO | 4 MD |
| **오디오 디자이너** | SFX 7종 + BGM 2종 + AudioMixer 라우팅 | OGG, .mixer | 3 MD |
| **엔지니어 연동 (본인)** | Postprocessor, 플레이스홀더, Import 검증 | C#, .spriteatlas | 2 MD |
| **합계** | | | **14 MD** |

### 협업 채널

- Figma: UI/블록 드래프트 공유
- Notion/Linear: 에셋 요청 티켓 (파일명, 해상도, 데드라인 명시)
- Git LFS: 원본 PSD/AEP 버전 관리 (`.gitattributes` 설정 필요)

---

## 7. 가상 크레딧 예산

### 7.1 무료 경로 (권고)

| 카테고리 | 출처 | 라이선스 | 비용 |
|----------|------|---------|------|
| UI 베이스 | Kenney.nl UI Pack | CC0 | $0 |
| SFX | Freesound.org (CC0 필터) | CC0 | $0 |
| BGM | Incompetech (Kevin MacLeod) | CC-BY | $0 |
| 폰트 | Pretendard (길형진) | OFL | $0 |
| 명화 | WikiArt 퍼블릭 도메인 | PD | $0 |
| **합계** | | | **$0** |

### 7.2 저비용 경로 ($280)

- Midjourney 1개월 $30 (앱 아이콘 이터레이션 40장)
- 프리랜서 아이콘 리터치 $250 (Fiverr 전문가 5-day)

### 7.3 프리미엄 경로 ($2,100)

- Synty POLYGON Fantasy Rivals (UI 차용) $40
- Unity Asset Store Fantasy UI Premium $60
- SFX 외주 (Upwork 오디오 디자이너) $800
- BGM 작곡 외주 $700
- 앱 아이콘 전문 디자이너 $500

### 7.4 라이선스 추적 표

라이선스 의무 (CC-BY 등) 충족을 위해 `Assets/_Project/LICENSES.md` 작성 필수:

```markdown
# 서드파티 에셋 라이선스

## BGM
- bgm_lobby.ogg — "Carefree" by Kevin MacLeod (incompetech.com), CC-BY 4.0
- bgm_gameplay.ogg — "Pixel Peeker Polka" by Kevin MacLeod, CC-BY 4.0

## Font
- Pretendard — OFL 1.1, 길형진

## SFX
- sfx_mix_plop.ogg — Freesound user `PaulMorek`, CC0
- ...
```

---

## 8. 리스크 & 완화

| 리스크 | 영향 | 완화책 |
|--------|------|-------|
| 저작권 분쟁 | 출시 보류 | PD/CC0 우선, 라이선스 문서 유지 |
| AI 생성물 저작권 | 애플 심사 반려 가능 | AI 결과물은 아이콘만 사용 + 인간 후처리로 2차 저작물화 |
| 아틀라스 2048 초과 | iPhone 6 GPU OOM | SpriteAtlas Variant (2x / 3x) 분리 |
| 오디오 용량 초과 | 앱 번들 200MB | BGM Streaming 모드 + Vorbis 품질 0.7 |
| 한국어 폰트 atlas 용량 | TMP SDF 50MB+ | KR 상용 2350자만 subset, Dynamic SDF는 체크아웃 |

---

## 9. 승인 요청 사항

1. **제작 방식 D (하이브리드) + 무료 경로 $0** 승인 여부
2. Phase 1~4 우선순위 검토 (특히 갤러리와 BGM 순서 변경 가능)
3. Git LFS 도입 여부 (PSD/AEP 원본 관리용)
4. 아트 디렉터 지정 여부 (스타일 가이드 확정권자)

---

**문서 종료.** 본 Plan은 `docs/project_summary.md` 및 `Tasklist.md` 에 병합 예정.
