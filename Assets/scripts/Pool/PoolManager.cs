using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string key;
        public GameObject prefab;
        public int preload = 8;
    }

    public static PoolManager Instance { get; private set; }

    [Header("Pools")]
    public List<Pool> pools = new();

    private readonly Dictionary<string, Queue<GameObject>> _bank = new();
    private readonly Dictionary<GameObject, string> _reverse = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var p in pools)
        {
            var q = new Queue<GameObject>();
            for (int i = 0; i < p.preload; i++)
            {
                var go = Instantiate(p.prefab);
                go.SetActive(false);
                q.Enqueue(go);
                _reverse[go] = p.key;
            }
            _bank[p.key] = q;
        }
    }

    public GameObject Spawn(string key, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (!_bank.TryGetValue(key, out var q) || q.Count == 0)
        {
            // ленивое расширение
            var pool = pools.Find(x => x.key == key);
            if (pool == null) return null;
            var extra = Instantiate(pool.prefab);
            _reverse[extra] = key;
            extra.transform.SetPositionAndRotation(pos, rot);
            if (parent) extra.transform.SetParent(parent, true);
            extra.SetActive(true);
            return extra;
        }

        var go = q.Dequeue();
        if (parent) go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return go;
    }

    public void Despawn(GameObject go)
    {
        if (go == null) return;
        if (!_reverse.TryGetValue(go, out var key)) { Destroy(go); return; }
        go.SetActive(false);
        go.transform.SetParent(transform, true);
        _bank[key].Enqueue(go);
    }
}
