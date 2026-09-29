using UnityEngine;
using TMPro;

public class Boid : Agent
{
    public bool isDead = false;

    [Header("Flocking Radii")]
    [SerializeField, Range(0.5f, 10f)] private float separationRadius = 2.5f;
    [SerializeField, Range(1f, 20f)] private float cohesionRadius = 8f;
    [SerializeField, Range(1f, 20f)] private float alignmentRadius = 8f;

    [Header("Flocking Weights")]
    [SerializeField, Range(0f, 5f)] private float separationWeight = 2.5f;
    [SerializeField, Range(0f, 5f)] private float cohesionWeight = 0.8f;
    [SerializeField, Range(0f, 5f)] private float alignmentWeight = 0.5f;

    [Header("Evade & Limits Settings")]
    [SerializeField, Range(0f, 2f)] private float sideEscapeWeight = 0.4f;
    [SerializeField, Range(1f, 5f)] private float maxEvadeForceMultiplier = 2.0f;
    [SerializeField, Range(5f, 50f)] private float worldLimitRadius = 15f;
    [SerializeField, Range(1f, 10f)] private float wallPushWeight = 3f;

    [Header("Fruit Interaction Settings")]
    [SerializeField, Range(1f, 30f)] private float fruitDetectionRadius = 15f;
    [SerializeField, Range(0f, 5f)] private float fruitWeight = 2.5f;
    [SerializeField, Range(0f, 1f)] private float eatingSeparationFactor = 0.3f;

    [Header("UI Feedback")]
    [SerializeField] private TextMeshPro stateTextUI;

    public string CurrentStateDebug { get; private set; } = "Flocking";

    protected override void Start()
    {
        base.Start();

        if (stateTextUI == null)
        {
            stateTextUI = GetComponentInChildren<TextMeshPro>();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterBoid(this);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterBoid(this);
        }
    }

    public void Revive()
    {
        isDead = false;
        Velocity = Random.insideUnitCircle * maxSpeed;
        UpdateDebugText("FLOCKING");
    }

    private void Update()
    {
        if (isDead)
        {
            Velocity = Vector2.zero;
            acceleration = Vector2.zero;
            UpdateDebugText("DEAD");
            return;
        }

        HunterAI hunter = GameManager.Instance != null ? GameManager.Instance.Hunter : null;

        // 1. Huida prioritaria del Cazador
        if (hunter != null)
        {
            float distToHunter = Vector2.Distance(transform.position, hunter.transform.position);
            if (distToHunter < hunter.VisionRadius)
            {
                UpdateDebugText("EVADING");
                
                Vector2 evadeForce = Evade(hunter);
                Vector2 sideEscape = new Vector2(-hunter.Velocity.y, hunter.Velocity.x).normalized;
                evadeForce += sideEscape * maxForce * sideEscapeWeight;

                Vector2 pureEvade = Vector2.ClampMagnitude(evadeForce, maxForce * maxEvadeForceMultiplier);
                
                AddForce(pureEvade);
                ApplyPhysics();
                return;
            }
        }

        // 2. Comportamiento Estándar
        Vector2 totalSteering = Vector2.zero;
        Fruit targetFruit = FindNearestFruit();

        if (targetFruit != null)
        {
            float distToFruit = Vector2.Distance(transform.position, targetFruit.transform.position);

            if (distToFruit <= targetFruit.ConsumeRadius)
            {
                UpdateDebugText("EAT");
                Velocity = Vector2.zero;
                acceleration = Vector2.zero;
                targetFruit.Consume(targetFruit.DamagePerSecond * Time.deltaTime);
            }
            else
            {
                UpdateDebugText("SEEK FRUIT");
                
                Vector2 fruitSeek = Seek(targetFruit.transform.position) * fruitWeight;
                Vector2 softSeparation = GetSeparationForce() * (separationWeight * eatingSeparationFactor); 
                
                totalSteering = fruitSeek + softSeparation;
            }
        }
        else
        {
            UpdateDebugText("FLOCKING");
            totalSteering += CalculateFlocking();
        }

        totalSteering += CalculateWorldBoundsForce();

        AddForce(totalSteering);
        ApplyPhysics();
    }

    private void UpdateDebugText(string state)
    {
        CurrentStateDebug = state;
        if (stateTextUI != null)
        {
            stateTextUI.text = state;
        }
    }

    private Fruit FindNearestFruit()
    {
        if (GameManager.Instance == null) return null;

        Fruit nearest = null;
        float minDistance = fruitDetectionRadius;

        foreach (var fruit in GameManager.Instance.ActiveFruits)
        {
            if (fruit == null) continue;
            float dist = Vector2.Distance(transform.position, fruit.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = fruit;
            }
        }

        return nearest;
    }

    private Vector2 CalculateWorldBoundsForce()
    {
        if (Vector2.Distance(transform.position, Vector2.zero) > worldLimitRadius)
        {
            return Seek(Vector2.zero) * wallPushWeight;
        }
        return Vector2.zero;
    }

    private Vector2 CalculateFlocking()
    {
        Vector2 force = Vector2.zero;
        force += GetCohesionForce() * cohesionWeight;
        force += GetAlignmentForce() * alignmentWeight;
        force += GetSeparationForce() * separationWeight;
        return force;
    }

    private Vector2 GetCohesionForce()
    {
        Vector2 centerOfMass = Vector2.zero;
        int count = 0;

        foreach (var other in GameManager.Instance.ActiveBoids)
        {
            if (other == this || other == null || other.isDead) continue;
            float d = Vector2.Distance(transform.position, other.transform.position);
            if (d < cohesionRadius && d > 0.01f)
            {
                centerOfMass += (Vector2)other.transform.position;
                count++;
            }
        }

        if (count == 0) return Vector2.zero;
        centerOfMass /= count;
        return Seek(centerOfMass);
    }

    private Vector2 GetAlignmentForce()
    {
        Vector2 avgVelocity = Vector2.zero;
        int count = 0;

        foreach (var other in GameManager.Instance.ActiveBoids)
        {
            if (other == this || other == null || other.isDead) continue;
            float d = Vector2.Distance(transform.position, other.transform.position);
            if (d < alignmentRadius && d > 0.01f)
            {
                avgVelocity += other.Velocity;
                count++;
            }
        }

        if (count == 0) return Vector2.zero;
        avgVelocity /= count;
        
        Vector2 desired = avgVelocity.normalized * maxSpeed;
        return Vector2.ClampMagnitude(desired - Velocity, maxForce);
    }

    private Vector2 GetSeparationForce()
    {
        Vector2 separationVector = Vector2.zero;
        int count = 0;

        foreach (var other in GameManager.Instance.ActiveBoids)
        {
            if (other == this || other == null || other.isDead) continue;

            float distance = Vector2.Distance(transform.position, other.transform.position);

            if (distance < separationRadius && distance > 0.001f)
            {
                // Repulsión inversamente proporcional a la distancia
                Vector2 pushDirection = (Vector2)(transform.position - other.transform.position);
                separationVector += pushDirection.normalized / distance;
                count++;
            }
        }

        if (count == 0) return Vector2.zero;

        Vector2 desiredVelocity = (separationVector / count).normalized * maxSpeed;
        return Vector2.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }
}