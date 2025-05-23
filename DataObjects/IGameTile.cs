﻿using System.Numerics;

namespace Match_3.DataObjects;

public interface IGameObject
{
    public Vector2 Position { get; }
    
    public Shape Body { get; }
    
    public string ToString() => $"Tile at: {Position}; ---- with type: {Body.TileKind}";
}