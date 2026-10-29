using UnityEngine;

public abstract class Agent : MonoBehaviour
{
    [Header("Steering Parameters")]
    [SerializeField, Range(1f, 30f)] protected float maxSpeed = 5f;
    [SerializeField, Range(1f, 30f)] protected float maxForce = 5f;

    [Header("Arrive Settings")]
    [SerializeField, Range(0.01f, 0.5f)] private float stopThreshold = 0.05f;

    public Vector3 Velocity { get; protected set; }
    protected Vector3 acceleration;
    protected MeshRenderer agentMeshRenderer;

    public float MaxSpeed => maxSpeed;
    public float MaxForce => maxForce;

    protected virtual void Start()
    {
        agentMeshRenderer = GetComponent<MeshRenderer>();
        Vector2 randomCircle = Random.insideUnitCircle * maxSpeed;
        Velocity = new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    public void ResetVelocity()
    {
        Velocity = Vector3.zero;
        acceleration = Vector3.zero;
    }

    protected void ApplyPhysics()
    {
        Velocity += acceleration * Time.deltaTime;
        Velocity = Vector3.ClampMagnitude(Velocity, maxSpeed);

        Vector3 pos = transform.position;
        pos += Velocity * Time.deltaTime;
        pos.y = 0f;
        transform.position = pos;

        acceleration = Vector3.zero;

        if (Velocity.sqrMagnitude > 0.001f)
        {
            transform.forward = Velocity.normalized;
        }
    }

    public void AddForce(Vector3 forceVector) => acceleration += forceVector;

    public Vector3 Seek(Vector3 targetPosition)
    {
        targetPosition.y = transform.position.y;
        Vector3 desiredVelocity = (targetPosition - transform.position).normalized * maxSpeed;
        return Vector3.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }

    public Vector3 Pursuit(Agent targetAgent)
    {
        if (targetAgent == null) return Vector3.zero;

        float distanceToTarget = Vector3.Distance(transform.position, targetAgent.transform.position);
        float predictionTime = distanceToTarget / maxSpeed;
        Vector3 futurePosition = targetAgent.transform.position + targetAgent.Velocity * predictionTime;

        return Seek(futurePosition);
    }

    public Vector3 Flee(Vector3 targetPosition)
    {
        targetPosition.y = transform.position.y;
        Vector3 desiredVelocity = (transform.position - targetPosition).normalized * maxSpeed;
        return Vector3.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }

    public Vector3 Arrive(Vector3 targetPosition, float slowingRadius)
    {
        targetPosition.y = transform.position.y;
        Vector3 desiredVector = targetPosition - transform.position;
        float distanceToTarget = desiredVector.magnitude;

        if (distanceToTarget < stopThreshold) return -Velocity;

        float calculatedSpeed = (distanceToTarget < slowingRadius) ? maxSpeed * (distanceToTarget / slowingRadius) : maxSpeed;
        Vector3 desiredVelocity = desiredVector.normalized * calculatedSpeed;
        return Vector3.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }

    public Vector3 Evade(Agent targetAgent)
    {
        if (targetAgent == null) return Vector3.zero;

        float distanceToTarget = Vector3.Distance(transform.position, targetAgent.transform.position);
        float predictionTime = distanceToTarget / maxSpeed;
        Vector3 futurePosition = targetAgent.transform.position + targetAgent.Velocity * predictionTime;

        return Flee(futurePosition);
    }
}