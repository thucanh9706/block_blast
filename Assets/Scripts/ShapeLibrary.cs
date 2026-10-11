using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>Một hình khối (tập các ô, gốc ở góc dưới-trái, y hướng lên).</summary>
public class ShapeData
{
    public Vector2Int[] Cells;
    public int Width;
    public int Height;
    public string Key;
    public float Weight;

    public ShapeData(List<Vector2Int> cells)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var c in cells)
        {
            minX = Mathf.Min(minX, c.x); minY = Mathf.Min(minY, c.y);
            maxX = Mathf.Max(maxX, c.x); maxY = Mathf.Max(maxY, c.y);
        }
        Cells = new Vector2Int[cells.Count];
        for (int i = 0; i < cells.Count; i++)
            Cells[i] = new Vector2Int(cells[i].x - minX, cells[i].y - minY);
        Width = maxX - minX + 1;
        Height = maxY - minY + 1;

        var sorted = new List<Vector2Int>(Cells);
        sorted.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
        var sb = new StringBuilder();
        foreach (var c in sorted) sb.Append(c.x).Append(',').Append(c.y).Append(';');
        Key = sb.ToString();

        int n = Cells.Length;
        Weight = n <= 4 ? 1f : (n == 5 ? 0.9f : (n == 6 ? 0.7f : 0.5f));
    }
}

public static class ShapeLibrary
{
    public static readonly Color[] Palette =
    {
        new Color(0.88f, 0.10f, 0.10f), // đỏ
        new Color(0.96f, 0.55f, 0.04f), // cam
        new Color(0.98f, 0.90f, 0.10f), // vàng
        new Color(0.30f, 0.80f, 0.08f), // xanh lá
        new Color(0.08f, 0.78f, 0.80f), // cyan
        new Color(0.20f, 0.42f, 0.96f), // xanh dương
        new Color(0.62f, 0.25f, 0.90f), // tím
        new Color(1.00f, 0.40f, 0.70f), // hồng
    };

    public static readonly List<ShapeData> All = new List<ShapeData>();
    static float totalWeight;

    // Các hình gốc, sẽ tự sinh 4 hướng xoay (trùng thì loại)
    static readonly string[][] Bases =
    {
        new[] { "X" },
        new[] { "XX" },
        new[] { "XXX" },
        new[] { "XXXX" },
        new[] { "XXXXX" },
        new[] { "XX", "XX" },
        new[] { "XXX", "XXX", "XXX" },
        new[] { "XXX", "XXX" },
        new[] { "X.", "XX" },
        new[] { "X..", "X..", "XXX" },
        new[] { "X.", "X.", "XX" },
        new[] { ".X", ".X", "XX" },
        new[] { "XXX", ".X." },
        new[] { "XX.", ".XX" },
        new[] { ".XX", "XX." },
    };

    static ShapeLibrary() { Build(); }

    static ShapeData Parse(string[] rows)
    {
        var list = new List<Vector2Int>();
        for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
                if (rows[r][c] == 'X') list.Add(new Vector2Int(c, rows.Length - 1 - r));
        return new ShapeData(list);
    }

    static ShapeData Rotate(ShapeData s)
    {
        var l = new List<Vector2Int>();
        foreach (var c in s.Cells) l.Add(new Vector2Int(c.y, -c.x));
        return new ShapeData(l);
    }

    static void Build()
    {
        var keys = new HashSet<string>();
        foreach (var b in Bases)
        {
            var s = Parse(b);
            for (int i = 0; i < 4; i++)
            {
                if (keys.Add(s.Key)) All.Add(s);
                s = Rotate(s);
            }
        }
        totalWeight = 0f;
        foreach (var s in All) totalWeight += s.Weight;
    }

    /// <summary>Chọn ngẫu nhiên có trọng số (khối to xuất hiện ít hơn).</summary>
    public static ShapeData Pick()
    {
        float r = Random.value * totalWeight;
        foreach (var s in All)
        {
            r -= s.Weight;
            if (r <= 0f) return s;
        }
        return All[All.Count - 1];
    }
}
