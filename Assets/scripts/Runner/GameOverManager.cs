using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject gameOverUI;            // панель с кнопками (Restart / MainMenu / Continue)

    [Header("Refs")]
    public MaoRunnerFixed player;            // перетащи сюда SuperRig (компонент MaoRunnerFixed)
    public GhostEnergy energy;               // перетащи сюда компонент GhostEnergy из сцены

    [Header("Continue Settings")]
    public int   maxContinues        = 1;    // сколько раз за забег можно продолжить
    [Range(0,100)]
    public int   energyRestorePct    = 50;   // восстановим % от максимума энергии
    public float reviveSpeedFactor   = 0.8f; // вернём скорость как 80% от предсмертной
    public float minReviveSpeed      = 12f;  // но не ниже этого минимума (чтобы не стоять)
    public float invulnerabilityTime = 2f;   // сек. неуязвимости после продолжения
    public float reviveForwardOffset = 2.5f; // смещение вперёд при возрождении (чтобы не воскреснуть внутри объекта)

    private int continuesUsed = 0;

    // Вызывается из MaoRunnerFixed.Die()
    public void GameOver()
    {
        if (gameOverUI != null) gameOverUI.SetActive(true);
        // Важно: мы НЕ ставим Time.timeScale = 0, чтобы спавнеры продолжали жить, как у тебя сейчас.
        // Если хочешь стоп-кадр — можно добавить опцию.
    }

    // Кнопка Continue (OnClick)
    public void ContinueRun()
    {
        if (continuesUsed >= maxContinues) return;
        if (player == null) return;

        continuesUsed++;

        // 1) Скрыть окно
        if (gameOverUI != null) gameOverUI.SetActive(false);

        // 2) Возродить игрока
        player.Revive(
            reviveSpeedFactor,
            minReviveSpeed,
            invulnerabilityTime,
            reviveForwardOffset
        );

        // 3) Чуть восстановить энергию
        if (energy != null)
        {
            int target = Mathf.RoundToInt(energy.maxEnergy * (energyRestorePct / 100f));
            int need = target - Mathf.RoundToInt(energy.currentEnergy);
            if (need > 0)
            {
                energy.currentEnergy = Mathf.Min(energy.maxEnergy, energy.currentEnergy + need);
                //сразу обновим UI
                var updateMethod = energy.GetType().GetMethod("UpdateUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (updateMethod != null) updateMethod.Invoke(energy, null);
            }
        }
    }

    // Зови из кнопки Restart (или при перезагрузке сцены) если нужно
    public void ResetContinues() => continuesUsed = 0;
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
