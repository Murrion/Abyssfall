using UnityEngine;
using UnityEngine.UI;

/// Attach to any persistent GameObject ("HUDManager").
/// Right-click component header → "Rebuild Canvas" after any size change.
[ExecuteAlways]
public class AbyssfallHUD : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerStats stats;

    // ---- Live UI refs ----
    private Image hpFill, manaFill, staminaFill, corruptFill, pdFill, depthPressFill;
    private Text  hpText, manaText, corruptPctText;
    private Text  leyerNumText, pdText, damageText, speedText, defenceText;
    private Text  depthText, depthTierText, depthZoneText, depthPressText;
    private Image[] comboDots;
    private Text  comboCountText, comboTimerText;
    private Image vigL, vigR, vigT, vigB;

    private Canvas hudCanvas;

    // ── Layout constants (tweak here, then Rebuild Canvas) ──────────
    const float BOT_H    = 160f;   // bottom panel height
    const float TOP_H    = 50f;    // top bar height
    const float PORT_W   = 250f;   // portrait + bars section width
    const float STATS_W  = 250f;   // right stats section width
    const float MM_SIZE  = 200f;   // minimap square size
    const float WEP_H    = 200f;   // weapon panel height (above stats)

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
#if UNITY_EDITOR
        if (!Application.isPlaying) return;
#endif
        if (stats == null || hudCanvas == null) return;
        Tick();
    }

    // =========================================================
    // Tick
    // =========================================================

    private void Tick()
    {
        Fill(hpFill,         stats.currentHp,      stats.maxHp);
        Fill(manaFill,       stats.currentMana,     stats.maxMana);
        Fill(staminaFill,    stats.CurrentStamina,  stats.MaxStamina);
        Fill(corruptFill,    stats.corruption,      100f);
        Fill(pdFill,         stats.pd,              stats.pdToNextLeyer);
        Fill(depthPressFill, stats.depthPressure,   100f);

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

        float vigA = Mathf.Lerp(0.04f, 0.60f, stats.corruption / 100f);
        SetAlpha(vigL, vigA); SetAlpha(vigR, vigA);
        SetAlpha(vigT, vigA * 0.6f); SetAlpha(vigB, vigA * 0.6f);
    }

    void TickCombo()
    {
        if (comboDots == null) return;
        int step = stats.ComboStep;
        int max  = stats.ComboMaxSteps;
        for (int i = 0; i < comboDots.Length; i++)
            if (comboDots[i] != null)
                comboDots[i].color = i < step ? Hex("D03030") : Hex("12081E");

        SetText(comboCountText, $"{step}<size=14>/{max}</size>");
        float left = Mathf.Max(0f, stats.ComboWindowEnd - Time.time);
        SetText(comboTimerText, step > 0 ? $"{left:0.0}s" : "");
    }

    // =========================================================
    // Helpers
    // =========================================================

    static void Fill(Image img, float cur, float max)
    { if (img != null && max > 0f) img.fillAmount = Mathf.Clamp01(cur / max); }
    static void SetText(Text t, string s) { if (t != null) t.text = s; }
    static void SetAlpha(Image img, float a)
    { if (img == null) return; var c = img.color; c.a = a; img.color = c; }
    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#"+h, out Color c); return c; }
    static Color C(int r, int g, int b, int a = 255)
        => new Color(r/255f, g/255f, b/255f, a/255f);

    // =========================================================
    // BUILDER
    // =========================================================

    [ContextMenu("Rebuild Canvas")]
    void BuildCanvas()
    {
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
        sc.referenceResolution = new Vector2(1280, 720);  // ← smaller ref = bigger UI
        sc.matchWidthOrHeight  = 0.5f;

        cgo.AddComponent<GraphicRaycaster>();
        Transform root = cgo.transform;

        BuildVignette(root);
        BuildTopBar(root);
        BuildBottomPanel(root);
        BuildMinimap(root);
        BuildWeaponPanel(root);
        BuildComboFloat(root);
        BuildBuffBar(root);
    }

    // ── Palette ─────────────────────────────────────────────────────
    static readonly Color BG      = C(6,  5,  16);
    static readonly Color BG2     = C(10, 8,  24);
    static readonly Color BORDER  = C(42, 34, 69);
    static readonly Color BORDER2 = C(30, 24, 48);
    static readonly Color HP_F    = C(190, 36, 36);
    static readonly Color HP_BG   = C(44,  8,  8);
    static readonly Color MN_F    = C(30,  36, 148);
    static readonly Color MN_BG   = C(8,   8,  52);
    static readonly Color ST_F    = C(100, 100,100);
    static readonly Color ST_BG   = C(9,   8,  16);
    static readonly Color CR_F    = C(148,  0, 220);
    static readonly Color CR_BG   = C(14,   0,  28);
    static readonly Color PD_F    = C(100, 52, 170);
    static readonly Color PRI     = C(148, 104, 204);
    static readonly Color SEC     = C(64,  52,  88);
    static readonly Color DIM     = C(36,  28,  52);

    // ── Primitive factories ──────────────────────────────────────────

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
        t.alignment = anchor; t.raycastTarget = false; t.supportRichText = true;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
              ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        return t;
    }

    // Set anchors + offsets
    static RectTransform SA(GameObject go,
        float x0, float y0, float x1, float y1,
        float l,  float b,  float r,  float t)
    {
        var rt    = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = new Vector2(l,  b);  rt.offsetMax = new Vector2(r,  t);
        return rt;
    }

    // Full-stretch with insets
    static void Stretch(GameObject go, float l=0, float b=0, float r=0, float t=0)
        => SA(go, 0,0,1,1, l, b, -r, -t);

    // Fixed-height strip at top
    static void StTop(GameObject go, float h, float l=0, float r=0)
        => SA(go, 0,1,1,1, l,-h,-r,0);

    // Bottom-left corner
    static void BL(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    // Bottom-right corner
    static void BR(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.right;
        rt.pivot = Vector2.right;
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta        = new Vector2(w, h);
    }

    // Bottom-center
    static void BC(GameObject go, float yBot, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f,0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, yBot);
        rt.sizeDelta        = new Vector2(w, h);
    }

    // Horizontal line at edge
    static void LineTop(GameObject go)
        => SA(go, 0,1,1,1, 0,-1,0,0);
    static void LineRight(GameObject go)
        => SA(go, 1,0,1,1, -1,0,0,0);
    static void LineLeft(GameObject go)
        => SA(go, 0,0,0,1, 0,0,1,0);

    // Bar: returns fill image
    Image Bar(string id, Transform parent, Color bg, Color fill,
              float x0, float y0, float x1, float y1,
              float l,  float b,  float r,  float t)
    {
        var bgI = Img(id+"_BG", parent, bg);
        SA(bgI.gameObject, x0,y0,x1,y1, l,b,r,t);

        // subtle border
        var brd = Img(id+"_Brd", bgI.transform, new Color(BORDER2.r,BORDER2.g,BORDER2.b,0.4f));
        Stretch(brd.gameObject);

        var fi = Img(id+"_Fill", bgI.transform, fill);
        Stretch(fi.gameObject);
        fi.type       = Image.Type.Filled;
        fi.fillMethod = Image.FillMethod.Horizontal;
        fi.fillOrigin = 0;
        fi.fillAmount = 1f;
        return fi;
    }

    // =========================================================
    // SECTIONS
    // =========================================================

    void BuildVignette(Transform root)
    {
        vigL = Img("Vig_L", root, C(60,0,120, 70));
        SA(vigL.gameObject, 0,0,0,1, 0,0,18,0);

        vigR = Img("Vig_R", root, C(60,0,120, 70));
        SA(vigR.gameObject, 1,0,1,1, -18,0,0,0);

        vigT = Img("Vig_T", root, C(60,0,120, 50));
        SA(vigT.gameObject, 0,1,1,1, 0,-12,0,0);

        vigB = Img("Vig_B", root, C(60,0,120, 50));
        SA(vigB.gameObject, 0,0,1,0, 0,0,0,12);
    }

    // ── TOP BAR ─────────────────────────────────────────────────────
    void BuildTopBar(Transform root)
    {
        var bar = Img("TopBar", root, C(7,5,17,248));
        StTop(bar.gameObject, TOP_H);
        Img("TopBar_Bot", bar.transform, BORDER)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 0, 1);

        // Left: 3 resource tokens
        BuildTopResources(bar.transform);

        // Center-left: WARSTWA
        var tierGO = GO("Tier", bar.transform);
        SA(tierGO, 0.22f,0, 0.36f,1, 0,0,0,0);
        var tL = Txt("TierLbl", tierGO.transform, "WARSTWA", 10, SEC); StTop(tL.gameObject, 16);
        depthTierText = Txt("TierVal", tierGO.transform, "TIER II", 13, PRI);
        SA(depthTierText.gameObject, 0,0.38f, 1,0.85f, 2,0,-2,0);
        depthZoneText = Txt("ZoneVal", tierGO.transform, "KATAKUMBY", 10, DIM);
        SA(depthZoneText.gameObject, 0,0, 1,0.42f, 2,2,-2,0);

        // Center: GŁĘBOKOŚĆ
        var dGO = GO("Depth", bar.transform);
        SA(dGO, 0.36f,0, 0.52f,1, 0,0,0,0);
        var dL = Txt("DepLbl", dGO.transform, "GŁĘBOKOŚĆ", 10, SEC); StTop(dL.gameObject, 16);
        depthText = Txt("DepVal", dGO.transform, "87m", 22, PRI);
        SA(depthText.gameObject, 0,0, 1,1, 0,0,0,-16);

        // Center-right: PRESJA GŁĘBI
        var pGO = GO("Press", bar.transform);
        SA(pGO, 0.52f,0, 0.66f,1, 2,0,-2,0);
        var pL = Txt("PressLbl", pGO.transform, "PRESJA GŁĘBI", 10, SEC); StTop(pL.gameObject, 16);
        depthPressFill = Bar("PBar", pGO.transform, C(10,8,24), C(96,0,192),
            0, 0.3f, 1, 0.58f, 2,0,-2,0);
        depthPressText = Txt("PressVal", pGO.transform, "42 PS", 11, C(96,52,152));
        SA(depthPressText.gameObject, 0,0, 1,0.36f, 0,2,0,0);

        // Right: mutators
        BuildMutators(bar.transform);
    }

    void BuildTopResources(Transform parent)
    {
        (Color ic, Color tx, string val)[] res =
        {
            (C(130,68,18), C(210,120,52), "1487"),
            (C(18, 26,100), C(88,120,200), "320"),
            (C(28,  0, 52), C(148, 52,204), "6"),
        };
        for (int i = 0; i < 3; i++)
        {
            var row = GO("Res"+i, parent);
            SA(row.gameObject, 0,0, 0,1, 6+i*84, 0, 84+i*84, 0);

            var ic = Img("RIC"+i, row.transform, res[i].ic);
            SA(ic.gameObject, 0,0.5f, 0,0.5f, 3,-9,21,9);
            ic.sprite = CircleSprite();

            var vt = Txt("RVL"+i, row.transform, res[i].val, 13, res[i].tx, TextAnchor.MiddleLeft);
            SA(vt.gameObject, 0,0, 1,1, 24,0,0,0);

            if (i < 2)
            {
                var div = Img("RDv"+i, row.transform, BORDER2);
                SA(div.gameObject, 1,0.1f, 1,0.9f, -1,0,0,0);
            }
        }
    }

    void BuildMutators(Transform parent)
    {
        string[] nm  = { "MGŁA KRWI", "ECHO", "TOKSYNA" };
        Color[]  col = { C(210,52,52), C(52,108,196), C(52,172,88) };
        Color[]  brd = { C(80,18,18),  C(18,36,72),   C(18,36,20)  };

        for (int i = 0; i < nm.Length; i++)
        {
            var bg = Img("Mut"+i, parent, C(14,8,22));
            // evenly spaced in right 22% of top bar
            float x0 = 0.78f + i * 0.065f;
            SA(bg.gameObject, x0,0.1f, x0+0.062f,0.9f, 2,0,-2,0);
            Img("MutBrd"+i, bg.transform, new Color(brd[i].r,brd[i].g,brd[i].b, 0.55f));
            Stretch(Img("MutBrdX"+i, bg.transform, Color.clear).gameObject);
            Txt("MTxt"+i, bg.transform, nm[i], 9, col[i]);
        }

        // Spaczenie tag
        var ct = Img("CorTag", parent, C(13,8,24));
        SA(ct.gameObject, 0.972f,0.1f, 1f,0.9f, 2,0,-4,0);
        Txt("CorTagT", ct.transform, "SPACZENIE 42%", 9, C(148,52,204));
    }

    // ── BOTTOM PANEL ────────────────────────────────────────────────
    void BuildBottomPanel(Transform root)
    {
        var panel = Img("BotPanel", root, C(7,5,18,238));
        SA(panel.gameObject, 0,0,1,0, 0,0,0,BOT_H);

        Img("BotPanel_Top", panel.transform, BORDER)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 1);

        BuildPortraitSection(panel.transform);
        BuildHotbar(panel.transform);
        BuildStatsSection(panel.transform);
    }

    void BuildPortraitSection(Transform parent)
    {
        var sec = Img("PortSec", parent, C(9,7,20));
        SA(sec.gameObject, 0,0,0,1, 0,0,PORT_W,0);
        Img("PortSec_R", sec.transform, BORDER).rectTransform
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 1);

        // Top row: portrait box + bars  (top 105px of BOT_H)
        float botRowH = 44f;
        float topRowH = BOT_H - botRowH;

        var topRow = GO("Port_TopRow", sec.transform);
        SA(topRow.gameObject, 0,0,1,1, 0,botRowH,0,0);

        // Portrait box 100×topRowH
        float boxW = 100f;
        var portBox = Img("PortBox", topRow.transform, C(12,10,26));
        SA(portBox.gameObject, 0,0,0,1, 0,0,boxW,0);
        Img("PortBox_R", portBox.transform, BORDER2)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 1);

        Txt("PortIcon", portBox.transform, "⚔", 38, PRI);

        // Accent line
        var acc = Img("PortAcc", portBox.transform, C(148,104,204,70));
        SA(acc.gameObject, 0,0,1,0, 0,0,0,3);

        // Bars column
        var bars = GO("BarsCol", topRow.transform);
        SA(bars.gameObject, 0,0,1,1, boxW+6,6,-6,-6);

        // HP
        var hpLbl = Txt("HP_Lbl", bars.transform, "HP", 11, C(80,34,34), TextAnchor.MiddleLeft);
        SA(hpLbl.gameObject, 0,1,1,1, 2,-18,0,0);
        hpFill = Bar("HP", bars.transform, HP_BG, HP_F, 0,1,1,1, 2,-38,0,-20);
        hpText = Txt("HP_T", hpFill.transform.parent, "", 10, C(255,190,190,190));
        Stretch(hpText.gameObject);

        // Mana
        var mnLbl = Txt("MN_Lbl", bars.transform, "MANA", 11, C(32,34,84), TextAnchor.MiddleLeft);
        SA(mnLbl.gameObject, 0,0.5f,1,0.5f, 2,2,0,18);
        manaFill = Bar("MN", bars.transform, MN_BG, MN_F, 0,0.5f,1,0.5f, 2,-8,0,8);
        manaText = Txt("MN_T", manaFill.transform.parent, "", 10, C(160,180,255,190));
        Stretch(manaText.gameObject);

        // Stamina (slim)
        var stLbl = Txt("ST_Lbl", bars.transform, "STAMINA", 9, C(64,64,64), TextAnchor.MiddleLeft);
        SA(stLbl.gameObject, 0,0,1,0, 2,22,0,34);
        staminaFill = Bar("ST", bars.transform, ST_BG, ST_F, 0,0,1,0, 2,10,0,18);

        // Bottom row: Corruption
        var botRow = GO("Port_BotRow", sec.transform);
        SA(botRow.gameObject, 0,0,1,0, 0,0,0,botRowH);
        Img("BotRow_T", botRow.transform, BORDER2)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 1);

        Txt("CrLbl", botRow.transform, "SPACZENIE", 10, C(62,34,86), TextAnchor.MiddleLeft)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 6, 84);

        corruptFill = Bar("CR", botRow.transform, CR_BG, CR_F, 0,0.28f,1,0.72f, 88,0,-52,0);

        corruptPctText = Txt("CrPct", botRow.transform, "42%", 13, C(140,52,180));
        SA(corruptPctText.gameObject, 1,0,1,1, -50,0,0,0);
    }

    void BuildHotbar(Transform parent)
    {
        var sec = Img("HotbarSec", parent, C(8,6,20));
        SA(sec.gameObject, 0,0,1,1, PORT_W,0,-STATS_W,0);
        Img("Hb_R", sec.transform, BORDER).rectTransform
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 1);

        float itemRowH = 46f;
        float abilRowH = BOT_H - itemRowH;

        // ── Ability row ──
        var abilRow = GO("AbilRow", sec.transform);
        SA(abilRow.gameObject, 0,0,1,1, 0,itemRowH,0,0);

        string[] keys  = { "Z", "X", "C", "V" };
        string[] icons = { "🔥", "⚡", "🌑", "💀" };
        Color[]  brdC  = { C(74,56,112), C(148,104,204), C(74,56,112), C(114,68,160) };
        bool[]   ult   = { false, false, false, true };

        float sw = 74f, sg = 8f, sepW = 12f;
        float total = 4*sw + 3*sg + sepW;
        float sx = -total*0.5f;

        for (int i = 0; i < 4; i++)
        {
            float sepOff = i >= 3 ? sepW : 0f;
            float xl = sx + i*(sw+sg) + sepOff;
            float sz = ult[i] ? sw+6 : sw;

            var slot = Img("Abl_"+keys[i], abilRow.transform, C(10,8,26));
            SA(slot.gameObject, 0.5f,0.5f,0.5f,0.5f, xl,-sz*0.5f, xl+sz,sz*0.5f);

            // border frame
            var brd = Img("AblBrd_"+i, slot.transform, new Color(brdC[i].r,brdC[i].g,brdC[i].b,0.6f));
            Stretch(brd.gameObject);

            Txt("AblIc_"+i,  slot.transform, icons[i], 28, Color.white)
                .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 4, sz-18);

            var kTxt = Txt("AblKey_"+i, slot.transform, keys[i], 10, C(80,62,104));
            kTxt.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right,  4, 14);
            kTxt.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 3, 14);

            // separator before ult
            if (i == 2)
            {
                float sepX = xl + sw + sg*0.4f;
                var sep = Img("AblSep", abilRow.transform, C(48,36,88,120));
                SA(sep.gameObject, 0.5f,0.5f,0.5f,0.5f, sepX,-28, sepX+3,28);
            }
        }

        // ── Item row ──
        var itemRow = GO("ItemRow", sec.transform);
        SA(itemRow.gameObject, 0,0,1,0, 0,0,0,itemRowH);
        Img("IR_T", itemRow.transform, BORDER2)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 1);

        string[] itIc  = { "🧪", "🗡", "📜", "💎", "", "" };
        string[] itKey = { "1",  "2",  "3",  "4", "5","6" };
        float iw = 50f, ih = 34f, ig = 5f;
        float itTot = 6*iw + 5*ig;
        float ix0 = -itTot*0.5f;

        for (int i = 0; i < 6; i++)
        {
            float ix = ix0 + i*(iw+ig);
            var s = Img("Itm_"+i, itemRow.transform, C(8,6,22));
            SA(s.gameObject, 0.5f,0.5f,0.5f,0.5f, ix,-ih*0.5f, ix+iw,ih*0.5f);
            Img("ItmBrd_"+i, s.transform, new Color(BORDER2.r,BORDER2.g,BORDER2.b,0.5f));
            Stretch(Img("IB"+i, s.transform, Color.clear).gameObject);

            if (!string.IsNullOrEmpty(itIc[i]))
                Txt("ItmIc_"+i, s.transform, itIc[i], 20, Color.white)
                    .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 2, ih-12);

            Txt("ItmK_"+i, s.transform, itKey[i], 9, C(64,52,88))
                .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right,  2, 12);
        }
    }

    void BuildStatsSection(Transform parent)
    {
        var sec = Img("StatsSec", parent, C(9,7,20));
        SA(sec.gameObject, 1,0,1,1, -STATS_W,0,0,0);

        // ── Top row: Leyer box + stat columns ──
        float botRowH = 44f;
        var topRow = GO("Stats_Top", sec.transform);
        SA(topRow.gameObject, 0,0,1,1, 0,botRowH,0,0);

        float boxW = 96f;
        var leyBox = Img("LeyBox", topRow.transform, C(12,10,26));
        SA(leyBox.gameObject, 0,0,0,1, 0,0,boxW,0);
        Img("LeyBox_R", leyBox.transform, BORDER2)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 1);

        Txt("LeyLbl", leyBox.transform, "LEYER", 10, C(64,46,88))
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 4, 16);

        leyerNumText = Txt("LeyNum", leyBox.transform, "18", 28, PRI);
        SA(leyerNumText.gameObject, 0,0.32f,1,0.82f, 0,0,0,0);

        pdFill = Bar("PD", leyBox.transform, C(9,8,16), PD_F,
            0.08f,0.04f, 0.92f,0.18f, 0,0,0,0);

        pdText = Txt("PDTxt", leyBox.transform, "127/220 PD", 8, C(62,34,84));
        SA(pdText.gameObject, 0,0,1,0.2f, 0,1,0,0);

        // Stats column
        var statsCol = GO("StatsCol", topRow.transform);
        SA(statsCol.gameObject, 0,0,1,1, boxW+6,6,-6,-6);

        (string lbl, Color col, System.Action<Text> set)[] sd =
        {
            ("OBRAŻENIA", C(210,68,68),  t => damageText  = t),
            ("SPEED",     C(100,168,210),t => speedText   = t),
            ("OBRONA",    C(84,170,128), t => defenceText = t),
        };
        for (int i = 0; i < 3; i++)
        {
            float ym = 1f-(i+1)/3f, yx = 1f-i/3f;
            var row = GO("SR"+i, statsCol.transform);
            SA(row.gameObject, 0,ym,1,yx, 0,1,0,-1);

            Txt("SL"+i, row.transform, sd[i].lbl, 10, SEC, TextAnchor.MiddleLeft)
                .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 2, 90);

            var vt = Txt("SV"+i, row.transform, "—", 15, sd[i].col, TextAnchor.MiddleRight);
            SA(vt.gameObject, 0.45f,0,1,1, 0,0,-3,0);
            sd[i].set(vt);
        }

        // Bottom row
        var botRow = GO("StatsBot", sec.transform);
        SA(botRow.gameObject, 0,0,1,0, 0,0,0,botRowH);
        Img("SBot_T", botRow.transform, BORDER2)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 1);

        string[] btns = { "EKWIP.", "SKILLE", "BESTIA." };
        for (int i = 0; i < 3; i++)
        {
            var b = Img("Btn"+i, botRow.transform, C(10,8,26), true);
            SA(b.gameObject, i/3f,0.1f,(i+1)/3f,0.9f, 3,0,-3,0);
            Txt("BtnT"+i, b.transform, btns[i], 9, C(80,62,104));
        }
    }

    // ── MINIMAP ──────────────────────────────────────────────────────
    void BuildMinimap(Transform root)
    {
        var panel = Img("Minimap", root, C(7,6,15));
        BL(panel.gameObject, 0, BOT_H, MM_SIZE, MM_SIZE);

        Img("MM_R", panel.transform, BORDER).rectTransform
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 0, 1);
        Img("MM_T", panel.transform, BORDER).rectTransform
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, 1);

        // Circular mask
        var maskImg = Img("MM_Mask", panel.transform, Color.white);
        SA(maskImg.gameObject, 0,0,1,1, 7,7,-7,-18);
        maskImg.sprite = CircleSprite();
        maskImg.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        Img("MM_BG", maskImg.transform, C(10,9,22)); Stretch(Img("MM_BG2",maskImg.transform,Color.clear).gameObject);

        // Grid
        var hl = Img("MM_H", maskImg.transform, C(44,34,68,40));
        SA(hl.gameObject, 0,0.5f,1,0.5f, 0,-1,0,1);
        var vl = Img("MM_V", maskImg.transform, C(44,34,68,40));
        SA(vl.gameObject, 0.5f,0,0.5f,1, -1,0,1,0);

        // Player dot
        var pd = Img("MM_Pl", maskImg.transform, PRI);
        SA(pd.gameObject, 0.5f,0.5f,0.5f,0.5f, -6,-6,6,6);
        pd.sprite = CircleSprite();

        // Enemy dot
        var en = Img("MM_En", maskImg.transform, C(180,0,0));
        SA(en.gameObject, 0.72f,0.78f,0.72f,0.78f, -5,-5,5,5);
        en.sprite = CircleSprite();

        // Compass
        (string d, float ax, float ay, float ox, float oy)[] comp =
        {
            ("N",0.5f,1f,-5,-15),("S",0.5f,0f,-5,3),
            ("W",0f,0.5f,3,-5), ("E",1f,0.5f,-14,-5),
        };
        foreach (var (d,ax,ay,ox,oy) in comp)
        {
            var ct = Txt("MM_"+d, panel.transform, d, 11, C(64,48,90));
            SA(ct.gameObject, ax,ay,ax,ay, ox,oy,ox+13,oy+13);
        }
    }

    // ── WEAPON / STATUS PANEL ────────────────────────────────────────
    void BuildWeaponPanel(Transform root)
    {
        // Sits above the right stats section
        var panel = Img("WepPanel", root, C(7,6,15));
        BR(panel.gameObject, 0, BOT_H, STATS_W, WEP_H);

        Img("Wep_T",  panel.transform, BORDER).rectTransform
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top,  0, 1);
        Img("Wep_L",  panel.transform, BORDER).rectTransform
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 1);

        // Header
        var hdr = Txt("WepHdr", panel.transform, "BROŃ MODULARNA", 10, SEC, TextAnchor.MiddleLeft);
        SA(hdr.gameObject, 0,1,1,1, 8,-20,0,0);
        Img("WepHdrLine", panel.transform, BORDER2)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 20, 1);

        // Weapon icon + name
        var wic = Img("WepIcon", panel.transform, C(12,10,26));
        SA(wic.gameObject, 0,1,0,1, 8,-66,52,-24);
        Txt("WepIcT", wic.transform, "🗡", 24, Color.white);

        Txt("WepName", panel.transform, "Klingi Otchłani", 13, C(148,100,186), TextAnchor.MiddleLeft)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 26, 18);

        var wtype = Txt("WepType", panel.transform, "MELEE · BLEED · ECHO", 9, DIM, TextAnchor.MiddleLeft);
        SA(wtype.gameObject, 0,1,1,1, 58,-60,0,-44);

        // Stat boxes: DMG | ENCHANTY
        var dmgBox = Img("DmgBox", panel.transform, C(10,8,22));
        SA(dmgBox.gameObject, 0,1,0.5f,1, 8,-118,0,-70);
        Txt("DmgLbl", dmgBox.transform, "DMG",  9, SEC);
        SA(Txt("DmgLbl2", dmgBox.transform, "DMG", 9, SEC).gameObject, 0,1,1,1, 0,-14,0,0);
        Txt("DmgVal", dmgBox.transform, "187", 18, C(210,68,68));

        var encBox = Img("EncBox", panel.transform, C(10,8,22));
        SA(encBox.gameObject, 0.5f,1,1,1, 4,-118,-8,-70);
        Txt("EncLbl", encBox.transform, "ENCHANTY", 9, SEC);
        SA(Txt("EncLbl2", encBox.transform, "ENCHANTY", 9, SEC).gameObject, 0,1,1,1, 0,-14,0,0);
        Txt("EncVal", encBox.transform, "3/∞", 18, C(132,52,186));

        // Mission + boss alert
        var mLbl = Txt("MisLbl", panel.transform, "MISJA", 9, SEC, TextAnchor.MiddleLeft);
        SA(mLbl.gameObject, 0,1,1,1, 8,-138,0,-124);

        Txt("MisName", panel.transform, "Zew Otchłani", 12, C(128,84,168), TextAnchor.MiddleLeft)
            .rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Bottom, 40, 18);

        var boss = Img("BossAlert", panel.transform, C(10,0,18));
        SA(boss.gameObject, 0,0,1,0, 8,10,-8,38);
        Img("BossAlertBrd", boss.transform, new Color(CR_F.r,CR_F.g,CR_F.b,0.3f));
        Stretch(Img("BAB2",boss.transform,Color.clear).gameObject);
        Txt("BossTxt", boss.transform, "💀  BOSS W POBLIŻU", 11, C(172,0,210));
    }

    // ── COMBO FLOAT ─────────────────────────────────────────────────
    void BuildComboFloat(Transform root)
    {
        var panel = Img("ComboFloat", root, C(8,6,20,235));
        BC(panel.gameObject, BOT_H + 6, 210, 42);

        Img("CF_Brd", panel.transform, new Color(BORDER.r,BORDER.g,BORDER.b,0.7f));
        Stretch(Img("CF_B2", panel.transform, Color.clear).gameObject);

        var lbl = Txt("CF_Lbl", panel.transform, "COMBO", 9, C(96,62,120));
        SA(lbl.gameObject, 0,0, 0.18f,1, 4,0,0,0);

        // Dots
        comboDots = new Image[3];
        float dr = 8f, dgap = 5f;
        float dtot = 3*dr*2 + 2*dgap;
        for (int i = 0; i < 3; i++)
        {
            float dx = -dtot*0.5f + i*(dr*2+dgap);
            var d = Img("Dot"+i, panel.transform, C(15,8,30));
            SA(d.gameObject, 0.5f,0.5f,0.5f,0.5f, -60+i*22,-dr,-60+i*22+dr*2,dr);
            d.sprite = CircleSprite();
            comboDots[i] = d;
        }

        comboCountText = Txt("CF_Cnt", panel.transform, "0<size=14>/3</size>", 22, C(210,52,52), TextAnchor.MiddleLeft);
        SA(comboCountText.gameObject, 0.58f,0, 0.82f,1, 2,0,0,0);

        comboTimerText = Txt("CF_Tim", panel.transform, "", 10, C(64,34,72));
        SA(comboTimerText.gameObject, 0.82f,0, 1f,1, 0,0,-4,0);
    }

    // ── BUFF BAR ────────────────────────────────────────────────────
    // Row of buff/debuff icons above the hotbar center
    void BuildBuffBar(Transform root)
    {
        var bar = GO("BuffBar", root);
        BC(bar.gameObject, BOT_H + 54, 480, 32);

        string[] buffs = { "🔥","⚡","🛡","💧","☠","🌀" };
        Color[]  bcols = {
            C(200,80,20), C(160,160,40), C(60,120,200),
            C(40,140,200), C(140,20,140), C(80,40,200)
        };
        float bsz = 30f, bgap = 6f;
        float btot = buffs.Length * bsz + (buffs.Length-1)*bgap;
        float bx0 = -btot*0.5f;

        for (int i = 0; i < buffs.Length; i++)
        {
            float bx = bx0 + i*(bsz+bgap);
            var slot = Img("Buff"+i, bar.transform, C(10,8,22));
            SA(slot.gameObject, 0.5f,0.5f,0.5f,0.5f, bx,-bsz*0.5f, bx+bsz,bsz*0.5f);
            Img("BuffBrd"+i, slot.transform, new Color(bcols[i].r,bcols[i].g,bcols[i].b,0.35f));
            Stretch(Img("BB2"+i,slot.transform,Color.clear).gameObject);
            Txt("BuffIc"+i, slot.transform, buffs[i], 16, Color.white);

            // Duration bar at bottom of buff icon
            var dur = Img("BuffDur"+i, slot.transform, bcols[i]);
            SA(dur.gameObject, 0,0,1,0, 0,0,0,3);
            dur.type = Image.Type.Filled;
            dur.fillMethod = Image.FillMethod.Horizontal;
            dur.fillAmount = 1f - i*0.15f; // placeholder
        }
    }

    // ── Circle sprite ────────────────────────────────────────────────
    static Sprite _circle;
    static Sprite CircleSprite()
    {
        if (_circle != null) return _circle;
        const int S = 64; float half = S * 0.5f;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float d = Mathf.Sqrt((x-half)*(x-half)+(y-half)*(y-half));
            tex.SetPixel(x, y, new Color(1,1,1, Mathf.Clamp01(half-d)));
        }
        tex.Apply();
        _circle = Sprite.Create(tex, new Rect(0,0,S,S), new Vector2(0.5f,0.5f));
        return _circle;
    }
}
