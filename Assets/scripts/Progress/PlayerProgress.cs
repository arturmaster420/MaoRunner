using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProgress : MonoBehaviour
{
    public static PlayerProgress Instance { get; private set; }

    [Header("Levels Settings")]
    [Range(1, 1000)] public int maxLevel = 100;
    [Tooltip("Сколько XP нужно для перехода с 1 на 2 уровень")]
    public int baseXPRequired = 100;

    public enum GrowthMode { Linear, Exponential, CustomCurve }
    public GrowthMode growthMode = GrowthMode.Exponential;

    [Tooltip("Множитель роста XP на уровень (актуален для Linear/Exponential)")]
    [Min(0f)] public float xpGrowthMultiplier = 1.25f;

    [Tooltip("Кривая произвольного роста XP (X = уровень, Y = множитель к baseXPRequired). " +
             "Нормируй X на [1..maxLevel], например ключи 1, 10, 50, maxLevel.")]
    public AnimationCurve customCurve = AnimationCurve.Linear(1, 1, 100, 5);

    [Tooltip("Множитель наград за левел-ап (если будешь начислять бонусы)")]
    public float levelRewardMultiplier = 1f;

    [Header("Currency Settings")]
    [Tooltip("Префабы монет (можно добавлять/заменять). Размер списка должен совпадать со списком Coin Values.")]
    public List<GameObject> coinPrefabs = new();
    [Tooltip("Номинал каждой монеты (по индексам к префабам).")]
    public List<int> coinValues = new();

    [Tooltip("Префабы опыта (для разных визуалов). Должен совпадать по длине с XP Values.")]
    public List<GameObject> xpPrefabs = new();
    [Tooltip("Количество XP для каждого префаба (по индексам).")]
    public List<int> xpValues = new();

    [Header("Persistence Settings")]
    public bool autoSave = true;
    [Tooltip("Ключи PlayerPrefs для сохранения.")]
    public string keyLevel = "player_level";
    public string keyXP = "player_xp";
    public string keyCoins = "player_coins";

    [Header("Runtime (read-only)")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int totalCoins = 0;

    // События
    public event Action<int> OnLevelChanged;        // новый уровень
    public event Action<int, int> OnXPChanged;       // текущее XP, нужно для след. уровня
    public event Action<int> OnCoinsChanged;        // новые монеты

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        ValidateLists();
        DispatchAll();
    }

    void OnApplicationQuit() { if (autoSave) Save(); }
    void OnApplicationPause(bool pause) { if (autoSave && pause) Save(); }

    public int CurrentLevel => currentLevel;
    public int CurrentXP => currentXP;
    public int TotalCoins => totalCoins;
    public int XPForNextLevel => Mathf.Max(1, GetXPForLevel(Mathf.Min(currentLevel + 1, maxLevel)));

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        totalCoins += amount;
        if (autoSave) PlayerPrefs.SetInt(keyCoins, totalCoins);
        OnCoinsChanged?.Invoke(totalCoins);
    }

    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        if (currentLevel >= maxLevel) return;

        currentXP += amount;

        // Луп апов: вдруг за один подбор сразу несколько уровней
        while (currentLevel < maxLevel && currentXP >= XPForNextLevel)
        {
            currentXP -= XPForNextLevel;
            currentLevel++;
            OnLevelChanged?.Invoke(currentLevel);
        }

        if (autoSave)
        {
            PlayerPrefs.SetInt(keyXP, currentXP);
            PlayerPrefs.SetInt(keyLevel, currentLevel);
        }
        OnXPChanged?.Invoke(currentXP, XPForNextLevel);
    }

    public void ResetProgress()
    {
        currentLevel = 1;
        currentXP = 0;
        totalCoins = 0;
        Save();
        DispatchAll();
    }

    public int GetCoinValueForPrefab(GameObject prefab)
    {
        int idx = coinPrefabs.IndexOf(prefab);
        return (idx >= 0 && idx < coinValues.Count) ? Mathf.Max(0, coinValues[idx]) : 0;
    }

    public int GetXPValueForPrefab(GameObject prefab)
    {
        int idx = xpPrefabs.IndexOf(prefab);
        return (idx >= 0 && idx < xpValues.Count) ? Mathf.Max(0, xpValues[idx]) : 0;
    }

    public int GetXPForLevel(int level)
    {
        level = Mathf.Clamp(level, 1, maxLevel);
        switch (growthMode)
        {
            case GrowthMode.Linear:
                // baseXP + (level-1) * baseXP * (multiplier-1)
                return Mathf.RoundToInt(baseXPRequired * (1f + (level - 1) * Mathf.Max(0f, xpGrowthMultiplier - 1f)));
            case GrowthMode.Exponential:
                // baseXP * (multiplier)^(level-1)
                return Mathf.RoundToInt(baseXPRequired * Mathf.Pow(Mathf.Max(1.0001f, xpGrowthMultiplier), level - 1));
            case GrowthMode.CustomCurve:
                float t = level; // X = уровень
                float factor = Mathf.Max(0.01f, customCurve.Evaluate(t));
                return Mathf.RoundToInt(baseXPRequired * factor);
            default:
                return baseXPRequired;
        }
    }

    private void ValidateLists()
    {
        if (coinPrefabs.Count != coinValues.Count)
            Debug.LogWarning("[PlayerProgress] coinPrefabs и coinValues разной длины. Лишние элементы будут игнорированы.");
        if (xpPrefabs.Count != xpValues.Count)
            Debug.LogWarning("[PlayerProgress] xpPrefabs и xpValues разной длины. Лишние элементы будут игнорированы.");
    }

    private void DispatchAll()
    {
        OnLevelChanged?.Invoke(currentLevel);
        OnXPChanged?.Invoke(currentXP, XPForNextLevel);
        OnCoinsChanged?.Invoke(totalCoins);
    }

    private void Load()
    {
        currentLevel = Mathf.Clamp(PlayerPrefs.GetInt(keyLevel, 1), 1, maxLevel);
        currentXP = Mathf.Max(0, PlayerPrefs.GetInt(keyXP, 0));
        totalCoins = Mathf.Max(0, PlayerPrefs.GetInt(keyCoins, 0));
        // Санити-чек: если XP превышает порог уровня — аккуратно поднимем уровень
        while (currentLevel < maxLevel && currentXP >= XPForNextLevel)
        {
            currentXP -= XPForNextLevel;
            currentLevel++;
        }
    }

    private void Save()
    {
        PlayerPrefs.SetInt(keyLevel, currentLevel);
        PlayerPrefs.SetInt(keyXP, currentXP);
        PlayerPrefs.SetInt(keyCoins, totalCoins);
        PlayerPrefs.Save();
    }
}