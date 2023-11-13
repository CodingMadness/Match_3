using System.Numerics;

namespace Match_3.DataObjects;

public interface IGameTile
{
    public Vector2 Cell { get; }
    public Shape Body { get; }
}