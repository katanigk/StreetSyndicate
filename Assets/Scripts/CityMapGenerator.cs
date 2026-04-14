using System;
using System.Collections.Generic;

/// <summary>
/// Procedural city grid for the Ops board: same gameplay "essence" (tile categories & weights)
/// with a new street layout and zoning roll each <see cref="GameSessionState.CityMapSeed"/>.
/// </summary>
public enum CityTileKind
{
    Street = 0,
    Residential,
    Commercial,
    Industrial,
    Waterfront,
    Park,
    Civic
}

[Serializable]
public sealed class CityMapLayout
{
    public int Width;
    public int Height;
    public CityTileKind[] Cells;

    public CityTileKind Get(int x, int y)
    {
        return Cells[y * Width + x];
    }
}

public static class CityMapGenerator
{
    /// <summary>Starter grid 20×20 for faster iteration; raise when gameplay needs a larger city.</summary>
    public const int GridWidth = 20;

    public const int GridHeight = 20;

    /// <summary>Build a layout from seed — identical seed always yields the same city.</summary>
    public static CityMapLayout Generate(int seed)
    {
        var rng = new Random(seed);

        int w = GridWidth;
        int h = GridHeight;

        int innerH = Math.Max(1, h - 2);
        int innerW = Math.Max(1, w - 2);
        // More horizontal streets → more stacked blocks between top and bottom of the map.
        int numHStreets = Math.Min(3 + rng.Next(0, Math.Max(1, h / 3)), innerH);
        int numVStreets = Math.Min(2 + rng.Next(0, Math.Max(1, w / 3)), innerW);

        var streetRows = new HashSet<int>();
        var streetCols = new HashSet<int>();
        for (int n = 0; n < 512 && streetRows.Count < numHStreets; n++)
            streetRows.Add(1 + rng.Next(0, innerH));
        for (int n = 0; n < 512 && streetCols.Count < numVStreets; n++)
            streetCols.Add(1 + rng.Next(0, innerW));

        var cells = new CityTileKind[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                bool isStreet = streetRows.Contains(y) || streetCols.Contains(x);
                if (isStreet)
                    cells[i] = CityTileKind.Street;
                else
                    cells[i] = PickBlockKind(rng, x, y, w, h);
            }
        }

        return new CityMapLayout { Width = w, Height = h, Cells = cells };
    }

    /// <summary>Same category weights every run — only layout and rolls change with the seed.</summary>
    private static CityTileKind PickBlockKind(Random rng, int x, int y, int w, int h)
    {
        bool edge = x <= 0 || y <= 0 || x >= w - 1 || y >= h - 1;
        int roll = rng.Next(0, 100);

        if (edge && roll < 14)
            return CityTileKind.Waterfront;

        roll = rng.Next(0, 100);
        if (roll < 36)
            return CityTileKind.Residential;
        if (roll < 62)
            return CityTileKind.Commercial;
        if (roll < 77)
            return CityTileKind.Industrial;
        if (roll < 89)
            return CityTileKind.Park;
        return CityTileKind.Civic;
    }

    /// <summary>Short English label for UI (player-facing).</summary>
    public static string GetTileDisplayLabel(CityTileKind kind)
    {
        switch (kind)
        {
            case CityTileKind.Street: return "Street";
            case CityTileKind.Residential: return "Residential block";
            case CityTileKind.Commercial: return "Commercial block";
            case CityTileKind.Industrial: return "Industrial block";
            case CityTileKind.Waterfront: return "Waterfront block";
            case CityTileKind.Park: return "Park / open space";
            case CityTileKind.Civic: return "Civic / institutions";
            default: return "Block";
        }
    }

    public static UnityEngine.Color GetPreviewColor(CityTileKind kind)
    {
        switch (kind)
        {
            case CityTileKind.Street:
                return new UnityEngine.Color(0.22f, 0.22f, 0.24f, 1f);
            case CityTileKind.Residential:
                return new UnityEngine.Color(0.38f, 0.32f, 0.28f, 1f);
            case CityTileKind.Commercial:
                return new UnityEngine.Color(0.35f, 0.38f, 0.44f, 1f);
            case CityTileKind.Industrial:
                return new UnityEngine.Color(0.42f, 0.34f, 0.28f, 1f);
            case CityTileKind.Waterfront:
                return new UnityEngine.Color(0.28f, 0.36f, 0.42f, 1f);
            case CityTileKind.Park:
                return new UnityEngine.Color(0.26f, 0.38f, 0.30f, 1f);
            case CityTileKind.Civic:
                return new UnityEngine.Color(0.44f, 0.40f, 0.34f, 1f);
            default:
                return UnityEngine.Color.gray;
        }
    }
}
