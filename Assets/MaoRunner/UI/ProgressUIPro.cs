using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif
using MaoRunner;

public class ProgressUIPro : MonoBehaviour
{
    [Header("Bindings (optional)")]
    public Slider xpBar;

#if TMP_PRESENT
    public TMP_Text levelText;
    public TMP_Text coinsText;
#else
    public UnityEngine.UI.Text levelText;
    public UnityEngine.UI.Text coinsText;
#endif

    [Header("Icons (optional)")]
    public Image levelIcon;
    public Image coinsIcon;

    [Header("Style")]
    public Color levelColor = Color.white;
    public Color coinsColor = Color.white;
    public Color barColor = new Color(0f, 1f, 1f);
    [Range(0f, 1f)] public float barAlpha = 0.8f;

    private PlayerProgress _progress;

    void Awake()
    {
        // Найти прогресс (Instance или поиск по сцене)
        _progress = PlayerProgress.Instance ?? FindObjectOfType<PlayerProgress>();
    }

    void OnEnable()
    {
        if (_progress == null) return;

        _progress.OnLevelChanged += HandleLevelChanged;
        _progress.OnXpChanged += HandleXpChanged;
        _progress.OnCoinsChanged += HandleCoinsChanged;

        RefreshAll();
    }

    void OnDisable()
    {
        if (_progress == null) return;

        _progress.OnLevelChanged -= HandleLevelChanged;
        _progress.OnXpChanged -= HandleXpChanged;
        _progress.OnCoinsChanged -= HandleCoinsChanged;
    }

    private void HandleLevelChanged(int lvl) => RefreshAll();
    private void HandleXpChanged(int cur, int req) => RefreshAll();
    private void HandleCoinsChanged(int coins) => RefreshAll();

    private void RefreshAll()
    {
        if (_progress == null) return;

        // XP bar
        if (xpBar != null)
        {
            int req = Mathf.Max(1, _progress.XpForNextLevel);
            xpBar.minValue = 0;
            xpBar.maxValue = req;
            xpBar.value = Mathf.Clamp(_progress.CurrentXP, 0, req);

            // Цвет заливки
            var fill = xpBar.fillRect ? xpBar.fillRect.GetComponent<Image>() : null;
            if (fill != null) fill.color = new Color(barColor.r, barColor.g, barColor.b, barAlpha);
        }

        // Texts
        if (levelText != null) levelText.text = _progress.CurrentLevel.ToString();
        if (coinsText != null) coinsText.text = _progress.TotalCoins.ToString();

        // Цвета
        if (levelText != null) levelText.color = levelColor;
        if (coinsText != null) coinsText.color = coinsColor;
    }
}