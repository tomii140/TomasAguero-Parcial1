using UnityEngine;

public class WorldBounds : MonoBehaviour
{
    [Header("Bounds Settings")]
    [SerializeField, Range(10f, 100f)] private float limitRadius = 60f;
    [SerializeField, Range(1f, 20f)] private float pushForce = 8f;

    private Agent myAgent;

    private void Start()
    {
        myAgent = GetComponent<Agent>();
    }

    private void Update()
    {
        if (myAgent == null) return;

        if (Vector2.Distance(transform.position, Vector2.zero) > limitRadius)
        {
            myAgent.AddForce(myAgent.Seek(Vector2.zero) * pushForce);
        }
    }
}