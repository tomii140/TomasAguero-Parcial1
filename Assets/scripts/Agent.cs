using UnityEngine;

public abstract class Agent : MonoBehaviour 
{
    [Header("Steering Settings")]
    public float vMax = 5f;
    public float fMax = 5f;
    public Vector2 velocity;
    protected Vector2 aceleracion;
    protected SpriteRenderer miRender;

    protected virtual void Start() 
    {
        miRender = GetComponent<SpriteRenderer>();
        velocity = Random.insideUnitCircle * vMax;
    }

    protected void AplicarFisicas()
    {
        velocity += aceleracion * Time.deltaTime;
        velocity = Vector2.ClampMagnitude(velocity, vMax);
        transform.position += (Vector3)velocity * Time.deltaTime;
        aceleracion = Vector2.zero;

        if (velocity.x != 0 && miRender != null) 
        {
            miRender.flipX = velocity.x < 0;
        }
    }

    public void MeterFuerza(Vector2 f) => aceleracion += f;

    public Vector2 Seek(Vector2 target)
    {
        Vector2 deseada = (target - (Vector2)transform.position).normalized * vMax;
        return Vector2.ClampMagnitude(deseada - velocity, fMax);
    }

    public Vector2 Flee(Vector2 target)
    {
        Vector2 deseada = ((Vector2)transform.position - target).normalized * vMax;
        return Vector2.ClampMagnitude(deseada - velocity, fMax);
    }

    public Vector2 Arrive(Vector2 target, float rFrenado) 
    {
        Vector2 deseada = target - (Vector2)transform.position;
        float d = deseada.magnitude;
        if (d < 0.1f) return -velocity;

        float vC = (d < rFrenado) ? vMax * (d / rFrenado) : vMax;
        return Vector2.ClampMagnitude((deseada.normalized * vC) - velocity, fMax);
    }

    public Vector2 Evade(Agent target) 
    {
        if (target == null) return Vector2.zero;
        float dist = Vector2.Distance(transform.position, target.transform.position);
        float T = dist / vMax;
        Vector2 futuraPos = (Vector2)target.transform.position + target.velocity * T;
        return Flee(futuraPos);
    }
}