using System.Collections.Generic;
using UnityEngine;

public class TrackSpawnerWithObstacles : MonoBehaviour
{
    [Header("Track Settings")]
    public GameObject floorPrefab;
    public int numberOfSegments = 10;
    public float segmentLength = 10f;
    public Transform player;

    [Tooltip("Базовая дистанция удаления сегментов (при малой скорости)")]
    public float baseSafeZone = 50f;

    [Tooltip("Базовое расстояние до спавна нового сегмента")]
    public float baseSpawnBuffer = 10f;

    private float spawnZ = 0f;
    private Queue<GameObject> activeSegments = new Queue<GameObject>();
    private MaoRunnerFixed maoRunner;

    // ========================= ОБСТАКЛЫ =========================
    [System.Serializable]
    public class ObstacleEntry
    {
        public string key;
        public GameObject prefab;
        [Range(0f, 1f)] public float chance = 0.4f;
    }

    [Header("Obstacles")]
    public List<ObstacleEntry> obstaclePrefabs;
    public float minZOffset = 2f;
    public float maxZOffset = 8f;
    public float laneDistance = 3f;

    // ========================= COINS =========================
    [System.Serializable]
    public class CoinEntry
    {
        public string key;
        public GameObject prefab;
        public int value = 1;
        [Range(0f, 1f)] public float chance = 0.3f;
    }

    [Header("Coins")]
    public List<CoinEntry> coinPrefabs;
    public float coinHeight = 0.8f;

    // ========================= ENERGY =========================
    [System.Serializable]
    public class EnergyEntry
    {
        public string key;
        public GameObject prefab;
        public int value = 1;
        [Range(0f, 1f)] public float chance = 0.3f;
    }

    [Header("Energy Spheres")]
    public List<EnergyEntry> energyPrefabs;
    public float energyHeight = 1f;

    // ========================= ENEMIES =========================
    [System.Serializable]
    public class EnemyEntry
    {
        public string key;
        public GameObject prefab;
        [Range(0f, 1f)] public float chance = 0.2f; // тип А
    }

    [System.Serializable]
    public class SpecialEnemyEntry
    {
        public string key;
        public GameObject prefab;
        public float spawnEveryMeters = 1000f; // тип Б
    }

    [Header("Enemies")]
    public List<EnemyEntry> enemyPrefabs;
    public List<SpecialEnemyEntry> specialEnemies;

    private float distanceTravelled = 0f;

    // ========================= ДИНАМИКА ПЛОТНОСТИ =========================
    [Header("Dynamic Density Balancing")]
    [Tooltip("Насколько сильно уменьшается шанс появления объектов при высокой скорости (0 = нет, 1 = сильно)")]
    [Range(0f, 1f)] public float densityReductionFactor = 0.6f;

    [Tooltip("Минимальный множитель плотности на максимальной скорости")]
    [Range(0.1f, 1f)] public float minDensityMultiplier = 0.4f;

    void Start()
    {
        maoRunner = FindObjectOfType<MaoRunnerFixed>();
    }

    void Update()
    {
        if (player == null) return;

        distanceTravelled = player.position.z;

        // адаптация параметров под скорость
        float currentSpeed = maoRunner != null ? maoRunner.forwardSpeed : 10f;
        float speedFactor = maoRunner != null ? Mathf.Clamp01(currentSpeed / maoRunner.maxForwardSpeed) : 0f;

        float safeZone = Mathf.Lerp(baseSafeZone, baseSafeZone * 2f, speedFactor);
        float spawnBuffer = Mathf.Lerp(baseSpawnBuffer, baseSpawnBuffer * 2f, speedFactor);

        if (activeSegments.Count < numberOfSegments || player.position.z + spawnBuffer > spawnZ - segmentLength)
        {
            SpawnSegment(speedFactor);
        }

        DeleteOldSegment(safeZone);
    }

    private void SpawnSegment(float speedFactor)
    {
        GameObject segment = Instantiate(floorPrefab, Vector3.forward * spawnZ, Quaternion.identity);
        activeSegments.Enqueue(segment);
        spawnZ += segmentLength;

        float densityMultiplier = Mathf.Lerp(1f, minDensityMultiplier, speedFactor * densityReductionFactor);

        SpawnObstacles(segment.transform, densityMultiplier);
        SpawnCoins(segment.transform, densityMultiplier);
        SpawnEnergy(segment.transform, densityMultiplier);
        SpawnEnemies(segment.transform, densityMultiplier);
    }

    private void DeleteOldSegment(float safeZone)
    {
        if (activeSegments.Count == 0) return;

        if (player.position.z - safeZone > activeSegments.Peek().transform.position.z)
        {
            Destroy(activeSegments.Dequeue());
        }
    }

    private void SpawnObstacles(Transform parent, float densityMultiplier)
    {
        foreach (var entry in obstaclePrefabs)
        {
            float adjustedChance = entry.chance * densityMultiplier;
            if (Random.value < adjustedChance)
            {
                int lane = Random.Range(-1, 2);
                float zOffset = Random.Range(minZOffset, maxZOffset);
                Vector3 pos = parent.position + Vector3.forward * zOffset + Vector3.right * lane * laneDistance;
                Instantiate(entry.prefab, pos, Quaternion.identity, parent);
            }
        }
    }

    private void SpawnCoins(Transform parent, float densityMultiplier)
    {
        foreach (var entry in coinPrefabs)
        {
            float adjustedChance = entry.chance * densityMultiplier;
            if (Random.value < adjustedChance)
            {
                int lane = Random.Range(-1, 2);
                Vector3 pos = parent.position + Vector3.forward * Random.Range(minZOffset, maxZOffset) +
                              Vector3.right * lane * laneDistance + Vector3.up * coinHeight;
                var coin = Instantiate(entry.prefab, pos, Quaternion.identity, parent);

                var pickup = coin.GetComponent<Pickup>();
                if (pickup != null)
                {
                    pickup.type = Pickup.PickupType.Coin;
                    pickup.value = entry.value;
                }
            }
        }
    }

    private void SpawnEnergy(Transform parent, float densityMultiplier)
    {
        foreach (var entry in energyPrefabs)
        {
            float adjustedChance = entry.chance * densityMultiplier;
            if (Random.value < adjustedChance)
            {
                int lane = Random.Range(-1, 2);
                Vector3 pos = parent.position + Vector3.forward * Random.Range(minZOffset, maxZOffset) +
                              Vector3.right * lane * laneDistance + Vector3.up * energyHeight;
                var energy = Instantiate(entry.prefab, pos, Quaternion.identity, parent);

                var pickup = energy.GetComponent<Pickup>();
                if (pickup != null)
                {
                    pickup.type = Pickup.PickupType.Energy;
                    pickup.value = entry.value;
                }
            }
        }
    }

    private void SpawnEnemies(Transform parent, float densityMultiplier)
    {
        foreach (var entry in enemyPrefabs)
        {
            float adjustedChance = entry.chance * densityMultiplier;
            if (Random.value < adjustedChance)
            {
                int lane = Random.Range(-1, 2);
                Vector3 pos = parent.position + Vector3.forward * Random.Range(minZOffset, maxZOffset) +
                              Vector3.right * lane * laneDistance;
                Instantiate(entry.prefab, pos, Quaternion.identity, parent);
            }
        }

        foreach (var entry in specialEnemies)
        {
            if (distanceTravelled > 0 && Mathf.FloorToInt(distanceTravelled) % Mathf.FloorToInt(entry.spawnEveryMeters) == 0)
            {
                int lane = Random.Range(-1, 2);
                Vector3 pos = parent.position + Vector3.forward * Random.Range(minZOffset, maxZOffset) +
                              Vector3.right * lane * laneDistance;
                Instantiate(entry.prefab, pos, Quaternion.identity, parent);
            }
        }
    }
}
