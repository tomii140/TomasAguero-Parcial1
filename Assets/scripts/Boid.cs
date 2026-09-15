using UnityEngine;
using TMPro;

public class Boid : Agent
{
    public bool isDead = false;
    private HunterAI cachedHunter;

    [Header("Flocking Radii & Weights")]
    [SerializeField] private float separationRadius = 2.5f;
    [SerializeField] private float cohesionRadius = 8f;
    [SerializeField] private float alignmentRadius = 8f;

    [Header("Flocking Weights")]
    [SerializeField, Range(0f, 5f)] private float separationWeight = 2.5f;
    [SerializeField, Range(0f, 5f)] private float cohesionWeight = 0.8f;
    [SerializeField, Range(0f, 5f)] private float alignmentWeight = 0.5f;

    [Header("Evade & Boundaries")]
    [SerializeField] private float sideEscapeWeight = 0.4f;
    [SerializeField] private float maxEvadeForceMultiplier = 2.0f;
    [SerializeField] private float worldLimitRadius = 15f;
    [SerializeField] private float wallPushWeight = 3f;

    [Header("Fruit Interaction")]
    [SerializeField] private float fruitDetectionRadius = 15f;
    [SerializeField, Range(0f, 5f)] private float fruitWeight = 2.5f;

    [Header("Debug Visual UI")]
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
            GameManager.Instance.RegisterBoid(this);

        cachedHunter = FindFirstObjectByType<HunterAI>();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterBoid(this);
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

        // 1. Huida del Cazador (Prioridad 1 Absoluta)
        if (cachedHunter != null)
        {
            float distToHunter = Vector2.Distance(transform.position, cachedHunter.transform.position);
            if (distToHunter < cachedHunter.VisionRadius)
            {
                UpdateDebugText("EVADING");
                
                Vector2 evadeForce = Evade(cachedHunter);
                Vector2 sideEscape = new Vector2(-cachedHunter.Velocity.y, cachedHunter.Velocity.x).normalized;
                evadeForce += sideEscape * maxForce * sideEscapeWeight;

                Vector2 pureEvade = Vector2.ClampMagnitude(evadeForce, maxForce * maxEvadeForceMultiplier);
                
                AddForce(pureEvade);
                ApplyPhysics();
                return; // Ignora frutas y flocking mientras escapa
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
                targetFruit.Consume(targetFruit.DamagePerSecond * Time.deltaTime);
            }
            else
            {
                UpdateDebugText("SEEK FRUIT");
                
                Vector2 fruitSeek = Seek(targetFruit.transform.position) * fruitWeight;
                Vector2 softSeparation = GetSeparationForce() * (separationWeight * 0.3f); 
                
                totalSteering = fruitSeek + softSeparation;

                if (Velocity.sqrMagnitude < 0.2f)
                {
                    Vector2 dirToFruit = ((Vector2)targetFruit.transform.position - (Vector2)transform.position).normalized;
                    Velocity = dirToFruit * (maxSpeed * 0.5f);
                }
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
            if (other == this || other.isDead) continue;
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
            if (other == this || other.isDead) continue;
            float d = Vector2.Distance(transform.position, other.transform.position);
            if (d < alignmentRadius && d > 0.01f)
            {
                avgVelocity += other.Velocity;
                count++;
            }
        }

        if (count == 0) return Vector2.zero;
        avgVelocity /= count;
        Vector2 desiredVelocity = avgVelocity.normalized * maxSpeed;
        return Vector2.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }

    private Vector2 GetSeparationForce()
    {
        Vector2 separationSteer = Vector2.zero;
        int count = 0;

        foreach (var other in GameManager.Instance.ActiveBoids)
        {
            if (other == this || other.isDead) continue;
            float d = Vector2.Distance(transform.position, other.transform.position);
            if (d < separationRadius && d > 0.01f)
            {
                Vector2 diff = (Vector2)transform.position - (Vector2)other.transform.position;
                separationSteer += diff.normalized / d;
                count++;
            }
        }

        if (count == 0) return Vector2.zero;
        separationSteer /= count;
        Vector2 desiredVelocity = separationSteer.normalized * maxSpeed;
        return Vector2.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }
}