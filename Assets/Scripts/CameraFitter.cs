using System.Collections;
using UnityEngine;

/// <summary>
/// Giữ khung nhìn world 9.2 x 16.36 (tỉ lệ 9:16) luôn nằm gọn trong cửa sổ, bất kể tỉ lệ màn hình.
/// Đồng thời lo rung camera và kéo giãn sprite nền.
/// </summary>
public class CameraFitter : MonoBehaviour
{
    public const float WorldW = 9.2f;
    public const float WorldH = 9.2f * 2236f / 1080f;
    public static readonly Vector2 Center = new Vector2(0f, 0f);

    Camera cam;
    Transform bg;
    SpriteRenderer bgSr;
    Vector3 shakeOffset;
    Coroutine shakeRoutine;

    public void Init(Camera c, Transform background)
    {
        cam = c;
        bg = background;
        bgSr = background != null ? background.GetComponent<SpriteRenderer>() : null;
        Fit();
    }

    void LateUpdate() { Fit(); }

void Fit()
    {
        if (cam == null) return;
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float size = Mathf.Max(WorldH * 0.5f, (WorldW * 0.5f) / aspect);
        cam.orthographicSize = size;
        cam.transform.position = new Vector3(Center.x, Center.y, -10f) + shakeOffset;

        if (bg != null && bgSr != null && bgSr.sprite != null)
        {
            Vector2 sz = bgSr.sprite.bounds.size;
            float w = size * 2f * aspect + 2f;
            float h = size * 2f + 2f;
            float s = Mathf.Max(w / sz.x, h / sz.y); // phủ kín màn hình, không méo ảnh
            bg.position = new Vector3(Center.x, Center.y, 5f);
            bg.localScale = new Vector3(s, s, 1f);
        }
    }

    public void Shake(float magnitude, float duration)
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine(magnitude, duration));
    }

    IEnumerator ShakeRoutine(float mag, float dur)
    {
        float el = 0f;
        while (el < dur)
        {
            el += Time.deltaTime;
            float k = 1f - el / dur;
            shakeOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * mag * k;
            yield return null;
        }
        shakeOffset = Vector3.zero;
        shakeRoutine = null;
    }
}
