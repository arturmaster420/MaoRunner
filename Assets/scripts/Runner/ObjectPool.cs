using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [System.Serializable] public class Pool { public string key; public GameObject prefab; public int size = 10; }
    public List<Pool> pools = new List<Pool>();

    private readonly Dictionary<string, Queue<GameObject>> dict = new();

    void Awake()
    {
        foreach (var p in pools)
        {
            var q = new Queue<GameObject>();
            for (int i = 0; i < p.size; i++)
            {
                var go = Instantiate(p.prefab, transform);
                go.SetActive(false);
                var po = go.GetComponent<PooledObject>() ?? go.AddComponent<PooledObject>();
                po.pool = this; po.key = p.key;
                q.Enqueue(go);
            }
            dict[p.key] = q;
        }
    }

    public GameObject Get(string key, Transform parent, Vector3 pos, Quaternion rot)
    {
        if (!dict.TryGetValue(key, out var q) || q.Count == 0)
        {
            var info = pools.Find(pl => pl.key == key);
            if (info == null || info.prefab == null) { Debug.LogError("No pool for key " + key); return null; }
            var extra = Instantiate(info.prefab, transform);
            extra.SetActive(false);
            var po = extra.GetComponent<PooledObject>() ?? extra.AddComponent<PooledObject>();
            po.pool = this; po.key = key;
            if (!dict.ContainsKey(key)) dict[key] = new Queue<GameObject>();
            dict[key].Enqueue(extra);
            q = dict[key];
        }

        var obj = q.Dequeue();
        obj.transform.SetParent(parent, false);
        obj.transform.SetPositionAndRotation(pos, rot);
        obj.SetActive(true);
        return obj;
    }

    public void Return(string key, GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform, false);
        if (!dict.ContainsKey(key)) dict[key] = new Queue<GameObject>();
        dict[key].Enqueue(obj);
    }
}
