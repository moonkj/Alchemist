using UnityEngine;

namespace Alchemist.Domain.Player
{
    /// <summary>
    /// Application.persistentDataPath 기반 실제 IPathProvider.
    /// WHY: 테스트는 in-memory 또는 Path.GetTempPath() 기반 더미를 주입해 Unity 런타임 의존 제거.
    /// </summary>
    public sealed class PersistentPathProvider : IPathProvider
    {
        public string SaveRoot => Application.persistentDataPath;
    }
}
