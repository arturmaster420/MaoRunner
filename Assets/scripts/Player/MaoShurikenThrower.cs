using UnityEngine;

public class MaoShurikenThrower : MonoBehaviour
{
    [Header("Shuriken Settings")]
    public GameObject shurikenPrefab;   // префаб сюрикена
    public Transform throwPoint;         // точка вылета

    private void Update()
    {
        // Бросок по клавише Q
        if (Input.GetKeyDown(KeyCode.Q))
        {
            TryThrowShuriken();
        }
    }

    private void TryThrowShuriken()
    {
        // Проверяем есть ли доступные сюрикены
        if (!ShurikenManager.Instance.UseShuriken())
            return; // если 0 — выходим

        // Создаём сюрикен
        Instantiate(shurikenPrefab, throwPoint.position, throwPoint.rotation);
    }
}
