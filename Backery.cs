using System.Numerics;

namespace Match_3;

public static class Backery
{
    public static Balls GetTileTypeTypeByNoise(float noise)
    {
        noise = MathF.Round(noise, 2, MidpointRounding.ToPositiveInfinity);

        if (noise <= 0f || noise >= 1.0f)
        {
            noise = Random.Shared.NextSingle();
            return GetTileTypeTypeByNoise(noise);
        }
   
        else if (noise <= 0.1)
            noise *= 10;

        return noise switch
        {
            <= 0.25f => Balls.Red,
            > 0.25f and <= 0.35f => Balls.Orange,
            > 0.35f and <= 0.45f => Balls.Blue,
            > 0.45f and <= 0.55f => Balls.Green,
            > 0.55f and <= 0.65f => Balls.Purple,
            > 0.65f and <= 0.75f => Balls.Violet,
            > 0.75f and <= 1f => Balls.Yellow,
            _ => Balls.Empty,
        };
    }

    public static ITile CreateTile_1(Vector2 gridPos, float noise)
    {
        return new Tile
        {
            Current = gridPos,
            CoordsB4Swap = -Vector2.One,
            Selected = false,
            TileShape = new CandyShape(noise)
        };
    }

    //public static ITile CreateTile(float noise)
    //{       
    //    CandyShape tmp = new(noise);

    //    //if (sweet == Balls.Donut)
    //    {             
    //        noise = MathF.Round(noise, 2, MidpointRounding.ToPositiveInfinity);

    //        if (noise <= 0f)
    //            noise += Random.Shared.NextSingle();

    //        else if (noise <= 0.1)
    //            noise *= 10;

    //        switch (noise)
    //        {
    //            case <= 0.15f:
    //                tmp = new CandyShape(noise)
    //                {
    //                    FrameLocation = new Vector2(0f, 1f) * ITile.Size,
    //                    Form = ShapeKind.Circle,
    //                    Layer = Coat.A
    //                };

    //                break;
    //            case > 0.15f and <= 0.2f:
    //                tmp = new CandyShape(noise)
    //                {
    //                    FrameLocation = new Vector2(0f, 2f) * ITile.Size,
    //                    Form = ShapeKind.Circle,
    //                    Layer = Coat.B
    //                };
    //                break;
    //            case > 0.2f and <= 0.25f:
    //                tmp = new CandyShape(noise)
    //                {
    //                    FrameLocation = new Vector2(4f, 3f) * ITile.Size,
    //                    Form = ShapeKind.Circle,
    //                    Layer = Coat.C
    //                };
    //                break;
    //            case > 0.25f and <= 0.35f:
    //                tmp = new CandyShape(noise)
    //                {
    //                    FrameLocation = new Vector2(0f, 4f) * ITile.Size,
    //                    Form = ShapeKind.Circle,
    //                    Layer = Coat.D
    //                };
    //                break;
    //            case > 0.35f and <= 0.45f:
    //                tmp = new CandyShape(noise)
    //                {
    //                    FrameLocation = new Vector2(0f, 5f) * ITile.Size,
    //                    Form = ShapeKind.Circle,
    //                    Layer = Coat.E
    //                };
    //                break;
    //            case > 0.45f and <= 0.65f:
    //                tmp = new CandyShape(noise)
    //                {
    //                    FrameLocation = new Vector2(4f, 6f) * ITile.Size,
    //                    Form = ShapeKind.Heart,
    //                    Layer = Coat.F
    //                };
    //                break;
    //            case > 0.65f and <= 1f:
    //                tmp = new CandyShape(noise)
    //                {
    //                    FrameLocation = new Vector2(0f, 7f) * ITile.Size,
    //                    Form = ShapeKind.Heart,
    //                    Layer = Coat.G
    //                };
    //                break;
    //            default:
    //                break;
    //        }
    //        var mapTile = new Tile
    //        {
    //            Current = start,
    //            CoordsB4Swap = -Vector2.One,
    //            Selected = false,
    //            TileShape = tmp,  
    //        };
    //        return mapTile;
    //    }

    //    throw new NotImplementedException("For the rest of Sweets I do not have an implementawtion yet! will come later...!");
    //}
}
