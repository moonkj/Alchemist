namespace Alchemist.Domain.Player
{
    /// <summary>
    /// 세이브 파일 경로 추상화.
    /// WHY: 테스트는 Application.persistentDataPath 를 사용할 수 없어 임시 디렉터리 주입이 필요.
    /// </summary>
    public interface IPathProvider
    {
        /// <summary>세이브 파일 저장 루트 디렉터리 절대 경로.</summary>
        string SaveRoot { get; }
    }
}
