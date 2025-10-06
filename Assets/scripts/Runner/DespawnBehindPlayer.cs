using UnityEngine;

public class DespawnBehindPlayer : MonoBehaviour
{
    public Transform player;
    public float despawnDistance = 25f;
    PooledObject po;

    void Awake() { po = GetComponent<PooledObject>(); }

    void Update()
    {
        if (player == null) return;
        if (player.position.z - transform.position.z > despawnDistance)
        {
            if (po != null) po.ReturnToPool(); else Destroy(gameObject);
        }
    }
}
