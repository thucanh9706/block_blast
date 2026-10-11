using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Toàn bộ UI (menu, HUD, pause, game over) dựng bằng code trên một Canvas 1080x1920.</summary>
public class UIController : MonoBehaviour
{
    public Action OnPlay, OnResume, OnRestart, OnMenu, OnPause, OnQuit, OnToggleSound, OnClickSfx;

    static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
    static readonly Color Green = new Color(0.30f, 0.78f, 0.45f);
    static readonly Color Blue = new Color(0.30f, 0.55f, 0.95f);
    static readonly Color Red = new Color(0.90f, 0.36f, 0.40f);
    static readonly Color Grey = new Color(0.45f, 0.50f, 0.65f);

    Camera cam;
    Font font;
    RectTransform canvasRT, safe, hudContent;
    GameObject menuPanel, hudPanel, pausePanel, overPanel;
    Text scoreText, bestText, menuBestText, overScoreText, overBestText, bannerText;
    readonly List<Text> soundLabels = new List<Text>();
    int shown, target;
    Coroutine bannerRoutine, punchRoutine;

    public void Build(Camera worldCam)
    {
        cam = worldCam;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();

        var cgo = new GameObject("Canvas", typeof(RectTransform));
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2236);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        cgo.AddComponent<GraphicRaycaster>();
        canvasRT = (RectTransform)cgo.transform;

        // Vùng 1080x1920 tương ứng đúng khung world 9:16
        safe = NewRect("Safe", canvasRT);
        Center(safe, new Vector2(1080, 2236), Vector2.zero);

        BuildHud();
        BuildMenu();
        BuildPause();
        BuildOver();
    }

    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        var module = es.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
    }

    // ---------- Helpers ----------
    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Center(RectTransform rt, Vector2 size, Vector2 pos)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    RectTransform MakePanel(string name, float dimAlpha, out GameObject panel)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvasRT, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = new Color(0.03f, 0.04f, 0.12f, dimAlpha);
        img.raycastTarget = dimAlpha > 0f;
        var content = NewRect("Content", rt);
        Center(content, new Vector2(1080, 2236), Vector2.zero);
        panel = go;
        return content;
    }

    Text MakeText(RectTransform parent, string text, int size, Color color, Vector2 pos, Vector2 box)
    {
        var rt = NewRect("Text", parent);
        Center(rt, box, pos);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.45f);
        o.effectDistance = new Vector2(3f, -3f);
        return t;
    }

    Button MakeButton(RectTransform parent, string label, Vector2 pos, Vector2 size, Color color, int fontSize, Action cb, List<Text> labelSink = null)
    {
        var go = new GameObject(label + "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Center((RectTransform)go.transform, size, pos);

        var img = go.GetComponent<Image>();
        img.sprite = SpriteFactory.Button;
        img.type = Image.Type.Sliced;
        img.color = color;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        colors.selectedColor = Color.white;
        btn.colors = colors;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(() =>
        {
            if (OnClickSfx != null) OnClickSfx();
            if (cb != null) cb();
        });

        var t = MakeText((RectTransform)go.transform, label, fontSize, Color.white, Vector2.zero, size);
        if (labelSink != null) labelSink.Add(t);
        return btn;
    }

Button MakeIconButton(RectTransform parent, Sprite sprite, string name, Vector2 pos, Vector2 size, Action cb)
    {
        var go = new GameObject(name + "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Center((RectTransform)go.transform, size, pos);

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        colors.selectedColor = Color.white;
        btn.colors = colors;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener(() =>
        {
            if (OnClickSfx != null) OnClickSfx();
            if (cb != null) cb();
        });
        return btn;
    }


    // ---------- Panels ----------
void BuildHud()
    {
        var c = MakePanel("HUD", 0f, out hudPanel);
        hudContent = c;

        // Giữa: SCORE + điểm hiện tại
        MakeText(c, "SCORE", 52, new Color(0.30f, 0.85f, 1f), new Vector2(0, 955), new Vector2(600, 70));
        scoreText = MakeText(c, "0", 135, Color.white, new Vector2(0, 855), new Vector2(900, 150));

        // Trái: vương miện + điểm cao nhất
        if (SpriteFactory.Crown != null)
        {
            var crown = NewRect("Crown", c);
            Center(crown, new Vector2(122, 92), new Vector2(-431, 925));
            var ci = crown.gameObject.AddComponent<Image>();
            ci.sprite = SpriteFactory.Crown;
            ci.preserveAspect = true;
            ci.raycastTarget = false;
        }
        bestText = MakeText(c, "0", 46, new Color(1f, 0.87f, 0.35f), new Vector2(-431, 838), new Vector2(300, 60));

        // Phải: bánh răng (mở menu tạm dừng / cài đặt)
        if (SpriteFactory.Gear != null)
            MakeIconButton(c, SpriteFactory.Gear, "Pause", new Vector2(450, 913), new Vector2(92, 101), () => { if (OnPause != null) OnPause(); });
        else
            MakeButton(c, "II", new Vector2(430, 913), new Vector2(120, 120), Grey, 60, () => { if (OnPause != null) OnPause(); });

        bannerText = MakeText(c, "", 120, Color.white, new Vector2(0, 203), new Vector2(1000, 200));
        bannerText.gameObject.SetActive(false);
    }

    void BuildMenu()
    {
        var c = MakePanel("Menu", 0.78f, out menuPanel);
        MakeText(c, "BLOCK", 190, new Color(0.45f, 0.75f, 1f), new Vector2(0, 560), new Vector2(1000, 220));
        MakeText(c, "BLAST", 190, Gold, new Vector2(0, 380), new Vector2(1000, 220));
        menuBestText = MakeText(c, "BEST 0", 60, Color.white, new Vector2(0, 215), new Vector2(800, 80));
        MakeButton(c, "PLAY", new Vector2(0, 50), new Vector2(560, 160), Green, 80, () => { if (OnPlay != null) OnPlay(); });
        MakeButton(c, "SOUND: ON", new Vector2(0, -140), new Vector2(560, 120), Blue, 52, () => { if (OnToggleSound != null) OnToggleSound(); }, soundLabels);
        MakeButton(c, "QUIT", new Vector2(0, -300), new Vector2(560, 120), Red, 52, () => { if (OnQuit != null) OnQuit(); });
    }

    void BuildPause()
    {
        var c = MakePanel("Pause", 0.78f, out pausePanel);
        MakeText(c, "PAUSED", 150, Color.white, new Vector2(0, 340), new Vector2(1000, 200));
        MakeButton(c, "RESUME", new Vector2(0, 100), new Vector2(560, 150), Green, 70, () => { if (OnResume != null) OnResume(); });
        MakeButton(c, "RESTART", new Vector2(0, -80), new Vector2(560, 120), Blue, 52, () => { if (OnRestart != null) OnRestart(); });
        MakeButton(c, "SOUND: ON", new Vector2(0, -230), new Vector2(560, 120), Grey, 52, () => { if (OnToggleSound != null) OnToggleSound(); }, soundLabels);
        MakeButton(c, "MENU", new Vector2(0, -380), new Vector2(560, 120), Red, 52, () => { if (OnMenu != null) OnMenu(); });
    }

    void BuildOver()
    {
        var c = MakePanel("GameOver", 0.82f, out overPanel);
        MakeText(c, "GAME OVER", 130, Red, new Vector2(0, 420), new Vector2(1000, 180));
        MakeText(c, "SCORE", 50, new Color(0.7f, 0.78f, 1f), new Vector2(0, 290), new Vector2(600, 70));
        overScoreText = MakeText(c, "0", 170, Color.white, new Vector2(0, 175), new Vector2(900, 190));
        overBestText = MakeText(c, "BEST 0", 64, Gold, new Vector2(0, 40), new Vector2(900, 90));
        MakeButton(c, "RESTART", new Vector2(0, -150), new Vector2(560, 160), Green, 72, () => { if (OnRestart != null) OnRestart(); });
        MakeButton(c, "MENU", new Vector2(0, -330), new Vector2(560, 120), Blue, 52, () => { if (OnMenu != null) OnMenu(); });
    }

    void SetPanels(bool menu, bool hud, bool pause, bool over)
    {
        menuPanel.SetActive(menu);
        hudPanel.SetActive(hud);
        pausePanel.SetActive(pause);
        overPanel.SetActive(over);
    }

    // ---------- Public API ----------
    public void ShowMenu(int best)
    {
        menuBestText.text = "BEST " + best;
        SetPanels(true, false, false, false);
    }

    public void ShowHud() { SetPanels(false, true, false, false); }
    public void ShowPause() { SetPanels(false, true, true, false); }

    public void ShowGameOver(int score, int best, bool isNewBest)
    {
        overScoreText.text = score.ToString();
        overBestText.text = isNewBest ? "NEW BEST!" : "BEST " + best;
        overBestText.color = isNewBest ? new Color(1f, 0.9f, 0.3f) : Gold;
        SetPanels(false, true, false, true);
    }

public void SetBest(int best) { bestText.text = best.ToString(); }

    public void SetSoundLabel(bool on)
    {
        foreach (var t in soundLabels) t.text = on ? "SOUND: ON" : "SOUND: OFF";
    }

    public void SetScore(int s, bool instant)
    {
        bool up = s > target;
        target = s;
        if (instant)
        {
            shown = s;
            scoreText.text = s.ToString();
        }
        else if (up)
        {
            if (punchRoutine != null) StopCoroutine(punchRoutine);
            punchRoutine = StartCoroutine(PunchText(scoreText.rectTransform));
        }
    }

    void Update()
    {
        if (scoreText != null && shown != target)
        {
            int step = Mathf.Max(1, Mathf.CeilToInt((target - shown) * 8f * Time.deltaTime));
            shown = shown < target ? Mathf.Min(target, shown + step) : target;
            scoreText.text = shown.ToString();
        }
    }

    IEnumerator PunchText(RectTransform rt)
    {
        float el = 0f, d = 0.18f;
        while (el < d)
        {
            el += Time.deltaTime;
            rt.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(el / d * Mathf.PI));
            yield return null;
        }
        rt.localScale = Vector3.one;
        punchRoutine = null;
    }

    /// <summary>Chữ điểm "+N" bay lên tại một vị trí world.</summary>
    public void Popup(Vector3 world, string text, Color color, int size = 90)
    {
        Vector2 sp = cam.WorldToScreenPoint(world);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, sp, null, out local);
        var t = MakeText(hudContent, text, size, color, local, new Vector2(600, 140));
        StartCoroutine(PopupRoutine(t));
    }

    IEnumerator PopupRoutine(Text t)
    {
        var rt = t.rectTransform;
        Vector2 p0 = rt.anchoredPosition;
        Color c = t.color;
        float el = 0f, d = 0.95f;
        while (el < d)
        {
            if (t == null) yield break;
            el += Time.deltaTime;
            float k = el / d;
            rt.anchoredPosition = p0 + Vector2.up * (170f * Mathf.Sqrt(k));
            rt.localScale = Vector3.one * (1f + 0.3f * Mathf.Max(0f, 1f - k * 5f));
            c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            t.color = c;
            yield return null;
        }
        if (t != null) Destroy(t.gameObject);
    }

    /// <summary>Banner lớn giữa màn hình (COMBO, GREAT!, PERFECT...).</summary>
    public void Banner(string text, Color color)
    {
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine(text, color));
    }

    IEnumerator BannerRoutine(string text, Color color)
    {
        bannerText.gameObject.SetActive(true);
        bannerText.text = text;
        var rt = bannerText.rectTransform;
        float el = 0f;
        while (el < 1.3f)
        {
            el += Time.deltaTime;
            float s;
            if (el < 0.18f) s = Mathf.Lerp(0.4f, 1.2f, el / 0.18f);
            else if (el < 0.3f) s = Mathf.Lerp(1.2f, 1f, (el - 0.18f) / 0.12f);
            else s = 1f;
            float a = el < 0.9f ? 1f : 1f - (el - 0.9f) / 0.4f;
            rt.localScale = Vector3.one * s;
            bannerText.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(a));
            yield return null;
        }
        bannerText.gameObject.SetActive(false);
        bannerRoutine = null;
    }
}
