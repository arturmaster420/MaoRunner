using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class TrackSpawnerPooled : MonoBehaviour
{
    [Header("Links")]
    public ObjectPool pool;
    public Transform player;

    [Header("Track")]
    public GameObject floorPrefab;
    public int numberOfSegments = 12;
    public float segmentLength = 10f;
    public float safeZone = 25f;

    [Header("Obstacles")]
    public string[] obstacleKeys = { "HighObstacle", "LowObstacle" };
    [Range(0f, 1f)] public float obstacleChancePerLane = 0.45f;
    public float minZOffset = 2f, maxZOffset = 8f;
    public float laneDistance = 3f;

    [Header("Pickups")]
    public string pickupKey = "pickup";
    [Range(0f, 1f)] public float pickupChance = 0.15f;
    public float pickupHeight = 0.8f;

    float spawnZ = 0f;
    readonly List<GameObject> activeSegments = new();

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        for (int i = 0; i < numberOfSegments; i++) SpawnSegment();
    }

    void Update()
    {
        if (player == null) return;
        if (player.position.z + safeZone > spawnZ - numberOfSegments * segmentLength)
        {
            SpawnSegment();
            DeleteOldSegment();
        }
    }

    void SpawnSegment()
    {
        var segPos = new Vector3(0f, 0f, spawnZ);
        var seg = Instantiate(floorPrefab, segPos, Quaternion.identity, transform);
        activeSegments.Add(seg);

        for (int lane = 0; lane < 3; lane++)
        {
            if (Random.value < obstacleChancePerLane && obstacleKeys.Length > 0)
            {
                string key = obstacleKeys[Random.Range(0, obstacleKeys.Length)];
                float zOff = Random.Range(minZOffset, maxZOffset);
                float x = (lane - 1) * laneDistance;
                var pos = new Vector3(x, 0f, spawnZ + zOff);

                var go = pool.Get(key, seg.transform, pos, Quaternion.identity);
                var desp = go.GetComponent<DespawnBehindPlayer>() ?? go.AddComponent<DespawnBehindPlayer>();
                desp.player = player; desp.despawnDistance = safeZone + 5f;
            }
        }

        if (Random.value < pickupChance && !string.IsNullOrEmpty(pickupKey))
        {
            int lane = Random.Range(0, 3);
            float zOff = Random.Range(minZOffset, maxZOffset);
            float x = (lane - 1) * laneDistance;
            var pos = new Vector3(x, pickupHeight, spawnZ + zOff);

            var go = pool.Get(pickupKey, seg.transform, pos, Quaternion.identity);
            var desp = go.GetComponent<DespawnBehindPlayer>() ?? go.AddComponent<DespawnBehindPlayer>();
            desp.player = player; desp.despawnDistance = safeZone + 5f;
        }

        spawnZ += segmentLength;
    }

    void DeleteOldSegment()
    {
        if (activeSegments.Count == 0) return;
        if (player.position.z - activeSegments[0].transform.position.z > safeZone)
        {
            var first = activeSegments[0];
            foreach (var po in first.GetComponentsInChildren<PooledObject>(true))
                po.ReturnToPool();
            Destroy(first);
            activeSegments.RemoveAt(0);
        }
    }
}
