using UnityEngine;
using TMPro;

public class Boid : Agent
{
    private FiniteStateMachine fsm;

    public BoidFlockingState FlockingState { get; private set; }
    public BoidEvadeState EvadeState { get; private set; }
    public BoidSeekObjectState SeekObjectState { get; private set; }
    public BoidConsumeState ConsumeState { get; private set; }
    public BoidDeadState DeadState { get; private set; }

    public bool isDead = false;
    private int currentHealth = 2;

    [Header("Flocking Radii & Weights")]
    [SerializeField, Range(0.5f, 30f)] private float separationRadius = 2.0f;
    [SerializeField, Range(2f, 30f)] private float alignmentRadius = 6.0f;
    [SerializeField, Range(2f, 30f)] private float cohesionRadius = 6.0f;
    [SerializeField, Range(0f, 30f)] private float separationWeight = 2.0f;
    [SerializeField, Range(0f, 30f)] private float alignmentWeight = 1.0f;
    [SerializeField, Range(0f, 30f)] private float cohesionWeight = 1.0f;

    public float SeparationWeight => separationWeight;

    [Header("Perception Sensors")]
    [SerializeField, Range(1f, 30f)] private float hunterPerceptionRadius = 10f; // Sensor propio para ver al Cazador
    [SerializeField, Range(1f, 30f)] private float fruitPerceptionRadius = 10f;  // Sensor propio para ver frutas
    [SerializeField, Range(0.5f, 30f)] private float fruitWeight = 2.0f;

    public float HunterPerceptionRadius => hunterPerceptionRadius;
    public float FruitWeight => fruitWeight;

    [Header("UI Feedback")]
    [SerializeField] private TextMeshPro stateTextUI;
    private MeshRenderer meshRenderer;

    protected override void Start()
    {
        base.Start();
        meshRenderer = GetComponent<MeshRenderer>();
        if (GameManager.Instance != null) GameManager.Instance.RegisterBoid(this);

        fsm = new FiniteStateMachine();
        FlockingState = new BoidFlockingState(this);
        EvadeState = new BoidEvadeState(this);
        SeekObjectState = new BoidSeekObjectState(this);
        ConsumeState = new BoidConsumeState(this);
        DeadState = new BoidDeadState(this);

        fsm.ChangeState(FlockingState);
        currentHealth = 2;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.UnregisterBoid(this);
    }

    public void Revive()
    {
        isDead = false;
        currentHealth = 2;
        fsm.ChangeState(FlockingState);
    }

    public void ReceiveDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            isDead = true;
            fsm.ChangeState(DeadState);
        }
    }

    private void Update()
    {
        EvaluateFSMTransitions();
        fsm.Update();
        ApplyPhysics();
        UpdateDebugText();
    }

    private void EvaluateFSMTransitions()
    {
        if (isDead)
        {
            if (fsm.CurrentState != DeadState) fsm.ChangeState(DeadState);
            return;
        }

        // 1. Evade (Usa el sensor propio del Boid para detectar al Cazador)
        HunterAI hunter = GameManager.Instance != null ? GameManager.Instance.Hunter : null;
        if (hunter != null)
        {
            float distToHunter = Vector3.Distance(transform.position, hunter.transform.position);
            if (distToHunter < hunterPerceptionRadius)
            {
                if (fsm.CurrentState != EvadeState) fsm.ChangeState(EvadeState);
                return;
            }
        }

        // 2. Consume / Seek Object (Usa el sensor propio para frutas)
        Fruit targetFruit = FindNearestFruit();
        if (targetFruit != null)
        {
            float distToFruit = Vector3.Distance(transform.position, targetFruit.transform.position);
            if (distToFruit <= targetFruit.ConsumeRadius)
            {
                if (fsm.CurrentState != ConsumeState) fsm.ChangeState(ConsumeState);
            }
            else
            {
                if (fsm.CurrentState != SeekObjectState) fsm.ChangeState(SeekObjectState);
            }
            return;
        }

        // 3. Flocking por defecto
        if (fsm.CurrentState != FlockingState)
        {
            fsm.ChangeState(FlockingState);
        }
    }

    public void SetColorFeedback(Color color)
    {
        if (meshRenderer != null) meshRenderer.material.color = color;
    }

    private void UpdateDebugText()
    {
        if (stateTextUI != null && fsm.CurrentState != null)
        {
            string stateName = fsm.CurrentState.GetType().Name.Replace("Boid", "").Replace("State", "");
            stateTextUI.text = stateName;
        }
    }

    public Fruit FindNearestFruit()
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

    public Vector3 CalculateFlocking()
    {
        return (GetSeparationForce() * separationWeight) +
               (GetAlignmentForce() * alignmentWeight) +
               (GetCohesionForce() * cohesionWeight);
    }

    public Vector3 GetSeparationForce()
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
                    diff /= d;
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

    private void OnDrawGizmos()
    {
        // Gizmos obligatorios según rúbrica
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, alignmentRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, cohesionRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, hunterPerceptionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fruitPerceptionRadius);
    }
}