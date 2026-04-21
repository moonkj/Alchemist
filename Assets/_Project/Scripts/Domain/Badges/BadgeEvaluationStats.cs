namespace Alchemist.Domain.Badges
{
    /// <summary>
    /// 배지 판정에 쓰이는 "IPromptContext 외" 부가 통계 컨테이너.
    /// WHY struct: 턴마다 할당 없이 값 전달. 필드 추가 시 BadgeCondition.Evaluate 시그니처 보존.
    /// </summary>
    public readonly struct BadgeEvaluationStats
    {
        /// <summary>스테이지가 시작할 때 확정된 목표 이동 수(Par). 0 이면 미지정.</summary>
        public readonly int ParMoves;

        /// <summary>스테이지의 이동 한도(하드캡). 0 이면 미지정.</summary>
        public readonly int MoveLimit;

        /// <summary>프롬프트 Goal 진행률[0,1]. 1 이면 완전 달성.</summary>
        public readonly float PromptProgress;

        /// <summary>프롬프트 Goal All+Any 평가 결과(완전 충족).</summary>
        public readonly bool PromptSatisfied;

        /// <summary>스테이지 종료 이벤트인지(턴 중 평가 vs 스테이지 종료).</summary>
        public readonly bool IsStageEnd;

        public BadgeEvaluationStats(
            int parMoves,
            int moveLimit,
            float promptProgress,
            bool promptSatisfied,
            bool isStageEnd)
        {
            ParMoves = parMoves;
            MoveLimit = moveLimit;
            PromptProgress = promptProgress;
            PromptSatisfied = promptSatisfied;
            IsStageEnd = isStageEnd;
        }

        public static BadgeEvaluationStats MidStage(int parMoves, int moveLimit)
        {
            return new BadgeEvaluationStats(parMoves, moveLimit, 0f, false, false);
        }
    }
}
