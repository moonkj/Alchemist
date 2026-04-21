#!/usr/bin/env bash
# perf_evaluate.sh — 야간 성능 결과 평가 스크립트
# 사용: perf_evaluate.sh <results_dir> <threshold_ms>
# 동작:
#   - <results_dir> 하위의 JUnit/JSON 결과에서 "avgFrameMs" 추출
#   - threshold 를 초과하는 케이스가 하나라도 있으면 exit 1
#   - 회귀가 있으면 stdout 에 요약 리포트 출력
# 제약:
#   - 실제 성능 테스트 아직 미구현 시 "no results" 로 정상 종료(exit 0)
#     → CI 가 첫 릴리스 전에도 깨지지 않게.
set -euo pipefail

RESULTS_DIR="${1:-PerfResults}"
THRESHOLD_MS="${2:-16.7}"

if [[ ! -d "$RESULTS_DIR" ]]; then
  echo "[perf] results dir 없음: $RESULTS_DIR — skip"
  exit 0
fi

shopt -s nullglob
JSON_FILES=("$RESULTS_DIR"/**/*.json "$RESULTS_DIR"/*.json)
if [[ ${#JSON_FILES[@]} -eq 0 ]]; then
  echo "[perf] 결과 JSON 없음 — 성능 테스트 미구현 단계로 간주, skip"
  exit 0
fi

FAIL=0
echo "[perf] threshold: ${THRESHOLD_MS} ms/frame"
for f in "${JSON_FILES[@]}"; do
  # avgFrameMs 키 추출 (jq 가 없을 수도 있어 fallback)
  if command -v jq >/dev/null 2>&1; then
    AVG=$(jq -r '.avgFrameMs // empty' "$f" 2>/dev/null || echo "")
    SCENARIO=$(jq -r '.scenario // .name // "unknown"' "$f" 2>/dev/null || echo "unknown")
  else
    AVG=$(grep -oE '"avgFrameMs"[[:space:]]*:[[:space:]]*[0-9.]+' "$f" | head -1 | grep -oE '[0-9.]+$' || echo "")
    SCENARIO=$(basename "$f" .json)
  fi

  if [[ -z "$AVG" ]]; then
    echo "  - [skip] $f (avgFrameMs 없음)"
    continue
  fi

  # bash 부동소수 비교는 awk 로
  OVER=$(awk -v a="$AVG" -v t="$THRESHOLD_MS" 'BEGIN{print (a+0 > t+0) ? "1" : "0"}')
  if [[ "$OVER" == "1" ]]; then
    echo "  - [FAIL] $SCENARIO: ${AVG} ms (> ${THRESHOLD_MS})"
    FAIL=1
  else
    echo "  - [ok]   $SCENARIO: ${AVG} ms"
  fi
done

if [[ "$FAIL" == "1" ]]; then
  echo "[perf] 회귀 감지 — Issue 자동 생성 단계로 넘어감"
  exit 1
fi
echo "[perf] 모든 시나리오 threshold 이내"
exit 0
