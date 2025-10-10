using UnityEngine;

public class PooledAutoDespawn : MonoBehaviour
{
    public float lifeTime = 5f;
    float _t;

    void OnEnable() => _t = 0f;

    void Update()
    {
        _t += Time.deltaTime;
        if (_t >= lifeTime) PoolManager.Instance.Despawn(gameObject);
    }

    void OnDisable() => _t = 0f;
}
