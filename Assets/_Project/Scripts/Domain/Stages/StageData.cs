using System;
using UnityEngine;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Prompts;

namespace Alchemist.Domain.Stages
{
    /// <summary>
    /// 스테이지 정적 데이터 SO. 런타임 편집 금지(ReadOnly).
    /// WHY(D22): parMoves(Efficiency 분모) 와 maxMoves(하드캡)를 분리해
    /// Phase 1 의 "par==limit" 단순화를 해소. 스테이지별 난이도 정의.
    /// 특수 블록 초기 배치는 BlockPlacement 배열로 데이터 주도(데이터 변경 → 코드 변경 無).
    /// </summary>
    [CreateAssetMenu(fileName = "Stage", menuName = "Alchemist/Stage Data", order = 100)]
    public sealed class StageData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _stageId = "stage.unknown";

        [Header("Moves (D22: par ≠ maxMoves)")]
        [Tooltip("Efficiency 분모. 0 이하면 효율 보너스 미적용.")]
        [SerializeField] private int _parMoves = 10;

        [Tooltip("하드캡. 초과 시 스테이지 종료.")]
        [SerializeField] private int _maxMoves = 15;

        [Header("Seeding")]
        [SerializeField] private int _boardSeed = 12345;

        [Header("Palette")]
        [Tooltip("0~3 슬롯. 1→2→3 점진 해제 제어.")]
        [SerializeField] private int _paletteSlotCount = 1;

        [Header("Initial Special Placements")]
        [SerializeField] private BlockPlacement[] _initialPlacements;

        public string StageId { get { return _stageId; } }
        public int ParMoves { get { return _parMoves; } }
        public int MaxMoves { get { return _maxMoves; } }
        public int BoardSeed { get { return _boardSeed; } }
        public int PaletteSlotCount { get { return _paletteSlotCount; } }
        public BlockPlacement[] InitialPlacements
        {
            // WHY: 외부 수정 방지를 위해 null 을 빈 배열로 치환해 반환(캐시 없음 — SO 변경 빈도 낮음).
            get { return _initialPlacements ?? Array.Empty<BlockPlacement>(); }
        }

        /// <summary>
        /// 스테이지 초기 프롬프트 id 는 resource key 로만 저장(순환 의존 방지).
        /// WHY: StageData SO 가 Prompt 정적 필드에 직접 링크하면 SO 직렬화 편집이 불가.
        /// 런타임에서 id 로 조회해 Prompt 인스턴스에 매핑(Prompt 샘플 + DailyPuzzle 풀 기반).
        /// </summary>
        [Header("Prompt")]
        [SerializeField] private string _initialPromptId = "p1.sample.purple10";
        public string InitialPromptId { get { return _initialPromptId; } }

        /// <summary>id 기반으로 알려진 Prompt 를 반환. 미해석 시 Purple10 fallback.</summary>
        public Prompt ResolveInitialPrompt()
        {
            return ResolvePromptById(_initialPromptId);
        }

        public static Prompt ResolvePromptById(string id)
        {
            if (string.IsNullOrEmpty(id)) return Prompt.SamplePurple10;
            if (id == Prompt.SamplePurple10.Id) return Prompt.SamplePurple10;
            if (id == Prompt.SampleChain3.Id) return Prompt.SampleChain3;
            if (id == Prompt.SampleMix.Id) return Prompt.SampleMix;
            if (id == Prompt.SampleAdvanced1.Id) return Prompt.SampleAdvanced1;
            if (id == Prompt.SampleDaily1.Id) return Prompt.SampleDaily1;
            return Prompt.SamplePurple10;
        }
    }

    /// <summary>특수 블록 초기 배치 엔트리. (row, col, kind, color).</summary>
    [Serializable]
    public struct BlockPlacement
    {
        public int Row;
        public int Col;
        public BlockKind Kind;
        public ColorId Color;
    }
}
