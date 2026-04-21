namespace Alchemist.Domain.Board
{
    /// <summary>Per-cell terrain layer. Ground is passable baseline; Filter transforms color;
    /// Wall blocks movement and propagation.</summary>
    public enum CellLayer : byte
    {
        Ground = 0,
        Filter = 1,
        Wall = 2,
    }
}
