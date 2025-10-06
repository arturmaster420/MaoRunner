using UnityEngine;
using UnityEngine.Pool;

public class PooledObject : MonoBehaviour
{
    public string key;
    [HideInInspector] public ObjectPool pool;
    public void ReturnToPool() { pool?.Return(key, gameObject); }
}
