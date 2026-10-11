using System.Collections.Generic;
using UnityEngine;

/// <summary>Logic thuần của bàn cờ 8x8. x = cột (trái->phải), y = hàng (dưới->trên). -1 = ô trống, >=0 = chỉ số màu.</summary>
public class Board
{
    public const int Size = 8;
    public readonly int[,] Cells = new int[Size, Size];

    public Board() { Clear(); }

    public void Clear()
    {
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                Cells[x, y] = -1;
    }

    public bool IsEmpty()
    {
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                if (Cells[x, y] >= 0) return false;
        return true;
    }

    public bool CanPlace(ShapeData s, int ox, int oy)
    {
        foreach (var c in s.Cells)
        {
            int x = ox + c.x, y = oy + c.y;
            if (x < 0 || y < 0 || x >= Size || y >= Size) return false;
            if (Cells[x, y] >= 0) return false;
        }
        return true;
    }

    public bool CanPlaceAnywhere(ShapeData s)
    {
        for (int ox = 0; ox <= Size - s.Width; ox++)
            for (int oy = 0; oy <= Size - s.Height; oy++)
                if (CanPlace(s, ox, oy)) return true;
        return false;
    }

    public void Place(ShapeData s, int ox, int oy, int color)
    {
        foreach (var c in s.Cells) Cells[ox + c.x, oy + c.y] = color;
    }

    /// <summary>Các hàng/cột sẽ đầy nếu đặt khối vào (không thay đổi bàn cờ) - dùng cho preview.</summary>
    public void GetCompletedLines(ShapeData s, int ox, int oy, List<int> rows, List<int> cols)
    {
        rows.Clear(); cols.Clear();
        var addRow = new int[Size];
        var addCol = new int[Size];
        foreach (var c in s.Cells) { addCol[ox + c.x]++; addRow[oy + c.y]++; }

        for (int y = 0; y < Size; y++)
        {
            int n = addRow[y];
            for (int x = 0; x < Size; x++) if (Cells[x, y] >= 0) n++;
            if (n == Size) rows.Add(y);
        }
        for (int x = 0; x < Size; x++)
        {
            int n = addCol[x];
            for (int y = 0; y < Size; y++) if (Cells[x, y] >= 0) n++;
            if (n == Size) cols.Add(x);
        }
    }

    /// <summary>Các hàng/cột đang đầy trên bàn cờ hiện tại.</summary>
    public void FindCompletedLines(List<int> rows, List<int> cols)
    {
        rows.Clear(); cols.Clear();
        for (int y = 0; y < Size; y++)
        {
            bool full = true;
            for (int x = 0; x < Size && full; x++) if (Cells[x, y] < 0) full = false;
            if (full) rows.Add(y);
        }
        for (int x = 0; x < Size; x++)
        {
            bool full = true;
            for (int y = 0; y < Size && full; y++) if (Cells[x, y] < 0) full = false;
            if (full) cols.Add(x);
        }
    }

    /// <summary>Xóa các hàng/cột (hàng và cột giao nhau xóa cùng lúc), trả về danh sách ô đã xóa + màu của chúng.</summary>
    public void ClearLines(List<int> rows, List<int> cols, List<Vector2Int> clearedCells, List<int> clearedColors)
    {
        var mark = new bool[Size, Size];
        foreach (int r in rows) for (int x = 0; x < Size; x++) mark[x, r] = true;
        foreach (int c in cols) for (int y = 0; y < Size; y++) mark[c, y] = true;

        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                if (mark[x, y] && Cells[x, y] >= 0)
                {
                    clearedCells.Add(new Vector2Int(x, y));
                    clearedColors.Add(Cells[x, y]);
                    Cells[x, y] = -1;
                }
    }
}
