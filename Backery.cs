using System.Numerics;

namespace Match_3;

public static class Backery
{
    public static Sweets GetSweetTypeByNoise(float noise)
    {
        noise = MathF.Round(noise, 2, MidpointRounding.ToPositiveInfinity);

        if (noise <= 0f)
        {
            noise += Random.Shared.NextSingle();
            return GetSweetTypeByNoise(noise);
        }

        else if (noise <= 0.1)
            noise *= 10;

        return noise switch
        {
            <= 0.25f => Sweets.Donut,
            > 0.25f and <= 0.35f => Sweets.Cookie,
            > 0.35f and <= 0.45f => Sweets.Cupcake,
            > 0.45f and <= 0.65f => Sweets.Bonbon,
            > 0.65f and <= 1f => Sweets.Gummies,
            _ => Sweets.Empty,
        };
    }

    public static ITile CreateTile(Vector2 start, float noise)
    {
        var sweetType = Sweets.Donut; //GetSweetTypeByNoise(noise);

        if (sweetType == Sweets.Donut)
        {        
            Donut tmp = new(noise);
            noise = MathF.Round(noise, 2, MidpointRounding.ToPositiveInfinity);

            if (noise <= 0f)
                noise += Random.Shared.NextSingle();

            else if (noise <= 0.1)
                noise *= 10;

            switch (noise)
            {
                case <= 0.15f:
                    tmp = new Donut(noise)
                    {
                        FrameLocation = new Vector2(0f, 1f) * Grid.TileSize,
                        Form = ShapeKind.Circle,
                        Layer = Coat.A,
                    };

                    break;
                case > 0.15f and <= 0.2f:
                    tmp = new Donut(noise)
                    {
                        FrameLocation = new Vector2(0f, 2f) * Grid.TileSize,
                        Form = ShapeKind.Circle,
                        Layer = Coat.B
                    };
                    break;
                case > 0.2f and <= 0.25f:
                    tmp = new Donut(noise)
                    {
                        FrameLocation = new Vector2(0f, 3f) * Grid.TileSize,
                        Form = ShapeKind.Circle,
                        Layer = Coat.C
                    };
                    break;
                case > 0.25f and <= 0.35f:
                    tmp = new Donut(noise)
                    {
                        FrameLocation = new Vector2(0f, 4f) * Grid.TileSize,
                        Form = ShapeKind.Circle,
                        Layer = Coat.D
                    };
                    break;
                case > 0.35f and <= 0.45f:
                    tmp = new Donut(noise)
                    {
                        FrameLocation = new Vector2(0f, 5f) * Grid.TileSize,
                        Form = ShapeKind.Circle,
                        Layer = Coat.E
                    };
                    break;
                case > 0.45f and <= 0.65f:
                    tmp = new Donut(noise)
                    {
                        FrameLocation = new Vector2(0f, 6f) * Grid.TileSize,
                        Form = ShapeKind.Circle,
                        Layer = Coat.F
                    };
                    break;
                case > 0.65f and <= 1f:
                    tmp = new Donut(noise)
                    {
                        FrameLocation = new Vector2(0f, 7f) * Grid.TileSize,
                        Form = ShapeKind.Circle,
                        Layer = Coat.G
                    };
                    break;
                default:
                    break;
            }
            var mapTile = new Tile
            {
                Current = start,
                CoordsB4Swap = -Vector2.One,
                Selected = false,
                TileShape = tmp,  
            };
            return mapTile;
        }

        throw new NotImplementedException("For the rest of Sweets I do not have an implementawtion yet! will come later...!");
    }
}
