using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string key;
        public GameObject prefab;
        public int preload = 8;

        [Header("Optional Category (used by TrackSpawner)")]
        public string category = "Misc";

        [HideInInspector] public Queue<GameObject> objects = new();
    }

    [SerializeField] public List<Pool> pools = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var pool in pools)
        {
            if (pool.prefab == null)
            {
                Debug.LogWarning($"Pool '{pool.key}' has no prefab assigned.");
                continue;
            }

            for (int i = 0; i < pool.preload; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                pool.objects.Enqueue(obj);
            }
        }
    }

    public GameObject Spawn(string key, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        Pool pool = pools.Find(p => p.key == key);
        if (pool == null)
        {
            Debug.LogWarning($"Pool with key '{key}' not found.");
            return null;
        }

        GameObject obj;
        if (pool.objects.Count > 0)
        {
            obj = pool.objects.Dequeue();
        }
        else
        {
            obj = Instantiate(pool.prefab);
        }

        obj.transform.SetParent(parent);
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    public void Despawn(string key, GameObject obj)
    {
        Pool pool = pools.Find(p => p.key == key);
        if (pool == null)
        {
            Debug.LogWarning($"Pool with key '{key}' not found.");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        pool.objects.Enqueue(obj);
    }
}