using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaoRunner.Infrastructure
{
    public static class SceneLoaderPro
    {
        public const string MainMenuScene = "MainMenuScene";
        public const string PlayerScene = "PlayerScene";
        public const string PlayScene = "PlayScene";

        public static void LoadMainMenu() => Load(MainMenuScene);
        public static void LoadPlayer() => Load(PlayerScene);
        public static void LoadPlay() => Load(PlayScene);

        public static AsyncOperation LoadAsync(string scene) => SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        public static void Load(string scene) => SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }
}
