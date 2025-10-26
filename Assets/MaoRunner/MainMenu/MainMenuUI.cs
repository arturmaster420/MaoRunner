using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaoRunner.Infrastructure;

namespace MaoRunner.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public Button startButton;
        public Button optionsButton;
        public Button exitButton;
        public TMP_Text titleText;

        void Start()
        {
            if (startButton) startButton.onClick.AddListener(() => SceneLoaderPro.LoadPlayer());
            if (optionsButton) optionsButton.onClick.AddListener(() => Debug.Log("[MainMenu] Options clicked (stub)"));
            if (exitButton) exitButton.onClick.AddListener(() => Application.Quit());
        }
    }
}
