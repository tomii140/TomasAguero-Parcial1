using UnityEngine;

public class WorldBounds : MonoBehaviour
{
    [Header("Bounds Settings")]
    [SerializeField, Range(10f, 100f)] private float limitRadius = 25f;
    [SerializeField, Range(1f, 20f)] private float pushForce = 8f;

    private Agent myAgent;

    private void Start()
    {
        myAgent = GetComponent<Agent>();
    }

    private void Update()
    {
        if (myAgent == null) return;

        // Si el agente supera el radio del límite, aplica fuerza hacia el centro (0,0)
        if (Vector2.Distance(transform.position, Vector2.zero) > limitRadius)
        {
            myAgent.AddForce(myAgent.Seek(Vector2.zero) * pushForce);
        }
    }

    // Dibuja el límite del mapa con un círculo blanco en la vista Scene
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(Vector3.zero, limitRadius);
    }
}