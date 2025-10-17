using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-5)]
public class TrackSpawnerWithDependencies : MonoBehaviour
{
    public enum Category { ObstacleLow, ObstacleHigh, Enemy, Pickup, Boss }

    [Serializable]
    public class SpawnRule
    {
        [Header("Base")]
        public string key;
        public Category category = Category.ObstacleLow;
        [Min(0f)] public float weight = 1f;
        public List<int> allowedLanes = new List<int>();

        [Header("Dependencies")]
        public List<Category> incompatibleCategories = new();
        public List<Category> requiresCategories = new();
        [Min(0)] public int dependencyRadius = 0;

        [Header("Conditions")]
        public float minSpeed = 0f;
        public float maxSpeed = 999f;
        public bool onlyInBonus = false;
        public bool onlyOutsideBonus = false;
        [Min(0)] public int minDistanceBetweenSame = 0;
    }

    [Serializable]
    public class Wave
    {
        [Header("Wave Range")]
        public float startAtDistance = 0f;
        public float endAtDistance = 100f;

        [Header("Content Control")]
        [Tooltip("Ключи объектов, которые разрешены в этой волне.")]
        public List<string> allowedKeys = new();

        [Header("Slot Multiplier")]
        [Range(0.5f, 3f)] public float slotScaleByWave = 1f;

        [Header("Weight Multipliers")]
        [Range(0f, 5f)] public float pickupWeightMult = 1f;
        [Range(0f, 5f)] public float obstacleWeightMult = 1f;
        [Range(0f, 5f)] public float enemyWeightMult = 1f;
        [Range(0f, 5f)] public float bossWeightMult = 1f;
    }

    [Serializable]
    public class RuleList { public List<SpawnRule> items = new(); }

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
    public int baseSlotsPerSegment = 2;
    [Tooltip("X=скорость км/ч, Y=множитель слотов [0.5..2]")]
    public AnimationCurve slotScaleBySpeed = AnimationCurve.Linear(0, 1, 300, 1);

    [Header("Density Settings")]
    [Range(0f, 1f)] public float baseDensity = 0.5f;
    public AnimationCurve densityBySpeed = AnimationCurve.Linear(0, 0, 300, 0);

    [Header("Gap Settings (в длинах прыжка)")]
    public float minHighGap = 1f;
    public float minCrossGap = 1f;

    [Header("Bonus Corridors")]
    public int bonusSegments = 15;
    [Range(0f, 1f)] public float bonusChanceAtHighSpeed = 0.05f;
    public float bonusSpeedThresholdKph = 120f;
    public bool pickupsOnlyInBonus = true;
    [Range(0.5f, 3f)] public float slotScaleInBonus = 1.5f;

    [Header("Waves of Difficulty")]
    public List<Wave> waves = new();

    [Header("Content Rules")]
    public RuleList obstacleRules = new();
    public RuleList enemyRules = new();
    public RuleList pickupRules = new();
    public RuleList bossRules = new();

    [Header("Global Weight Multipliers")]
    [Range(0f, 5f)] public float pickupWeightMult = 1f;
    [Range(0f, 5f)] public float obstacleWeightMult = 1f;
    [Range(0f, 5f)] public float enemyWeightMult = 1f;
    [Range(0f, 5f)] public float bossWeightMult = 1f;

    [Header("Runner Link")]
    public MaoRunnerFixed runner;
    public float estimatedJumpTime = 0.35f;
    public float pickupHeight = 0.3f;

    [Header("Debug")]
    public bool debugSpawnChecks = false;
    public bool showSlotGizmos = false;

    private readonly List<GameObject> activeSegments = new();
    private float spawnZ;
    private System.Random rnd;
    private int currentBonusLeft = 0;
    private bool[,] occupied;
    private HashSet<Category>[,] reservations;

    private void Awake() => rnd = new System.Random(Environment.TickCount);

    private void Start()
    {
        spawnZ = 0f;
        for (int i = 0; i < numberOfSegments; i++) SpawnSegment();
    }

    private void Update()
    {
        if (player == null) return;
        CleanupSegments();
        while (activeSegments.Count < numberOfSegments) SpawnSegment();
    }

    // ---------- SEGMENT SPAWNING ----------
    private void SpawnSegment()
    {
        var segGO = Instantiate(floorPrefab, new Vector3(0, 0, spawnZ), Quaternion.identity, transform);
        activeSegments.Add(segGO);

        float kph = GetSpeedKphSafe();
        var activeWave = GetActiveWave();

        bool isBonus = TryStartOrContinueBonus(kph);
        float slotMult = GetSlotMultiplier(kph, activeWave, isBonus);
        int slots = Mathf.Max(1, Mathf.RoundToInt(baseSlotsPerSegment * slotMult));
        float density = ComputeDensity(kph);

        occupied = new bool[laneCount, slots];
        reservations = new HashSet<Category>[laneCount, slots];
        for (int l = 0; l < laneCount; l++)
            for (int s = 0; s < slots; s++)
                reservations[l, s] = new HashSet<Category>();

        // --- FILTER RULES BY ACTIVE WAVE ---
        List<SpawnRule> filteredObstacles = FilterByWave(obstacleRules.items, activeWave);
        List<SpawnRule> filteredEnemies = FilterByWave(enemyRules.items, activeWave);
        List<SpawnRule> filteredPickups = FilterByWave(pickupRules.items, activeWave);
        List<SpawnRule> filteredBosses = FilterByWave(bossRules.items, activeWave);

        // --- SPAWN CONTENT ---
        TryFillCategory(segGO.transform, slots, density, isBonus, Category.ObstacleLow, filteredObstacles);
        TryFillCategory(segGO.transform, slots, density, isBonus, Category.Enemy, filteredEnemies);
        TryFillCategory(segGO.transform, slots, density, isBonus, Category.Pickup, filteredPickups);
        TryFillCategory(segGO.transform, slots, density, isBonus, Category.Boss, filteredBosses);

        spawnZ += baseSegmentLength;
    }

    private float GetSlotMultiplier(float kph, Wave activeWave, bool isBonus)
    {
        float baseMult = Mathf.Clamp(slotScaleBySpeed.Evaluate(kph), 0.5f, 2f);
        if (activeWave != null) return baseMult * activeWave.slotScaleByWave;
        if (isBonus) return baseMult * slotScaleInBonus;
        return baseMult;
    }

    private void CleanupSegments()
    {
        while (activeSegments.Count > 0)
        {
            var first = activeSegments[0];
            if (player.position.z - safeZone > first.transform.position.z + baseSegmentLength)
            {
                activeSegments.RemoveAt(0);
                Destroy(first);
            }
            else break;
        }
    }

    // ---------- SPAWNING ----------
    private void TryFillCategory(Transform parent, int totalSlots, float density, bool isBonus, Category cat, List<SpawnRule> rules)
    {
        if (rules == null || rules.Count == 0) return;
        var (candidates, weights) = BuildWeightedList(rules, cat);
        if (candidates.Count == 0) return;

        var slotOrder = BuildRandomOrder(totalSlots);
        for (int sIndex = 0; sIndex < slotOrder.Count; sIndex++)
        {
            int slot = slotOrder[sIndex];
            for (int lane = 0; lane < laneCount; lane++)
            {
                if (occupied[lane, slot]) continue;
                if (UnityEngine.Random.value > density) continue;

                var rule = WeightedPick(candidates, weights);
                if (rule == null) continue;
                if (!CanSpawn(rule, isBonus)) continue;
                if (!LaneAllowed(rule, lane)) continue;
                if (!PassDependencies(rule, lane, slot)) continue;
                if (!PassSameMinDistance(rule, lane, slot)) continue;

                Vector3 pos = SlotToWorld(slot, lane, totalSlots);
                SpawnFromPool(rule.key, pos, parent);
                occupied[lane, slot] = true;
                reservations[lane, slot].Add(rule.category);

                if (debugSpawnChecks)
                    Debug.Log($"[✓] {rule.key} lane={lane} slot={slot} z={pos.z:F1}");
            }
        }
    }

    // ---------- CONDITIONS ----------
    private bool CanSpawn(SpawnRule rule, bool isBonus)
    {
        float kph = GetSpeedKphSafe();
        if (kph < rule.minSpeed || kph > rule.maxSpeed) return false;
        if (rule.onlyInBonus && !isBonus) return false;
        if (rule.onlyOutsideBonus && isBonus) return false;
        return true;
    }

    private bool LaneAllowed(SpawnRule rule, int lane)
    {
        if (rule.allowedLanes == null || rule.allowedLanes.Count == 0) return true;
        return rule.allowedLanes.Contains(lane);
    }

    private bool PassDependencies(SpawnRule rule, int lane, int slot)
    {
        int r = Mathf.Max(0, rule.dependencyRadius);
        foreach (var cat in rule.incompatibleCategories)
            if (HasCategoryAround(lane, slot, cat, r)) return false;

        if (rule.requiresCategories != null && rule.requiresCategories.Count > 0)
        {
            bool ok = false;
            foreach (var cat in rule.requiresCategories)
                if (HasCategoryAround(lane, slot, cat, r)) { ok = true; break; }
            if (!ok) return false;
        }
        return true;
    }

    private bool PassSameMinDistance(SpawnRule rule, int lane, int slot)
    {
        int r = Mathf.Max(0, rule.minDistanceBetweenSame);
        if (r == 0) return true;
        return !HasCategoryAround(lane, slot, rule.category, r);
    }

    private bool HasCategoryAround(int lane, int slot, Category cat, int radius)
    {
        int s0 = Mathf.Max(0, slot - radius);
        int s1 = Mathf.Min(reservations.GetLength(1) - 1, slot + radius);
        for (int l = 0; l < laneCount; l++)
            for (int s = s0; s <= s1; s++)
                if (reservations[l, s].Contains(cat)) return true;
        return false;
    }

    // ---------- HELPERS ----------
    private (List<SpawnRule>, List<float>) BuildWeightedList(List<SpawnRule> src, Category cat)
    {
        var rules = new List<SpawnRule>();
        var weights = new List<float>();
        float mult = cat switch
        {
            Category.Pickup => pickupWeightMult,
            Category.Enemy => enemyWeightMult,
            Category.Boss => bossWeightMult,
            _ => obstacleWeightMult
        };

        foreach (var r in src)
        {
            float w = Mathf.Max(0f, r.weight) * mult;
            if (w > 0f) { rules.Add(r); weights.Add(w); }
        }
        return (rules, weights);
    }

    private List<int> BuildRandomOrder(int n)
    {
        var list = new List<int>(n);
        for (int i = 0; i < n; i++) list.Add(i);
        for (int i = n - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    private SpawnRule WeightedPick(List<SpawnRule> c, List<float> w)
    {
        if (c.Count == 0) return null;
        float total = 0f; for (int i = 0; i < w.Count; i++) total += w[i];
        float r = (float)rnd.NextDouble() * total, acc = 0f;
        for (int i = 0; i < c.Count; i++) { acc += w[i]; if (r <= acc) return c[i]; }
        return c[c.Count - 1];
    }

    private Vector3 SlotToWorld(int slot, int lane, int totalSlots)
    {
        float slotLen = baseSegmentLength / totalSlots;
        float x = (lane - (laneCount - 1) * 0.5f) * laneDistance;
        float z = spawnZ + slot * slotLen + slotLen * 0.5f;
        return new Vector3(x, 0f, z);
    }

    private void SpawnFromPool(string key, Vector3 pos, Transform parent)
    {
        if (PoolManager.Instance != null)
            PoolManager.Instance.Spawn(key, pos, Quaternion.identity, parent);
    }

    private float GetSpeedKphSafe()
    {
        if (!runner && player) runner = player.GetComponent<MaoRunnerFixed>();
        return runner ? Mathf.Max(0f, runner.forwardSpeed * 3.6f) : 0f;
    }

    private float ComputeDensity(float kph)
    {
        float add = Mathf.Clamp01(densityBySpeed.Evaluate(kph));
        return Mathf.Clamp01(baseDensity + add);
    }

    private bool TryStartOrContinueBonus(float kph)
    {
        if (currentBonusLeft > 0) { currentBonusLeft--; return true; }
        if (kph >= bonusSpeedThresholdKph && UnityEngine.Random.value < bonusChanceAtHighSpeed)
        { currentBonusLeft = Mathf.Max(1, bonusSegments) - 1; return true; }
        return false;
    }

    private Wave GetActiveWave()
    {
        if (waves == null || waves.Count == 0) return null;
        float z = player ? player.position.z : 0f;
        foreach (var w in waves)
            if (z >= w.startAtDistance && z <= w.endAtDistance)
                return w;
        return null;
    }

    private List<SpawnRule> FilterByWave(List<SpawnRule> src, Wave wave)
    {
        if (wave == null || wave.allowedKeys == null || wave.allowedKeys.Count == 0)
            return src;
        var list = new List<SpawnRule>();
        foreach (var rule in src)
            if (wave.allowedKeys.Contains(rule.key))
                list.Add(rule);
        return list;
    }
}