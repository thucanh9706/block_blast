using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Hiệu ứng: hạt vỡ, khối nổ khi xóa hàng, nhún scale.</summary>
public class FxManager : MonoBehaviour
{
    class P
    {
        public Transform t;
        public SpriteRenderer sr;
        public Vector2 v;
        public float life, max, spin, s0;
    }

    readonly List<P> live = new List<P>();
    readonly Stack<P> pool = new Stack<P>();

    P Get()
    {
        if (pool.Count > 0)
        {
            var p = pool.Pop();
            p.t.gameObject.SetActive(true);
            return p;
        }
        var go = new GameObject("fx");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Particle;
        sr.sortingOrder = 30;
        return new P { t = go.transform, sr = sr };
    }

    public void Burst(Vector3 pos, Color c, int count, float speed = 4f)
    {
        c.a = 1f;
        for (int i = 0; i < count; i++)
        {
            var p = Get();
            p.t.position = pos;
            float a = Random.value * Mathf.PI * 2f;
            float sp = speed * Random.Range(0.4f, 1f);
            p.v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp + Vector2.up * 1.5f;
            p.max = p.life = Random.Range(0.35f, 0.7f);
            p.s0 = Random.Range(0.12f, 0.28f);
            p.spin = Random.Range(-360f, 360f);
            p.sr.color = c;
            p.t.localScale = Vector3.one * p.s0;
            live.Add(p);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = live.Count - 1; i >= 0; i--)
        {
            var p = live[i];
            p.life -= dt;
            if (p.life <= 0f)
            {
                p.t.gameObject.SetActive(false);
                pool.Push(p);
                live.RemoveAt(i);
                continue;
            }
            p.v += Vector2.down * 12f * dt;
            p.t.position += (Vector3)(p.v * dt);
            p.t.Rotate(0f, 0f, p.spin * dt);
            float k = p.life / p.max;
            p.t.localScale = Vector3.one * p.s0 * k;
            var col = p.sr.color; col.a = k; p.sr.color = col;
        }
    }

    /// <summary>Khối bị xóa: phồng lên, chớp trắng, thu nhỏ rồi vỡ thành hạt. delay tạo hiệu ứng lan sóng.</summary>
    public void PopBlock(Vector3 pos, Color c, float delay)
    {
        StartCoroutine(PopRoutine(pos, c, delay));
    }

    IEnumerator PopRoutine(Vector3 pos, Color c, float delay)
    {
        var go = new GameObject("pop");
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.Block;
        sr.color = c;
        sr.sortingOrder = 25;
        float baseScale = SpriteFactory.FitScale(sr.sprite, 0.94f);
        go.transform.localScale = Vector3.one * baseScale;

        if (delay > 0f) yield return new WaitForSeconds(delay);

        float t = 0f, dur = 0.28f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float s = k < 0.3f ? Mathf.Lerp(1f, 1.2f, k / 0.3f) : Mathf.Lerp(1.2f, 0f, (k - 0.3f) / 0.7f);
            go.transform.localScale = Vector3.one * baseScale * s;
            sr.color = Color.Lerp(c, Color.white, Mathf.Clamp01(k * 3f));
            yield return null;
        }
        Burst(pos, c, 5);
        Destroy(go);
    }

    public void Punch(Transform t, float baseScale)
    {
        StartCoroutine(PunchRoutine(t, baseScale));
    }

    IEnumerator PunchRoutine(Transform t, float b)
    {
        float el = 0f, d = 0.18f;
        while (el < d)
        {
            if (t == null) yield break;
            el += Time.deltaTime;
            float k = el / d;
            t.localScale = Vector3.one * b * (1f + 0.25f * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }
        if (t != null) t.localScale = Vector3.one * b;
    }
}
