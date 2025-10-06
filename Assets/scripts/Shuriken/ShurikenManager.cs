using UnityEngine;
using TMPro;

public class ShurikenManager : MonoBehaviour
{
    public static ShurikenManager Instance;
    public TextMeshProUGUI shurikenText;

    public int maxShurikens = 10;
    private int currentShurikens = 2;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start() => UpdateUI();

    public void AddShurikens(int amount)
    {
        currentShurikens = Mathf.Clamp(currentShurikens + amount, 0, maxShurikens);
        UpdateUI();
    }

    public bool UseShuriken()
    {
        if (currentShurikens <= 0) return false;
        currentShurikens--;
        UpdateUI();
        return true;
    }

    private void UpdateUI()
    {
        if (shurikenText != null)
            shurikenText.text = currentShurikens + " / " + maxShurikens;
    }

    public int GetShurikens() => currentShurikens;
}
