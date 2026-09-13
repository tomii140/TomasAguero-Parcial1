using UnityEngine;

public class CamaraSeguidora : MonoBehaviour
{
    public Transform objetivo; 
    [Range(0, 1)] public float suavizado = 0.125f; 
    public Vector3 offset = new Vector3(0, 0, -10); 

    void FixedUpdate()
    {
        if (objetivo == null)
        {
            HunterAI h = FindObjectOfType<HunterAI>();
            if (h) objetivo = h.transform;
            return;
        }

        Vector3 posicionDeseada = objetivo.position + offset;
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavizado);
    }
}