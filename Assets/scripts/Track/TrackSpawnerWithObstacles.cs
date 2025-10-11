using System;
using System.Collections.Generic;
using UnityEngine;

public class TrackSpawnerWithDependenciesV2 : MonoBehaviour
{
    [Header("Track Settings")]
    public GameObject floorPrefab;
    public int numberOfSegments = 10;
    public float baseSegmentLength = 12f;
    public Transform player;
    public float safeZone = 50f;

    [Header("Lane Settings")]
    public int laneCount = 3;
    public float laneDistance = 3f;

    [Header("Dynamic Slot Settings")]
    public int baseSlotsPerSegment = 1;
    public AnimationCurve slotScaleBySpeed = AnimationCurve.Linear(0, 1, 200, 1);

    [Header("Density Settings")]
    [Range(0f, 1f)] public float baseDensity = 0.5f;
    public AnimationCurve densityBySpeed = AnimationCurve.Linear(0, 1, 200, 1);

    [Header("Gap Settings (in slot counts)")]
    [Min(0)] public int minLowGap = 1;
    [Min(0)] public int minHighGap = 1;
    [Min(0)] public int minCrossGap = 1;

    [Header("Bonus Corridors")]
    public int bonusSegments = 15;
    [Range(0f, 1f)] public float bonusChanceAtHighSpeed = 0.007f;
    public float bonusSpeedThresholdKph = 120f;
    public bool pickupsOnlyInBonus = true;

    [Serializable]
    public class Wave
    {
        public float speedThresholdKph = 80f;
        [Range(0.1f, 3f)] public float densityMultiplier = 1f;
        [Range(0.1f, 3f)] public float obstacleMult = 1f;
        [Range(0.1f, 3f)] public float enemyMult = 1f;
        [Range(0.1f, 3f)] public float pickupMult = 1f;
        [Range(0.1f, 3f)] public float bossMult = 1f;
    }
    [Header("Waves of Difficulty")]
    public List<Wave> waves = new();

    // ======== Новая структура ========
    [Serializable]
    public class SpawnRule
    {
        [Header("Base")]
        public string key;
        public string category;
        public float weight = 1f;

        [Header("Dependencies")]
        public List<string> incompatibleCategories = new();
        public List<string> requiresCategories = new();
        public float dependencyRadius = 0f;

        [Header("Conditions")]
        public float minSpeed = 0f;
        public float maxSpeed = 9999f;
        public bool onlyInBonus = false;
        public bool onlyOutsideBonus = false;
        public float minDistanceBetweenSame = 0f;
    }

    [Header("Content Lists (Pool Keys)")]
    public List<SpawnRule> lowObstacleRules = new();
    public List<SpawnRule> highObstacleRules = new();
    public List<SpawnRule> enemyRules = new();
    public List<SpawnRule> pickupRules = new();
    public List<SpawnRule> bossRules = new();

    [Header("Global Weight Multipliers (by Category)")]
    [Min(0f)] public float pickupWeightMult = 1f;
    [Min(0f)] public float obstacleWeightMult = 1f;
    [Min(0f)] public float enemyWeightMult = 1f;
    [Min(0f)] public float bossWeightMult = 1f;

    [Header("Runner Link")]
    public MaoRunnerFixed runner;
    public float estimatedJumpTime = 0.3f;
    public float pickupHeight = 0.3f;

    [Header("Debug Mode")]
    public bool debugSpawnChecks = false;

    private float spawnZ = 0f;
    private readonly Queue<GameObject> activeSegments = new();
    private PoolManager pool => PoolManager.Instance;

    private struct LaneState { public int lowGap; public int highGap; }
    private LaneState[] laneState;
    private int bonusLeft = 0;

    private class SlotMemory
    {
        public List<string> categories = new();
    }
    private List<SlotMemory> slotHistory = new();

    // ======== Глобальные зависимости категорий ========
    private static readonly Dictionary<string, List<string>> GlobalIncompatibility = new()
    {
        {"ObstacleLow", new() {"ObstacleHigh", "Enemy", "Pickup", "Boss"}},
        {"ObstacleHigh", new() {"ObstacleLow", "Enemy", "Pickup", "Boss"}},
        {"Enemy", new() {"ObstacleLow", "ObstacleHigh", "Pickup", "Boss"}},
        {"Pickup", new() {"Enemy", "Boss"}},
        {"Boss", new() {"Enemy", "ObstacleLow", "ObstacleHigh"}}
    };

    private void Awake()
    {
        laneState = new LaneState[laneCount];
        for (int i = 0; i < laneCount; i++)
            laneState[i] = new LaneState { lowGap = 999, highGap = 999 };
    }

    private void Update()
    {
        if (activeSegments.Count < numberOfSegments)
            SpawnSegment();
        DeleteOldSegment();
    }

    private float CurrentKph => (runner != null) ? runner.forwardSpeed * 3.6f : 0f;
    private Wave CurrentWave
    {
        get
        {
            Wave w = null;
            foreach (var x in waves) if (CurrentKph >= x.speedThresholdKph) w = x;
            return w;
        }
    }

    private void SpawnSegment()
    {
        GameObject seg = Instantiate(floorPrefab, Vector3.forward * spawnZ, Quaternion.identity);
        activeSegments.Enqueue(seg);
        spawnZ += baseSegmentLength;

        int slots = Mathf.Max(1, Mathf.RoundToInt(baseSlotsPerSegment * slotScaleBySpeed.Evaluate(CurrentKph)));
        float density = baseDensity * densityBySpeed.Evaluate(CurrentKph);
        var wave = CurrentWave;
        if (wave != null) density *= wave.densityMultiplier;

        bool bonus = false;
        if (bonusLeft > 0) { bonus = true; bonusLeft--; }
        else if (CurrentKph >= bonusSpeedThresholdKph && UnityEngine.Random.value < bonusChanceAtHighSpeed)
        { bonus = true; bonusLeft = bonusSegments - 1; }

        bool[,] occupied = new bool[laneCount, slots];
        slotHistory.Clear();

        for (int s = 0; s < slots; s++)
        {
            slotHistory.Add(new SlotMemory());

            if (UnityEngine.Random.value > Mathf.Clamp01(density))
            { AdvanceGaps(); continue; }

            for (int lane = 0; lane < laneCount; lane++)
            {
                if (occupied[lane, s]) continue;
                TrySpawnInLane(seg.transform, lane, s, slots, occupied, bonus, wave);
            }
            AdvanceGaps();
        }
    }

    private void DeleteOldSegment()
    {
        if (activeSegments.Count == 0) return;
        if (player.position.z - safeZone > activeSegments.Peek().transform.position.z)
            Destroy(activeSegments.Dequeue());
    }

    private void AdvanceGaps()
    {
        for (int i = 0; i < laneState.Length; i++)
        {
            laneState[i].lowGap++;
            laneState[i].highGap++;
        }
    }

    private void TrySpawnInLane(Transform parent, int lane, int slot, int totalSlots, bool[,] occupied, bool bonus, Wave wave)
    {
        var allRules = new List<SpawnRule>();
        allRules.AddRange(pickupRules);
        allRules.AddRange(enemyRules);
        allRules.AddRange(bossRules);
        allRules.AddRange(lowObstacleRules);
        allRules.AddRange(highObstacleRules);

        List<SpawnRule> valid = new();

        foreach (var rule in allRules)
        {
            if (CanSpawn(rule, lane, slot, bonus)) valid.Add(rule);
        }

        if (valid.Count == 0) return;

        // Выбор по весам
        float totalWeight = 0f;
        foreach (var r in valid) totalWeight += r.weight;
        float rnd = UnityEngine.Random.value * totalWeight;

        float acc = 0f;
        SpawnRule chosen = valid[0];
        foreach (var r in valid)
        {
            acc += r.weight;
            if (rnd <= acc) { chosen = r; break; }
        }

        if (occupied[lane, slot]) return;
        occupied[lane, slot] = true;

        Vector3 pos = parent.position + Vector3.right * ((lane - (laneCount - 1) / 2f) * laneDistance);
        if (chosen.category.ToLower().Contains("pickup")) pos.y += pickupHeight;

        pool.Spawn(chosen.key, pos, Quaternion.identity, parent);
        slotHistory[slot].categories.Add(chosen.category);
        RegisterPlaced(lane, chosen.category);
    }

    private bool CanSpawn(SpawnRule rule, int lane, int slot, bool bonus)
    {
        float kph = CurrentKph;

        if (rule.onlyInBonus && !bonus) return false;
        if (rule.onlyOutsideBonus && bonus) return false;
        if (kph < rule.minSpeed || kph > rule.maxSpeed) return false;
        if (string.IsNullOrEmpty(rule.category)) return false;

        // --- Глобальные несовместимости ---
        if (GlobalIncompatibility.TryGetValue(rule.category, out var globalBlock))
        {
            foreach (var cat in globalBlock)
            {
                if (slot < slotHistory.Count && slotHistory[slot].categories.Contains(cat))
                    return false;
            }
        }

        // --- Локальные несовместимости ---
        if (rule.incompatibleCategories != null && rule.incompatibleCategories.Count > 0)
        {
            if (slot < slotHistory.Count)
            {
                foreach (var existing in slotHistory[slot].categories)
                {
                    if (rule.incompatibleCategories.Contains(existing))
                        return false;
                }
            }
        }

        // --- Проверка requires ---
        if (rule.requiresCategories != null && rule.requiresCategories.Count > 0)
        {
            bool found = false;
            int radius = Mathf.RoundToInt(rule.dependencyRadius);

            for (int i = -radius; i <= radius; i++)
            {
                int idx = slot + i;
                if (idx < 0 || idx >= slotHistory.Count) continue;

                foreach (var cat in slotHistory[idx].categories)
                {
                    if (rule.requiresCategories.Contains(cat))
                        found = true;
                }
            }
            if (!found) return false;
        }

        return true;
    }

    private void RegisterPlaced(int lane, string category)
    {
        if (category.ToLower().Contains("low"))
        {
            laneState[lane].lowGap = 0;
            laneState[lane].highGap = Mathf.Min(laneState[lane].highGap, minCrossGap);
        }
        else if (category.ToLower().Contains("high"))
        {
            laneState[lane].highGap = 0;
            laneState[lane].lowGap = Mathf.Min(laneState[lane].lowGap, minCrossGap);
        }
    }
}