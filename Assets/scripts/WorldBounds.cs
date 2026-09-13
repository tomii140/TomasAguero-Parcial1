using UnityEngine;

public class WorldBounds : MonoBehaviour
{
    public float limitRadius= 60f;
    public float pushForce= 8f;
    private Agent myAgent;

    void Start() => myAgent= GetComponent<Agent>();

    void Update()
    {
        if (myAgent== null) return;

        if (Vector2.Distance(transform.position, Vector2.zero)> limitRadius)
        {
            myAgent.AddForce(myAgent.Seek(Vector2.zero)* pushForce);
        }
    }
}