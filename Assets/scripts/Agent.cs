using UnityEngine;

public abstract class Agent : MonoBehaviour
{
    [Header("Steering Parameters")]
    [SerializeField] protected float maxSpeed = 5f;
    [SerializeField] protected float maxForce = 5f;

    public Vector2 Velocity { get; protected set; }
    protected Vector2 acceleration;
    protected SpriteRenderer agentRenderer;

    public float MaxSpeed => maxSpeed;
    public float MaxForce => maxForce;

    protected virtual void Start()
    {
        agentRenderer = GetComponent<SpriteRenderer>();
        Velocity = Random.insideUnitCircle * maxSpeed;
    }

    protected void ApplyPhysics()
    {
        Velocity += acceleration * Time.deltaTime;
        Velocity = Vector2.ClampMagnitude(Velocity, maxSpeed);
        transform.position += (Vector3)Velocity * Time.deltaTime;
        acceleration = Vector2.zero;

        if (Velocity.x != 0 && agentRenderer != null)
        {
            agentRenderer.flipX = Velocity.x < 0;
        }
    }

    public void AddForce(Vector2 forceVector) => acceleration += forceVector;

    public Vector2 Seek(Vector2 targetPosition)
    {
        Vector2 desiredVelocity = (targetPosition - (Vector2)transform.position).normalized * maxSpeed;
        return Vector2.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }

    public Vector2 Pursuit(Agent targetAgent)
    {
        if (targetAgent == null) return Vector2.zero;

        float distanceToTarget = Vector2.Distance(transform.position, targetAgent.transform.position);
        float predictionTime = distanceToTarget / maxSpeed;
        Vector2 futurePosition = (Vector2)targetAgent.transform.position + targetAgent.Velocity * predictionTime;

        return Seek(futurePosition);
    }

    public Vector2 Flee(Vector2 targetPosition)
    {
        Vector2 desiredVelocity = ((Vector2)transform.position - targetPosition).normalized * maxSpeed;
        return Vector2.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }

    public Vector2 Arrive(Vector2 targetPosition, float slowingRadius)
    {
        Vector2 desiredVector = targetPosition - (Vector2)transform.position;
        float distanceToTarget = desiredVector.magnitude;
        
        if (distanceToTarget < 0.05f) return -Velocity;

        float calculatedSpeed = (distanceToTarget < slowingRadius) ? maxSpeed * (distanceToTarget / slowingRadius) : maxSpeed;
        Vector2 desiredVelocity = desiredVector.normalized * calculatedSpeed;
        return Vector2.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }

    public Vector2 Evade(Agent targetAgent)
    {
        if (targetAgent == null) return Vector2.zero;
        
        float distanceToTarget = Vector2.Distance(transform.position, targetAgent.transform.position);
        float predictionTime = distanceToTarget / maxSpeed;
        Vector2 futurePosition = (Vector2)targetAgent.transform.position + targetAgent.Velocity * predictionTime;
        
        return Flee(futurePosition);
    }
}