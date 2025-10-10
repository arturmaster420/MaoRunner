using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MaoRunnerFixed : MonoBehaviour
{
    [Header("Скорости")]
    public float forwardSpeed = 5f;
    public float maxForwardSpeed = 30f;
    public float speedGainPerSecond = 0.1f;

    [Header("Движение по полосам")]
    public float laneDistance = 3f;
    public float laneSwitchSpeed = 15f;

    [Header("Прыжок и слайд")]
    public float jumpForce = 9f;
    public float gravity = -30f;
    public float slideDuration = 1f;
    public float slideHeight = 0.6f;

    [Header("Адаптация по скорости")]
    public float jumpLengthInfluence = 1f;
    public float slideLengthInfluence = 1f;
    public float minAnimSpeed = 0.85f;
    public float maxAnimSpeed = 1.35f;
    public float baseSpeedKmh = 30f;   // базовая "референсная" скорость
    public float maxSpeedKmh = 200f;  // верхняя граница для нормализации
    public float ForwardSpeedKph => forwardSpeed * 3.6f;


    private CharacterController controller;
    private Animator animator;

    private int currentLane = 1;   // 0 left, 1 middle, 2 right
    private float verticalVelocity;
    private bool isSliding = false;
    private float originalHeight;
    private Vector3 move;

    private bool isDead = false;
    private bool invulnerable = false;
    private float lastAliveSpeed = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        originalHeight = controller.height;
    }

    void Update()
    {
        if (isDead) return;

        // запоминаем "живую" скорость (нужна для Revive)
        lastAliveSpeed = forwardSpeed;

        HandleInput();
        ApplyMovement();

        // Ускорение со временем
        forwardSpeed = Mathf.Min(maxForwardSpeed, forwardSpeed + speedGainPerSecond * Time.deltaTime);

        // Обновление UI (км/ч)
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateSpeed(forwardSpeed);

        // Адаптация скорости анимаций
        UpdateAnimSpeedByForward();
        // 🔹 Ускоряем анимации в зависимости от forwardSpeed
        if (animator != null)
        {
            float speedRatio = Mathf.Clamp01(forwardSpeed / maxForwardSpeed);

            animator.SetFloat("RunSpeed", Mathf.Lerp(1f, 3f, speedRatio));
            animator.SetFloat("JumpSpeed", Mathf.Lerp(1f, 2.5f, speedRatio));
            animator.SetFloat("SlideSpeed", Mathf.Lerp(1f, 2f, speedRatio));
        }
    }

    private void UpdateAnimSpeedByForward()
    {
        if (animator == null) return;

        float kmh = forwardSpeed * 3.6f;
        float t = Mathf.InverseLerp(baseSpeedKmh, maxSpeedKmh, kmh);
        float animMul = Mathf.Lerp(minAnimSpeed, maxAnimSpeed, t);

        animator.SetFloat("RunSpeed", animMul);
        animator.SetFloat("JumpSpeed", animMul);
        animator.SetFloat("SlideSpeed", animMul);
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.A) && currentLane > 0) currentLane--;
        if (Input.GetKeyDown(KeyCode.D) && currentLane < 2) currentLane++;

        if (controller.isGrounded)
        {
            if (animator) animator.SetBool("isRunning", true);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = jumpForce;
                if (animator) animator.SetTrigger("Jump");
            }

            if (Input.GetKeyDown(KeyCode.LeftControl) && !isSliding)
                StartCoroutine(Slide());
        }
    }

    private void ApplyMovement()
    {
        // гравитация
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -1f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // движение вперёд
        move = Vector3.forward * forwardSpeed;

        // смена полос (X)
        float desiredX = (currentLane - 1) * laneDistance;
        float diffX = desiredX - transform.position.x;
        move.x = diffX * laneSwitchSpeed;

        // вертикаль
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);

        // фикс крена
        var euler = transform.eulerAngles;
        transform.eulerAngles = new Vector3(0f, euler.y, 0f);

        // stepOffset в воздухе
        controller.stepOffset = controller.isGrounded ? 0.3f : 0f;
    }

    private System.Collections.IEnumerator Slide()
    {
        isSliding = true;
        if (animator) animator.SetTrigger("Slide");

        controller.height = slideHeight;

        // длину слайда регулирует slideDuration; при желании масштабируй по скорости:
        float kmh = forwardSpeed * 3.6f;
        float t = Mathf.InverseLerp(baseSpeedKmh, maxSpeedKmh, kmh);
        float dur = slideDuration * Mathf.Lerp(1f, slideLengthInfluence, t);

        yield return new WaitForSeconds(dur);

        controller.height = originalHeight;
        isSliding = false;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isDead || invulnerable) return;

        if (hit.gameObject.CompareTag("Obstacle") || hit.gameObject.CompareTag("Enemy"))
            Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        forwardSpeed = 0f;
        if (animator) animator.SetTrigger("Die");

        var gm = FindObjectOfType<GameOverManager>();
        if (gm != null) gm.GameOver();
    }

    /// <summary>
    /// Возрождение: поднимаем скорость, даём неуязвимость, сдвигаем вперёд и выравниваем по полосе.
    /// </summary>
    public void Revive(float reviveSpeedFactor, float minReviveSpeed, float invulnSeconds, float forwardOffset)
    {
        // небольшой безопасный сдвиг вперёд и выравнивание по центру полосы
        var pos = transform.position;
        float desiredX = (currentLane - 1) * laneDistance;

        // чтобы корректно "телепортнуть" CharacterController — временно выключим его
        controller.enabled = false;
        transform.position = new Vector3(desiredX, pos.y, pos.z + forwardOffset);
        controller.enabled = true;

        // восстановим движение
        isDead = false;
        verticalVelocity = -1f;

        float revived = Mathf.Max(minReviveSpeed, lastAliveSpeed * reviveSpeedFactor);
        forwardSpeed = Mathf.Min(revived, maxForwardSpeed);

        if (animator)
        {
            animator.ResetTrigger("Die");
            animator.SetBool("isRunning", true);
            animator.Play("Run"); // сразу в бег
        }

        // краткая неуязвимость
        StopAllCoroutines();
        StartCoroutine(Invulnerability(invulnSeconds));
    }

    private System.Collections.IEnumerator Invulnerability(float time)
    {
        invulnerable = true;
        float timer = time;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        invulnerable = false;
    }
}
