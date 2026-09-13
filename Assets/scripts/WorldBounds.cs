using UnityEngine;

public class WorldBounds : MonoBehaviour
{
    public float R_limite = 60f; 
    public float Fuerza_push = 8f;
    private Agent miAgente;

    void Start() => miAgente = GetComponent<Agent>();

    void Update()
    {
        if (miAgente == null) return;

        if (Vector2.Distance(transform.position, Vector2.zero) > R_limite)
        {
            miAgente.MeterFuerza(miAgente.Seek(Vector2.zero) * Fuerza_push);
        }
    }
}