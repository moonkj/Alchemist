# Jelly Aggressive Patch — "말캉말캉" 긴급 증폭

> 작성자: Game Feel Engineer Teammate
> 작성일: 2026-04-22
> 대상: `Assets/_Project/Scripts/Bootstrap/MinimalGameScene.cs`
> 배경: v3 P급 개선 후에도 유저 피드백 "말캉한 느낌 전혀 없음, 애니메이션이 적용 안 되는 것 같다". 수치가 너무 보수적이라 **눈에 보이지 않는 수준**. 본 패치는 **진폭 2~3배, 다축 동시 변형, 드래그 물리 스프링 도입**을 통해 "이제야 살아있다" 체감을 확보한다.

---

## 1. 진단 — "왜 딱딱하게 느껴지는가" 근본원인 5가지

### R1. Idle 진폭이 지각 한계 미만 (±2.8%)
- 사람 눈은 2D 스프라이트의 스케일 변화를 **±4% 미만**에서 거의 인지 못 함 (특히 60fps 짧은 주기). ±2.8% × 0.9×(CellSize) = 약 **2픽셀 미만** 변동. 즉 "있으나 마나".
- 주기 2.2 rad/s (T=2.86s)는 느리고 균일해서 시선이 정지 상태로 오판.
- X/Y 모두 `sin(t×2.2+φ)` 유사 주파수라 XY 차이가 크지 않음 → **스케일 단순 "커졌다 작아졌다"**로만 보이고 squash-stretch 질감이 안 살아남.

### R2. Drag 중 블록이 손가락과 완전히 **붙어** 있음 (lag 0)
- `_blocks[r,c].transform.position = new Vector3(world.x, world.y, 0f)` — 즉시 텔레포트.
- 젤리가 느껴지려면 **손가락 < 블록 < 관성 lag** 의 시차가 필요. 현재는 시차가 없으니 뇌는 "강체가 끌려다닌다"로 해석.
- stretch 방향 없음 (Y=X=1.15 isotropic) → "부풀어 오른 박스" 그 이상도 이하도 아님.

### R3. Gravity 착지가 **선형 감쇠 Lerp(pos, base, dt×12)**
- Lerp 방식은 exponential decay라 **착지 순간의 임팩트가 증발**. 시각적으로는 부드럽게 멈추기만 해서 "무게감/탄성" 둘 다 소실.
- Ease-Out-Bounce 또는 landing squash(Y 수축) 없음 → 블록이 **공기처럼 사라짐**.
- 열별 스태거가 없어 보드 전체가 동기화된 단일 덩어리로 이동 → 각 블록이 "개체"로 안 느껴짐.

### R4. Refill 진입은 스프링 없는 0.9→1.0 선형 수렴
- 새 블록이 위에서 떨어져 **아무런 환영 인사 없이** 딱 자리잡음. 오버슈트 0 = 깜짝 팝 0 = 각인 0.
- 알파 fade-in도 없어 **이미 있었던 것처럼** 튀어나와 등장감 제로.

### R5. Mix Bounce 자체는 괜찮으나 **잔향(에코)**이 없음
- 0.7→1.45→1.0으로 끝나면 1.0 안착 직후 즉시 정지. 실제 젤리/액체는 **수렴 후에도 미세 sin 감쇠 진동**이 남음. 이 "잔향"이 없으면 "애니메이션 끝났네" → 딱딱 인지 복귀.

### (보조) R6. 회전 성분 zero — 모든 블록이 축을 맞춘 정렬 상태 유지
- 인간의 liveliness 감지는 **회전 미세 wobble**에 매우 예민. ±1~2도만 돌아도 "살아있다" 신호.

---

## 2. 공격적 값 상향 + 신규 애니 레이어

### A. Idle 브리딩 강화

| 항목 | 이전 | 권고 | 변화 |
|---|---|---|---|
| 스케일 진폭 (X) | ±2.8% | **±7.0%** | 2.5배 |
| 스케일 진폭 (Y) | ±2.2% | **±8.5%** (X와 반위상) | 3.9배 |
| 주기 (X) | 2.2 rad/s | **3.0 rad/s** (T≈2.1s) | + 36% |
| 주기 (Y) | 2.4 rad/s | **3.4 rad/s** (반위상 π) | 다른 주기로 리사주 |
| 회전 wobble | 없음 | **±1.5°** @ 1.4 rad/s | 신규 |
| Lerp 수렴 속도 | dt×8 | **dt×14** | 반응 더 빠르게 |

**수식 (핵심):**
```
breatheX = 1 + sin(t × 3.0 + phase) × 0.070
breatheY = 1 + sin(t × 3.4 + phase + π) × 0.085   // 반위상 → 명확한 squash-stretch
rotZ     = sin(t × 1.4 + phase × 1.3) × 1.5       // 도 단위
```
X가 부풀면 Y가 수축하는 **부피 보존형** 애니메이션이 되어 "젤리 숨쉬기"가 확실히 인지된다.

---

### B. Drag follow 물리 (Critically-damped spring + velocity stretch)

**핵심:** 블록이 손가락을 **0.08초 정도 뒤따라**오게 하고, 이동 방향으로 길쭉해지고 수직으로 납작해진다.

| 항목 | 이전 | 권고 |
|---|---|---|
| Position follow | teleport | `Vector3.SmoothDamp` smoothTime **0.08s**, maxSpeed 40 |
| Scale X (진행방향) | 1.15 등방 | **1.0 + 0.28 × clamp(|v|/6, 0, 1)** (최대 1.28) |
| Scale Y (수직) | 1.15 등방 | **1.0 − 0.18 × clamp(|v|/6, 0, 1)** (최소 0.82, 부피 보존 흉내) |
| Rotation | 0 | `atan2(v.y, v.x) × Rad2Deg × 0.18` 최대 ±10° |
| 속도 EMA 스무딩 | — | `vel = vel×0.75 + (Δpos/Δt)×0.25` |
| Lift(Z) | 0 | 정렬 order +10 (이미 적용), z=-0.1 오프셋 |

`SmoothDamp`는 critically-damped spring이라 **overshoot 없이 부드럽게 추격**. 이게 "젤리 물리"의 정답.

**코드 (OnDragMove):**
```csharp
private Vector3 _dragVel;     // SmoothDamp 속도 상태
private Vector3 _lastDragWorld;
private Vector2 _dragVelEma;  // 속도 EMA (stretch용)

private void OnDragMove(Vector3 world)
{
    if (_dragR < 0) return;
    var tr = _blocks[_dragR, _dragC].transform;

    // 1) 속도 EMA (stretch 방향 계산)
    Vector2 inst = (world - _lastDragWorld) / Mathf.Max(0.0001f, Time.deltaTime);
    _dragVelEma = _dragVelEma * 0.75f + inst * 0.25f;
    _lastDragWorld = world;

    // 2) 블록 위치: 손가락을 스프링으로 뒤따라감 (lag 약 0.08s)
    Vector3 target = new Vector3(world.x, world.y, -0.1f);
    tr.position = Vector3.SmoothDamp(tr.position, target, ref _dragVel, 0.08f, 40f);

    // 3) 속도 기반 squash-stretch (x 길쭉, y 납작)
    float speedN = Mathf.Clamp01(_dragVelEma.magnitude / 6f);
    float sx = 1.0f + 0.28f * speedN;
    float sy = 1.0f - 0.18f * speedN;
    // 드래그 기본 "들어올림" 1.12 배율 위에 stretch 적용
    tr.localScale = new Vector3(CellSize * 1.12f * sx, CellSize * 1.12f * sy, CellSize);

    // 4) 진행 방향으로 회전 (속도가 충분할 때만, 작을 땐 0 수렴)
    float rotZ = 0f;
    if (_dragVelEma.sqrMagnitude > 0.25f)
        rotZ = Mathf.Atan2(_dragVelEma.y, _dragVelEma.x) * Mathf.Rad2Deg * 0.18f;
    rotZ = Mathf.Clamp(rotZ, -10f, 10f);
    tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.Euler(0f, 0f, rotZ), Time.deltaTime * 12f);
}
```
**주의:** `OnDragBegin`에서 `_dragVel = Vector3.zero; _lastDragWorld = world; _dragVelEma = Vector2.zero;` 초기화, `OnDragEnd` 직전 `tr.rotation = Quaternion.identity;` 복귀.

---

### C. Gravity 착지 바운스 (Ease-Out-Bounce + Y squash + 열 스태거)

| 항목 | 이전 | 권고 |
|---|---|---|
| 낙하 커브 | `Lerp(dt×12)` 선형 감쇠 | **Ease-Out-Bounce 320ms** |
| 열별 스태거 | 없음 | c번째 열 시작 지연 **(c%3) × 25ms** (0/25/50 rotate) |
| 착지 Y squash | 없음 | **Y 0.82 → 1.0, 140ms, ease_out_back** |
| 착지 X 반대 | 없음 | **X 1.10 → 1.0** (부피 보존 반대 성분) |
| 낙하 sfx | 없음 | (옵션) 짧은 피치 떨어지는 pluck |

**Ease-Out-Bounce 수식:**
```
easeOutBounce(t):
  n1 = 7.5625; d1 = 2.75
  if t < 1/d1:       return n1 * t * t
  else if t < 2/d1:  t -= 1.5/d1; return n1 * t * t + 0.75
  else if t < 2.5/d1: t -= 2.25/d1; return n1 * t * t + 0.9375
  else:               t -= 2.625/d1; return n1 * t * t + 0.984375
```
낙하: `y = startY + (endY - startY) × easeOutBounce(t/dur)`

---

### D. Refill 등장 스프링 오버슈트

| 항목 | 이전 | 권고 |
|---|---|---|
| 스케일 커브 | 0.9 → 1.0 선형 | **0.30 → 1.15 → 1.00 spring_soft 280ms** |
| 알파 | 1 즉시 | **0 → 1, 180ms ease_out_quart** |
| 행 스태거 | 없음 | r번째 행 지연 **r × 18ms** |

**스케일 3구간:**
- 0~35% : 0.30 → 1.15 (ease_out_cubic, pop up)
- 35~70%: 1.15 → 0.92 (ease_in_out_sine, undershoot)
- 70~100%: 0.92 → 1.00 (ease_out_quad, settle)

---

### E. Mix Bounce 잔향 에코 (신규 레이어)

기존 0.7→1.45→1.0 (450ms) 는 유지. **끝난 직후** 250ms 동안 **감쇠 sin 진동** 추가:

```
echoDur = 0.25s
echo(t) = 1 + sin(t × 2π × 4.5) × 0.055 × (1 - t/echoDur)^2   // ±5.5% 시작, 2차 감쇠
// X에는 echo, Y에는 역위상 echo 적용
```
이 **에코**는 "액체가 출렁거리며 멈춘다"는 물리적 사실성을 준다. 빠진 250ms의 대가는 거의 없고 체감은 크다.

---

### F. Idle 회전 드리프트 (옵션, 강력 추천)

A에 이미 `rotZ = sin(t × 1.4 + phase×1.3) × 1.5°` 로 포함됨. 각 블록 `_jellyPhase` 값이 이미 0~2π 랜덤이라 **블록마다 기울기가 다른 순간**이 있어 격자 전체가 **정렬되지 않은 살아있는 조직**처럼 보인다.

---

## 3. 붙여넣기 가능한 C# 스니펫 (완전판)

리더는 `MinimalGameScene.cs` 에서 아래 4개 메서드를 **교체/신규 추가** 하면 된다.

### 3.1 상단 필드 (클래스 멤버) — 추가

```csharp
// ---- Drag follow 물리 ----
private Vector3 _dragVel;         // SmoothDamp 내부 상태
private Vector3 _lastDragWorld;   // 손가락 마지막 위치 (속도 계산용)
private Vector2 _dragVelEma;      // EMA 스무딩된 속도
```

### 3.2 UpdateScaleDecay 교체 (Idle 브리딩 강화 + 회전 wobble)

```csharp
private void UpdateScaleDecay()
{
    // WHY: ±2.8% 진폭은 지각 한계 미만 → ±7~8.5% 로 상향 + XY 반위상 squash-stretch
    //      + 회전 ±1.5° wobble 로 "살아있는 조직" 체감 확보.
    for (int r = 0; r < Rows; r++)
    for (int c = 0; c < Cols; c++)
    {
        if (_blocks[r, c] == null || !_blocks[r, c].enabled) continue;
        if (_dragR == r && _dragC == c) continue;
        if (_bouncing[r, c]) continue;

        var tr = _blocks[r, c].transform;
        float ph = _jellyPhase[r, c];
        // X, Y 서로 반위상 + 서로 다른 주기 → 확실한 squash-stretch
        float breatheX = 1f + Mathf.Sin(Time.time * 3.0f + ph) * 0.070f;
        float breatheY = 1f + Mathf.Sin(Time.time * 3.4f + ph + Mathf.PI) * 0.085f;
        Vector3 targetScale = new Vector3(CellSize * breatheX, CellSize * breatheY, CellSize);
        tr.localScale = Vector3.Lerp(tr.localScale, targetScale, Time.deltaTime * 14f);

        // 미세 회전 wobble — 블록마다 위상/속도 살짝 달라 전체가 정렬되지 않음
        float rotZ = Mathf.Sin(Time.time * 1.4f + ph * 1.3f) * 1.5f;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, rotZ);
        tr.rotation = Quaternion.Slerp(tr.rotation, targetRot, Time.deltaTime * 10f);

        // 위치는 약간 더 빠르게 베이스로 수렴 (기존 12 → 14)
        tr.position = Vector3.Lerp(tr.position, _basePos[r, c], Time.deltaTime * 14f);
    }
}
```

### 3.3 OnDragBegin / OnDragMove / OnDragEnd 교체

```csharp
private void OnDragBegin(Vector3 world)
{
    if (!FindCell(world, out int r, out int c)) return;
    if (_colorGrid[r, c] == ColorId.None) return;
    _dragR = r; _dragC = c;
    _blocks[r, c].sortingOrder = 10;

    // 물리 상태 초기화
    _dragVel = Vector3.zero;
    _lastDragWorld = world;
    _dragVelEma = Vector2.zero;
}

private void OnDragMove(Vector3 world)
{
    if (_dragR < 0) return;
    var tr = _blocks[_dragR, _dragC].transform;

    // 1) 손가락 속도 (EMA 스무딩 — 프레임 편차 흡수)
    float dt = Mathf.Max(0.0001f, Time.deltaTime);
    Vector2 inst = (Vector2)(world - _lastDragWorld) / dt;
    _dragVelEma = _dragVelEma * 0.75f + inst * 0.25f;
    _lastDragWorld = world;

    // 2) 위치: SmoothDamp (critically-damped, lag 약 0.08s) → "따라잡지 못하는" 젤리감
    Vector3 target = new Vector3(world.x, world.y, -0.1f);
    tr.position = Vector3.SmoothDamp(tr.position, target, ref _dragVel, 0.08f, 40f);

    // 3) 속도 기반 squash-stretch (부피 보존 흉내)
    float speedN = Mathf.Clamp01(_dragVelEma.magnitude / 6f);
    float sx = 1.0f + 0.28f * speedN;
    float sy = 1.0f - 0.18f * speedN;
    float lift = 1.12f; // 드래그 중 기본 확대
    tr.localScale = new Vector3(CellSize * lift * sx, CellSize * lift * sy, CellSize);

    // 4) 진행 방향 rotation (속도 작을 땐 0 수렴)
    float rotZ = 0f;
    if (_dragVelEma.sqrMagnitude > 0.25f)
        rotZ = Mathf.Atan2(_dragVelEma.y, _dragVelEma.x) * Mathf.Rad2Deg * 0.18f;
    rotZ = Mathf.Clamp(rotZ, -10f, 10f);
    tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.Euler(0f, 0f, rotZ), dt * 12f);
}

private void OnDragEnd(Vector3 world)
{
    if (_dragR < 0) return;
    int sr = _dragR, sc = _dragC;
    _dragR = _dragC = -1;
    _blocks[sr, sc].sortingOrder = 0;
    _blocks[sr, sc].transform.rotation = Quaternion.identity;  // 회전 리셋은 SpringBack 에서 서서히

    if (!FindCell(world, out int tr2, out int tc2) || (tr2 == sr && tc2 == sc)) { SnapBack(sr, sc); return; }
    if (!IsAdjacent(sr, sc, tr2, tc2)) { SnapBack(sr, sc); return; }
    SnapBack(sr, sc);
    TryMix(sr, sc, tr2, tc2);
}
```

### 3.4 MixBounceAnim 교체 — 기존 시퀀스 + **에코 잔향**

```csharp
private IEnumerator MixBounceAnim(int tr, int tc, ColorId fromColor, ColorId toColor)
{
    if (tr < 0 || tc < 0) yield break;
    _bouncing[tr, tc] = true;
    var sr = _blocks[tr, tc];
    var t = sr.transform;
    Color a = ColorToUnity(fromColor);
    Color b = ColorToUnity(toColor);

    // --- Phase 1: 3단 squash-stretch 바운스 (0.45s, 기존) ---
    float dur = 0.45f;
    float elapsed = 0f;
    while (elapsed < dur)
    {
        float u = elapsed / dur;
        float s = u < 0.25f
            ? Mathf.Lerp(1f, 0.70f, u / 0.25f)
            : u < 0.55f
                ? Mathf.Lerp(0.70f, 1.45f, (u - 0.25f) / 0.30f)
                : Mathf.Lerp(1.45f, 1.0f, (u - 0.55f) / 0.45f);
        float sy = u < 0.25f
            ? Mathf.Lerp(1f, 1.30f, u / 0.25f)
            : u < 0.55f
                ? Mathf.Lerp(1.30f, 0.75f, (u - 0.25f) / 0.30f)
                : Mathf.Lerp(0.75f, 1.0f, (u - 0.55f) / 0.45f);
        t.localScale = new Vector3(CellSize * s, CellSize * sy, CellSize);

        float cu = Mathf.Clamp01(u / 0.45f);
        sr.color = Color.Lerp(a, b, EaseInOutCubic(cu));
        elapsed += Time.deltaTime;
        yield return null;
    }

    // --- Phase 2: 에코 잔향 (±5.5% 감쇠 sin, 0.25s) ---
    // WHY: 젤리/액체는 멈춘 뒤에도 미세 진동. 잔향이 "살아있다" 체감 핵심.
    float echoDur = 0.25f;
    float echoFreq = 4.5f; // Hz 상당 (주기 약 0.22s)
    float echoAmp = 0.055f;
    float eT = 0f;
    while (eT < echoDur)
    {
        float u = eT / echoDur;
        float decay = (1f - u) * (1f - u); // 2차 감쇠
        float ex = 1f + Mathf.Sin(eT * 2f * Mathf.PI * echoFreq) * echoAmp * decay;
        float ey = 1f - Mathf.Sin(eT * 2f * Mathf.PI * echoFreq) * echoAmp * decay; // 반위상
        t.localScale = new Vector3(CellSize * ex, CellSize * ey, CellSize);
        eT += Time.deltaTime;
        yield return null;
    }

    t.localScale = Vector3.one * CellSize;
    sr.color = b;
    _bouncing[tr, tc] = false;
}
```

### 3.5 신규 GravityLandBounce — `ApplyGravity` 호출 후 코루틴으로 돌림

```csharp
/// <summary>이동된 블록 각각에 대해 열 스태거 + Ease-Out-Bounce 낙하 + 착지 Y-squash.</summary>
private IEnumerator GravityLandBounce(int row, int col, Vector3 fromPos, Vector3 toPos)
{
    if (_blocks[row, col] == null) yield break;
    _bouncing[row, col] = true;
    var t = _blocks[row, col].transform;

    // 열별 스태거 지연 (c%3 × 25ms)
    float delay = (col % 3) * 0.025f;
    if (delay > 0f) yield return new WaitForSeconds(delay);

    // Phase 1: Ease-Out-Bounce 낙하 (320ms)
    float fallDur = 0.32f;
    float fT = 0f;
    while (fT < fallDur)
    {
        float u = fT / fallDur;
        float e = EaseOutBounce(u);
        t.position = Vector3.LerpUnclamped(fromPos, toPos, e);
        fT += Time.deltaTime;
        yield return null;
    }
    t.position = toPos;

    // Phase 2: 착지 Y squash — Y 0.82 → 1.0, X 1.10 → 1.0 (140ms, ease_out_back)
    float squashDur = 0.14f;
    float sT = 0f;
    while (sT < squashDur)
    {
        float u = sT / squashDur;
        float e = EaseOutBack(u);
        float sy = Mathf.LerpUnclamped(0.82f, 1.0f, e);
        float sx = Mathf.LerpUnclamped(1.10f, 1.0f, e);
        t.localScale = new Vector3(CellSize * sx, CellSize * sy, CellSize);
        sT += Time.deltaTime;
        yield return null;
    }
    t.localScale = Vector3.one * CellSize;
    _bouncing[row, col] = false;
}

private static float EaseOutBounce(float t)
{
    const float n1 = 7.5625f, d1 = 2.75f;
    if (t < 1f / d1)       return n1 * t * t;
    else if (t < 2f / d1) { t -= 1.5f / d1;   return n1 * t * t + 0.75f; }
    else if (t < 2.5f / d1){ t -= 2.25f / d1;  return n1 * t * t + 0.9375f; }
    else                   { t -= 2.625f / d1; return n1 * t * t + 0.984375f; }
}

private static float EaseOutBack(float t)
{
    const float c1 = 1.70158f, c3 = c1 + 1f;
    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
}
```

**ApplyGravity 수정 가이드 (최소 침습):**
현재 `ApplyGravity()` 는 `_blocks[writeRow, c].transform.position = _basePos[r, c];` 로 바로 "위 위치"에 두고 `UpdateScaleDecay` 의 Lerp가 감쇠시키고 있음. 이를 다음과 같이 바꾼다:
```csharp
// 교체:
Vector3 fromPos = _basePos[r, c];          // 시작 (기존 위)
Vector3 toPos   = _basePos[writeRow, c];   // 도착
_blocks[writeRow, c].transform.position = fromPos;
StartCoroutine(GravityLandBounce(writeRow, c, fromPos, toPos));
```
이로써 선형 감쇠 대신 **Bounce + Squash** 가 적용된다.

### 3.6 신규 RefillSpawnAnim — `Refill()` 에서 호출

```csharp
/// <summary>Refill 블록의 스프링 등장 (scale 0.30 → 1.15 → 1.00) + 알파 fade-in + 행 스태거.</summary>
private IEnumerator RefillSpawnAnim(int row, int col, Vector3 spawnPos, Vector3 targetPos)
{
    if (_blocks[row, col] == null) yield break;
    _bouncing[row, col] = true;
    var sr = _blocks[row, col];
    var t = sr.transform;

    // 행별 스태거 (r × 18ms)
    float delay = row * 0.018f;
    if (delay > 0f) yield return new WaitForSeconds(delay);

    // 알파 초기화
    Color baseCol = sr.color;
    baseCol.a = 0f; sr.color = baseCol;

    // 낙하 + 스프링 스케일 (총 280ms)
    float dur = 0.28f;
    float elapsed = 0f;
    while (elapsed < dur)
    {
        float u = elapsed / dur;

        // 위치: fromPos → targetPos, ease_out_quart
        float pe = 1f - Mathf.Pow(1f - u, 4f);
        t.position = Vector3.LerpUnclamped(spawnPos, targetPos, pe);

        // 스케일 3구간: 0.30 → 1.15 → 0.92 → 1.00
        float s;
        if (u < 0.35f)      s = Mathf.Lerp(0.30f, 1.15f, EaseOutCubic(u / 0.35f));
        else if (u < 0.70f) s = Mathf.Lerp(1.15f, 0.92f, (u - 0.35f) / 0.35f);
        else                s = Mathf.Lerp(0.92f, 1.00f, (u - 0.70f) / 0.30f);
        t.localScale = Vector3.one * (CellSize * s);

        // 알파 fade-in (180ms, ease_out_quart)
        float aU = Mathf.Clamp01(elapsed / 0.18f);
        var col = sr.color;
        col.a = 1f - Mathf.Pow(1f - aU, 4f);
        sr.color = col;

        elapsed += Time.deltaTime;
        yield return null;
    }
    t.position = targetPos;
    t.localScale = Vector3.one * CellSize;
    var finalCol = sr.color; finalCol.a = 1f; sr.color = finalCol;
    _bouncing[row, col] = false;
}

private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
```

**Refill() 수정 가이드:**
```csharp
// 기존:
// _blocks[r, c].transform.position = new Vector3(_basePos[r, c].x, spawnY, 0f);
// _blocks[r, c].transform.localScale = Vector3.one * (CellSize * 0.9f);
// 교체:
Vector3 spawn = new Vector3(_basePos[r, c].x, spawnY, 0f);
Vector3 tgt   = _basePos[r, c];
_blocks[r, c].transform.position = spawn;
_blocks[r, c].transform.localScale = Vector3.one * (CellSize * 0.30f);
StartCoroutine(RefillSpawnAnim(r, c, spawn, tgt));
```

---

## 4. 수치 최종 테이블 (리더 체크용)

| 파라미터 | 이전 | 권고 | 배수 |
|---|---|---|---|
| Idle scale amplitude (X) | ±2.8% | **±7.0%** | ×2.5 |
| Idle scale amplitude (Y) | ±2.2% | **±8.5%** (반위상) | ×3.9 |
| Idle frequency (X) | 2.2 rad/s | **3.0 rad/s** | ×1.36 |
| Idle frequency (Y) | 2.4 rad/s | **3.4 rad/s** (+π offset) | ×1.42 |
| Idle 회전 wobble | 없음 | **±1.5° @ 1.4 rad/s** | 신규 |
| Idle 위치 Lerp | dt×12 | **dt×14** | ×1.17 |
| Drag follow 물리 | teleport | **SmoothDamp smoothTime 0.08s** (maxSpeed 40) | 신규 |
| Drag velocity stretch (X) | 없음 | **k_x = +0.28, |v|/6 clamp** | 신규 |
| Drag velocity stretch (Y) | 없음 | **k_y = −0.18** (부피보존) | 신규 |
| Drag rotation | 없음 | **atan2(vy,vx) × 0.18, ±10° clamp** | 신규 |
| Drag 속도 EMA α | — | **0.25 (new) / 0.75 (prev)** | 신규 |
| Gravity fall curve | 선형 Lerp(dt×12) | **Ease-Out-Bounce 320ms** | 신규 커브 |
| Gravity 열 스태거 | 없음 | **(col % 3) × 25ms** | 신규 |
| Gravity 착지 squash | 없음 | **Y 0.82 → 1.0, X 1.10 → 1.0, 140ms, ease_out_back** | 신규 |
| Refill 스케일 커브 | 0.9 → 1.0 선형 | **0.30 → 1.15 → 0.92 → 1.00 spring_soft 280ms** | 신규 |
| Refill 알파 fade | 즉시 | **0 → 1, 180ms ease_out_quart** | 신규 |
| Refill 행 스태거 | 없음 | **row × 18ms** | 신규 |
| Mix echo sin decay | 없음 | **±5.5%, 4.5Hz, 250ms, (1-u)² 감쇠** | 신규 |

---

## 5. 주의 사항 / 리스크

- **회전 누적 리셋**: Drag 끝에서 `rotation = identity` 로 강제 리셋하지 않으면 `UpdateScaleDecay` 의 wobble과 겹쳐 첫 프레임에 확 돌아감. Slerp로 수렴시키도록 조치해 둔 상태.
- **SmoothDamp 의 `_dragVel` 상태**: `OnDragBegin` 에서 반드시 Vector3.zero 초기화. 안 그러면 직전 드래그의 관성이 남아 첫 프레임 끌림.
- **_bouncing 플래그 충돌**: GravityLandBounce / RefillSpawnAnim 이 `_bouncing[r,c]=true` 중이면 Idle 브리딩이 건너뛰므로 연속 캐스케이드 시에도 깜박임 없음. 단 MixBounce 가 끝나기 전 블록이 폭발 대상이 되면 동시 코루틴이 겹칠 수 있으니 폭발 직전 `StopCoroutine` 보다는 `_bouncing=false` 로 Idle 복귀 후 ExplodeAnim 실행이 안전.
- **성능**: Rows × Cols = 42 블록 × SmoothDamp 1회 + Quaternion.Slerp 1회 = 무시 가능. 모바일 60fps 유지.
- **Reduce Motion 대응**: 유저가 추후 OS 접근성 토글 시 amp × 0.3, rotation 0, echo dur 0 로 스케일하는 `_motionScale` 플래그 도입 권장 (본 패치 범위 밖).

---

## 6. 기대 체감 변화

| 구간 | Before | After |
|---|---|---|
| 대기 중 보드 | 정적 격자 (±2픽셀 변동 인지 불가) | **젤리 조직** (±6~7픽셀, 회전 살짝) |
| 블록 집을 때 | 단순 1.15× 등방 확대 | **들어올림 + 길쭉** (방향 stretch + 회전) |
| 드래그 이동 | 손가락에 딱 붙은 강체 | **0.08s 뒤따르는 젤리 질량감** |
| 가운데로 놓음 (Mix) | 바운스 후 뚝 멈춤 | 바운스 + **잔향 진동** (액체 출렁임) |
| 매치 후 낙하 | 부드럽게 제자리 | **통 튀기는 Ease-Out-Bounce + 착지 눌림** |
| 새 블록 등장 | 이미 있었던 것처럼 | **팝 up + 오버슈트 + 페이드인** |

종합: "애니메이션 적용 안 된 것 같다" → "과하게 움직여서 어지럽다" 수준으로 **반대편 과제**가 뜰 것. 그 경우 `_motionScale = 0.7` 로 일괄 축소할 수 있는 마스터 게인을 다음 스프린트에 추가한다.

---

문서 끝.
