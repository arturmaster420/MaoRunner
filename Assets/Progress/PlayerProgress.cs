
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaoRunner
{
    public class PlayerProgress : MonoBehaviour
    {
        public static PlayerProgress Instance { get; private set; }

        [Header("XP Curve")]
        public int baseXP = 100;
        [Range(1.0f, 3.0f)] public float expExponent = 1.2f;
        [Range(1, 1000)] public int maxLevel = 100;

        [Header("Pickups → Values (optional)")]
        public List<GameObject> coinPrefabs = new List<GameObject>();
        public List<int> coinValues = new List<int>();
        public List<GameObject> xpPrefabs = new List<GameObject>();
        public List<int> xpValues = new List<int>();

        [Header("Persistence Keys")]
        public string keyLevel = "player_level";
        public string keyXP = "player_xp";
        public string keyCoins = "player_coins";

        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentXP = 0;
        [SerializeField] private int totalCoins = 0;

        public event Action<int> OnLevelChanged;
        public event Action<int,int> OnXpChanged;
        public event Action<int> OnCoinsChanged;

        public int CurrentLevel => currentLevel;
        public int CurrentXP => currentXP;
        public int TotalCoins => totalCoins;
        public int XpForNextLevel => GetXPForLevel(Mathf.Min(currentLevel + 1, maxLevel));

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
            DispatchAll();
        }

        public void AddCoins(int amount)
        {
            if (amount == 0) return;
            totalCoins = Mathf.Max(0, totalCoins + amount);
            Save();
            OnCoinsChanged?.Invoke(totalCoins);
        }

        public void AddXP(int amount)
        {
            if (amount <= 0) return;
            currentXP += amount;
            while (currentLevel < maxLevel && currentXP >= XpForNextLevel)
            {
                currentXP -= XpForNextLevel;
                currentLevel = Mathf.Min(currentLevel + 1, maxLevel);
                OnLevelChanged?.Invoke(currentLevel);
            }
            Save();
            OnXpChanged?.Invoke(currentXP, XpForNextLevel);
        }

        public int GetCoinValueForPrefab(GameObject prefab)
        {
            int idx = coinPrefabs.IndexOf(prefab);
            if (idx >= 0 && idx < coinValues.Count) return Mathf.Max(0, coinValues[idx]);
            return 0;
        }
        public int GetXPValueForPrefab(GameObject prefab)
        {
            int idx = xpPrefabs.IndexOf(prefab);
            if (idx >= 0 && idx < xpValues.Count) return Mathf.Max(0, xpValues[idx]);
            return 0;
        }

        public void GrantCoinPickup(GameObject coinPrefab) => AddCoins(GetCoinValueForPrefab(coinPrefab));
        public void GrantXPPickup(GameObject xpPrefab) => AddXP(GetXPValueForPrefab(xpPrefab));

        public int GetXPForLevel(int level)
        {
            level = Mathf.Clamp(level, 1, maxLevel);
            double value = baseXP * System.Math.Pow(Mathf.Max(1.0001f, expExponent), level - 1);
            return Mathf.Max(1, (int)System.Math.Round(value));
        }

        [ContextMenu("Reset Player Progress")]
        public void ResetProgress()
        {
            currentLevel = 1; currentXP = 0; totalCoins = 0;
            Save(); DispatchAll();
        }

        void Load()
        {
            currentLevel = Mathf.Clamp(PlayerPrefs.GetInt(keyLevel, 1), 1, maxLevel);
            currentXP = Mathf.Max(0, PlayerPrefs.GetInt(keyXP, 0));
            totalCoins = Mathf.Max(0, PlayerPrefs.GetInt(keyCoins, 0));
            while (currentLevel < maxLevel && currentXP >= XpForNextLevel)
            {
                currentXP -= XpForNextLevel;
                currentLevel++;
            }
        }
        void Save()
        {
            PlayerPrefs.SetInt(keyLevel, currentLevel);
            PlayerPrefs.SetInt(keyXP, currentXP);
            PlayerPrefs.SetInt(keyCoins, totalCoins);
            PlayerPrefs.Save();
        }
        void DispatchAll()
        {
            OnLevelChanged?.Invoke(currentLevel);
            OnCoinsChanged?.Invoke(totalCoins);
            OnXpChanged?.Invoke(currentXP, XpForNextLevel);
        }
        void OnValidate()
        {
            if (coinPrefabs.Count != coinValues.Count)
                Debug.LogWarning("[PlayerProgress] coinPrefabs and coinValues have different lengths.");
            if (xpPrefabs.Count != xpValues.Count)
                Debug.LogWarning("[PlayerProgress] xpPrefabs and xpValues have different lengths.");
        }
    }
}
