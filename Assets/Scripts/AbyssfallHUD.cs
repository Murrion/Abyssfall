using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// Attach to any persistent GameObject ("HUDManager").
/// Right-click component header → "Rebuild Canvas" to regenerate UI.
[ExecuteAlways]
public class AbyssfallHUD : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerStats stats;

    [Header("Textures")]
    [SerializeField] private Texture2D hpFrameTexture;   // UIhealthTexture.png

    // ── Live refs ─────────────────────────────────────────────────
    private Image hpFill, manaFill, staminaFill;
    private Text  hpText, manaText;

    private Canvas hudCanvas;

    // ── Layout ────────────────────────────────────────────────────
    const float BOT_H  = 160f;
    const float PORT_W = 250f;

    const string TEX_PATH = "Assets/Textures/HUD/UIhealthTexture.png";

    // =========================================================
    // Lifecycle
    // =========================================================

    private void Awake()
    {
        if (stats == null)
            stats = FindFirstObjectByType<PlayerStats>();

        TryLoadTexture();

        if (transform.Find("HUDCanvas") == null)
            BuildCanvas();
        else
            hudCanvas = GetComponentInChildren<Canvas>();
    }

    void TryLoadTexture()
    {
#if UNITY_EDITOR
        if (hpFrameTexture == null)
            hpFrameTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_PATH);
#endif
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        if (stats == null || hudCanvas == null) return;
        Tick();
    }

    // =========================================================
    // Tick
    // =========================================================

    void Tick()
    {
        Fill(hpFill,      stats.currentHp,     stats.maxHp);
        Fill(manaFill,    stats.currentMana,    stats.maxMana);
        Fill(staminaFill, stats.CurrentStamina, stats.MaxStamina);

        SetText(hpText,   $"{(int)stats.currentHp} / {(int)stats.maxHp}");
        SetText(manaText, $"{(int)stats.currentMana} / {(int)stats.maxMana}");
    }

    // =========================================================
    // Builder
    // =========================================================

    [ContextMenu("Rebuild Canvas")]
    void BuildCanvas()
    {
        TryLoadTexture();

        var old = transform.Find("HUDCanvas");
        if (old != null) DestroyImmediate(old.gameObject);

        var cgo = new GameObject("HUDCanvas");
        cgo.transform.SetParent(transform, false);

        var cv = cgo.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 100;
        hudCanvas = cv;

        var sc = cgo.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280, 720);
        sc.matchWidthOrHeight  = 0.5f;

        cgo.AddComponent<GraphicRaycaster>();
        BuildPortrait(cgo.transform);
    }

    // ── Portrait section ─────────────────────────────────────────
    void BuildPortrait(Transform root)
    {
        // Outer anchor — bottom-left
        var panel = GO("Portrait_Panel", root);
        SetBL(panel, 0, 0, PORT_W, BOT_H);

        // ── Background: UIhealthTexture fills the full panel ──────
        if (hpFrameTexture != null)
        {
            var bg = panel.AddComponent<RawImage>();
            bg.texture        = hpFrameTexture;
            bg.raycastTarget  = false;
            // stretch to panel (already at root level of panel GO)
            var bgRT = panel.GetComponent<RectTransform>();
            // The RawImage is on the panel itself — nothing extra needed
        }
        else
        {
            // Fallback plain background
            var bg = panel.AddComponent<Image>();
            bg.color = C(9, 7, 20, 240);
            bg.raycastTarget = false;
        }

        // ── Dark overlay to improve bar readability ───────────────
        var overlay = Img("Overlay", panel.transform, C(0, 0, 0, 110));
        Stretch(overlay.gameObject);

        // Right border
        Img("Panel_RLine", panel.transform, C(42, 34, 69))
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 1);
        // Top border
        Img("Panel_TLine", panel.transform, C(42, 34, 69))
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 1);

        // ── Content area ──────────────────────────────────────────
        // The UIhealthTexture is 512×512 stretched to PORT_W × BOT_H (250×160).
        // Its two red bar lines land at roughly:
        //   upper line: ~18% from top  → 131px from bottom  (HP)
        //   lower line: ~75% from top  →  40px from bottom  (Mana)
        // Stamina sits as a slim strip at the very bottom edge.

        // ── HP bar ───────────────────────────────────────────────
        // BG tinted over the texture's upper red channel
        hpFill = Bar("HP", panel.transform,
            C(80, 0, 0, 180), C(200, 30, 30, 220),
            // anchor: full width, centered on the texture's upper bar line
            0f, 0f, 1f, 0f,   // anchorMin/Max — anchored to bottom
            16f, 120f, -16f, 140f);  // offsets: 120-140px from bottom

        // HP text centered on the bar
        hpText = Txt("HP_Text", hpFill.transform.parent, "", 13,
                     C(255, 200, 200, 240));
        SA(hpText.gameObject, 0,0,1,0, 16,120,-16,140);

        // ── Mana bar ─────────────────────────────────────────────
        manaFill = Bar("Mana", panel.transform,
            C(0, 0, 80, 180), C(30, 50, 190, 220),
            0f, 0f, 1f, 0f,
            16f, 30f, -16f, 48f);   // 30-48px from bottom

        manaText = Txt("Mana_Text", manaFill.transform.parent, "", 13,
                       C(160, 185, 255, 240));
        SA(manaText.gameObject, 0,0,1,0, 16,30,-16,48);

        // ── Stamina bar (slim, no label) ─────────────────────────
        staminaFill = Bar("Stamina", panel.transform,
            C(20, 20, 20, 160), C(110, 110, 110, 220),
            0f, 0f, 1f, 0f,
            16f, 10f, -16f, 20f);   // 10-20px from bottom

        // Bar labels (left edge, inside bars)
        var hpLbl = Txt("HP_Lbl", panel.transform, "HP", 9, C(255,160,160,180), TextAnchor.MiddleLeft);
        SA(hpLbl.gameObject, 0,0,0,0, 18,125,52,139);

        var mnLbl = Txt("Mn_Lbl", panel.transform, "MP", 9, C(160,180,255,180), TextAnchor.MiddleLeft);
        SA(mnLbl.gameObject, 0,0,0,0, 18,35,52,47);
    }

    // =========================================================
    // Helpers
    // =========================================================

    static void Fill(Image img, float cur, float max)
    { if (img != null && max > 0f) img.fillAmount = Mathf.Clamp01(cur / max); }

    static void SetText(Text t, string s) { if (t != null) t.text = s; }

    static Color C(int r, int g, int b, int a = 255)
        => new Color(r/255f, g/255f, b/255f, a/255f);

    static readonly Color BORDER2 = C(30, 24, 48);

    // ── Primitive factories ───────────────────────────────────────

    static GameObject GO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static Image Img(string name, Transform parent, Color col, bool ray = false)
    {
        var img = GO(name, parent).AddComponent<Image>();
        img.color = col; img.raycastTarget = ray;
        return img;
    }

    static Text Txt(string name, Transform parent, string s, int sz, Color col,
                    TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        var t = GO(name, parent).AddComponent<Text>();
        t.text = s; t.fontSize = sz; t.color = col;
        t.alignment = anchor; t.raycastTarget = false;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
              ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        return t;
    }

    static void SA(GameObject go,
        float x0, float y0, float x1, float y1,
        float l, float b, float r, float t)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = new Vector2(l,  b);  rt.offsetMax = new Vector2(r,  t);
    }

    static void Stretch(GameObject go)
        => SA(go, 0,0,1,1, 0,0,0,0);

    static void SetBL(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    Image Bar(string id, Transform parent, Color bg, Color fill,
              float x0, float y0, float x1, float y1,
              float l, float b, float r, float t)
    {
        var bgI = Img(id+"_BG", parent, bg);
        SA(bgI.gameObject, x0,y0,x1,y1, l,b,r,t);

        var brd = Img(id+"_Brd", bgI.transform,
                      new Color(BORDER2.r, BORDER2.g, BORDER2.b, 0.35f));
        Stretch(brd.gameObject);

        var fi = Img(id+"_Fill", bgI.transform, fill);
        Stretch(fi.gameObject);
        fi.type       = Image.Type.Filled;
        fi.fillMethod = Image.FillMethod.Horizontal;
        fi.fillOrigin = 0;
        fi.fillAmount = 1f;
        return fi;
    }
}
