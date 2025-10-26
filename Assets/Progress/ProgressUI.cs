using UnityEngine;
using UnityEngine.UI;
using System.Collections;
#if TMP_PRESENT
using TMPro;
#endif
using MaoRunner;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ProgressUI : MonoBehaviour
{
    [Header("Bindings (optional)")]
    public Slider xpBar;
#if TMP_PRESENT
    public TextMeshProUGUI levelTMP;
    public TextMeshProUGUI coinsTMP;
#else
    public Text levelTextLegacy;
    public Text coinsTextLegacy;
#endif
    public Image levelIcon;
    public Image coinsIcon;

    [Header("Auto Create UI")]
    public bool autoCreateIfEmpty = true;
    public Vector2 anchoredPos = new Vector2(20f, -20f);

    [Header("Layout Settings")]
    public float verticalSpacing = 30f;
    public float iconSize = 28f;
    public float textSize = 22f;
    public Color textColor = Color.white;

    [Header("XP Bar Style")]
    public float barHeight = 14f;
    public Color barColor = new Color(0.2f, 0.8f, 1f, 0.9f);
    public float barAlpha = 0.9f;

    [Header("Icons (PNG Sprites)")]
    public Sprite levelSprite;
    public Sprite coinSprite;
    public Color iconColor = Color.white;

    [Header("XP Animation")]
    public bool pulseOnXP = true;
    public float pulseScale = 1.15f;
    public float pulseDuration = 0.25f;

    private Coroutine pulseRoutine;

    void Awake()
    {
        if (autoCreateIfEmpty && xpBar == null)
            CreateHUD();
    }

    void OnEnable()
    {
        if (PlayerProgress.Instance == null) return;
        PlayerProgress.Instance.OnLevelChanged += OnLevelChanged;
        PlayerProgress.Instance.OnXpChanged += OnXPChanged;
        PlayerProgress.Instance.OnCoinsChanged += OnCoinsChanged;

        OnLevelChanged(PlayerProgress.Instance.CurrentLevel);
        OnXPChanged(PlayerProgress.Instance.CurrentXP, PlayerProgress.Instance.XpForNextLevel);
        OnCoinsChanged(PlayerProgress.Instance.TotalCoins);
    }

    void OnDisable()
    {
        if (PlayerProgress.Instance == null) return;
        PlayerProgress.Instance.OnLevelChanged -= OnLevelChanged;
        PlayerProgress.Instance.OnXpChanged -= OnXPChanged;
        PlayerProgress.Instance.OnCoinsChanged -= OnCoinsChanged;
    }

    // ---- UPDATE UI ----
    void OnLevelChanged(int lvl) => SetLevelText($"LVL: {lvl}");
    void OnCoinsChanged(int c) => SetCoinsText($"{c}");

    void OnXPChanged(int xp, int next)
    {
        if (xpBar == null) return;
        xpBar.minValue = 0;
        xpBar.maxValue = Mathf.Max(1, next);
        xpBar.value = xp;

        if (pulseOnXP && gameObject.activeInHierarchy)
        {
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseXPBar());
        }
    }

    IEnumerator PulseXPBar()
    {
        if (xpBar == null || xpBar.fillRect == null) yield break;
        Transform t = xpBar.fillRect;
        float timer = 0f;
        while (timer < pulseDuration)
        {
            timer += Time.deltaTime;
            float scale = Mathf.Lerp(1f, pulseScale, Mathf.Sin(timer / pulseDuration * Mathf.PI));
            t.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // ---- TEXT SETTERS ----
    void SetLevelText(string s)
    {
#if TMP_PRESENT
        if (levelTMP) { levelTMP.text = s; return; }
#else
        if (levelTextLegacy) { levelTextLegacy.text = s; return; }
#endif
    }
    void SetCoinsText(string s)
    {
#if TMP_PRESENT
        if (coinsTMP) { coinsTMP.text = s; return; }
#else
        if (coinsTextLegacy) { coinsTextLegacy.text = s; return; }
#endif
    }

    // ---- CREATE UI ----
    public void CreateHUD()
    {
        var canvasGO = new GameObject("UI_ProgressPanel");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGO.transform, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = anchoredPos;

        // Level Row
        CreateRow(panel.transform, "Level", out levelIcon,
#if TMP_PRESENT
            out levelTMP
#else
            out levelTextLegacy
#endif
            );

        // Coins Row
        CreateRow(panel.transform, "Coins", out coinsIcon,
#if TMP_PRESENT
            out coinsTMP
#else
            out coinsTextLegacy
#endif
            , offsetY: -verticalSpacing);

        // XP Bar
        var xpBarObj = new GameObject("XPBar");
        xpBarObj.transform.SetParent(panel.transform, false);
        var img = xpBarObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.3f);
        var rt = xpBarObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(0, -verticalSpacing * 2);
        rt.sizeDelta = new Vector2(260, barHeight);

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(xpBarObj.transform, false);
        var fill = fillGO.AddComponent<Image>();
        fill.color = new Color(barColor.r, barColor.g, barColor.b, barAlpha);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(1, 1);
        fillRt.offsetMin = new Vector2(2, 2);
        fillRt.offsetMax = new Vector2(-2, -2);

        var slider = xpBarObj.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.targetGraphic = fill;
        xpBar = slider;

        UpdateIconSprites();
        UpdateStyle();
    }

    void CreateRow(Transform parent, string name, out Image icon,
#if TMP_PRESENT
        out TextMeshProUGUI text,
#else
        out Text text,
#endif
        float offsetY = 0f)
    {
        GameObject iconGO = new GameObject(name + "Icon");
        iconGO.transform.SetParent(parent, false);
        icon = iconGO.AddComponent<Image>();
        icon.color = iconColor;
        var iconRt = icon.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 1);
        iconRt.anchorMax = new Vector2(0, 1);
        iconRt.pivot = new Vector2(0, 1);
        iconRt.anchoredPosition = new Vector2(0, offsetY);
        iconRt.sizeDelta = new Vector2(iconSize, iconSize);

        GameObject textGO = new GameObject(name + "Text");
        textGO.transform.SetParent(parent, false);
#if TMP_PRESENT
        text = textGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = textSize;
        text.color = textColor;
#else
        text = textGO.AddComponent<Text>();
        text.fontSize = Mathf.RoundToInt(textSize);
        text.color = textColor;
#endif
        var trt = text.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 1);
        trt.anchorMax = new Vector2(0, 1);
        trt.pivot = new Vector2(0, 1);
        trt.anchoredPosition = new Vector2(iconSize + 5f, offsetY - (iconSize - textSize) / 2f);
        trt.sizeDelta = new Vector2(240, iconSize);
    }

    public void UpdateIconSprites()
    {
        if (levelIcon && levelSprite) levelIcon.sprite = levelSprite;
        if (coinsIcon && coinSprite) coinsIcon.sprite = coinSprite;
    }

    public void UpdateStyle()
    {
#if TMP_PRESENT
        if (levelTMP) { levelTMP.fontSize = textSize; levelTMP.color = textColor; }
        if (coinsTMP) { coinsTMP.fontSize = textSize; coinsTMP.color = textColor; }
#else
        if (levelTextLegacy) { levelTextLegacy.fontSize = Mathf.RoundToInt(textSize); levelTextLegacy.color = textColor; }
        if (coinsTextLegacy) { coinsTextLegacy.fontSize = Mathf.RoundToInt(textSize); coinsTextLegacy.color = textColor; }
#endif
        if (xpBar && xpBar.fillRect)
        {
            var fill = xpBar.fillRect.GetComponent<Image>();
            if (fill)
                fill.color = new Color(barColor.r, barColor.g, barColor.b, barAlpha);
        }
        if (levelIcon) { levelIcon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize); levelIcon.color = iconColor; }
        if (coinsIcon) { coinsIcon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize); coinsIcon.color = iconColor; }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ProgressUI))]
public class ProgressUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ProgressUI ui = (ProgressUI)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Сгенерируй панель с TMP, настрой цвета, размеры и иконки.", MessageType.Info);

        if (GUILayout.Button("🪄 Generate HUD"))
        {
            ui.CreateHUD();
            EditorUtility.SetDirty(ui);
        }

        if (GUILayout.Button("🎨 Refresh Style"))
        {
            ui.UpdateIconSprites();
            ui.UpdateStyle();
            EditorUtility.SetDirty(ui);
        }
    }
}
#endif