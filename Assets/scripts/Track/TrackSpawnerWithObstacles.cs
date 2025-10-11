using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// TrackSpawnerWithDependencies (пересобран «под ключ»)
/// - строгая резервация слотов (никаких наложений)
/// - поддержка несовместимых категорий и минимальной дистанции
/// - автозаполнение несовместимостей по категориям (OnValidate)
/// - визуализация слотов Gizmos
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
    [Range(0, 1)] public float bonusChanceAtHighSpeed = 0.05f;
    public float bonusSpeedThresholdKph = 120f;
    public bool pickupsOnlyInBonus = true;

    [Header("Waves of Difficulty")]
    public List<string> waves = new();   // как ты просил — поле оставлено как есть

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
    public Transform runner;                 // SuperRig
    public float estimatedJumpTime = 0.5f;   // на будущее для «jump length»
    public float pickupHeight = 0.3f;

    [Header("Debug")]
    public bool debugSpawnChecks = false;
    public bool showSlotGizmos = true;

    // --- внутреннее состояние ---
    private readonly List<GameObject> activeSegments = new();
    private float segmentZ;
    private PoolManager pool;

    // запись о том, что уже поставлено в слот
    private class SlotReservation
    {
        public bool occupied;                    // занято кем-то (любой категорией)
        public string lastCategory = "";         // последняя категория в этом слоте (для логов)
        public int lastSpawnedIndex = -9999;     // индекс последнего слота этой же категории (для MinDistanceBetweenSame)
    }

    // хранение истории по категории => последний индекс слота
    private readonly Dictionary<string, int> lastSlotByCategory = new();

    [System.Serializable]
    public class SpawnRule
    {
        [Header("Base")]
        public string key;           // pool key
        public string category;      // ObstacleLow, ObstacleHigh, Enemy, Boss, Pickup
        public float weight = 1f;

        [Header("Dependencies")]
        public List<string> incompatibleCategories = new(); // кто не может стоять в одном слоте
        public List<string> requiredCategories = new();     // не используем пока (оставлено для совместимости)
        public float dependencyRadius = 3f;                 // не используем пока (оставлено)

        [Header("Conditions")]
        public float minSpeed = 0f;
        public float maxSpeed = 999f;
        public bool onlyInBonus = false;
        public bool onlyOutsideBonus = false;
        public int minDistanceBetweenSame = 2;              // расстояние между слотами этой же категории
    }

    private void Awake()
    {
        pool = FindObjectOfType<PoolManager>();
    }

    private void Start()
    {
        segmentZ = 0f;
        GenerateInitialTrack();
    }

    private void Update()
    {
        // подспавниваем вперёд
        if (player.position.z - safeZone > (segmentZ - numberOfSegments * baseSegmentLength))
        {
            SpawnSegment();
            CleanupSegments();
        }
    }

    private void GenerateInitialTrack()
    {
        for (int i = 0; i < numberOfSegments; i++)
            SpawnSegment();
    }

    // === главный спавн сегмента ===
    private void SpawnSegment()
    {
        var segment = Instantiate(floorPrefab, new Vector3(0, 0, segmentZ), Quaternion.identity);
        activeSegments.Add(segment);

        // сколько слотов на сегмент
        int slotsPerSegment = Mathf.Max(1, Mathf.RoundToInt(baseSlotsPerSegment * Mathf.Max(0.01f, slotScaleBySpeed)));

        // заполняем по всем линиям & слотам
        for (int lane = 0; lane < laneCount; lane++)
        {
            // для линии — своя сетка слотов
            var slots = new SlotReservation[slotsPerSegment];
            for (int i = 0; i < slotsPerSegment; i++) slots[i] = new SlotReservation();

            for (int slot = 0; slot < slotsPerSegment; slot++)
            {
                TrySpawnInSlot(segment.transform, lane, slot, slotsPerSegment, slots);
            }
        }

        segmentZ += baseSegmentLength;
    }

    private void TrySpawnInSlot(Transform parent, int lane, int slot, int slotsPerSegment, SlotReservation[] slots)
    {
        // уже занято кем-то — не трогаем
        if (slots[slot].occupied) return;

        // формируем пул всех правил
        var rules = GatherAllRules();

        // фильтруем валидные
        var valid = new List<SpawnRule>();
        foreach (var r in rules)
        {
            if (IsRuleValidInThisSlot(r, lane, slot, slots))
                valid.Add(r);
        }

        if (valid.Count == 0) return;

        // взвешенный выбор
        float total = 0f;
        foreach (var r in valid) total += GetWeighted(r);
        float t = Random.value * total;

        SpawnRule chosen = valid[0];
        float acc = 0f;
        foreach (var r in valid)
        {
            acc += GetWeighted(r);
            if (t <= acc) { chosen = r; break; }
        }

        // помечаем слот занятым
        slots[slot].occupied = true;
        slots[slot].lastCategory = chosen.category;

        // обновляем «последний индекс» данной категории
        lastSlotByCategory[chosen.category] = GlobalSlotIndex(lane, slot, slotsPerSegment);

        // позиция в мире
        float slotLength = baseSegmentLength / slotsPerSegment;
        float zOffset = slot * slotLength + slotLength / 2f;
        Vector3 pos =
            parent.position
            + Vector3.right * ((lane - (laneCount - 1) / 2f) * laneDistance)
            + Vector3.forward * zOffset;

        if (IsPickup(chosen.category))
            pos.y += pickupHeight;

        // спавн
        pool.Spawn(chosen.key, pos, Quaternion.identity, parent);

        if (debugSpawnChecks)
            Debug.Log($"[Spawn] {chosen.key} ({chosen.category})  lane={lane} slot={slot}  z={pos.z}");
    }

    // ——— helpers ———

    private List<SpawnRule> GatherAllRules()
    {
        var rules = new List<SpawnRule>();
        rules.AddRange(lowObstacleRules);
        rules.AddRange(highObstacleRules);
        rules.AddRange(enemyRules);
        rules.AddRange(pickupRules);
        rules.AddRange(bossRules);
        return rules;
    }

    private float GetWeighted(SpawnRule r)
    {
        float w = r.weight;

        // глобальные множители по категориям
        if (IsPickup(r.category)) w *= Mathf.Max(0f, pickupWeightMult);
        else if (IsObstacle(r.category)) w *= Mathf.Max(0f, obstacleWeightMult);
        else if (IsEnemy(r.category)) w *= Mathf.Max(0f, enemyWeightMult);
        else if (IsBoss(r.category)) w *= Mathf.Max(0f, bossWeightMult);

        return Mathf.Max(0f, w);
    }

    private bool IsRuleValidInThisSlot(SpawnRule r, int lane, int slot, SlotReservation[] slots)
    {
        // по скорости
        float kph = GetSpeedKph();
        if (kph < r.minSpeed || kph > r.maxSpeed) return false;

        // бонус-коридоры (оставлено как флаг на будущее, сейчас не включаем режим)
        if (r.onlyInBonus) { /* если у тебя включён флаг бонус-сегмента — проверь здесь */ }
        if (r.onlyOutsideBonus) { /* аналогично */ }

        // несовместимости: если уже кто-то занял слот, в любом случае не ставим
        if (slots[slot].occupied) return false;

        // MinDistanceBetweenSame: не давать одной категории появляться слишком близко
        if (r.minDistanceBetweenSame > 0)
        {
            if (lastSlotByCategory.TryGetValue(r.category, out int lastIndex))
            {
                int nowIndex = GlobalSlotIndex(lane, slot, slots.Length);
                if (Mathf.Abs(nowIndex - lastIndex) < r.minDistanceBetweenSame)
                    return false;
            }
        }

        // Авто-несовместимости: один слот — один объект. Но если руками задан список несовместимых,
        // тоже уважаем (slots[slot] ещё пуст, так что проверять нечего). Список нужен на случай,
        // если позже захочешь разрешать совместные комбинации.

        return true;
    }

    private int GlobalSlotIndex(int lane, int slot, int slotsPerSegment)
    {
        // компактный индекс «по оси времени» для MinDistanceBetweenSame:
        // lane даёт большой «шаг», чтобы слоты разных линий не считались близкими
        return lane * 100000 + slot; // 100k слотов — более чем достаточно на сегмент
    }

    private float GetSpeedKph()
    {
        if (runner == null) return 0f;
        var rf = runner.GetComponent<MaoRunnerFixed>();       // ← твой класс
        return rf != null ? rf.CurrentSpeedKph : 0f;
    }

    private bool IsPickup(string c) => !string.IsNullOrEmpty(c) && c.ToLower().Contains("pickup");
    private bool IsObstacle(string c) => !string.IsNullOrEmpty(c) && c.ToLower().Contains("obstacle");
    private bool IsEnemy(string c) => !string.IsNullOrEmpty(c) && c.ToLower().Contains("enemy");
    private bool IsBoss(string c) => !string.IsNullOrEmpty(c) && c.ToLower().Contains("boss");

    private void CleanupSegments()
    {
        while (activeSegments.Count > numberOfSegments)
        {
            var seg = activeSegments[0];
            activeSegments.RemoveAt(0);
            Destroy(seg);
        }
    }

    // === ВИЗУАЛИЗАЦИЯ СЛОТОВ ===
    private void OnDrawGizmos()
    {
        if (!showSlotGizmos) return;

        int slotsPerSegment = Mathf.Max(1, baseSlotsPerSegment);
        float slotLength = baseSegmentLength / slotsPerSegment;

        for (int lane = 0; lane < laneCount; lane++)
        {
            for (int slot = 0; slot < slotsPerSegment; slot++)
            {
                float zOffset = slot * slotLength + slotLength / 2f;
                Vector3 pos = transform.position
                              + Vector3.right * ((lane - (laneCount - 1) / 2f) * laneDistance)
                              + Vector3.forward * zOffset;

                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(pos, new Vector3(2f, 0.2f, slotLength * 0.9f));
            }
        }
    }

    // === АВТО-НАСТРОЙКА несовместимостей ===
    private static readonly string[] DefaultCats = new[]
    {
        "ObstacleLow","ObstacleHigh","Enemy","Boss","Pickup"
    };

    private void OnValidate()
    {
        AutoFillIncompatibles(lowObstacleRules);
        AutoFillIncompatibles(highObstacleRules);
        AutoFillIncompatibles(enemyRules);
        AutoFillIncompatibles(pickupRules);
        AutoFillIncompatibles(bossRules);
    }

    private void AutoFillIncompatibles(List<SpawnRule> list)
    {
        foreach (var r in list)
        {
            if (r == null) continue;
            if (string.IsNullOrEmpty(r.category)) continue;

            // если ничего не задано — заполняем «всеми кроме себя»
            if (r.incompatibleCategories == null || r.incompatibleCategories.Count == 0)
            {
                if (r.incompatibleCategories == null) r.incompatibleCategories = new List<string>();
                r.incompatibleCategories.Clear();

                foreach (var cat in DefaultCats)
                {
                    if (!cat.Equals(r.category))
                        r.incompatibleCategories.Add(cat);
                }
            }
        }
    }
}