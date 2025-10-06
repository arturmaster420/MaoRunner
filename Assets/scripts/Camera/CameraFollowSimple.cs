using UnityEngine;

public class CameraFollowSimple : MonoBehaviour
{
    public Transform target;        // перетащи сюда Mao
    public Vector3 offset = new Vector3(0f, 3.5f, -6f);
    public float followLerp = 10f;  // сглаживание

    void LateUpdate()
    {
        if (!target) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, followLerp * Time.deltaTime);
        transform.LookAt(target.position + Vector3.forward * 5f); // смотрим чуть вперёд
    }
}
