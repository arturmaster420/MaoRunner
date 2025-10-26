using UnityEngine;
using MaoRunner;

public class PickupProgressHook : MonoBehaviour
{
    public enum PickupKind { Coin, XP }
    public PickupKind kind = PickupKind.Coin;

    [Tooltip("Если 0 — возьмём номинал из PlayerProgress по префабу.")]
    public int amountOverride = 0;

    [Tooltip("Уничтожать объект после подбора этим скриптом.")]
    public bool destroyOnPickup = true;

    [Tooltip("Какой тег у игрока.")]
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        var pp = PlayerProgress.Instance;
        if (pp == null) return;

        int amount = amountOverride;
        if (amount <= 0)
        {
            if (kind == PickupKind.Coin) amount = pp.GetCoinValueForPrefab(gameObject);
            else amount = pp.GetXPValueForPrefab(gameObject);
            if (amount <= 0) amount = 1; // safety
        }

        if (kind == PickupKind.Coin) pp.AddCoins(amount);
        else pp.AddXP(amount);

        if (destroyOnPickup) Destroy(gameObject);
    }
}