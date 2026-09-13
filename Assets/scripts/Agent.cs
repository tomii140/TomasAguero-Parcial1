using UnityEngine;

public abstract class Agent : MonoBehaviour
{
    [Header("Steering Settings")]
    public float vMax= 5f;
    public float fMax= 5f;
    public Vector2 velocity;
    protected Vector2 acceleration;
    protected SpriteRenderer myRenderer;

    protected virtual void Start()
    {
        myRenderer= GetComponent<SpriteRenderer>();
        velocity= Random.insideUnitCircle* vMax;
    }

    protected void ApplyPhysics()
    {
        velocity += acceleration* Time.deltaTime;
        velocity= Vector2.ClampMagnitude(velocity, vMax);
        transform.position += (Vector3)velocity* Time.deltaTime;
        acceleration= Vector2.zero;

        if (velocity.x != 0 && myRenderer != null)
        {
            myRenderer.flipX= velocity.x< 0;
        }
    }

    public void AddForce(Vector2 f) => acceleration += f;

    public Vector2 Seek(Vector2 target)
    {
        Vector2 desired= (target - (Vector2)transform.position).normalized* vMax;
        return Vector2.ClampMagnitude(desired - velocity, fMax);
    }

    public Vector2 Flee(Vector2 target)
    {
        Vector2 desired= ((Vector2)transform.position - target).normalized* vMax;
        return Vector2.ClampMagnitude(desired - velocity, fMax);
    }

    public Vector2 Arrive(Vector2 target, float brakeRadius)
    {
        Vector2 desired= target - (Vector2)transform.position;
        float d= desired.magnitude;
        if (d< 0.1f) return -velocity;

        float speed= (d< brakeRadius) ? vMax* (d / brakeRadius) : vMax;
        return Vector2.ClampMagnitude((desired.normalized* speed) - velocity, fMax);
    }

    public Vector2 Evade(Agent target)
    {
        if (target== null) return Vector2.zero;
        float dist= Vector2.Distance(transform.position, target.transform.position);
        float t= dist / vMax;
        Vector2 futurePos= (Vector2)target.transform.position + target.velocity* t;
        return Flee(futurePos);
    }
}