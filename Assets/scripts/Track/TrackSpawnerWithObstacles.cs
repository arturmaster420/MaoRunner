using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-5)]
public class TrackSpawnerWithDependencies : MonoBehaviour
{
    // ===================== ENUMS =====================
    public enum Category { ObstacleLow, ObstacleHigh, Enemy, Pickup, Boss }

    // ===================== RULES =====================
    [Serializable]
    public class SpawnRule
    {
        [Header("Base")]
        public string key;
        public Category category = Category.ObstacleLow;
        [Min(0f)] public float weight = 1f;

        [Tooltip("На каких линиях разрешено спавнить (0=левая,1=центр,2=правая...). Пусто = разрешены все.")]
        public List<int> allowedLanes = new List<int>();

        [Header("Dependencies")]
        [Tooltip("Категории, с которыми нельзя соседствовать в этом слоте (и в радиусе).")]
        public List<Category> incompatibleCategories = new List<Category>();
        [Tooltip("Категории, которые обязаны присутствовать в радиусе (если список не пуст).")]
        public List<Category> requiresCategories = new List<Category>();
        [Tooltip("Радиус проверки зависимостей (в слотах вперёд/назад). 0 — только текущий слот.")]
        [Min(0)] public int dependencyRadius = 0;

        [Header("Conditions")]
        [Tooltip("Мин. скорость (км/ч), чтобы правило могло сработать.")]
        public float minSpeed = 0f;
        [Tooltip("Макс. скорость (км/ч). 999 — нет ограничений сверху.")]
        public float maxSpeed = 999f;

        [Tooltip("Спавнить только в бонус-коридоре.")]
        public bool onlyInBonus = false;
        [Tooltip("Спавнить только вне бонус-коридора.")]
        public bool onlyOutsideBonus = false;

        [Tooltip("Мин. дистанция между экземплярами ЭТОЙ ЖЕ категории (в слотах, по оси Z).")]
        [Min(0)] public int minDistanceBetweenSame = 0;
    }

    // Волны сложности — просто контейнер с множителями
    [Serializable]
    public class Wave
    {
        [Tooltip("Начинается с этой дистанции (по Z игрока).")]
        public float startAtDistance = 0f;
        [Range(0f, 5f)] public float pickupWeightMult = 1f;
        [Range(0f, 5f)] public float obstacleWeightMult = 1f;
        [Range(0f, 5f)] public float enemyWeightMult = 1f;
        [Range(0f, 5f)] public float bossWeightMult = 1f;
    }

    // Обёртки списков правил по категориям
    [Serializable] public class RuleList { public List<SpawnRule> items = new List<SpawnRule>(); }

    // ===================== INSPECTOR =====================

    [Header("Track Settings")]
    public GameObject floorPrefab;
    [Min(1)] public int numberOfSegments = 10;
    [Min(4f)] public float baseSegmentLength = 12f;
    public Transform player;
    [Min(0f)] public float safeZone = 50f;

    [Header("Lane Settings")]
    [Min(1)] public int laneCount = 3;
    [Min(1f)] public float laneDistance = 3f;

    [Header("Dynamic Slot Settings")]
    [Min(1)] public int baseSlotsPerSegment = 2;
    [Tooltip("X = скорость (км/ч) [0..300], Y = множитель слотов [0.5..2]. Итог: slots = round(baseSlotsPerSegment * curveY).")]
    public AnimationCurve slotScaleBySpeed = AnimationCurve.Linear(0, 1, 300, 1);

    [Header("Density Settings")]
    [Tooltip("Базовая плотность: 0..1. Это вероятность попытки заполнить слот в линии.")]
    [Range(0f, 1f)] public float baseDensity = 0.5f;
    [Tooltip("Добавка к плотности по скорости: X = км/ч [0..300], Y = добавка [0..1].")]
    public AnimationCurve densityBySpeed = AnimationCurve.Linear(0, 0, 300, 0);

    [Header("Gap Settings (in jump lengths)")]
    [Tooltip("Мин. разрыв для HIGH (в длинах прыжка). 1 = одна длина прыжка.")]
    [Min(0f)] public float minHighGap = 1f;
    [Tooltip("Мин. разрыв между LOW и HIGH (в длинах прыжка).")]
    [Min(0f)] public float minCrossGap = 1f;

    [Header("Bonus Corridors")]
    [Min(0)] public int bonusSegments = 15;
    [Range(0f, 1f)] public float bonusChanceAtHighSpeed = 0.05f;
    [Tooltip("Порог скорости (км/ч), после которого может стартовать бонус-коридор.")]
    public float bonusSpeedThresholdKph = 120f;
    public bool pickupsOnlyInBonus = true;

    [Header("Waves of Difficulty")]
    public List<Wave> waves = new List<Wave>();

    [Header("Content Lists (Pool Keys)")]
    public RuleList lowObstacleRules = new RuleList();
    public RuleList highObstacleRules = new RuleList();
    public RuleList enemyRules = new RuleList();
    public RuleList pickupRules = new RuleList();
    public RuleList bossRules = new RuleList();

    [Header("Global Weight Multipliers (by Category)")]
    [Range(0f, 5f)] public float pickupWeightMult = 1f;
    [Range(0f, 5f)] public float obstacleWeightMult = 1f;
    [Range(0f, 5f)] public float enemyWeightMult = 1f;
    [Range(0f, 5f)] public float bossWeightMult = 1f;

    [Header("Runner Link")]
    [Tooltip("Ссылка на объект игрока с MaoRunnerFixed.")]
    public MaoRunnerFixed runner;
    [Tooltip("Оценочное время прыжка (сек) — нужно, чтобы перевести jump lengths в слоты.")]
    [Min(0.1f)] public float estimatedJumpTime = 0.35f;
    [Tooltip("Высота пикапов над полом (метры).")]
    public float pickupHeight = 0.3f;

    [Header("Debug")]
    public bool debugSpawnChecks = false;
    public bool showSlotGizmos = false;

    // ===================== RUNTIME =====================

    private readonly List<GameObject> activeSegments = new List<GameObject>();
    private float spawnZ;
    private System.Random rnd;
    private int currentBonusLeft = 0;

    // Резервация категорий в слотах: [lane, slot] -> набор категорий
    private bool[,] occupied; // true если слот занят чем угодно
    private HashSet<Category>[,] reservations;

    // ===================== UNITY =====================

    private void Awake()
    {
        rnd = new System.Random(Environment.TickCount);
    }

    private void Start()
    {
        spawnZ = 0f;

        for (int i = 0; i < numberOfSegments; i++)
            SpawnSegment();
    }

    private void Update()
    {
        if (player == null) return;

        // Поддерживаем «килим» из сегментов
        CleanupSegments();

        while (activeSegments.Count < numberOfSegments)
            SpawnSegment();
    }

    private void OnDrawGizmos()
    {
        if (!showSlotGizmos) return;

        Gizmos.color = Color.green;

        float segLen = baseSegmentLength;
        int slots = Mathf.Max(1, baseSlotsPerSegment);
        float slotLen = segLen / slots;

        // рисуем сетку слотов для ТЕКУЩЕГО объекта (первого сегмента)
        for (int lane = 0; lane < laneCount; lane++)
        {
            for (int s = 0; s < slots; s++)
            {
                float x = ((float)lane - (laneCount - 1) * 0.5f) * laneDistance;
                float z = s * slotLen + slotLen * 0.5f;

                Vector3 pos = transform.position + new Vector3(x, 0f, z);
                Gizmos.DrawWireCube(pos, new Vector3(2f, 0.2f, slotLen * 0.9f));
            }
        }
    }

    // ===================== SEGMENTS =====================

    private void SpawnSegment()
    {
        // 1) Создаём пол
        var segGO = Instantiate(floorPrefab, new Vector3(0, 0, spawnZ), Quaternion.identity, transform);
        activeSegments.Add(segGO);

        // 2) Вычисляем параметры слотов под текущую скорость
        float kph = GetSpeedKphSafe();
        int slots = ComputeSlotsPerSegment(kph);
        float density = ComputeDensity(kph);
        bool isBonus = TryStartOrContinueBonus(kph);

        if (debugSpawnChecks)
            Debug.Log($"[SEGMENT] z={spawnZ} slots={slots} density={density:F2} bonus={isBonus}");

        // 3) Инициализируем матрицу занятости
        occupied = new bool[laneCount, slots];
        reservations = new HashSet<Category>[laneCount, slots];
        for (int l = 0; l < laneCount; l++)
            for (int s = 0; s < slots; s++)
                reservations[l, s] = new HashSet<Category>();

        // 4) Пытаемся заполнить слоты по категориям (порядок: препятствия, враги, пикапы, боссы)
        TryFillCategory(segGO.transform, slots, density, isBonus, Category.ObstacleLow, lowObstacleRules.items);
        TryFillCategory(segGO.transform, slots, density, isBonus, Category.ObstacleHigh, highObstacleRules.items);
        TryFillCategory(segGO.transform, slots, density, isBonus, Category.Enemy, enemyRules.items);
        TryFillCategory(segGO.transform, slots, density, isBonus, Category.Pickup, pickupRules.items);
        TryFillCategory(segGO.transform, slots, density, isBonus, Category.Boss, bossRules.items);

        // 5) Готово — двигаем «хвост» трека
        spawnZ += baseSegmentLength;
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

    // ===================== FILL =====================

    private void TryFillCategory(Transform parent, int totalSlots, float density, bool isBonus, Category cat, List<SpawnRule> rules)
    {
        if (rules == null || rules.Count == 0) return;

        // Собираем кандидатов + их веса с учётом глобальных множителей и волны
        var (candidates, weights) = BuildWeightedList(rules, cat);

        if (candidates.Count == 0) return;

        // Равномерно обходим слоты в случайном порядке
        var slotOrder = BuildRandomOrder(totalSlots);

        for (int sIndex = 0; sIndex < slotOrder.Count; sIndex++)
        {
            int slot = slotOrder[sIndex];

            for (int lane = 0; lane < laneCount; lane++)
            {
                if (occupied[lane, slot]) continue;
                if (UnityEngine.Random.value > density) continue;

                // Выбираем правило с весами
                var rule = WeightedPick(candidates, weights);
                if (rule == null) continue;

                // Проверяем all conditions
                if (!CanSpawn(rule, isBonus)) continue;
                if (!LaneAllowed(rule, lane)) continue;
                if (!PassGaps(rule, slot, totalSlots)) continue;
                if (!PassDependencies(rule, lane, slot)) continue;
                if (!PassSameMinDistance(rule, lane, slot)) continue;

                // Спавн
                Vector3 pos = SlotToWorld(slot, lane, totalSlots);
                SpawnFromPool(rule.key, pos, parent);
                occupied[lane, slot] = true;
                reservations[lane, slot].Add(rule.category);

                if (debugSpawnChecks)
                    Debug.Log($"[✓] Spawned {rule.key} (cat={rule.category}) lane={lane} slot={slot} z={pos.z:F1}");
            }
        }
    }

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

    // Разрывы по длине прыжка (слоты)
    private bool PassGaps(SpawnRule rule, int slot, int totalSlots)
    {
        if (rule.category == Category.ObstacleHigh)
        {
            if (HasMinGap(slot, totalSlots, Mathf.Max(1, JumpLengthsToSlots(minHighGap, totalSlots)))) return true;
            else return false;
        }
        if (rule.category == Category.ObstacleLow)
        {
            if (HasMinGap(slot, totalSlots, Mathf.Max(1, JumpLengthsToSlots(minCrossGap, totalSlots)))) return true;
            else return false;
        }
        return true;
    }

    private bool PassDependencies(SpawnRule rule, int lane, int slot)
    {
        int r = Mathf.Max(0, rule.dependencyRadius);

        // incompatible
        foreach (var cat in rule.incompatibleCategories)
            if (HasCategoryAround(lane, slot, cat, r)) return false;

        // requires
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
        {
            for (int s = s0; s <= s1; s++)
            {
                if (reservations[l, s].Contains(cat))
                    return true;
            }
        }
        return false;
    }

    private bool HasMinGap(int slot, int totalSlots, int minSlotsGap)
    {
        // Проверяем, что слева и справа есть минимум пустых слотов
        for (int l = 0; l < laneCount; l++)
        {
            // влево
            for (int s = Mathf.Max(0, slot - minSlotsGap); s < slot; s++)
                if (occupied[l, s]) return false;

            // вправо
            for (int s = slot + 1; s <= Mathf.Min(totalSlots - 1, slot + minSlotsGap); s++)
                if (occupied[l, s]) return false;
        }
        return true;
    }

    // ===================== HELPERS =====================

    private (List<SpawnRule>, List<float>) BuildWeightedList(List<SpawnRule> src, Category cat)
    {
        var outRules = new List<SpawnRule>();
        var outWeights = new List<float>();

        float mult = 1f;
        switch (cat)
        {
            case Category.Pickup: mult = pickupWeightMult; break;
            case Category.ObstacleLow:
            case Category.ObstacleHigh:
                mult = obstacleWeightMult; break;
            case Category.Enemy: mult = enemyWeightMult; break;
            case Category.Boss: mult = bossWeightMult; break;
        }

        // учтём текущую волну (если настроена)
        var waveMult = GetWaveMultipliers();
        if (cat == Category.Pickup) mult *= waveMult.pickup;
        if (cat == Category.ObstacleLow || cat == Category.ObstacleHigh) mult *= waveMult.obstacle;
        if (cat == Category.Enemy) mult *= waveMult.enemy;
        if (cat == Category.Boss) mult *= waveMult.boss;

        foreach (var r in src)
        {
            float w = Mathf.Max(0f, r.weight) * mult;
            if (w <= 0f) continue;

            outRules.Add(r);
            outWeights.Add(w);
        }

        return (outRules, outWeights);
    }

    private (float pickup, float obstacle, float enemy, float boss) GetWaveMultipliers()
    {
        if (waves == null || waves.Count == 0) return (1f, 1f, 1f, 1f);

        float z = player != null ? player.position.z : 0f;
        Wave last = null;
        foreach (var w in waves)
            if (z >= w.startAtDistance) last = w;

        if (last == null) return (1f, 1f, 1f, 1f);
        return (last.pickupWeightMult, last.obstacleWeightMult, last.enemyWeightMult, last.bossWeightMult);
    }

    private List<int> BuildRandomOrder(int n)
    {
        var list = new List<int>(n);
        for (int i = 0; i < n; i++) list.Add(i);

        // Fisher–Yates
        for (int i = n - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    private SpawnRule WeightedPick(List<SpawnRule> candidates, List<float> weights)
    {
        if (candidates.Count == 0) return null;
        float total = 0f;
        for (int i = 0; i < weights.Count; i++) total += weights[i];
        if (total <= 0f) return null;

        float r = (float)rnd.NextDouble() * total;
        float acc = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            acc += weights[i];
            if (r <= acc) return candidates[i];
        }
        return candidates[candidates.Count - 1];
    }

    private int ComputeSlotsPerSegment(float kph)
    {
        float factor = Mathf.Clamp(slotScaleBySpeed.Evaluate(Mathf.Clamp(kph, 0f, 300f)), 0.5f, 2f);
        int slots = Mathf.Max(1, Mathf.RoundToInt(baseSlotsPerSegment * factor));
        return slots;
    }

    private float ComputeDensity(float kph)
    {
        float add = Mathf.Clamp01(densityBySpeed.Evaluate(Mathf.Clamp(kph, 0f, 300f)));
        float d = Mathf.Clamp01(baseDensity + add);
        return d;
    }

    private bool TryStartOrContinueBonus(float kph)
    {
        if (currentBonusLeft > 0)
        {
            currentBonusLeft--;
            return true;
        }

        if (kph >= bonusSpeedThresholdKph && UnityEngine.Random.value < bonusChanceAtHighSpeed)
        {
            currentBonusLeft = Mathf.Max(1, bonusSegments);
            currentBonusLeft--;
            return true;
        }

        return false;
    }

    private Vector3 SlotToWorld(int slot, int lane, int totalSlots)
    {
        float slotLen = baseSegmentLength / totalSlots;
        float x = (lane - (laneCount - 1) * 0.5f) * laneDistance;
        float z = spawnZ + slot * slotLen + slotLen * 0.5f;
        float y = 0f;

        return new Vector3(x, y, z);
    }

    private void SpawnFromPool(string key, Vector3 pos, Transform parent)
    {
        // Требуется существующий PoolManager с ключом key
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Spawn(key, pos, Quaternion.identity, parent);
        }
        else
        {
            // fallback — прямой Instantiate по Resources, если кто-то так хранит
            var go = new GameObject(key);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
        }
    }

    // Перевод длины прыжка -> в кол-во слотов
    private int JumpLengthsToSlots(float jumpLengths, int totalSlots)
    {
        if (runner == null) return Mathf.RoundToInt(jumpLengths); // грубо, но без runner
        float speed = Mathf.Max(0.1f, runner.forwardSpeed);       // m/s (у нас forwardSpeed в m/s)
        float jumpDist = speed * Mathf.Max(0.1f, estimatedJumpTime); // метры за прыжок
        float slotLenMeters = baseSegmentLength / Mathf.Max(1, totalSlots);

        int slots = Mathf.Max(0, Mathf.RoundToInt((jumpDist * jumpLengths) / slotLenMeters));
        return slots;
    }

    private float GetSpeedKphSafe()
    {
        if (runner == null && player != null)
            runner = player.GetComponent<MaoRunnerFixed>();

        if (runner == null) return 0f;

        // forwardSpeed у тебя в м/с -> км/ч
        return Mathf.Max(0f, runner.forwardSpeed * 3.6f);
    }
}