using System;

namespace Alchemist.Domain.Meta
{
    /// <summary>
    /// 챕터 명화 메타. 실제 픽셀 아트는 Phase 4 에서 Addressables 로 주입.
    /// WHY: POCO 로 유지해 세이브 직렬화와 테스트 용이성을 확보.
    /// </summary>
    public sealed class Artwork
    {
        public string Id { get; }
        public int Chapter { get; }
        /// <summary>로컬라이즈 키(실제 텍스트는 UI 레이어 Localizer 가 해석).</summary>
        public string LocalizedTitleKey { get; }
        public int TotalFragments { get; }

        /// <summary>복원된 조각 마스크. true = 복원됨. 길이 == TotalFragments.</summary>
        private readonly bool[] _fragmentMask;

        public int SolvedFragments { get; private set; }

        /// <summary>0~1 범위 복원 진행도.</summary>
        public float Progress => TotalFragments == 0 ? 0f : (float)SolvedFragments / TotalFragments;
        public bool IsCompleted => SolvedFragments >= TotalFragments && TotalFragments > 0;

        public Artwork(string id, int chapter, string titleKey, int totalFragments)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("id");
            if (totalFragments <= 0) throw new ArgumentOutOfRangeException(nameof(totalFragments));
            Id = id;
            Chapter = chapter;
            LocalizedTitleKey = titleKey ?? "";
            TotalFragments = totalFragments;
            _fragmentMask = new bool[totalFragments];
        }

        /// <summary>특정 조각 복원. 이미 복원된 인덱스면 false.</summary>
        public bool SolveFragment(int index)
        {
            if ((uint)index >= (uint)_fragmentMask.Length) return false;
            if (_fragmentMask[index]) return false;
            _fragmentMask[index] = true;
            SolvedFragments++;
            return true;
        }

        /// <summary>다음 미복원 조각을 복원. 없으면 -1.</summary>
        public int SolveNext()
        {
            for (int i = 0; i < _fragmentMask.Length; i++)
            {
                if (!_fragmentMask[i])
                {
                    _fragmentMask[i] = true;
                    SolvedFragments++;
                    return i;
                }
            }
            return -1;
        }

        public bool IsFragmentSolved(int index)
        {
            if ((uint)index >= (uint)_fragmentMask.Length) return false;
            return _fragmentMask[index];
        }

        /// <summary>세이브 직렬화용 조각 마스크 스냅샷. 복사본 반환.</summary>
        public bool[] SnapshotMask()
        {
            var copy = new bool[_fragmentMask.Length];
            for (int i = 0; i < _fragmentMask.Length; i++) copy[i] = _fragmentMask[i];
            return copy;
        }

        /// <summary>세이브 로드용 마스크 복원. 길이 불일치 시 최소 길이만 복사.</summary>
        public void RestoreMask(bool[] mask)
        {
            if (mask == null) return;
            int n = mask.Length < _fragmentMask.Length ? mask.Length : _fragmentMask.Length;
            SolvedFragments = 0;
            for (int i = 0; i < _fragmentMask.Length; i++) _fragmentMask[i] = false;
            for (int i = 0; i < n; i++)
            {
                _fragmentMask[i] = mask[i];
                if (mask[i]) SolvedFragments++;
            }
        }
    }
}
