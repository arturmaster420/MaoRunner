using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class MaoRunnerFixed : MonoBehaviour
{
    [Header("Скорости")]
    public float forwardSpeed = 10f;          // стартовая (~30 км/ч)
    public float maxForwardSpeed = 55f;       // предел (~200 км/ч)
    public float speedGainPerSecond = 0.15f;  // ускорение

    [Header("Движение по полосам")]
    public float laneDistance = 3f;
    public float laneSwitchSpeed = 15f;

    [Header("Прыжок и слайд")]
    public float jumpForce = 9f;
    public float gravity = -35f;
    public float slideDuration = 1.2f;
    public float slideHeight = 0.6f;

    [Space]
    [Header("Адаптация по скорости")]
    [Tooltip("Влияние скорости на длину прыжка (0 = нет, 1 = максимум)")]
    [Range(0f, 1f)] public float jumpLengthInfluence = 0.75f;

    [Tooltip("Влияние скорости на длительность слайда (0 = нет, 1 = максимум)")]
    [Range(0f, 1f)] public float slideLengthInfluence = 0.8f;

    private CharacterController controller;
    private Animator animator;

    private int currentLane = 1;
    private float verticalVelocity;
    private bool isSliding = false;
    private float originalHeight;
    private bool isDead = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        originalHeight = controller.height;
    }

    void Update()
    {
        if (isDead) return;

        HandleInput();
        ApplyMovement();

        // Ускорение скорости со временем
        forwardSpeed = Mathf.Min(maxForwardSpeed, forwardSpeed + speedGainPerSecond * Time.deltaTime);

        // Обновление UI скорости (в км/ч)
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateSpeed(forwardSpeed);

        // Ускорение всех анимаций с ростом скорости
        if (animator != null)
            animator.speed = 1f + (forwardSpeed / maxForwardSpeed) * 0.5f;
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.A) && currentLane > 0) currentLane--;
        if (Input.GetKeyDown(KeyCode.D) && currentLane < 2) currentLane++;

        if (controller.isGrounded)
        {
            animator.SetBool("isRunning", true);

            // прыжок
            if (Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = jumpForce;
                animator.SetTrigger("Jump");
            }

            // слайд
            if (Input.GetKeyDown(KeyCode.LeftControl) && !isSliding)
                StartCoroutine(Slide());
        }
    }

    private void ApplyMovement()
    {
        // гравитация
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -1f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // движение вперёд
        Vector3 move = Vector3.forward * forwardSpeed;

        // смена полос
        float desiredX = (currentLane - 1) * laneDistance;
        float diffX = desiredX - transform.position.x;
        move.x = diffX * laneSwitchSpeed;

        // вертикаль
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);

        // фикс вращения
        var euler = transform.eulerAngles;
        transform.eulerAngles = new Vector3(0f, euler.y, 0f);

        controller.stepOffset = controller.isGrounded ? 0.3f : 0f;
    }

    private IEnumerator Slide()
    {
        isSliding = true;
        animator.SetTrigger("Slide");

        // ⚡ ускорение анимации слайда при разгоне
        float slideAnimSpeed = 1f + (forwardSpeed / maxForwardSpeed) * 0.5f;
        animator.speed = slideAnimSpeed;

        // ⏱️ длительность слайда уменьшается с ростом скорости
        float speedFactor = forwardSpeed / maxForwardSpeed;
        float adjustedDuration = Mathf.Lerp(slideDuration, slideDuration * 0.4f, slideLengthInfluence * speedFactor);

        // ↓ уменьшаем коллайдер и опускаем pivot
        controller.height = slideHeight;
        controller.center = new Vector3(0, slideHeight / 2f, 0);

        yield return new WaitForSeconds(adjustedDuration);

        // ↑ возвращаем параметры
        controller.height = originalHeight;
        controller.center = new Vector3(0, originalHeight / 2f, 0);
        animator.speed = 1f;

        isSliding = false;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isDead) return;

        if (hit.gameObject.CompareTag("Obstacle") || hit.gameObject.CompareTag("Enemy"))
            Die();
    }

    private void Die()
    {
        isDead = true;
        forwardSpeed = 0f;

        if (animator != null)
            animator.SetTrigger("Die");

        var gm = FindObjectOfType<GameOverManager>();
        if (gm != null)
            gm.GameOver();
    }
}
