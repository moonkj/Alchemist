using UnityEngine;

namespace Alchemist.Services.Audio
{
    /// <summary>
    /// SfxId → AudioClip 룩업. WHY: 실제 오디오 파일은 Phase 4 시점에 없지만 Library 슬롯은 선언해
    /// 컴포넌트 inspector 로 바인딩만 가능케 함. 빈 슬롯은 Play() 가 no-op.
    /// </summary>
    [CreateAssetMenu(menuName = "Alchemist/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        // WHY: 인덱스 = (int)SfxId 로 O(1) 룩업. 추가 SFX 시 enum 과 배열 길이 동기화.
        [SerializeField] private AudioClip[] _clips = new AudioClip[7];

        public AudioClip Resolve(SfxId id)
        {
            int idx = (int)id;
            if (idx < 0 || idx >= _clips.Length) return null;
            return _clips[idx];
        }

        public int SlotCount => _clips != null ? _clips.Length : 0;
    }
}
