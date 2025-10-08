using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GhostEnergy : MonoBehaviour
{
    [Header("UI")]
    public Image energyBar;
    public TextMeshProUGUI energyText;

    [Header("Параметры")]
    public float maxEnergy = 100f;
    public float currentEnergy = 0f;
    public float ghostDuration = 5f;   // сколько секунд длится призрак
    public float ghostSpeedMultiplier = 1.5f; // ускорение

    private bool isGhost = false;
    private MaoRunnerFixed runner; // твой скрипт движения
    private CharacterController controller;

    void Start()
    {
         runner = GetComponent<MaoRunnerFixed>();
        controller = GetComponent<CharacterController>();
        UpdateUI();
    }

    void Update()
    {
        // Активируем призрак, если шкала полная
        if (!isGhost && currentEnergy >= maxEnergy && Input.GetKeyDown(KeyCode.F))
        {
            StartCoroutine(ActivateGhost());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Energy"))
        {
            currentEnergy = Mathf.Min(currentEnergy + 20f, maxEnergy); // +20 энергии
            UpdateUI();
            Destroy(other.gameObject);
        }
    }

    private System.Collections.IEnumerator ActivateGhost()
    {
        isGhost = true;
        currentEnergy = 0f;
        UpdateUI();

        // Эффекты
        float originalSpeed = runner.forwardSpeed;
        runner.forwardSpeed *= ghostSpeedMultiplier;
        Color ghostColor = new Color(1f, 1f, 1f, 0.5f);

        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var r in renderers)
            r.material.color = ghostColor;

        // Игнорируем столкновения с препятствиями
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Obstacle"), true);

        yield return new WaitForSeconds(ghostDuration);

        // Возврат
        runner.forwardSpeed = originalSpeed;
        foreach (var r in renderers)
            r.material.color = Color.white;
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Obstacle"), false);

        isGhost = false;
    }

    private void UpdateUI()
    {
        if (energyBar != null)
            energyBar.fillAmount = currentEnergy / maxEnergy;
        if (energyText != null)
            energyText.text = $"Energy: {Mathf.RoundToInt(currentEnergy)}%";
    }
}
