using UnityEngine;
public class RotateY : MonoBehaviour
{
    public float rotationSpeed = 120f;
    void Update()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}