using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverUI;

    private bool isGameOver = false;

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // включаем экран
        gameOverUI.SetActive(true);

        // ставим игру на паузу
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f; // снимаем паузу
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f; // снимаем паузу
        SceneManager.LoadScene("MainMenu"); // название сцены с меню
    }
}
