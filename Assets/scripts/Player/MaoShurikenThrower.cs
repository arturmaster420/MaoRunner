using UnityEngine;

public class MaoShurikenThrower : MonoBehaviour
{
    [Header("Shuriken")]
    public GameObject shurikenPrefab;     // префаб сюрикена
    public Transform throwPoint;         // точка вылета

    [Header("Синхронизация со скоростью Мао")]
    [Tooltip("Базовая скорость, чтобы на старте шурикен не был медленным (м/с).")]
    public float baseThrowSpeed = 10f;

    [Tooltip("Во сколько раз скорость полёта зависит от скорости Мао (1 = такая же в м/с).")]
    public float speedMultiplier = 1.0f;

    [Tooltip("Ссылка на скрипт бега для чтения текущей скорости.")]
    public MaoRunnerFixed maoRunner;      // можно не заполнять — найдём сами

    void Start()
    {
        if (maoRunner == null)
            maoRunner = FindObjectOfType<MaoRunnerFixed>();
    }

    void Update()
    {
        // бросок на Q
        if (Input.GetKeyDown(KeyCode.Q))
            TryThrowShuriken();
    }

    private void TryThrowShuriken()
    {
        // спрашиваем менеджер: есть ли шурикены? он же и спишет 1 шт и обновит UI
        if (!ShurikenManager.Instance.UseShuriken())
            return;

        if (shurikenPrefab == null || throwPoint == null) return;

        var go = Instantiate(shurikenPrefab, throwPoint.position, throwPoint.rotation);

        // вычисляем скорость полёта
        float maoKmh = (maoRunner != null) ? maoRunner.forwardSpeed : 0f;
        float maoMs = maoKmh * 0.27778f; // км/ч -> м/с
        float throwMs = baseThrowSpeed + maoMs * speedMultiplier;

        // задаём скорость через Rigidbody, если он есть
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;                 // обычно для метаемых лучше без гравитации
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.velocity = throwPoint.forward * throwMs;
        }
        // если Rigidbody нет — пусть работает ваш текущий Shuriken.cs
        // (при желании позже добавим туда приём "начальной скорости" через SetInitialSpeed)
    }
}
