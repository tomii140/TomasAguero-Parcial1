using UnityEngine;
using TMPro;

public class Boid : Agent
{
    public bool isDead = false;
    private int currentHealth = 2; // Muere al recibir 2 golpes

    [Header("Flocking Radii")]
    [SerializeField, Range(0.5f, 5f)] private float separationRadius = 2.0f;
    [SerializeField, Range(2f, 15f)] private float alignmentRadius = 6.0f;
    [SerializeField, Range(2f, 15f)] private float cohesionRadius = 6.0f;

    [Header("Flocking Weights")]
    [SerializeField, Range(0f, 5f)] private float separationWeight = 2.0f;
    [SerializeField, Range(0f, 5f)] private float alignmentWeight = 1.0f;
    [SerializeField, Range(0f, 5f)] private float cohesionWeight = 1.0f;

    [Header("Fruit Interaction")]
    [SerializeField, Range(1f, 20f)] private float fruitPerceptionRadius = 10f;
    [SerializeField, Range(0.5f, 5f)] private float fruitWeight = 2.0f;

    [Header("UI Feedback")]
    [SerializeField] private TextMeshPro stateTextUI;

    protected override void Start()
    {
        base.Start();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterBoid(this);
        }
        currentHealth = 2;
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
        currentHealth = 2;
    }

    public void ReceiveDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            isDead = true;
            Velocity = Vector3.zero;
        }
    }

    private void Update()
    {
        if (isDead)
        {
            Velocity = Vector3.zero;
            acceleration = Vector3.zero;
            UpdateDebugState("DEAD");
            return;
        }

        HunterAI hunter = GameManager.Instance != null ? GameManager.Instance.Hunter : null;

        // 1. Evade (Prioridad máxima frente al Cazador)
        if (hunter != null)
        {
            float distToHunter = Vector3.Distance(transform.position, hunter.transform.position);
            if (distToHunter < hunter.VisionRadius)
            {
                UpdateDebugState("EVADE");
                Vector3 evadeForce = Evade(hunter);
                Vector3 separationForce = GetSeparationForce() * separationWeight;
                AddForce(evadeForce + separationForce);
                ApplyPhysics();
                return;
            }
        }

        // 2. Seek Object & Consume (Frutas / Objetos de Interés)
        Fruit targetFruit = FindNearestFruit();
        if (targetFruit != null)
        {
            float distToFruit = Vector3.Distance(transform.position, targetFruit.transform.position);
            if (distToFruit <= targetFruit.ConsumeRadius)
            {
                UpdateDebugState("CONSUME");
                Velocity = Vector3.zero;
                acceleration = Vector3.zero;
                targetFruit.Consume(targetFruit.DamagePerSecond * Time.deltaTime);
            }
            else
            {
                UpdateDebugState("SEEK OBJECT");
                Vector3 arriveForce = Arrive(targetFruit.transform.position, 3.0f) * fruitWeight;
                Vector3 separationForce = GetSeparationForce() * separationWeight;
                AddForce(arriveForce + separationForce);
            }
            ApplyPhysics();
            return;
        }

        // 3. Flocking por defecto
        UpdateDebugState("FLOCKING");
        Vector3 flockingForce = CalculateFlocking();
        AddForce(flockingForce);
        ApplyPhysics();
    }

    private void UpdateDebugState(string state)
    {
        if (stateTextUI != null)
        {
            stateTextUI.text = state;
        }
    }

    private Fruit FindNearestFruit()
    {
        if (GameManager.Instance == null || GameManager.Instance.ActiveFruits == null) return null;

        Fruit nearest = null;
        float minDistance = float.MaxValue;

        foreach (var fruit in GameManager.Instance.ActiveFruits)
        {
            if (fruit == null) continue;
            float dist = Vector3.Distance(transform.position, fruit.transform.position);
            if (dist <= fruitPerceptionRadius && dist < minDistance)
            {
                minDistance = dist;
                nearest = fruit;
            }
        }
        return nearest;
    }

    private Vector3 CalculateFlocking()
    {
        return (GetSeparationForce() * separationWeight) +
               (GetAlignmentForce() * alignmentWeight) +
               (GetCohesionForce() * cohesionWeight);
    }

    private Vector3 GetSeparationForce()
    {
        if (GameManager.Instance == null || GameManager.Instance.ActiveBoids == null) return Vector3.zero;

        Vector3 force = Vector3.zero;
        int count = 0;

        foreach (var other in GameManager.Instance.ActiveBoids)
        {
            if (other != null && other != this && !other.isDead)
            {
                float d = Vector3.Distance(transform.position, other.transform.position);
                if (d > 0f && d < separationRadius)
                {
                    Vector3 diff = transform.position - other.transform.position;
                    diff.Normalize();
                    diff /= d; // Ponderación por distancia
                    force += diff;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            force /= count;
            force = force.normalized * maxSpeed;
            force -= Velocity;
            force = Vector3.ClampMagnitude(force, maxForce);
        }
        return force;
    }

    private Vector3 GetAlignmentForce()
    {
        if (GameManager.Instance == null || GameManager.Instance.ActiveBoids == null) return Vector3.zero;

        Vector3 sumVel = Vector3.zero;
        int count = 0;

        foreach (var other in GameManager.Instance.ActiveBoids)
        {
            if (other != null && other != this && !other.isDead)
            {
                float d = Vector3.Distance(transform.position, other.transform.position);
                if (d > 0f && d < alignmentRadius)
                {
                    sumVel += other.Velocity;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            sumVel /= count;
            sumVel = sumVel.normalized * maxSpeed;
            Vector3 steer = sumVel - Velocity;
            return Vector3.ClampMagnitude(steer, maxForce);
        }
        return Vector3.zero;
    }

    private Vector3 GetCohesionForce()
    {
        if (GameManager.Instance == null || GameManager.Instance.ActiveBoids == null) return Vector3.zero;

        Vector3 sumPos = Vector3.zero;
        int count = 0;

        foreach (var other in GameManager.Instance.ActiveBoids)
        {
            if (other != null && other != this && !other.isDead)
            {
                float d = Vector3.Distance(transform.position, other.transform.position);
                if (d > 0f && d < cohesionRadius)
                {
                    sumPos += other.transform.position;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            sumPos /= count;
            return Seek(sumPos);
        }
        return Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, alignmentRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, fruitPerceptionRadius);
    }
}