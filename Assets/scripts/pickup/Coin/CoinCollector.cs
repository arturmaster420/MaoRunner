using UnityEngine;

public class CoinCollector : MonoBehaviour
{
    private ScoreManager scoreManager;

    private void Start()
    {
        scoreManager = FindObjectOfType<ScoreManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Pickup pickup = other.GetComponent<Pickup>();
        if (pickup != null)
        {
            switch (pickup.type)
            {
                case Pickup.PickupType.Coin:
                    scoreManager.AddCoins(pickup.value);
                    break;

                case Pickup.PickupType.Energy:
                    scoreManager.AddEnergy(pickup.value);
                    break;
            }

            Destroy(other.gameObject);
        }
    }
}
