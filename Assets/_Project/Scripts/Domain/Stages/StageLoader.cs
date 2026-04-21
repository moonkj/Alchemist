using UnityEngine;

namespace Alchemist.Domain.Stages
{
    /// <summary>
    /// Resources 기반 스테이지 로더(Phase 2는 Addressables 미사용).
    /// WHY: 최소 의존 + 편집기/런타임 모두 동작. Addressables 전환 시 이 클래스만 교체.
    /// 기본 경로: Resources/Stages/{id}.asset → `Stages/{id}` 로 Load.
    /// </summary>
    public static class StageLoader
    {
        public const string ResourcesRoot = "Stages";

        /// <summary>id 로 StageData 로드. 실패 시 null.</summary>
        public static StageData Load(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return null;
            string path = ResourcesRoot + "/" + stageId;
            return Resources.Load<StageData>(path);
        }

        /// <summary>로드 실패 시 fallback 을 반환(ScriptableObject 생성하지 않음; 주어진 것 그대로 반환).</summary>
        public static StageData LoadOrFallback(string stageId, StageData fallback)
        {
            var s = Load(stageId);
            return s != null ? s : fallback;
        }

        /// <summary>
        /// 런타임용 프로그램 생성 StageData (에셋 없이 사용 가능).
        /// WHY: 초기 개발/테스트에서 SO 에셋 작성 전에도 GameRoot 가 합리적 기본값으로 부팅.
        /// </summary>
        public static StageData CreateDefault()
        {
            var s = ScriptableObject.CreateInstance<StageData>();
            s.name = "DefaultStage";
            // 기본값 초기화는 SerializeField 기본값에 의존(코드로 덮어쓸 필요 없음).
            return s;
        }
    }
}
