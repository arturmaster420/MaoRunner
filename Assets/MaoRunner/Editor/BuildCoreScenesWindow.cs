#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if TMP_PRESENT
using TMPro;
#endif

namespace MaoRunner.EditorTools
{
    public class BuildCoreScenesWindow : EditorWindow
    {
        [MenuItem("Tools/MaoRunner/Build Core Scenes")]
        public static void Open() => CreatePlayerAndMenu();

        static void CreatePlayerAndMenu()
        {
            CreatePlayerScene();
            CreateMainMenuScene();
            EnsureInBuild(MaoRunner.Infrastructure.SceneLoaderPro.PlayerScene);
            EnsureInBuild(MaoRunner.Infrastructure.SceneLoaderPro.MainMenuScene);
            AssetDatabase.SaveAssets();
        }

        // ---------------- PLAYER SCENE ----------------
        static void CreatePlayerScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MaoRunner.Infrastructure.SceneLoaderPro.PlayerScene;

            SpawnCamera();
            SpawnLight();
            EnsureEventSystem();

            var canvas = SpawnCanvas("PlayerCanvas");
            var root = canvas.GetComponent<RectTransform>();

            // === XP Bar ===
            var xpGO = new GameObject("XPBar");
            xpGO.transform.SetParent(root, false);
            var xpRect = xpGO.AddComponent<RectTransform>();
            xpRect.anchorMin = new Vector2(0f, 0f);
            xpRect.anchorMax = new Vector2(0f, 0f);
            xpRect.anchoredPosition = new Vector2(140f, 40f);
            xpRect.sizeDelta = new Vector2(420f, 22f);

            var bg = xpGO.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.16f, 0.14f, 0.85f);

            var slider = xpGO.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;

            var fillAreaGO = new GameObject("Fill");
            fillAreaGO.transform.SetParent(xpGO.transform, false);
            var fillImg = fillAreaGO.AddComponent<Image>();
            var fillRect = fillImg.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(1, 1);
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            fillImg.color = new Color(0.13f, 0.62f, 1f);
            slider.targetGraphic = fillImg;

            // === Level label ===
#if TMP_PRESENT
            TMP_Text levelText = MakeTMPText(root, "LevelText", "LVL: 1", 32,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(20f, 6f), new Vector2(260f, 60f),
                new Color(1f, .8f, .28f));
#else
            Text levelText = MakeUIText(root, "LevelText", "LVL: 1", 32,
                TextAnchor.MiddleLeft,
                new Vector2(20f, 6f), new Vector2(260f, 60f),
                new Color(1f, .8f, .28f));
#endif

            // === Coins text ===
#if TMP_PRESENT
            TMP_Text coinsText = MakeTMPText(root, "CoinsText", "0", 36,
                TextAlignmentOptions.MidlineRight,
                new Vector2(-40f, -10f), new Vector2(320f, 50f),
                new Color(1f, .8f, .28f));
#else
            Text coinsText = MakeUIText(root, "CoinsText", "0", 36,
                TextAnchor.MiddleRight,
                new Vector2(-40f, -10f), new Vector2(320f, 50f),
                new Color(1f, .8f, .28f));
#endif

            // === Buttons ===
            var runBtn = MakeButton(root, "RunButton", "RUN", new Vector2(1f, .5f), new Vector2(-220, 100));
            var shopBtn = MakeButton(root, "ShopButton", "SHOP", new Vector2(1f, .5f), new Vector2(-220, 0));
            var menuBtn = MakeButton(root, "MenuButton", "MENU", new Vector2(1f, .5f), new Vector2(-220, -100));

            // === Controller ===
            var ctrl = new GameObject("PlayerSceneController").AddComponent<MaoRunner.UI.PlayerSceneController>();
            ctrl.xpBar = slider;
            ctrl.levelText = levelText;
            ctrl.coinsText = coinsText;
            ctrl.runButton = runBtn;
            ctrl.shopButton = shopBtn;
            ctrl.menuButton = menuBtn;

            EditorSceneManager.SaveScene(scene, $"Assets/{MaoRunner.Infrastructure.SceneLoaderPro.PlayerScene}.unity");
        }

        // ---------------- MAIN MENU SCENE ----------------
        static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = MaoRunner.Infrastructure.SceneLoaderPro.MainMenuScene;

            SpawnCamera();
            SpawnLight();
            EnsureEventSystem();

            var canvas = SpawnCanvas("MainMenuCanvas");
            var root = canvas.GetComponent<RectTransform>();

            MakeButton(root, "StartButton", "START", new Vector2(.5f, .5f), new Vector2(0, 80))
                .onClick.AddListener(() => SceneManager.LoadScene(MaoRunner.Infrastructure.SceneLoaderPro.PlayerScene));
            MakeButton(root, "OptionsButton", "OPTIONS", new Vector2(.5f, .5f), new Vector2(0, 0));
            MakeButton(root, "ExitButton", "EXIT", new Vector2(.5f, .5f), new Vector2(0, -80))
                .onClick.AddListener(() => {
#if UNITY_EDITOR
                    EditorApplication.isPlaying = false;
#else
                    Application.Quit(); 
#endif
                });

            EditorSceneManager.SaveScene(scene, $"Assets/{MaoRunner.Infrastructure.SceneLoaderPro.MainMenuScene}.unity");
        }

        // ---------------- HELPERS ----------------

        static Canvas SpawnCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static void SpawnCamera()
        {
            var cam = new GameObject("MainCamera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.transform.position = new Vector3(0, 2, -10);
            cam.transform.rotation = Quaternion.identity;
        }

        static void SpawnLight()
        {
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        static Button MakeButton(RectTransform parent, string name, string text, Vector2 anchor, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(260, 60);
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            var btn = go.AddComponent<Button>();

#if TMP_PRESENT
            var tgo = new GameObject("Label");
            tgo.transform.SetParent(rt, false);
            var tr = tgo.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            var tmp = tgo.AddComponent<TMP_Text>();
            tmp.text = text; tmp.fontSize = 28; tmp.alignment = TextAlignmentOptions.Midline;
#else
            var tgo = new GameObject("Label");
            tgo.transform.SetParent(rt, false);
            var tr = tgo.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            var tmp = tgo.AddComponent<Text>();
            tmp.text = text; tmp.fontSize = 28; tmp.alignment = TextAnchor.MiddleCenter;
#endif
            return btn;
        }

#if TMP_PRESENT
        static TMP_Text MakeTMPText(RectTransform parent, string name, string text, int size,
            TextAlignmentOptions align, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var t = go.AddComponent<TMP_Text>();
            t.text = text; t.fontSize = size; t.alignment = align; t.color = color;
            return t;
        }
#else
        static Text MakeUIText(RectTransform parent, string name, string text, int size,
            TextAnchor align, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var t = go.AddComponent<Text>();
            t.text = text; t.fontSize = size; t.alignment = align; t.color = color;
            return t;
        }
#endif

        static void EnsureInBuild(string sceneName)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            string path = $"Assets/{sceneName}.unity";
            for (int i = 0; i < list.Count; i++)
                if (list[i].path == path) return;

            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif