using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI shurikenText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI speedText;

    [Header("Player")]
    public Transform player;

    private float startZ;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (player != null)
            startZ = player.position.z;
    }

    private void Update()
    {
        if (player != null)
        {
            float distance = player.position.z - startZ;
            if (distanceText != null)
                distanceText.text = "Distance: " + Mathf.FloorToInt(distance);
        }
    }

    public void UpdateCoins(int coins) =>
        coinsText.text = "Coins: " + coins;

    public void UpdateShurikenUI(int count) =>
        shurikenText.text = "Shuriken: " + count;

    public void UpdateKillsUI(int kills) =>
        killsText.text = "Kills: " + kills;

    public void UpdateSpeed(float speed) =>
        speedText.text = "Speed: " + Mathf.FloorToInt(speed * 3.6f) + " km/h";
}
