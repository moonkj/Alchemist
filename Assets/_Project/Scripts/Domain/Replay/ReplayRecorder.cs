using System;

namespace Alchemist.Domain.Replay
{
    /// <summary>
    /// 고정 크기 순환 버퍼 기반 리플레이 기록기. 과거 N 턴만 유지.
    /// Phase 3 범위: 기록만 — 재생은 Phase 3 Wave 2.
    /// WHY 순환 버퍼: 장시간 세션에서도 메모리 상한 보장.
    /// </summary>
    public sealed class ReplayRecorder
    {
        public const int DefaultCapacity = 256;

        private readonly ReplayFrame[] _buffer;
        private int _head;    // 다음 append 위치
        private int _count;   // 채워진 슬롯 수 (≤ Capacity)

        public ReplayRecorder(int capacity = DefaultCapacity)
        {
            if (capacity <= 0) capacity = DefaultCapacity;
            _buffer = new ReplayFrame[capacity];
        }

        public int Capacity => _buffer.Length;
        public int Count => _count;

        /// <summary>프레임 추가. 가득 차면 가장 오래된 항목을 덮어쓴다.</summary>
        public void Append(ReplayFrame frame)
        {
            _buffer[_head] = frame;
            _head = (_head + 1) % _buffer.Length;
            if (_count < _buffer.Length) _count++;
        }

        /// <summary>
        /// 시간순 인덱싱(0 = 가장 오래된, Count-1 = 가장 최근).
        /// WHY 속성 대신 메서드: 인덱서로 제공 시 for 문 호출부가 자연스러움.
        /// </summary>
        public ReplayFrame Get(int index)
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));
            int start = (_count == _buffer.Length) ? _head : 0;
            return _buffer[(start + index) % _buffer.Length];
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
            // 남은 슬롯 struct 는 재사용되므로 Array.Clear 불필요.
        }

        /// <summary>
        /// 시간순 복사본 반환. Phase 3 Wave 2 재생/직렬화에서 사용.
        /// WHY 새 배열: 내부 순환 순서 노출 방지.
        /// </summary>
        public ReplayFrame[] Snapshot()
        {
            var copy = new ReplayFrame[_count];
            for (int i = 0; i < _count; i++) copy[i] = Get(i);
            return copy;
        }
    }
}
