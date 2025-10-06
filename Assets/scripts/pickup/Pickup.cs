using UnityEngine;

public class Pickup : MonoBehaviour
{
    public enum PickupType { Coin, Energy, Shuriken }
    public PickupType type;

    [Tooltip("Количество добавляемых единиц")]
    public int value = 1;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        switch (type)
        {
            case PickupType.Coin:
                ScoreManager.Instance.AddCoins(value);
                break;

            case PickupType.Energy:
                ScoreManager.Instance.AddEnergy(value);
                break;

            case PickupType.Shuriken:
                ShurikenManager.Instance.AddShurikens(value);
                break;
        }

        Destroy(gameObject);
    }
}
