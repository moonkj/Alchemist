using System;

namespace Alchemist.Domain.Palette
{
    /// <summary>
    /// 팔레트 변경 이벤트 버스.
    /// WHY: View (PaletteView) 가 도메인을 양방향 의존하지 않도록 이벤트 인터페이스만 구독.
    /// 인덱스 기반 알림 → 전체 재그리기가 아닌 슬롯 단위 갱신 가능(할당 0).
    /// </summary>
    public interface IPaletteEvents
    {
        /// <summary>슬롯 index 가 Store/Use/Clear 등으로 변경될 때 발행.</summary>
        event Action<int> SlotChanged;

        /// <summary>사용 가능한 슬롯 개수가 변했을 때 (스테이지 로드/해제).</summary>
        event Action<int> UnlockedCountChanged;
    }
}
