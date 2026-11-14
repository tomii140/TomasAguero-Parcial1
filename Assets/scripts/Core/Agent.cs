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
        // Inicializo una velocidad aleatoria para evitar que todos los agentes arranquen estáticos o en formación perfecta.
        Vector2 randomCircle = Random.insideUnitCircle * maxSpeed;
        Velocity = new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    public void ResetVelocity()
    {
        Velocity = Vector3.zero;
        acceleration = Vector3.zero;
    }

    protected virtual void ApplyPhysics()
    {
        // Integración de Euler actualizo velocidad limitándola a la máxima permitida y calculo la posición objetivo.
        Velocity += acceleration * Time.deltaTime;
        Velocity = Vector3.ClampMagnitude(Velocity, maxSpeed);

        Vector3 displacement = Velocity * Time.deltaTime;
        Vector3 targetPos = transform.position + displacement;

        // Delegación de límites espaciales según el tipo de agente (Wrap para Boids, Clamp con rebote para el Cazador).
        if (WorldBounds.Instance != null)
        {
            if (this is Boid)
            {
                if (targetPos.x < WorldBounds.Instance.MinX || targetPos.x > WorldBounds.Instance.MaxX ||
                    targetPos.z < WorldBounds.Instance.MinZ || targetPos.z > WorldBounds.Instance.MaxZ)
                {
                    Debug.Log($"<color=cyan>[BOID AL LÍMITE]</color> {gameObject.name} intentó salir en pos: {targetPos}");
                }

                targetPos = WorldBounds.Instance.WrapPosition(targetPos);
            }
            else
            {
                Vector3 currentVel = Velocity;
                targetPos = WorldBounds.Instance.ClampPosition(ref targetPos, ref currentVel);
                Velocity = currentVel;
            }
        }

        transform.position = targetPos;
        acceleration = Vector3.zero;

        // Orientación suave hacia el vector de movimiento actual usando Slerp.
        if (Velocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(Velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    public void AddForce(Vector3 forceVector) => acceleration += forceVector;

    // STEERING BEHAVIOURS

    public Vector3 Seek(Vector3 targetPosition)
    {
        targetPosition.y = transform.position.y;
        Vector3 desiredVelocity = (targetPosition - transform.position).normalized * maxSpeed;
        return Vector3.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }

    public Vector3 Pursuit(Agent targetAgent)
    {
        if (targetAgent == null) return Vector3.zero;

        // Predicción lineal de la posición futura del objetivo basada en su velocidad actual.
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

        // Desaceleración proporcional a la distancia dentro del radio de frenado.
        float calculatedSpeed = (distanceToTarget < slowingRadius) ? maxSpeed * (distanceToTarget / slowingRadius) : maxSpeed;
        Vector3 desiredVelocity = desiredVector.normalized * calculatedSpeed;
        return Vector3.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }

    public Vector3 Evade(Agent targetAgent)
    {
        if (targetAgent == null) return Vector3.zero;

        // Evasión con predicción de trayectoria del cazador para maniobras de huida anticipadas.
        float distanceToTarget = Vector3.Distance(transform.position, targetAgent.transform.position);
        float predictionTime = distanceToTarget / maxSpeed;
        Vector3 futurePosition = targetAgent.transform.position + targetAgent.Velocity * predictionTime;

        return Flee(futurePosition);
    }
}