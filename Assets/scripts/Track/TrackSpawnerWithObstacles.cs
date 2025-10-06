using System.Collections.Generic;
using UnityEngine;

public class TrackSpawnerWithObstacles : MonoBehaviour
{
    [Header("Track Settings")]
    public GameObject floorPrefab;
    public int numberOfSegments = 10;
    public float segmentLength = 10f;
    public Transform player;
    public float safeZone = 50f;

    private float spawnZ = 0f;
    private Queue<GameObject> activeSegments = new Queue<GameObject>();

    [System.Serializable]
    public class ObstacleEntry
    {
        public string key;
        public GameObject prefab;
    }

    [Header("Obstacles")]
    public List<ObstacleEntry> obstaclePrefabs;
    [Range(0f, 1f)] public float obstacleChancePerLane = 0.4f;
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
    public List<EnemyEntry> enemyPrefabs;          // обычные враги (рандом по шансу)
    public List<SpecialEnemyEntry> specialEnemies; // особые враги (по дистанции)

    private float distanceTravelled = 0f;

    private void Update()
    {
        distanceTravelled = player.position.z;

        if (activeSegments.Count < numberOfSegments)
        {
            SpawnSegment();
        }

        DeleteOldSegment();
    }

    private void SpawnSegment()
    {
        GameObject segment = Instantiate(floorPrefab, Vector3.forward * spawnZ, Quaternion.identity);
        activeSegments.Enqueue(segment);
        spawnZ += segmentLength;

        SpawnObstacles(segment.transform);
        SpawnCoins(segment.transform);
        SpawnEnergy(segment.transform);
        SpawnEnemies(segment.transform);
    }

    private void DeleteOldSegment()
    {
        if (activeSegments.Count == 0) return;

        if (player.position.z - safeZone > activeSegments.Peek().transform.position.z)
        {
            Destroy(activeSegments.Dequeue());
        }
    }

    private void SpawnObstacles(Transform parent)
    {
        foreach (var entry in obstaclePrefabs)
        {
            if (Random.value < obstacleChancePerLane)
            {
                int lane = Random.Range(-1, 2);
                float zOffset = Random.Range(minZOffset, maxZOffset);
                Vector3 pos = parent.position + Vector3.forward * zOffset + Vector3.right * lane * laneDistance;
                Instantiate(entry.prefab, pos, Quaternion.identity, parent);
            }
        }
    }

    private void SpawnCoins(Transform parent)
    {
        foreach (var entry in coinPrefabs)
        {
            if (Random.value < entry.chance)
            {
                int lane = Random.Range(-1, 2);
                Vector3 pos = parent.position + Vector3.forward * Random.Range(minZOffset, maxZOffset) +
                              Vector3.right * lane * laneDistance + Vector3.up * coinHeight;
                var coin = Instantiate(entry.prefab, pos, Quaternion.identity, parent);

                // передаём value монетки
                var pickup = coin.GetComponent<Pickup>();
                if (pickup != null)
                {
                    pickup.type = Pickup.PickupType.Coin;
                    pickup.value = entry.value;
                }
            }
        }
    }

    private void SpawnEnergy(Transform parent)
    {
        foreach (var entry in energyPrefabs)
        {
            if (Random.value < entry.chance)
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

    private void SpawnEnemies(Transform parent)
    {
        // === Enemy Type A (рандомные) ===
        foreach (var entry in enemyPrefabs)
        {
            if (Random.value < entry.chance)
            {
                int lane = Random.Range(-1, 2);
                Vector3 pos = parent.position + Vector3.forward * Random.Range(minZOffset, maxZOffset) +
                              Vector3.right * lane * laneDistance;
                Instantiate(entry.prefab, pos, Quaternion.identity, parent);
            }
        }

        // === Enemy Type B (специальные, по дистанции) ===
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
