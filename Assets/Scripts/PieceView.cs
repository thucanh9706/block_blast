using System.Collections.Generic;
using UnityEngine;

/// <summary>Một khối (piece) trong khay: gồm nhiều SpriteRenderer, tâm hình học nằm tại transform.position.</summary>
public class PieceView : MonoBehaviour
{
    public const float TrayScale = 0.55f;

    public ShapeData Shape;
    public int ColorIndex;
    public int Slot;
    public Vector3 HomePos;

    readonly List<SpriteRenderer> blocks = new List<SpriteRenderer>();
    Color baseColor;

    public static PieceView Create(ShapeData shape, int colorIndex, int slot, Vector3 home, Transform parent)
    {
        var go = new GameObject("Piece_" + slot);
        go.transform.SetParent(parent, false);
        go.transform.position = home;

        var pv = go.AddComponent<PieceView>();
        pv.Shape = shape;
        pv.ColorIndex = colorIndex;
        pv.Slot = slot;
        pv.HomePos = home;
        pv.baseColor = ShapeLibrary.Palette[colorIndex];

        float s = SpriteFactory.FitScale(SpriteFactory.Block, 0.94f);
        foreach (var c in shape.Cells)
        {
            var b = new GameObject("b");
            b.transform.SetParent(go.transform, false);
            b.transform.localPosition = new Vector3(c.x - (shape.Width - 1) * 0.5f, c.y - (shape.Height - 1) * 0.5f, 0f);
            b.transform.localScale = Vector3.one * s;
            var sr = b.AddComponent<SpriteRenderer>();
            SpriteFactory.SetBlock(sr, colorIndex);
            pv.baseColor = sr.color;
            sr.sortingOrder = 5;
            pv.blocks.Add(sr);
        }
        return pv;
    }

    /// <summary>Vị trí tâm ô dưới-trái so với tâm khối, khi scale = 1.</summary>
    public Vector2 BottomLeftOffset()
    {
        return new Vector2(-(Shape.Width - 1) * 0.5f, -(Shape.Height - 1) * 0.5f);
    }

    public void SetSorting(int order)
    {
        foreach (var sr in blocks) sr.sortingOrder = order;
    }

    public void SetDim(bool dim)
    {
        Color c = baseColor;
        c.a = dim ? 0.3f : 1f;
        foreach (var sr in blocks) sr.color = c;
    }

    public bool Contains(Vector2 world)
    {
        float s = transform.localScale.x;
        float hw = Shape.Width * 0.5f * s + 0.45f;
        float hh = Shape.Height * 0.5f * s + 0.45f;
        Vector3 p = transform.position;
        return Mathf.Abs(world.x - p.x) <= hw && Mathf.Abs(world.y - p.y) <= hh;
    }

    public void MoveTo(Vector3 pos, float scale, float dur, float delay = 0f)
    {
        StopAllCoroutines();
        StartCoroutine(Tween.Move(transform, pos, Vector3.one * scale, dur, delay));
    }

    public void StopMoving() { StopAllCoroutines(); }

    /// <summary>Rung nhẹ khi người chơi bấm vào khối không còn chỗ đặt.</summary>
    public void Shake()
    {
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine());
    }

    System.Collections.IEnumerator ShakeRoutine()
    {
        Vector3 home = HomePos;
        float el = 0f, d = 0.25f;
        while (el < d)
        {
            el += Time.deltaTime;
            float k = 1f - el / d;
            transform.position = home + new Vector3(Mathf.Sin(el * 70f) * 0.12f * k, 0f, 0f);
            yield return null;
        }
        transform.position = home;
    }
}
