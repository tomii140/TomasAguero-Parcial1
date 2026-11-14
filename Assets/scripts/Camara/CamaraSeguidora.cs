using UnityEngine;

public class CamaraSeguidora : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Range(0.01f, 1f)] private float smoothTime = 0.125f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);

    private void Start()
    {
        ResolveTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            ResolveTarget();
            if (target == null) return;
        }

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothTime);
    }

    private void ResolveTarget()
    {
        if (GameManager.Instance != null && GameManager.Instance.Hunter != null)
        {
            target = GameManager.Instance.Hunter.transform;
        }
    }
}