using UnityEngine;

public class CamaraSeguidora : MonoBehaviour
{
    public Transform target;
    [Range(0, 1)] public float smoothTime= 0.125f;
    public Vector3 offset= new Vector3(0, 0, -10);

    void FixedUpdate()
    {
        if (target== null)
        {
            HunterAI h= Object.FindFirstObjectByType<HunterAI>();
            if (h) target= h.transform;
            return;
        }

        Vector3 desiredPos= target.position + offset;
        transform.position= Vector3.Lerp(transform.position, desiredPos, smoothTime);
    }
}