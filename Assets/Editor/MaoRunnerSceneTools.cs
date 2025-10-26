#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using MaoRunner.Infrastructure;

public static class MaoRunnerSceneTools
{
    [MenuItem("MaoRunner/Scenes/Ensure Default Scenes & Build Settings")]
    public static void EnsureScenes()
    {
        string scenesDir = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(scenesDir))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        // Создаём пустые сцены, если отсутствуют
        CreateSceneIfMissing(SceneNames.MAIN_MENU);
        CreateSceneIfMissing(SceneNames.CHARACTER_MENU);
        CreateSceneIfMissing(SceneNames.RUNNER_GAME);
        CreateSceneIfMissing(SceneNames.PLAYER_SCENE);

        // Добавляем в Build Settings в правильном порядке
        EnsureInBuildSettings(new string[] {
            PathFor(SceneNames.MAIN_MENU),
            PathFor(SceneNames.CHARACTER_MENU),
            PathFor(SceneNames.RUNNER_GAME),
            PathFor(SceneNames.PLAYER_SCENE),
        });

        EditorUtility.DisplayDialog("MaoRunner", "Сцены проверены и добавлены в Build Settings.", "OK");
    }

    static string PathFor(string sceneName) => $"Assets/Scenes/{sceneName}.unity";

    static void CreateSceneIfMissing(string sceneName)
    {
        string path = PathFor(sceneName);
        if (!File.Exists(path))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[MaoRunner] Created scene: {path}");
        }
    }

    static void EnsureInBuildSettings(string[] scenePaths)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var p in scenePaths)
        {
            bool exists = false;
            foreach (var s in list)
                if (s.path == p) { exists = true; break; }
            if (!exists)
                list.Add(new EditorBuildSettingsScene(p, true));
        }
        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
