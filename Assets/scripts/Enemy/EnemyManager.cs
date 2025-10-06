using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    private int kills = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddKill()
    {
        kills++;
        UIManager.Instance.UpdateKillsUI(kills);
    }
}
