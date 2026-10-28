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

    [Header("Evade Settings")]
    [SerializeField, Range(0f, 2f)] private float sideEscapeWeight = 0.4f;
    [SerializeField, Range(1f, 5f)] private float maxEvadeForceMultiplier = 2.0f;

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
        Vector2 randomCircle = Random.insideUnitCircle * maxSpeed;
        Velocity = new Vector3(randomCircle.x, 0f, randomCircle.y);
        UpdateDebugText("FLOCKING");
    }

    private void Update()
    {
        if (isDead)
        {
            Velocity = Vector3.zero;
            acceleration = Vector3.zero;
            UpdateDebugText("DEAD");
            return;
        }

        HunterAI hunter = GameManager.Instance != null ? GameManager.Instance.Hunter : null;

        // 1. Huida prioritaria del Cazador
        if (hunter != null)
        {
            float distToHunter = Vector3.Distance(transform.position, hunter.transform.position);
            if (distToHunter < hunter.VisionRadius)
            {
                UpdateDebugText("EVADING");
                Vector3 evadeForce = Evade(hunter);

                // Evasión lateral en el plano XZ usando rotación ortogonal de la velocidad del cazador
                Vector3 sideEscape = new Vector3(-hunter.Velocity.z, 0f, hunter.Velocity.x).normalized;
                evadeForce += sideEscape * maxForce * sideEscapeWeight;

                Vector3 pureEvade = Vector3.ClampMagnitude(evadeForce, maxForce * maxEvadeForceMultiplier);

                AddForce(pureEvade);
                ApplyPhysics();
                return;
            }
        }

        // 2. Comportamiento Estándar
        Vector3 totalSteering = Vector3.zero;
        Fruit targetFruit = FindNearestFruit();

        if (targetFruit != null)
        {
            float distToFruit = Vector3.Distance(transform.position, targetFruit.transform.position);

            if (distToFruit <= targetFruit.ConsumeRadius)
            {
                UpdateDebugText("EAT");
                Velocity = Vector3.zero;
                acceleration = Vector3.zero;
                targetFruit.Consume(targetFruit.DamagePerSecond * Time.deltaTime);
            }
            else
            {
                UpdateDebugText("SEEK FRUIT");

                Vector3 fruitSeek = Seek(targetFruit.transform.position) * fruitWeight;
                Vector3 softSeparation = GetSeparationForce() * (separationWeight * eatingSeparationFactor);

                totalSteering = fruitSeek + softSeparation;
            }
        }
        else
        {
            UpdateDebugText("FLOCKING");
            totalSteering += CalculateFlocking();
        }

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
            float dist = Vector3.Distance(transform.position, fruit.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = fruit;
            }
        }

        return nearest;
    }

    private Vector3 CalculateFlocking()
    {
        Vector3 force = Vector3.zero;
        force += GetCohesionForce() * cohesionWeight;
        force += GetAlignmentForce() * alignmentWeight;
        force += GetSeparationForce() * separationWeight;
        return force;
    }

    private Vector3 GetCohesionForce()
    {
        Vector3 centerOfMass = Vector3.zero;
        int count = 0;

        foreach (var other in GameManager.Instance.ActiveBoids)
        {
            if (other == this || other == null || other.isDead) continue;
            float d = Vector3.Distance(transform.position, other.transform.position);
            if (d < cohesionRadius && d > 0.01f)
            {
                centerOfMass += other.transform.position;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;
        centerOfMass /= count;
        return Seek(centerOfMass);
    }

    private Vector3 GetAlignmentForce()
    {
        Vector3 avgVelocity = Vector3.zero;
        int count = 0;

        foreach (var other in GameManager.Instance.ActiveBoids)
        {
            if (other == this || other == null || other.isDead) continue;
            float d = Vector3.Distance(transform.position, other.transform.position);
            if (d < alignmentRadius && d > 0.01f)
            {
                avgVelocity += other.Velocity;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;
        avgVelocity /= count;
        Vector3 desiredVelocity = avgVelocity.normalized * maxSpeed;
        return Vector3.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }

    private Vector3 GetSeparationForce()
    {
        Vector3 separationVector = Vector3.zero;
        int count = 0;

        foreach (var other in GameManager.Instance.ActiveBoids)
        {
            if (other == this || other == null || other.isDead) continue;
            float d = Vector3.Distance(transform.position, other.transform.position);
            if (d < separationRadius && d > 0.01f)
            {
                Vector3 repulseVector = transform.position - other.transform.position;
                repulseVector.y = 0f;
                separationVector += repulseVector.normalized / d;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;
        separationVector /= count;
        Vector3 desiredVelocity = separationVector.normalized * maxSpeed;
        return Vector3.ClampMagnitude(desiredVelocity - Velocity, maxForce);
    }
}