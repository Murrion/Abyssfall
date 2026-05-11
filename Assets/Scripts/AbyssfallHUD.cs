using UnityEngine;
using UnityEngine.UI;

/// Attach to any persistent GameObject (e.g. "HUDManager").
/// Call [ContextMenu] "Rebuild Canvas" once from the Inspector, or let Awake build it
/// automatically if no HUDCanvas child is found.
[ExecuteAlways]
public class AbyssfallHUD : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerStats stats;

    // ---- Live UI refs ----
    private Image   hpFill, manaFill, staminaFill, corruptFill, pdFill, depthPressFill;
    private Text    hpText, manaText, corruptPctText;
    private Text    leyerNumText, pdText, damageText, speedText, defenceText;
    private Text    depthText, depthTierText, depthZoneText, depthPressText;
    private Image[] comboDots;
    private Text    comboCountText, comboTimerText;
    private Image   vigL, vigR, vigT, vigB;

    private Canvas hudCanvas;

    // =========================================================
    // Lifecycle
    // =========================================================

    private void Awake()
    {
        if (stats == null)
            stats = FindFirstObjectByType<PlayerStats>();

        if (transform.Find("HUDCanvas") == null)
            BuildCanvas();
        else
            hudCanvas = GetComponentInChildren<Canvas>();
    }

    private void Update()
    {
        // In editor without play mode, skip live tick to avoid spam-creating objects
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        if (stats == null || hudCanvas == null) return;
        Tick();
    }

    // =========================================================
    // Tick — update every frame
    // =========================================================

    private void Tick()
    {
        Fill(hpFill,       stats.currentHp,      stats.maxHp);
        Fill(manaFill,     stats.currentMana,     stats.maxMana);
        Fill(staminaFill,  stats.CurrentStamina,  stats.MaxStamina);
        Fill(corruptFill,  stats.corruption,      100f);
        Fill(pdFill,       stats.pd,              stats.pdToNextLeyer);
        Fill(depthPressFill, stats.depthPressure, 100f);

        SetText(hpText,        $"{(int)stats.currentHp} / {(int)stats.maxHp}");
        SetText(manaText,      $"{(int)stats.currentMana} / {(int)stats.maxMana}");
        SetText(corruptPctText,$"{(int)stats.corruption}%");
        SetText(leyerNumText,  stats.leyer.ToString());
        SetText(pdText,        $"{(int)stats.pd}/{(int)stats.pdToNextLeyer} PD");
        SetText(damageText,    stats.damage.ToString("0"));
        SetText(speedText,     stats.Speed.ToString("0.0"));
        SetText(defenceText,   stats.defence.ToString("0"));
        SetText(depthText,     $"{(int)stats.depthMeters}m");
        SetText(depthTierText, $"TIER {stats.depthTier}");
        SetText(depthZoneText, stats.depthZone);
        SetText(depthPressText,$"{(int)stats.depthPressure} PS");

        TickCombo();

        float vigAlpha = Mathf.Lerp(0.04f, 0.55f, stats.corruption / 100f);
        SetAlpha(vigL, vigAlpha); SetAlpha(vigR, vigAlpha);
        SetAlpha(vigT, vigAlpha * 0.7f); SetAlpha(vigB, vigAlpha * 0.7f);
    }

    private void TickCombo()
    {
        if (comboDots == null) return;
        int step = stats.ComboStep;
        int max  = stats.ComboMaxSteps;
        for (int i = 0; i < comboDots.Length; i++)
        {
            if (comboDots[i] == null) continue;
            comboDots[i].color = i < step ? Hex("C83030") : Hex("0F0820");
        }
        SetText(comboCountText, $"{step}<size=10>/{max}</size>");
        float left = Mathf.Max(0f, stats.ComboWindowEnd - Time.time);
        SetText(comboTimerText, step > 0 ? $"{left:0.00}s" : "");
    }

    // =========================================================
    // Static helpers
    // =========================================================

    static void Fill(Image img, float cur, float max)
    {
        if (img != null && max > 0f) img.fillAmount = Mathf.Clamp01(cur / max);
    }
    static void SetText(Text t, string s) { if (t != null) t.text = s; }
    static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color; c.a = a; img.color = c;
    }
    static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString("#" + h, out Color c);
        return c;
    }
    static Color C(int r, int g, int b, int a = 255)
        => new Color(r / 255f, g / 255f, b / 255f, a / 255f);

    // =========================================================
    // BUILDER
    // =========================================================

    [ContextMenu("Rebuild Canvas")]
    private void BuildCanvas()
    {
        var old = transform.Find("HUDCanvas");
        if (old != null) DestroyImmediate(old.gameObject);

        var canvasGO = new GameObject("HUDCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        hudCanvas = canvas;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        Transform r = canvasGO.transform;

        BuildVignette(r);
        BuildTopBar(r);
        BuildBottomPanel(r);
        BuildMinimap(r);
        BuildComboFloat(r);
    }

    // ---- palette ----
    static readonly Color BG       = C(6,  5,  16);
    static readonly Color BG2      = C(10, 8,  24);
    static readonly Color BORDER   = C(42, 34, 69);
    static readonly Color BORDER2  = C(30, 24, 48);
    static readonly Color HP_FILL  = C(170, 32, 32);
    static readonly Color HP_BG    = C(40,  8,  8);
    static readonly Color MN_FILL  = C(26,  32, 128);
    static readonly Color MN_BG    = C(8,   8,  48);
    static readonly Color ST_FILL  = C(90,  90, 90);
    static readonly Color ST_BG    = C(9,   8,  16);
    static readonly Color CR_FILL  = C(136, 0,  204);
    static readonly Color CR_BG    = C(12,  0,  24);
    static readonly Color PD_FILL  = C(96,  48, 160);
    static readonly Color PRI      = C(138, 96, 192);
    static readonly Color SEC      = C(58,  48, 80);
    static readonly Color DIM      = C(32,  26, 48);

    // ---- primitive factories ----

    static GameObject GO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static Image Img(string name, Transform parent, Color col, bool raycast = false)
    {
        var go  = GO(name, parent);
        var img = go.AddComponent<Image>();
        img.color         = col;
        img.raycastTarget = raycast;
        return img;
    }

    static Text Txt(string name, Transform parent, string content, int size, Color col,
                    TextAnchor anchor = TextAnchor.MiddleCenter)
    {
        var go  = GO(name, parent);
        var txt = go.AddComponent<Text>();
        txt.text           = content;
        txt.fontSize       = size;
        txt.color          = col;
        txt.alignment      = anchor;
        txt.raycastTarget  = false;
        txt.supportRichText = true;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        return txt;
    }

    // Anchors via min/max + absolute offsets
    static RectTransform SA(GameObject go,
        float axMin, float ayMin, float axMax, float ayMax,
        float oxMin, float oyMin, float oxMax, float oyMax)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin  = new Vector2(axMin, ayMin);
        rt.anchorMax  = new Vector2(axMax, ayMax);
        rt.offsetMin  = new Vector2(oxMin, oyMin);
        rt.offsetMax  = new Vector2(oxMax, oyMax);
        return rt;
    }

    // Full stretch with optional insets
    static void Stretch(GameObject go, float l = 0, float b = 0, float r = 0, float t = 0)
        => SA(go, 0, 0, 1, 1, l, b, -r, -t);

    // Horizontal stretch, fixed height at bottom
    static void StBot(GameObject go, float h, float l = 0, float r = 0)
        => SA(go, 0, 0, 1, 0, l, 0, -r, h);

    // Horizontal stretch, fixed height at top
    static void StTop(GameObject go, float h, float l = 0, float r = 0)
        => SA(go, 0, 1, 1, 1, l, -h, -r, 0);

    // Thin horizontal line at top of parent
    static void LineTop(GameObject go)
        => SA(go, 0, 1, 1, 1, 0, -1, 0, 0);

    // Thin vertical line at right of parent
    static void LineRight(GameObject go)
        => SA(go, 1, 0, 1, 1, -1, 0, 0, 0);

    // Bottom-left corner, absolute size
    static void BL(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
        rt.pivot     = Vector2.zero;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    // Bottom-right corner, absolute size (pivot right)
    static void BR(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.right; rt.anchorMax = Vector2.right;
        rt.pivot     = Vector2.right;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    // Centered at bottom
    static void BC(GameObject go, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot     = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    // Filled bar — returns the fill Image
    Image Bar(string name, Transform parent, Color bg, Color fill,
        float axMin, float ayMin, float axMax, float ayMax,
        float oxMin, float oyMin, float oxMax, float oyMax)
    {
        var bgImg = Img(name + "_BG", parent, bg);
        SA(bgImg.gameObject, axMin, ayMin, axMax, ayMax, oxMin, oyMin, oxMax, oyMax);

        // border
        var brd = Img(name + "_Brd", bgImg.transform, BORDER2);
        Stretch(brd.gameObject);
        brd.color = new Color(BORDER2.r, BORDER2.g, BORDER2.b, 0.5f);

        var fillImg = Img(name + "_Fill", bgImg.transform, fill);
        Stretch(fillImg.gameObject);
        fillImg.type        = Image.Type.Filled;
        fillImg.fillMethod  = Image.FillMethod.Horizontal;
        fillImg.fillOrigin  = 0;
        fillImg.fillAmount  = 1f;
        return fillImg;
    }

    // =========================================================
    // SECTION BUILDERS
    // =========================================================

    void BuildVignette(Transform root)
    {
        vigL = Img("Vig_L", root, C(64, 0, 128, 80));
        SA(vigL.gameObject, 0, 0, 0, 1, 0, 0, 14, 0);

        vigR = Img("Vig_R", root, C(64, 0, 128, 80));
        SA(vigR.gameObject, 1, 0, 1, 1, -14, 0, 0, 0);

        vigT = Img("Vig_T", root, C(64, 0, 128, 60));
        SA(vigT.gameObject, 0, 1, 1, 1, 0, -10, 0, 0);

        vigB = Img("Vig_B", root, C(64, 0, 128, 60));
        SA(vigB.gameObject, 0, 0, 1, 0, 0, 0, 0, 10);
    }

    void BuildTopBar(Transform root)
    {
        var bar = Img("TopBar", root, C(8, 6, 18, 245));
        StTop(bar.gameObject, 40);

        Img("TopBar_BotLine", bar.transform, BORDER).rectTransform
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, 1);

        // ---- Left resources ----
        BuildTopResources(bar.transform);

        // ---- Center: GŁĘBOKOŚĆ ----
        var depthGO = GO("Depth_Center", bar.transform);
        SA(depthGO, 0.35f, 0, 0.5f, 1, 0, 0, 0, 0);

        var depthLbl = Txt("Depth_Lbl", depthGO.transform, "GŁĘBOKOŚĆ", 9, SEC);
        StTop(depthLbl.gameObject, 14);

        depthText = Txt("DepthVal", depthGO.transform, "87m", 18, PRI);
        SA(depthText.gameObject, 0, 0, 1, 1, 0, 0, 0, -14);

        // ---- WARSTWA / TIER ----
        var tierGO = GO("Tier_Center", bar.transform);
        SA(tierGO, 0.22f, 0, 0.35f, 1, 4, 0, -4, 0);

        var tlbl = Txt("Tier_Lbl", tierGO.transform, "WARSTWA", 9, SEC);
        StTop(tlbl.gameObject, 14);

        depthTierText = Txt("TierVal", tierGO.transform, "TIER II", 11, PRI);
        SA(depthTierText.gameObject, 0, 0.4f, 1, 0.9f, 0, 0, 0, 0);

        depthZoneText = Txt("ZoneVal", tierGO.transform, "KATAKUMBY", 9, DIM);
        SA(depthZoneText.gameObject, 0, 0, 1, 0.45f, 0, 0, 0, 0);

        // ---- PRESJA GŁĘBI ----
        var pressGO = GO("Press_Center", bar.transform);
        SA(pressGO, 0.5f, 0, 0.65f, 1, 4, 0, -4, 0);

        var pressLbl = Txt("Press_Lbl", pressGO.transform, "PRESJA GŁĘBI", 9, SEC);
        StTop(pressLbl.gameObject, 14);

        depthPressFill = Bar("PresBar", pressGO.transform, C(10,8,24), C(90,0,180),
            0, 0.35f, 1, 0.6f, 2, 0, -2, 0);

        depthPressText = Txt("PressVal", pressGO.transform, "42 PS", 10, C(90,48,144));
        SA(depthPressText.gameObject, 0, 0, 1, 0.38f, 0, 0, 0, 0);

        // ---- Mutators ----
        BuildMutators(bar.transform);
    }

    void BuildTopResources(Transform parent)
    {
        (Color icon, Color txt, string val)[] res =
        {
            (C(122, 64, 16), C(200,112,48),  "1487"),
            (C(16,  24, 96), C(80, 112,192), "320"),
            (C(26,  0,  48), C(138, 48,192), "6"),
        };
        for (int i = 0; i < res.Length; i++)
        {
            var row = GO("Res_"+i, parent);
            SA(row.gameObject, 0, 0, 0, 1, 4+i*80, 0, 80+i*80, 0);

            var ic = Img("ResIcon_"+i, row.transform, res[i].icon);
            SA(ic.gameObject, 0, 0.5f, 0, 0.5f, 2, -7, 16, 7);
            ic.sprite = CircleSprite();

            Txt("ResVal_"+i, row.transform, res[i].val, 11, res[i].txt,
                TextAnchor.MiddleLeft).rectTransform
                .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 20, 50);

            // right divider
            if (i < 2)
            {
                var div = Img("ResDivider_"+i, row.transform, BORDER2);
                SA(div.gameObject, 1, 0.1f, 1, 0.9f, -1, 0, 0, 0);
            }
        }
    }

    void BuildMutators(Transform parent)
    {
        string[] names  = { "MGŁA KRWI", "ECHO", "TOKSYNA" };
        Color[]  colors = { C(192,48,48), C(48,96,176), C(48,160,80) };
        Color[]  bords  = { C(74,16,16),  C(16,32,64),  C(16,32,16)  };

        for (int i = 0; i < names.Length; i++)
        {
            var bg = Img("Mutator_"+i, parent, C(14,8,20));
            SA(bg.gameObject, 0.78f+i*0.065f, 0.12f, 0.78f+(i+1)*0.065f, 0.88f, 2, 0, -2, 0);

            var brd = Img("MutBrd_"+i, bg.transform, bords[i]);
            Stretch(brd.gameObject);
            brd.color = new Color(bords[i].r, bords[i].g, bords[i].b, 0.6f);

            Txt("MutTxt_"+i, bg.transform, names[i], 8, colors[i]);
        }

        // Corruption % tag
        var ctag = Img("CorruptTag", parent, C(13,8,24));
        SA(ctag.gameObject, 0.97f, 0.12f, 1f, 0.88f, 2, 0, -4, 0);
        Txt("CorruptTag_T", ctag.transform, "SPACZENIE 42%", 8, C(138,48,192));
    }

    void BuildBottomPanel(Transform root)
    {
        var panel = Img("BottomPanel", root, C(7, 5, 18, 235));
        StBot(panel.gameObject, 120);

        Img("BotPanel_TopLine", panel.transform, BORDER)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 1);

        BuildPortraitSection(panel.transform);
        BuildHotbar(panel.transform);
        BuildStatsPanel(panel.transform);
    }

    void BuildPortraitSection(Transform parent)
    {
        var sec = Img("Portrait_Sec", parent, C(10, 8, 22));
        SA(sec.gameObject, 0, 0, 0, 1, 0, 0, 200, 0);
        Img("Portrait_RLine", sec.transform, BORDER).rectTransform
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 1);

        // ---- Top row (portrait box + bars) ----
        var topRow = GO("Portrait_TopRow", sec.transform);
        SA(topRow.gameObject, 0, 0, 1, 1, 0, 42, 0, 0);

        // Portrait box 80×80
        var portBox = Img("Portrait_Box", topRow.transform, C(12, 10, 26));
        SA(portBox.gameObject, 0, 0, 0, 1, 0, 0, 80, 0);
        Img("Portrait_BoxBrd", portBox.transform, BORDER2)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 1);

        Txt("Portrait_Icon", portBox.transform, "⚔", 34, PRI);

        // Accent line at bottom of portrait
        var accent = Img("Portrait_Accent", portBox.transform, C(138,96,192,80));
        SA(accent.gameObject, 0, 0, 1, 0, 0, 0, 0, 2);

        // Bars column
        var barsCol = GO("Bars_Col", topRow.transform);
        SA(barsCol.gameObject, 0, 0, 1, 1, 84, 4, -4, -4);

        hpFill = BuildLabeledBar(barsCol.transform,
            "HP",    C(74,32,32), HP_BG, HP_FILL, 1f,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -32), new Vector2(0, -16),
            out hpText, C(255,180,180,180));

        manaFill = BuildLabeledBar(barsCol.transform,
            "MANA",  C(32,32,80), MN_BG, MN_FILL, 0.5f,
            new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -3), new Vector2(0, 13),
            out manaText, C(150,170,255,180));

        staminaFill = BuildSlimBar(barsCol.transform, "Stam",
            "STAMINA", C(58,58,58), ST_BG, ST_FILL,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 10), new Vector2(0, 18));

        // ---- Bottom row (Corruption) ----
        var botRow = GO("Portrait_BotRow", sec.transform);
        SA(botRow.gameObject, 0, 0, 1, 0, 0, 0, 0, 42);

        Img("BotRow_TopLine", botRow.transform, BORDER2)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 1);

        Txt("Corrupt_Lbl", botRow.transform, "SPACZENIE", 8, C(58,32,80), TextAnchor.MiddleLeft)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 6, 72);

        corruptFill = Bar("CorruptBar", botRow.transform, CR_BG, CR_FILL,
            0, 0.25f, 1, 0.75f, 72, 0, -44, 0);

        corruptPctText = Txt("Corrupt_Pct", botRow.transform, "42%", 11, C(122,48,160));
        SA(corruptPctText.gameObject, 1, 0, 1, 1, -44, 0, 0, 0);
    }

    // Builds a labeled bar (HP, Mana style) with text overlay, returns fill image
    Image BuildLabeledBar(Transform parent,
        string labelStr, Color labelCol,
        Color bgCol, Color fillCol, float anchorY,
        Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax,
        out Text overlay, Color overlayCol)
    {
        var lbl = Txt(labelStr+"_Lbl", parent, labelStr, 9, labelCol, TextAnchor.MiddleLeft);
        var lblRT = lbl.rectTransform;
        lblRT.anchorMin = ancMin + new Vector2(0, 0.01f);
        lblRT.anchorMax = ancMax;
        lblRT.offsetMin = offMin + new Vector2(0, 14);
        lblRT.offsetMax = new Vector2(offMax.x, offMax.y + 14);

        var fill = Bar(labelStr+"_Bar", parent, bgCol, fillCol,
            ancMin.x, ancMin.y, ancMax.x, ancMax.y,
            offMin.x, offMin.y, offMax.x, offMax.y);

        overlay = Txt(labelStr+"_Txt", fill.transform.parent, "", 9, overlayCol);
        Stretch(overlay.gameObject);
        return fill;
    }

    Image BuildSlimBar(Transform parent, string id,
        string labelStr, Color labelCol,
        Color bgCol, Color fillCol,
        Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax)
    {
        Txt(id+"_Lbl", parent, labelStr, 8, labelCol, TextAnchor.MiddleLeft)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 18, 10);

        return Bar(id+"_Bar", parent, bgCol, fillCol,
            ancMin.x, ancMin.y, ancMax.x, ancMax.y,
            offMin.x, offMin.y, offMax.x, offMax.y);
    }

    void BuildHotbar(Transform parent)
    {
        var sec = Img("Hotbar_Sec", parent, C(8, 6, 22));
        SA(sec.gameObject, 0, 0, 1, 1, 200, 0, -200, 0);
        Img("Hotbar_RLine", sec.transform, BORDER).rectTransform
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 1);

        // ---- Ability row (top 78px) ----
        var abilRow = GO("Ability_Row", sec.transform);
        SA(abilRow.gameObject, 0, 0, 1, 1, 0, 42, 0, 0);

        string[] keys  = { "Z", "X", "C", "V" };
        string[] icons = { "🔥", "⚡", "🌑", "💀" };
        bool[]   ult   = { false, false, false, true };
        Color[]  brdC  = {
            C(74,56,112), C(138,96,192), C(74,56,112), C(106,64,144)
        };
        float slotW = 64f, gap = 6f, sepW = 10f;
        float totalW = 4 * slotW + 3 * gap + sepW;
        float startX = -totalW / 2f;

        for (int i = 0; i < keys.Length; i++)
        {
            float sz   = ult[i] ? 68f : slotW;
            float sepExtra = i >= 3 ? sepW : 0f;
            float xLeft = startX + i * (slotW + gap) + sepExtra;

            var slot = Img("Ability_"+keys[i], abilRow.transform, C(10,8,24));
            SA(slot.gameObject,
                0.5f, 0.5f, 0.5f, 0.5f,
                xLeft, -sz*0.5f, xLeft+sz, sz*0.5f);

            // border glow via outline
            var brd = Img("Slot_Brd", slot.transform, brdC[i]);
            Stretch(brd.gameObject);
            brd.color = new Color(brdC[i].r, brdC[i].g, brdC[i].b, 0.55f);

            var ic = Txt("Ability_Ic", slot.transform, icons[i], 26, Color.white);
            SA(ic.gameObject, 0, 0.35f, 1, 1, 0, 0, 0, 0);

            var kTxt = Txt("Ability_Key", slot.transform, keys[i], 9, C(74,58,96));
            kTxt.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right,  3, 12);
            kTxt.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 2, 12);

            // separator before ult
            if (i == 2)
            {
                var sep = Img("Ability_Sep", abilRow.transform, C(42,32,80,100));
                float sepX = xLeft + slotW + gap * 0.5f;
                SA(sep.gameObject, 0.5f, 0.5f, 0.5f, 0.5f, sepX, -26, sepX+3, 26);
            }
        }

        // ---- Items row (bottom 42px) ----
        var itemRow = GO("Item_Row", sec.transform);
        SA(itemRow.gameObject, 0, 0, 1, 0, 0, 0, 0, 42);

        Img("Items_TopLine", itemRow.transform, BORDER2)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 1);

        string[] itIcons = { "🧪", "🗡", "📜", "💎", "", "" };
        string[] itKeys  = { "1",  "2",  "3",  "4", "5", "6" };
        float itW = 46f, itH = 30f, itGap = 4f;
        float itTotalW = 6 * itW + 5 * itGap;
        float itStartX = -itTotalW / 2f;

        for (int i = 0; i < 6; i++)
        {
            float ix = itStartX + i * (itW + itGap);
            var slot = Img("Item_"+i, itemRow.transform, C(8,6,20));
            SA(slot.gameObject, 0.5f, 0.5f, 0.5f, 0.5f, ix, -itH*0.5f, ix+itW, itH*0.5f);

            var brd = Img("ItemBrd_"+i, slot.transform, BORDER2);
            Stretch(brd.gameObject);
            brd.color = new Color(BORDER2.r, BORDER2.g, BORDER2.b, 0.5f);

            if (!string.IsNullOrEmpty(itIcons[i]))
            {
                var ic = Txt("Item_Ic_"+i, slot.transform, itIcons[i], 18, Color.white);
                SA(ic.gameObject, 0, 0.25f, 1, 1, 0, 0, 0, 0);
            }

            var kTxt = Txt("Item_Key_"+i, slot.transform, itKeys[i], 8, C(58,48,80));
            kTxt.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right,  2, 10);
            kTxt.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 1, 10);
        }
    }

    void BuildStatsPanel(Transform parent)
    {
        var sec = Img("Stats_Sec", parent, C(10, 8, 22));
        SA(sec.gameObject, 1, 0, 1, 1, -200, 0, 0, 0);

        // ---- Top row ----
        var topRow = GO("Stats_TopRow", sec.transform);
        SA(topRow.gameObject, 0, 0, 1, 1, 0, 42, 0, 0);

        // Leyer box 78×78 (left)
        var leyBox = Img("Leyer_Box", topRow.transform, C(12,10,26));
        SA(leyBox.gameObject, 0, 0, 0, 1, 0, 0, 78, 0);
        Img("LeyBox_RLine", leyBox.transform, BORDER2).rectTransform
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 1);

        Txt("Leyer_Lbl", leyBox.transform, "LEYER", 9, C(58,42,80))
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 2, 14);

        leyerNumText = Txt("Leyer_Num", leyBox.transform, "18", 24, PRI);
        SA(leyerNumText.gameObject, 0, 0.35f, 1, 0.82f, 0, 0, 0, 0);

        pdFill = Bar("PD_Bar", leyBox.transform, C(9,8,16), PD_FILL,
            0.1f, 0.04f, 0.9f, 0.18f, 0, 0, 0, 0);

        pdText = Txt("PD_Txt", leyBox.transform, "127/220 PD", 7, C(58,32,80));
        SA(pdText.gameObject, 0, 0, 1, 0.18f, 0, 1, 0, 0);

        // Stats column (right of leyer box)
        var statsCol = GO("Stats_Col", topRow.transform);
        SA(statsCol.gameObject, 0, 0, 1, 1, 82, 4, -4, -4);

        (string lbl, Color col, System.Action<Text> setter)[] stats2 =
        {
            ("OBRAŻENIA", C(200,64,64),  t => damageText  = t),
            ("SPEED",     C(96,160,200), t => speedText   = t),
            ("OBRONA",    C(80,160,120), t => defenceText = t),
        };
        for (int i = 0; i < stats2.Length; i++)
        {
            float yMin = 1f - (i + 1) / 3f;
            float yMax = 1f - i / 3f;
            var row = GO("StatRow_"+i, statsCol.transform);
            SA(row.gameObject, 0, yMin, 1, yMax, 0, 1, 0, -1);

            Txt("SLbl_"+i, row.transform, stats2[i].lbl, 8, SEC, TextAnchor.MiddleLeft)
                .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 2, 80);

            var valT = Txt("SVal_"+i, row.transform, "—", 13, stats2[i].col,
                TextAnchor.MiddleRight);
            SA(valT.gameObject, 0.5f, 0, 1, 1, 0, 0, -2, 0);
            stats2[i].setter(valT);
        }

        // ---- Bottom row (buttons) ----
        var botRow = GO("Stats_BotRow", sec.transform);
        SA(botRow.gameObject, 0, 0, 1, 0, 0, 0, 0, 42);

        Img("StatsBot_TopLine", botRow.transform, BORDER2)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 1);

        string[] btns = { "EKWIP.", "SKILLE", "BESTIA." };
        for (int i = 0; i < btns.Length; i++)
        {
            var btn = Img("Btn_"+i, botRow.transform, C(10,8,24), true);
            SA(btn.gameObject, i/3f, 0.12f, (i+1)/3f, 0.88f, 3, 0, -3, 0);
            Txt("BtnTxt_"+i, btn.transform, btns[i], 7, C(74,58,96));
        }
    }

    void BuildMinimap(Transform root)
    {
        // 162×162 above portrait
        var panel = Img("Minimap_Panel", root, C(7,6,15));
        BL(panel.gameObject, 0, 120, 162, 162);

        // Right + top borders
        Img("MM_RLine", panel.transform, BORDER).rectTransform
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 1);
        Img("MM_TLine", panel.transform, BORDER).rectTransform
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 1);

        // Circular mask
        var maskImg = Img("MM_MaskImg", panel.transform, Color.white);
        SA(maskImg.gameObject, 0, 0, 1, 1, 6, 6, -6, -20);
        maskImg.sprite = CircleSprite();
        var mask = maskImg.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // BG inside mask
        var bg = Img("MM_BG", maskImg.transform, C(11,10,24));
        Stretch(bg.gameObject);

        // Grid lines
        var hl = Img("MM_HLine", maskImg.transform, C(42,32,64,40));
        SA(hl.gameObject, 0, 0.5f, 1, 0.5f, 0, -1, 0, 1);
        var vl = Img("MM_VLine", maskImg.transform, C(42,32,64,40));
        SA(vl.gameObject, 0.5f, 0, 0.5f, 1, -1, 0, 1, 0);

        // Player dot
        var dot = Img("MM_Player", maskImg.transform, PRI);
        SA(dot.gameObject, 0.5f, 0.5f, 0.5f, 0.5f, -6, -6, 6, 6);
        dot.sprite = CircleSprite();

        // Enemy dot
        var enemy = Img("MM_Enemy", maskImg.transform, C(170,0,0));
        SA(enemy.gameObject, 0.72f, 0.78f, 0.72f, 0.78f, -5,-5,5,5);
        enemy.sprite = CircleSprite();

        // Compass labels
        (string d, float ax, float ay, float ox, float oy)[] compass =
        {
            ("N", 0.5f, 1f, -5, -14), ("S", 0.5f, 0f, -5, 2),
            ("W", 0f, 0.5f, 2, -5),   ("E", 1f, 0.5f, -14, -5),
        };
        foreach (var (d, ax, ay, ox, oy) in compass)
        {
            var ct = Txt("MM_"+d, panel.transform, d, 10, C(58,42,80));
            SA(ct.gameObject, ax, ay, ax, ay, ox, oy, ox+12, oy+12);
        }
    }

    void BuildComboFloat(Transform root)
    {
        var panel = Img("Combo_Float", root, C(8,6,20,230));
        BC(panel.gameObject, 126, 190, 36);

        var brd = Img("Combo_Brd", panel.transform, C(58,42,80,100));
        Stretch(brd.gameObject);

        // "COMBO" label
        var lbl = Txt("Combo_Lbl", panel.transform, "COMBO", 8, C(90,58,112));
        SA(lbl.gameObject, 0, 0, 0.2f, 1, 2, 0, 0, 0);

        // Dots (3 for 3-hit combo)
        comboDots = new Image[3];
        float dotR = 7f, dotGap = 4f;
        float dotsW = 3 * dotR * 2 + 2 * dotGap;
        float dotStartX = -dotsW * 0.5f;

        var dotsHost = GO("Combo_Dots", panel.transform);
        SA(dotsHost.gameObject, 0.2f, 0, 0.6f, 1, 0, 0, 0, 0);

        for (int i = 0; i < 3; i++)
        {
            float dx = dotStartX + i * (dotR * 2 + dotGap);
            var d = Img("Dot_"+i, dotsHost.transform, C(15,8,30));
            SA(d.gameObject, 0.5f, 0.5f, 0.5f, 0.5f, dx, -dotR, dx+dotR*2, dotR);
            d.sprite = CircleSprite();
            comboDots[i] = d;
        }

        // Count text
        comboCountText = Txt("Combo_Count", panel.transform, "0<size=10>/3</size>", 18,
            C(200,48,48), TextAnchor.MiddleLeft);
        SA(comboCountText.gameObject, 0.6f, 0, 0.82f, 1, 2, 0, 0, 0);

        // Timer
        comboTimerText = Txt("Combo_Timer", panel.transform, "", 9, C(58,32,64));
        SA(comboTimerText.gameObject, 0.82f, 0, 1f, 1, 0, 0, -2, 0);
    }

    // =========================================================
    // Circle sprite (runtime generated)
    // =========================================================

    static Sprite _circle;
    static Sprite CircleSprite()
    {
        if (_circle != null) return _circle;
        const int S = 64;
        float half = S * 0.5f;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float dist = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half));
            float a    = Mathf.Clamp01(half - dist);
            tex.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        tex.Apply();
        _circle = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
        return _circle;
    }
}
