using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Điều phối toàn bộ game Block Blast: dựng bàn cờ, kéo thả bằng chuột, chấm điểm, game over, menu.</summary>
public class GameManager : MonoBehaviour
{
    enum State { Menu, Playing, Paused, Busy, GameOver }

    const float BoardCenterY = 1.73f;
    const float TrayY = -5.9f;
    const float DragLift = 0.55f;
    static readonly float[] TrayX = { -3.1f, 0f, 3.1f };

    Camera cam;
    CameraFitter fitter;
    SoundBank sound;
    FxManager fx;
    UIController ui;
    Transform worldRoot;

    readonly Board board = new Board();
    State state = State.Menu;
    int score, best, bestAtStart, streak, movesSinceClear;
    bool newBestAnnounced;

    Vector2 boardOrigin;   // tâm ô (0,0)
    float blockScale;
    readonly SpriteRenderer[,] blockSR = new SpriteRenderer[Board.Size, Board.Size];
    readonly SpriteRenderer[,] ghostSR = new SpriteRenderer[Board.Size, Board.Size];
    readonly SpriteRenderer[,] hintSR = new SpriteRenderer[Board.Size, Board.Size];
    readonly PieceView[] tray = new PieceView[3];

    PieceView dragging;
    int snapX, snapY;
    bool snapValid;
    PieceView lastPreviewPiece;
    int lastPreviewX = -99, lastPreviewY = -99;
    readonly List<int> tmpRows = new List<int>();
    readonly List<int> tmpCols = new List<int>();

    Vector3 CellWorld(int x, int y) { return new Vector3(boardOrigin.x + x, boardOrigin.y + y, 0f); }

    // ====================== Khởi tạo ======================
    void Awake()
    {
        Application.targetFrameRate = 60;
        best = PlayerPrefs.GetInt("BB_Best", 0);

        cam = Camera.main;
        if (cam == null)
        {
            var cgo = new GameObject("Main Camera");
            cgo.tag = "MainCamera";
            cam = cgo.AddComponent<Camera>();
        }
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.22f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 50f;

        worldRoot = new GameObject("World").transform;

        var bg = new GameObject("Background");
        bg.transform.SetParent(worldRoot, false);
        var bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = SpriteFactory.Background;
        bgSr.sortingOrder = -100;

        fitter = cam.GetComponent<CameraFitter>();
        if (fitter == null) fitter = cam.gameObject.AddComponent<CameraFitter>();
        fitter.Init(cam, bg.transform);

        sound = gameObject.AddComponent<SoundBank>();
        sound.Muted = PlayerPrefs.GetInt("BB_Muted", 0) == 1;
        fx = gameObject.AddComponent<FxManager>();
        ui = gameObject.AddComponent<UIController>();

        BuildBoard();
        BuildTrayBackdrop();

        ui.Build(cam);
        ui.OnPlay = StartGame;
        ui.OnRestart = StartGame;
        ui.OnResume = ResumeGame;
        ui.OnPause = PauseGame;
        ui.OnMenu = GoToMenu;
        ui.OnQuit = QuitGame;
        ui.OnToggleSound = ToggleSound;
        ui.OnClickSfx = () => sound.Play(sound.Click);
        ui.SetSoundLabel(!sound.Muted);
    }

    void Start() { GoToMenu(); }

    SpriteRenderer MakeSR(string name, Sprite sp, Color c, int order, float scale, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(worldRoot, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.color = c;
        sr.sortingOrder = order;
        return sr;
    }

void BuildBoard()
    {
        boardOrigin = new Vector2(-(Board.Size - 1) * 0.5f, BoardCenterY - (Board.Size - 1) * 0.5f);
        var center = new Vector3(0, BoardCenterY, 0);

        // Có art thì dùng: lưới 8x8 + khung neon. Không có thì quay về bảng vẽ bằng code.
        var grid = SpriteFactory.BoardGrid;
        bool art = grid != null;
        if (art)
        {
            var fill = SpriteFactory.Fill;
            if (fill != null)
                MakeSR("BoardBack", fill, new Color(0.09f, 0.11f, 0.32f, 0.85f), -1, SpriteFactory.FitScale(fill, 8.3f), center);
            MakeSR("BoardGrid", grid, Color.white, 0, SpriteFactory.FitScale(grid, 8.03f), center);
            var frame = SpriteFactory.BoardFrame;
            if (frame != null)
                MakeSR("BoardFrame", frame, Color.white, 1, SpriteFactory.FitScale(frame, 8.5f), center);
        }
        else
        {
            var panel = MakeSR("BoardPanel", SpriteFactory.Button, new Color(0.12f, 0.15f, 0.34f), 0, 1f, center);
            panel.drawMode = SpriteDrawMode.Sliced;
            panel.size = new Vector2(8.6f, 8.6f);
        }

        blockScale = SpriteFactory.FitScale(SpriteFactory.Block, 0.94f);
        float tileScale = SpriteFactory.FitScale(SpriteFactory.Cell, 0.96f);

        for (int x = 0; x < Board.Size; x++)
        {
            for (int y = 0; y < Board.Size; y++)
            {
                Vector3 p = CellWorld(x, y);
                if (!art)
                    MakeSR("Tile", SpriteFactory.Cell, new Color(0.19f, 0.23f, 0.45f), 1, tileScale, p);

                blockSR[x, y] = MakeSR("Block", SpriteFactory.Block, Color.white, 2, blockScale, p);
                blockSR[x, y].enabled = false;

                ghostSR[x, y] = MakeSR("Ghost", SpriteFactory.Block, Color.white, 3, blockScale, p);
                ghostSR[x, y].enabled = false;

                hintSR[x, y] = MakeSR("Hint", SpriteFactory.Block, new Color(1f, 1f, 1f, 0.35f), 4, blockScale, p);
                hintSR[x, y].enabled = false;
            }
        }
    }

void BuildTrayBackdrop()
    {
        // Mẫu giao diện không có khay nền: các khối nằm thẳng trên background.
    }

    // ====================== Luồng game ======================
    void GoToMenu()
    {
        if (dragging != null) CancelDrag();
        StopAllCoroutines();
        state = State.Menu;
        ClearTray();
        board.Clear();
        RefreshBoardVisuals();
        HidePreview();
        ui.ShowMenu(best);
    }

    void StartGame()
    {
        StopAllCoroutines();
        if (dragging != null) CancelDrag();
        HidePreview();

        score = 0; streak = 0; movesSinceClear = 0;
        bestAtStart = best;
        newBestAnnounced = false;
        board.Clear();
        RefreshBoardVisuals();
        ClearTray();

        ui.SetScore(0, true);
        ui.SetBest(best);
        ui.ShowHud();
        state = State.Playing;
        SpawnTray();
    }

    void PauseGame()
    {
        if (state != State.Playing) return;
        if (dragging != null) CancelDrag();
        state = State.Paused;
        ui.ShowPause();
    }

    void ResumeGame()
    {
        if (state != State.Paused) return;
        state = State.Playing;
        ui.ShowHud();
    }

    void ToggleSound()
    {
        sound.Muted = !sound.Muted;
        PlayerPrefs.SetInt("BB_Muted", sound.Muted ? 1 : 0);
        ui.SetSoundLabel(!sound.Muted);
    }

    void QuitGame()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnApplicationQuit() { PlayerPrefs.Save(); }

void RefreshBoardVisuals()
    {
        for (int x = 0; x < Board.Size; x++)
            for (int y = 0; y < Board.Size; y++)
            {
                int c = board.Cells[x, y];
                var sr = blockSR[x, y];
                sr.enabled = c >= 0;
                sr.transform.localScale = Vector3.one * blockScale;
                if (c >= 0) SpriteFactory.SetBlock(sr, c);
            }
    }

    // ====================== Khay khối ======================
    void ClearTray()
    {
        for (int i = 0; i < tray.Length; i++)
        {
            if (tray[i] != null) Destroy(tray[i].gameObject);
            tray[i] = null;
        }
    }

    ShapeData[] PickShapes()
    {
        var result = new ShapeData[3];
        for (int attempt = 0; attempt < 40; attempt++)
        {
            for (int i = 0; i < 3; i++) result[i] = ShapeLibrary.Pick();
            bool anyFits = false;
            for (int i = 0; i < 3; i++) if (board.CanPlaceAnywhere(result[i])) anyFits = true;
            if (anyFits) break;
        }
        return result;
    }

    void SpawnTray()
    {
        var shapes = PickShapes();
        for (int i = 0; i < 3; i++)
        {
            Vector3 home = new Vector3(TrayX[i], TrayY, 0f);
            var p = PieceView.Create(shapes[i], Random.Range(0, ShapeLibrary.Palette.Length), i, home, worldRoot);
            p.transform.localScale = Vector3.zero;
            p.MoveTo(home, PieceView.TrayScale, 0.3f, i * 0.08f);
            tray[i] = p;
        }
        UpdateDim();
    }

    bool TrayEmpty()
    {
        for (int i = 0; i < tray.Length; i++) if (tray[i] != null) return false;
        return true;
    }

    bool AnyMoveAvailable()
    {
        for (int i = 0; i < tray.Length; i++)
            if (tray[i] != null && board.CanPlaceAnywhere(tray[i].Shape)) return true;
        return false;
    }

    void UpdateDim()
    {
        for (int i = 0; i < tray.Length; i++)
            if (tray[i] != null) tray[i].SetDim(!board.CanPlaceAnywhere(tray[i].Shape));
    }

    // ====================== Input kéo thả ======================
    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (state == State.Playing) PauseGame();
            else if (state == State.Paused) ResumeGame();
        }

        if (state != State.Playing) return;
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 sp = mouse.position.ReadValue();
        Vector3 wp = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, -cam.transform.position.z));
        Vector2 mp = new Vector2(wp.x, wp.y);

        if (dragging == null)
        {
            if (mouse.leftButton.wasPressedThisFrame) TryPick(mp);
        }
        else
        {
            UpdateDrag(mp);
            if (mouse.leftButton.wasReleasedThisFrame) Drop();
        }
    }

    void TryPick(Vector2 mp)
    {
        for (int i = 0; i < tray.Length; i++)
        {
            var p = tray[i];
            if (p == null || !p.Contains(mp)) continue;

            if (!board.CanPlaceAnywhere(p.Shape))
            {
                sound.Play(sound.Invalid);
                p.Shake();
                return;
            }
            dragging = p;
            p.StopMoving();
            p.SetSorting(20);
            sound.Play(sound.Pick);
            return;
        }
    }

    void UpdateDrag(Vector2 mp)
    {
        Vector3 target = new Vector3(mp.x, mp.y + DragLift, 0f);
        float dt = Time.deltaTime;
        dragging.transform.position = Vector3.Lerp(dragging.transform.position, target, 1f - Mathf.Exp(-30f * dt));
        dragging.transform.localScale = Vector3.Lerp(dragging.transform.localScale, Vector3.one, 1f - Mathf.Exp(-22f * dt));

        snapValid = TrySnap(dragging, target, out snapX, out snapY);
        if (snapValid) ShowPreview(dragging, snapX, snapY);
        else HidePreview();
    }

    /// <summary>Tìm ô neo hợp lệ gần nhất (góc dưới-trái của khối) so với vị trí đang kéo.</summary>
    bool TrySnap(PieceView p, Vector3 center, out int ox, out int oy)
    {
        Vector2 bl = (Vector2)center + p.BottomLeftOffset();
        float fx = bl.x - boardOrigin.x;
        float fy = bl.y - boardOrigin.y;

        ox = oy = 0;
        bool found = false;
        float bestD = 0.75f * 0.75f;
        int baseX = Mathf.FloorToInt(fx), baseY = Mathf.FloorToInt(fy);
        for (int dx = 0; dx <= 1; dx++)
        {
            for (int dy = 0; dy <= 1; dy++)
            {
                int cx = baseX + dx, cy = baseY + dy;
                if (!board.CanPlace(p.Shape, cx, cy)) continue;
                float d = (cx - fx) * (cx - fx) + (cy - fy) * (cy - fy);
                if (d <= bestD) { bestD = d; ox = cx; oy = cy; found = true; }
            }
        }
        return found;
    }

void ShowPreview(PieceView p, int ox, int oy)
    {
        if (lastPreviewPiece == p && lastPreviewX == ox && lastPreviewY == oy) return;
        HidePreview();
        lastPreviewPiece = p; lastPreviewX = ox; lastPreviewY = oy;

        foreach (var c in p.Shape.Cells)
        {
            var g = ghostSR[ox + c.x, oy + c.y];
            g.enabled = true;
            SpriteFactory.SetBlock(g, p.ColorIndex, 0.55f);
        }

        board.GetCompletedLines(p.Shape, ox, oy, tmpRows, tmpCols);
        foreach (int r in tmpRows) for (int x = 0; x < Board.Size; x++) hintSR[x, r].enabled = true;
        foreach (int c in tmpCols) for (int y = 0; y < Board.Size; y++) hintSR[c, y].enabled = true;
    }

    void HidePreview()
    {
        lastPreviewPiece = null; lastPreviewX = lastPreviewY = -99;
        for (int x = 0; x < Board.Size; x++)
            for (int y = 0; y < Board.Size; y++)
            {
                ghostSR[x, y].enabled = false;
                hintSR[x, y].enabled = false;
            }
    }

    void CancelDrag()
    {
        var p = dragging;
        dragging = null;
        HidePreview();
        if (p != null)
        {
            p.SetSorting(5);
            p.MoveTo(p.HomePos, PieceView.TrayScale, 0.2f);
        }
    }

    void Drop()
    {
        var p = dragging;
        dragging = null;
        HidePreview();

        if (snapValid) PlacePiece(p, snapX, snapY);
        else
        {
            sound.Play(sound.Invalid);
            p.SetSorting(5);
            p.MoveTo(p.HomePos, PieceView.TrayScale, 0.22f);
        }
    }

    // ====================== Đặt khối + chấm điểm ======================
    void PlacePiece(PieceView p, int ox, int oy)
    {
        var shape = p.Shape;
        int color = p.ColorIndex;
        tray[p.Slot] = null;
        Destroy(p.gameObject);

        board.Place(shape, ox, oy, color);
        foreach (var c in shape.Cells)
        {
            var sr = blockSR[ox + c.x, oy + c.y];
            sr.enabled = true;
            SpriteFactory.SetBlock(sr, color);
            fx.Punch(sr.transform, blockScale);
        }
        sound.Play(sound.Place, 1f, Random.Range(0.95f, 1.05f));

        int gained = shape.Cells.Length;

        var rows = new List<int>();
        var cols = new List<int>();
        board.FindCompletedLines(rows, cols);
        int lines = rows.Count + cols.Count;

        if (lines > 0)
        {
            streak++;
            movesSinceClear = 0;

            var cleared = new List<Vector2Int>();
            var colors = new List<int>();
            board.ClearLines(rows, cols, cleared, colors);

            Vector2 origin = new Vector2(ox + (shape.Width - 1) * 0.5f, oy + (shape.Height - 1) * 0.5f);
            Vector3 avg = Vector3.zero;
            for (int i = 0; i < cleared.Count; i++)
            {
                Vector3 wp = CellWorld(cleared[i].x, cleared[i].y);
                avg += wp;
                blockSR[cleared[i].x, cleared[i].y].enabled = false;
                float dist = Vector2.Distance(new Vector2(cleared[i].x, cleared[i].y), origin);
                fx.PopBlock(wp, ShapeLibrary.Palette[colors[i]], dist * 0.035f);
            }
            avg /= Mathf.Max(1, cleared.Count);

            int lineScore = Mathf.RoundToInt(10f * lines * lines * (1f + 0.5f * (streak - 1)));
            gained += lineScore;

            bool perfect = board.IsEmpty();
            if (perfect) gained += 300;

            sound.PlayClear(lines, streak);
            if (lines >= 2 || streak >= 3) fitter.Shake(0.07f * Mathf.Min(lines, 4), 0.25f);

            ui.Popup(avg, "+" + gained, Color.white, 90);
            if (perfect) ui.Banner("PERFECT CLEAR!", new Color(1f, 0.9f, 0.3f));
            else if (streak >= 2) ui.Banner("COMBO x" + streak, new Color(1f, 0.6f, 0.25f));
            else if (lines == 2) ui.Banner("GREAT!", new Color(0.5f, 0.9f, 1f));
            else if (lines == 3) ui.Banner("AWESOME!", new Color(0.6f, 1f, 0.6f));
            else if (lines >= 4) ui.Banner("INCREDIBLE!", new Color(1f, 0.5f, 0.8f));
        }
        else
        {
            movesSinceClear++;
            if (movesSinceClear >= 3) streak = 0;
        }

        score += gained;
        ui.SetScore(score, false);
        if (score > best)
        {
            best = score;
            ui.SetBest(best);
            PlayerPrefs.SetInt("BB_Best", best);
            if (!newBestAnnounced && bestAtStart > 0)
            {
                newBestAnnounced = true;
                sound.Play(sound.NewBest, 0.8f);
                ui.Banner("NEW BEST!", new Color(1f, 0.9f, 0.3f));
            }
        }

        if (TrayEmpty()) SpawnTray();
        UpdateDim();
        if (!AnyMoveAvailable()) StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        state = State.Busy;
        yield return new WaitForSeconds(0.7f);

        sound.Play(sound.GameOver);
        Color grey = new Color(0.45f, 0.47f, 0.55f);
        for (int y = 0; y < Board.Size; y++)
        {
            for (int x = 0; x < Board.Size; x++)
                if (blockSR[x, y].enabled)
                    blockSR[x, y].color = Color.Lerp(blockSR[x, y].color, grey, 0.85f);
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(0.4f);

        PlayerPrefs.SetInt("BB_Best", best);
        PlayerPrefs.Save();
        ui.ShowGameOver(score, best, score > bestAtStart && score > 0);
        state = State.GameOver;
    }
}
