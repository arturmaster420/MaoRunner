using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    public TMP_Text coinText;
    public TMP_Text energyText;
    public TMP_Text distanceText;

    private int coins = 0;
    private int energy = 0;
    private float distance = 0f;
    public Transform player;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Update()
    {
        if (player != null)
        {
            distance = player.position.z;
            if (distanceText != null) distanceText.text = "Distance: " + Mathf.FloorToInt(distance);
        }
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        if (coinText != null) coinText.text = "Coins: " + coins;
    }

    public void AddEnergy(int amount)
    {
        energy += amount;
        if (energyText != null) energyText.text = "Energy: " + energy;
    }
}

