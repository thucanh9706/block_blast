using UnityEngine;

/// <summary>
/// Cung cấp sprite cho game. Nếu có file trong Assets/Resources/Sprites/ với đúng tên
/// (block, cell, button, particle, background) thì dùng asset đó (vd export từ Figma),
/// không thì tự sinh sprite placeholder bằng code.
/// </summary>
public static class SpriteFactory
{
    static Sprite block, cell, button, particle, background, crown, gear, boardGrid, boardFrame, fill;
    static readonly Sprite[] blockColors = new Sprite[8];

    static Sprite Load(string n) { return Resources.Load<Sprite>("Sprites/" + n); }

    /// <summary>Ô khối đã đặt / khối trong tray (tô màu bằng SpriteRenderer.color).</summary>
    public static Sprite Block
    {
        get
        {
            if (block == null) { block = Load("block"); if (block == null) block = Rounded(64, 15, true, 0); }
            return block;
        }
    }

    /// <summary>Ô trống của bàn cờ.</summary>
    public static Sprite Cell
    {
        get
        {
            if (cell == null) { cell = Load("cell"); if (cell == null) cell = Rounded(64, 12, false, 0); }
            return cell;
        }
    }

    /// <summary>Nền bo góc 9-slice dùng cho nút, bảng, khay.</summary>
    public static Sprite Button
    {
        get
        {
            if (button == null) { button = Load("button"); if (button == null) button = Rounded(64, 24, false, 26); }
            return button;
        }
    }

    public static Sprite Particle
    {
        get
        {
            if (particle == null) { particle = Load("particle"); if (particle == null) particle = Rounded(16, 4, false, 0); }
            return particle;
        }
    }

    public static Sprite Background
    {
        get
        {
            if (background == null) { background = Load("bg"); if (background == null) background = Gradient(); }
            return background;
        }
    }

    /// <summary>Hệ số scale để sprite có bề rộng đúng bằng targetSize (đơn vị world), bất kể PPU của asset.</summary>
    public static float FitScale(Sprite s, float targetSize)
    {
        return targetSize / Mathf.Max(0.0001f, s.bounds.size.x);
    }

    public static Sprite Crown { get { if (crown == null) crown = Load("crown"); return crown; } }
    public static Sprite Gear { get { if (gear == null) gear = Load("gear"); return gear; } }
    public static Sprite BoardGrid { get { if (boardGrid == null) boardGrid = Load("board_grid"); return boardGrid; } }
    public static Sprite BoardFrame { get { if (boardFrame == null) boardFrame = Load("board_frame"); return boardFrame; } }
    public static Sprite Fill { get { if (fill == null) fill = Load("fill"); return fill; } }

    /// <summary>Sprite khối có sẵn màu (block_0..block_7 trong Resources/Sprites, thứ tự theo ShapeLibrary.Palette). Null nếu chưa có asset.</summary>
    public static Sprite BlockColor(int i)
    {
        if (i < 0 || i >= blockColors.Length) return null;
        if (blockColors[i] == null) blockColors[i] = Load("block_" + i);
        return blockColors[i];
    }

    /// <summary>Gán sprite + màu cho một ô khối. Có asset thì dùng sprite màu sẵn (tint trắng), không thì dùng sprite trắng tô màu palette.</summary>
    public static void SetBlock(SpriteRenderer sr, int colorIndex, float alpha = 1f)
    {
        var s = BlockColor(colorIndex);
        if (s != null) { sr.sprite = s; sr.color = new Color(1f, 1f, 1f, alpha); }
        else { sr.sprite = Block; var c = ShapeLibrary.Palette[colorIndex]; c.a = alpha; sr.color = c; }
    }

    
static Sprite Rounded(int size, int radius, bool bevel, int border)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float half = size * 0.5f;
        var px = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                float a = Mathf.Clamp01(0.5f - dist);

                float shade = 1f;
                if (bevel)
                {
                    float t = (float)y / (size - 1);
                    float depth = -dist;
                    shade = depth < size * 0.11f ? Mathf.Lerp(0.68f, 0.92f, t) : Mathf.Lerp(0.90f, 1f, t);
                }
                px[y * size + x] = new Color(shade, shade, shade, a);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0,
            SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    static Sprite Gradient()
    {
        const int h = 128;
        var tex = new Texture2D(4, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color top = new Color(0.17f, 0.21f, 0.50f);
        Color bottom = new Color(0.06f, 0.07f, 0.20f);
        for (int y = 0; y < h; y++)
        {
            Color c = Color.Lerp(bottom, top, (float)y / (h - 1));
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, h), new Vector2(0.5f, 0.5f), h);
    }
}
