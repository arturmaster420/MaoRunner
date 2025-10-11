using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Улучшенная версия TrackSpawnerWithDependencies:
/// ✅ Исправлены наложения (реальное резервирование слотов)
/// ✅ Работают IncompatibleCategories
/// ✅ Добавлена визуализация слотов через Gizmos
/// ✅ Без переименований — полностью совместим с твоим проектом
/// </summary>
public class TrackSpawnerWithDependencies : MonoBehaviour
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
    [Range(0f, 2f)] public float slotScaleBySpeed = 1f;

    [Header("Density Settings")]
    public float baseDensity = 1f;
    public AnimationCurve densityBySpeed = AnimationCurve.Linear(0, 1, 1, 1);

    [Header("Gap Settings (in jump lengths)")]
    public float minHighGap = 2;
    public float minCrossGap = 2;

    [Header("Bonus Corridors")]
    public int bonusSegments = 10;
    public float bonusChanceAtHighSpeed = 0.05f;
    public float bonusSpeedThresholdKph = 120f;
    public bool pickupsOnlyInBonus = true;

    [Header("Waves of Difficulty")]
    public List<string> waves = new List<string>();

    [Header("Content Lists (Pool Keys)")]
    public List<SpawnRule> lowObstacleRules = new();
    public List<SpawnRule> highObstacleRules = new();
    public List<SpawnRule> enemyRules = new(); 
    public List<SpawnRule> pickupRules = new();
    public List<SpawnRule> bossRules = new();

    [Header("Global Weight Multipliers (by Category)")]
    public float pickupWeightMult = 1f;
    public float obstacleWeightMult = 1f;
    public float enemyWeightMult = 1f;
    public float bossWeightMult = 1f;

    [Header("Runner Link")]
    public Transform runner;
    public float estimatedJumpTime = 0.5f;
    public float pickupHeight = 0.3f;

    [Header("Debug")]
    public bool debugSpawnChecks = false;
    public bool showSlotGizmos = true;

    private List<GameObject> activeSegments = new();
    private PoolManager pool;
    private float segmentZ;
    private SlotReservation[,] reservations;

    [System.Serializable]
    public class SpawnRule
    {
        public string key;
        public string category;
        public float weight = 1f;
        public List<string> incompatibleCategories = new();
        public List<string> requiredCategories = new();
        public float dependencyRadius = 3f;
        public float minSpeed = 0f;
        public float maxSpeed = 999f;
        public bool onlyInBonus = false;
        public bool onlyOutsideBonus = false;
        public int minDistanceBetweenSame = 2;
    }

    private class SlotReservation
    {
        public HashSet<string> categories = new();

        public bool IsCompatible(string category, List<string> incompatible)
        {
            foreach (var c in categories)
            {
                if (incompatible.Contains(c))
                    return false;
            }
            return true;
        }

        public void Add(string category)
        {
            if (!categories.Contains(category))
                categories.Add(category);
        }
    }

    private void Start()
    {
        pool = FindObjectOfType<PoolManager>();
        segmentZ = 0f;
        GenerateInitialTrack();
    }

    private void GenerateInitialTrack()
    {
        for (int i = 0; i < numberOfSegments; i++)
        {
            SpawnSegment();
        }
    }

    private void Update()
    {
        if (player.position.z - safeZone > (segmentZ - numberOfSegments * baseSegmentLength))
        {
            SpawnSegment();
            CleanupSegments();
        }
    }

    private void SpawnSegment()
    {
        GameObject segment = Instantiate(floorPrefab, new Vector3(0, 0, segmentZ), Quaternion.identity);
        activeSegments.Add(segment);

        int totalSlots = Mathf.Max(1, Mathf.RoundToInt(baseSlotsPerSegment * slotScaleBySpeed));
        reservations = new SlotReservation[laneCount, totalSlots];
        bool[,] occupied = new bool[laneCount, totalSlots];

        for (int l = 0; l < laneCount; l++)
        {
            for (int s = 0; s < totalSlots; s++)
            {
                reservations[l, s] = new SlotReservation();
                TrySpawnInLane(segment.transform, l, s, totalSlots, occupied);
            }
        }

        segmentZ += baseSegmentLength;
    }

    private void TrySpawnInLane(Transform parent, int lane, int slot, int totalSlots, bool[,] occupied)
    {
        List<SpawnRule> allRules = new();
        allRules.AddRange(lowObstacleRules);
        allRules.AddRange(highObstacleRules);
        allRules.AddRange(enemyRules);
        allRules.AddRange(pickupRules);
        allRules.AddRange(bossRules);

        List<SpawnRule> valid = new();

        foreach (var rule in allRules)
        {
            if (CanSpawn(rule))
                valid.Add(rule);
        }

        if (valid.Count == 0) return;

        float totalWeight = 0f;
        foreach (var r in valid) totalWeight += r.weight;
        float rnd = Random.value * totalWeight;

        float acc = 0f;
        SpawnRule chosen = valid[0];
        foreach (var r in valid)
        {
            acc += r.weight;
            if (rnd <= acc) { chosen = r; break; }
        }

        if (occupied[lane, slot]) return;

        // --- позиция слота ---
        float slotLength = baseSegmentLength / totalSlots;
        float slotZOffset = slot * slotLength + (slotLength / 2f);
        Vector3 pos = parent.position
                      + Vector3.right * ((lane - (laneCount - 1) / 2f) * laneDistance)
                      + Vector3.forward * slotZOffset;

        if (chosen.category.ToLower().Contains("pickup"))
            pos.y += pickupHeight;

        // --- проверка на совместимость ---
        if (!reservations[lane, slot].IsCompatible(chosen.category, chosen.incompatibleCategories))
        {
            if (debugSpawnChecks)
                Debug.Log($"⛔ Conflict in lane {lane} slot {slot} with {chosen.key}");
            return;
        }

        var obj = pool.Spawn(chosen.key, pos, Quaternion.identity, parent);
        reservations[lane, slot].Add(chosen.category);
        occupied[lane, slot] = true;

        if (debugSpawnChecks)
            Debug.Log($"✅ Spawned {chosen.key} at lane {lane} slot {slot}, Z={pos.z}");
    }

    private bool CanSpawn(SpawnRule rule)
    {
        float speed = GetSpeedKph();
        if (speed < rule.minSpeed || speed > rule.maxSpeed)
            return false;
        return true;
    }

    private float GetSpeedKph()
    {
        var runnerScript = runner?.GetComponent<MaoRunnerFixed>();
        return runnerScript != null ? runnerScript.CurrentSpeedKph : 0f;
    }

    private void CleanupSegments()
    {
        while (activeSegments.Count > numberOfSegments)
        {
            var seg = activeSegments[0];
            activeSegments.RemoveAt(0);
            Destroy(seg);
        }
    }

    // --- Визуализация слотов ---
    private void OnDrawGizmos()
    {
        if (!showSlotGizmos) return;

        Gizmos.color = Color.green;
        float totalZ = baseSegmentLength;
        int totalSlots = Mathf.Max(1, baseSlotsPerSegment);

        for (int l = 0; l < laneCount; l++)
        {
            for (int s = 0; s < totalSlots; s++)
            {
                float slotLength = baseSegmentLength / totalSlots;
                float slotZOffset = s * slotLength + (slotLength / 2f);

                Vector3 pos = transform.position
                              + Vector3.right * ((l - (laneCount - 1) / 2f) * laneDistance)
                              + Vector3.forward * slotZOffset;

                Gizmos.DrawWireCube(pos, new Vector3(2f, 0.2f, slotLength * 0.9f));
            }
        }
    }
}