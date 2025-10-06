using UnityEngine;

public class Shuriken : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 3f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            Destroy(other.gameObject); // уничтожаем врага
            Destroy(gameObject);       // уничтожаем сюрикен

            // обновляем счётчик убитых
            EnemyManager.Instance.AddKill();
        }
    }
}
