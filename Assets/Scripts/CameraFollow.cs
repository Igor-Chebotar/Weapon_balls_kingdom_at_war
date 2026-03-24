using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 5f;
    [SerializeField] float zOffset = -10f;

    void LateUpdate()
    {
        if (target == null)
        {
            // попробуем найти
            var p = FindObjectOfType<PlayerController>();
            if (p != null) target = p.transform;
            return;
        }

        Vector3 desired = new Vector3(target.position.x, target.position.y, zOffset);
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
