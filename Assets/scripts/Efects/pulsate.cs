using UnityEngine;
public class Pulsate : MonoBehaviour
{
    public float speed = 2f;
    public float amplitude = 0.1f;
    private Vector3 startScale;
    void Start() { startScale = transform.localScale; }
    void Update()
    {
        float scale = 1 + Mathf.Sin(Time.time * speed) * amplitude;
        transform.localScale = startScale * scale;
    }
}