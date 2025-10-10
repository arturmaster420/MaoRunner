using System.Collections.Generic;
using UnityEngine;

public class TrackSpawnerWithObstacles : MonoBehaviour
{
    [Header("Track Settings")]
    public GameObject floorPrefab;
    public int numberOfSegments = 10;
    public float baseSegmentLength = 10f;
    public Transform player;
    public float safeZone = 50f;

    private float spawnZ = 0f;
    private Queue<GameObject> activeSegments = new Queue<GameObject>();

    [Header("Lane Settings")]
    public int laneCount = 3;
    public float laneDistance = 3f;

    [Header("Dynamic Slot Settings")]
    public int baseSlotsPerSegment = 6;
    public AnimationCurve slotScaleBySpeed = AnimationCurve.Linear(1f, 1f, 4f, 2f);

    [Header("Density Settings")]
    [Range(0f, 1f)] public float baseDensity = 0.6f;
    public AnimationCurve densityBySpeed = AnimationCurve.Linear(1f, 0.8f, 4f, 0.4f);

    [Header("Gap Settings (in jump lengths)")]
    public float minLowGap = 1.0f;
    public float minHighGap = 1.0f;
    public float minCrossGap = 0.7f;

    [Header("Bonus Corridors")]
    public int bonusSegments = 10;
    [Range(0f, 1f)] public float bonusChanceAtHighSpeed = 0.08f;
    public float bonusSpeedThresholdKph = 140f;
    public bool pickupsOnlyInBonus = true;
    private int bonusLeft = 0;

    // ================================
    // 🔹 WAVES — уровни сложности
    // ================================
    [System.Serializable]
    public class WaveSettings
    {
        [Header("Wave Trigger")]
        public float minSpeedKph = 0f;
        public float densityMultiplier = 1f;
        public float bonusChanceMultiplier = 1f;
        public float slotScaleMultiplier = 1f;

        [Header("Custom Weights Override")]
        public List<WeightedKey> overrideLowKeys;
        public List<WeightedKey> overrideHighKeys;
        public List<WeightedKey> overrideEnemyKeys;
        public List<WeightedKey> overridePickupKeys;
        public List<WeightedKey> overrideBossKeys;
    }

    [Header("Waves of Difficulty")]
    public List<WaveSettings> waves = new List<WaveSettings>();
    private WaveSettings activeWave;

    // ================================
    // 🔹 Списки контента
    // ================================
    [System.Serializable]
    public class WeightedKey
    {
        public string key;
        [Range(0.01f, 50f)] public float weight = 1f;
    }

    [Header("Content Lists (Pool Keys)")]
    public List<WeightedKey> lowObstacleKeys = new();
    public List<WeightedKey> highObstacleKeys = new();
    public List<WeightedKey> enemyKeys = new();
    public List<WeightedKey> pickupKeys = new();
    public List<WeightedKey> bossKeys = new();

    // 🔹 Глобальные множители весов
    [Header("Global Weight Multipliers (by Category)")]
    public float pickupWeightMult = 2f;
    public float obstacleWeightMult = 1f;
    public float enemyWeightMult = 0.8f;
    public float bossWeightMult = 0.5f;

    [Header("Runner Link")]
    public MaoRunnerFixed runner;
    public float estimatedJumpTime = 0.5f;
    public float pickupHeight = 0.3f;

    private float SpeedKph => runner != null ? runner.forwardSpeed * 3.6f : 30f;
    private float SpeedRatio => Mathf.Max(0.1f, SpeedKph / 30f);

    // ===============================================================

    void Update()
    {
        while (activeSegments.Count < numberOfSegments)
            SpawnSegment();

        DeleteOldSegment();
    }

    private void SpawnSegment()
    {
        GameObject segment = Instantiate(floorPrefab, Vector3.forward * spawnZ, Quaternion.identity);
        activeSegments.Enqueue(segment);
        spawnZ += baseSegmentLength;

        SpawnContent(segment.transform);
    }

    private void DeleteOldSegment()
    {
        if (activeSegments.Count == 0) return;
        if (player.position.z - safeZone > activeSegments.Peek().transform.position.z)
            Destroy(activeSegments.Dequeue());
    }

    // ===============================================================

    private void SpawnContent(Transform parent)
    {
        activeWave = GetActiveWave(SpeedKph);

        int slots = baseSlotsPerSegment;
        float stepZ = (baseSegmentLength / slots) *
                      slotScaleBySpeed.Evaluate(SpeedRatio) *
                      (activeWave != null ? activeWave.slotScaleMultiplier : 1f);

        float density = baseDensity *
                        densityBySpeed.Evaluate(SpeedRatio) *
                        (activeWave != null ? activeWave.densityMultiplier : 1f);

        bool isBonus = false;
        if (bonusLeft > 0)
        {
            bonusLeft--;
            isBonus = true;
        }
        else if (SpeedKph >= bonusSpeedThresholdKph &&
                 Random.value < bonusChanceAtHighSpeed *
                 (activeWave != null ? activeWave.bonusChanceMultiplier : 1f))
        {
            isBonus = true;
            bonusLeft = bonusSegments - 1;
        }

        var grid = new SlotType[laneCount, slots];
        var lastLow = new int[laneCount];
        var lastHigh = new int[laneCount];
        for (int i = 0; i < laneCount; i++) { lastLow[i] = -999; lastHigh[i] = -999; }

        float jumpMeters = runner != null ? runner.forwardSpeed * estimatedJumpTime : 10f;
        int minLowSlots = Mathf.CeilToInt((jumpMeters / stepZ) * minLowGap);
        int minHighSlots = Mathf.CeilToInt((jumpMeters / stepZ) * minHighGap);
        int minCrossSlots = Mathf.CeilToInt((jumpMeters / stepZ) * minCrossGap);

        var lowList = (activeWave != null && activeWave.overrideLowKeys.Count > 0) ? activeWave.overrideLowKeys : lowObstacleKeys;
        var highList = (activeWave != null && activeWave.overrideHighKeys.Count > 0) ? activeWave.overrideHighKeys : highObstacleKeys;
        var enemyList = (activeWave != null && activeWave.overrideEnemyKeys.Count > 0) ? activeWave.overrideEnemyKeys : enemyKeys;
        var pickupList = (activeWave != null && activeWave.overridePickupKeys.Count > 0) ? activeWave.overridePickupKeys : pickupKeys;
        var bossList = (activeWave != null && activeWave.overrideBossKeys.Count > 0) ? activeWave.overrideBossKeys : bossKeys;

        // сначала пикапы, потом препятствия
        TrySpawnType(parent, pickupList, SlotType.Pickup, density * (isBonus ? 2f : 1f),
            grid, stepZ, lastLow, lastHigh, minLowSlots, minHighSlots, minCrossSlots, true);
        if (!isBonus) TrySpawnType(parent, bossList, SlotType.Boss, density * 0.2f,
            grid, stepZ, lastLow, lastHigh, minLowSlots, minHighSlots, minCrossSlots);
        if (!isBonus) TrySpawnType(parent, enemyList, SlotType.Enemy, density * 0.5f,
            grid, stepZ, lastLow, lastHigh, minLowSlots, minHighSlots, minCrossSlots);
        if (!isBonus) TrySpawnType(parent, lowList, SlotType.Low, density,
            grid, stepZ, lastLow, lastHigh, minLowSlots, minHighSlots, minCrossSlots);
        if (!isBonus) TrySpawnType(parent, highList, SlotType.High, density,
            grid, stepZ, lastLow, lastHigh, minLowSlots, minHighSlots, minCrossSlots);
    }

    private void TrySpawnType(Transform parent, List<WeightedKey> list, SlotType type, float density, SlotType[,] grid,
                              float stepZ, int[] lastLow, int[] lastHigh,
                              int minLowSlots, int minHighSlots, int minCrossSlots, bool pickups = false)
    {
        if (list == null || list.Count == 0) return;

        for (int slot = 0; slot < baseSlotsPerSegment; slot++)
        {
            for (int lane = 0; lane < laneCount; lane++)
            {
                if (grid[lane, slot] != SlotType.None) continue;
                if (Random.value > density) continue;

                if (!pickups && (type == SlotType.Low || type == SlotType.High))
                {
                    int lastOwn = (type == SlotType.Low ? lastLow[lane] : lastHigh[lane]);
                    int lastCross = (type == SlotType.Low ? lastHigh[lane] : lastLow[lane]);
                    int minOwn = (type == SlotType.Low ? minLowSlots : minHighSlots);
                    if (slot - lastOwn < minOwn) continue;
                    if (slot - lastCross < minCrossSlots) continue;
                }

                WeightedKey wk = PickWeighted(list);
                if (wk == null) continue;

                Vector3 pos = LaneSlotPos(parent, lane, slot, stepZ);
                if (type == SlotType.Pickup) pos.y += pickupHeight;

                GameObject obj = PoolManager.Instance.Spawn(wk.key, pos, Quaternion.identity, parent);
                grid[lane, slot] = type;

                if (type == SlotType.Low) lastLow[lane] = slot;
                if (type == SlotType.High) lastHigh[lane] = slot;
            }
        }
    }

    private WeightedKey PickWeighted(List<WeightedKey> list)
    {
        if (list == null || list.Count == 0) return null;

        // выбираем множитель категории
        float typeMult = 1f;
        if (list == pickupKeys) typeMult = pickupWeightMult;
        else if (list == lowObstacleKeys || list == highObstacleKeys) typeMult = obstacleWeightMult;
        else if (list == enemyKeys) typeMult = enemyWeightMult;
        else if (list == bossKeys) typeMult = bossWeightMult;

        float total = 0f;
        foreach (var w in list) total += w.weight * typeMult;

        float r = Random.value * total;
        foreach (var w in list)
        {
            r -= w.weight * typeMult;
            if (r <= 0f) return w;
        }

        return list[list.Count - 1];
    }

    private Vector3 LaneSlotPos(Transform parent, int lane, int slot, float stepZ)
    {
        float laneX = (lane - (laneCount - 1) / 2f) * laneDistance;
        float z = parent.position.z + (slot + 0.5f) * stepZ;
        return new Vector3(laneX, parent.position.y, z);
    }

    private WaveSettings GetActiveWave(float kph)
    {
        WaveSettings selected = null;
        foreach (var w in waves)
        {
            if (kph >= w.minSpeedKph)
                selected = w;
        }
        return selected;
    }

    private enum SlotType { None, Low, High, Enemy, Boss, Pickup }
}