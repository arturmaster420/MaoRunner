using UnityEngine;

public class PoolAutoDespawn : MonoBehaviour
{
    public string poolKey; // 👈 добавляем, чтобы знать к какому пулу относится объект
    public float lifeTime = 5f;

    private void OnEnable()
    {
        CancelInvoke();
        Invoke(nameof(DespawnSelf), lifeTime);
    }

    private void DespawnSelf()
    {
        if (PoolManager.Instance != null)
        {
            // если ключ задан — возвращаем в пул
            if (!string.IsNullOrEmpty(poolKey))
                PoolManager.Instance.Despawn(poolKey, gameObject);
            else
                gameObject.SetActive(false); // fallback
        }
        else
        {
            Destroy(gameObject);
        }
    }
}