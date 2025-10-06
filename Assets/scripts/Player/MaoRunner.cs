using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MaoRunnerFixed : MonoBehaviour
{
    [Header("Скорости")]
    public float forwardSpeed = 5f;          // начальная скорость
    public float maxForwardSpeed = 30f;      // максимум
    public float speedGainPerSecond = 0.1f;  // ускорение в сек

    [Header("Движение по полосам")]
    public float laneDistance = 3f;
    public float laneSwitchSpeed = 15f;

    [Header("Прыжок и слайд")]
    public float jumpForce = 9f;             // сила прыжка (регулируй тут)
    public float gravity = -30f;             // гравитация (отрицательная!)
    public float slideDuration = 1f;
    public float slideHeight = 0.6f;

    private CharacterController controller;
    private Animator animator;

    private int currentLane = 1; // 0=левая, 1=средняя, 2=правая
    private float verticalVelocity;
    private bool isSliding = false;
    private float originalHeight;
    private Vector3 move;
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

        // Ускорение со временем
        forwardSpeed = Mathf.Min(maxForwardSpeed, forwardSpeed + speedGainPerSecond * Time.deltaTime);

        // В UI (км/ч)
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateSpeed(forwardSpeed);
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.A) && currentLane > 0) currentLane--;
        if (Input.GetKeyDown(KeyCode.D) && currentLane < 2) currentLane++;

        if (controller.isGrounded)
        {
            animator.SetBool("isRunning", true);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = jumpForce;      // задаём стартовую скорость вверх
                animator.SetTrigger("Jump");
            }

            if (Input.GetKeyDown(KeyCode.LeftControl) && !isSliding)
                StartCoroutine(Slide());
        }
    }

    private void ApplyMovement()
    {
        // ГРАВИТАЦИЯ — правильная версия:
        if (controller.isGrounded)
        {
            // на земле держим небольшую отрицательную скорость,
            // чтобы контроллер уверенно касался поверхности
            if (verticalVelocity < 0f) verticalVelocity = -1f;
        }
        else
        {
            // в воздухе накапливаем ускорение вниз
            verticalVelocity += gravity * Time.deltaTime;
        }

        // движение вперёд
        move = Vector3.forward * forwardSpeed;

        // смена полос (по X)
        float desiredX = (currentLane - 1) * laneDistance;
        float diffX = desiredX - transform.position.x;
        move.x = diffX * laneSwitchSpeed;

        // вертикаль
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);

        // фикс от крена по Z
        var euler = transform.eulerAngles;
        transform.eulerAngles = new Vector3(0f, euler.y, 0f);

        // необязательный бонус: уменьшать stepOffset в воздухе, чтобы прыжок был «чище»
        controller.stepOffset = controller.isGrounded ? 0.3f : 0f;
    }

    private System.Collections.IEnumerator Slide()
    {
        isSliding = true;
        animator.SetTrigger("Slide");

        controller.height = slideHeight;
        yield return new WaitForSeconds(slideDuration);
        controller.height = originalHeight;

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

        if (animator != null) animator.SetTrigger("Die");

        var gm = FindObjectOfType<GameOverManager>();
        if (gm != null) gm.GameOver();
    }
}
