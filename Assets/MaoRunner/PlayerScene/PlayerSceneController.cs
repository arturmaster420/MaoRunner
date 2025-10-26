
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if TMP_PRESENT
using TMPro;
#endif

namespace MaoRunner.UI
{
    public class PlayerSceneController : MonoBehaviour
    {
        [Header("References")]
        public Transform characterPreview;
        public Camera uiCamera;
        public MaoRunner.PlayerProgress progress;

        [Header("UI Bindings")]
        public Slider xpBar;
        #if TMP_PRESENT
        public TMP_Text levelText;
        public TMP_Text coinsText;
        #else
        public Text levelText;
        public Text coinsText;
        #endif

        [Header("Buttons")]
        public Button runButton;
        public Button shopButton;
        public Button menuButton;

        [Header("Character Rotate")]
        public float dragRotateSpeed = 120f;
        public float autoRotateSpeed = 15f;
        public bool autoRotate = true;

        bool _drag;
        Vector3 _lastMouse;

        void Awake()
        {
            if (progress == null) progress = MaoRunner.PlayerProgress.Instance ?? FindObjectOfType<MaoRunner.PlayerProgress>();
            BindButtons();
        }

        void OnEnable()
        {
            if (progress == null) return;
            progress.OnLevelChanged += OnLevelChanged;
            progress.OnXpChanged += OnXpChanged;
            progress.OnCoinsChanged += OnCoinsChanged;
            RefreshUI();
        }

        void OnDisable()
        {
            if (progress == null) return;
            progress.OnLevelChanged -= OnLevelChanged;
            progress.OnXpChanged -= OnXpChanged;
            progress.OnCoinsChanged -= OnCoinsChanged;
        }

        void Update()
        {
            if (characterPreview == null) return;

            if (Input.GetMouseButtonDown(0)) { _drag = true; _lastMouse = Input.mousePosition; }
            if (Input.GetMouseButtonUp(0)) _drag = false;

            if (_drag)
            {
                var delta = (Input.mousePosition - _lastMouse).x;
                characterPreview.Rotate(Vector3.up, delta * dragRotateSpeed * Time.deltaTime, Space.World);
                _lastMouse = Input.mousePosition;
            }
            else if (autoRotate)
            {
                characterPreview.Rotate(Vector3.up, autoRotateSpeed * Time.deltaTime, Space.World);
            }
        }

        void BindButtons()
        {
            if (runButton != null)
                runButton.onClick.AddListener(() => SceneManager.LoadScene(MaoRunner.Infrastructure.SceneLoaderPro.PlayScene, LoadSceneMode.Single));
            if (menuButton != null)
                menuButton.onClick.AddListener(() => SceneManager.LoadScene(MaoRunner.Infrastructure.SceneLoaderPro.MainMenuScene, LoadSceneMode.Single));
        }

        void OnLevelChanged(int lvl) => RefreshUI();
        void OnXpChanged(int cur, int req) => RefreshUI();
        void OnCoinsChanged(int total) => RefreshUI();

        public void RefreshUI()
        {
            if (progress == null) return;
            int cur = progress.CurrentXP;
            int req = progress.XpForNextLevel;

            if (xpBar != null)
            {
                xpBar.minValue = 0;
                xpBar.maxValue = Mathf.Max(1, req);
                xpBar.value = Mathf.Clamp(cur, 0, req);
            }

            if (levelText != null) levelText.text = $"LVL: {progress.CurrentLevel}";
            if (coinsText != null) coinsText.text = progress.TotalCoins.ToString();
        }
    }
}
