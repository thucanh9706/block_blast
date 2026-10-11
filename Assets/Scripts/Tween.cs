using System.Collections;
using UnityEngine;

public static class Tween
{
    /// <summary>Di chuyển + scale mượt (ease-out). Nên chạy bằng StartCoroutine của chính object đó.</summary>
    public static IEnumerator Move(Transform t, Vector3 pos, Vector3 scale, float dur, float delay = 0f)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (t == null) yield break;

        Vector3 p0 = t.position, s0 = t.localScale;
        float el = 0f;
        while (el < dur)
        {
            if (t == null) yield break;
            el += Time.deltaTime;
            float k = Mathf.Clamp01(el / dur);
            k = 1f - Mathf.Pow(1f - k, 3f);
            t.position = Vector3.LerpUnclamped(p0, pos, k);
            t.localScale = Vector3.LerpUnclamped(s0, scale, k);
            yield return null;
        }
        if (t != null) { t.position = pos; t.localScale = scale; }
    }
}
