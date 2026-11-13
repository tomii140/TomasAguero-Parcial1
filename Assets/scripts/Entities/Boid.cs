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
    public BoidRespawnState RespawnState { get; private set; }

    public bool IsDead { get; private set; } = false;
    public bool IsRespawning { get; private set; } = false;
    private int currentHealth = 2;

    [Header("Flocking Configuration")]
    [SerializeField, Range(0.5f, 30f)] private float separationRadius = 2.0f;
    [SerializeField, Range(2f, 30f)] private float alignmentRadius = 12.0f;
    [SerializeField, Range(2f, 30f)] private float cohesionRadius = 12.0f;
    [SerializeField, Range(0f, 30f)] private float separationWeight = 2.0f;
    [SerializeField, Range(0f, 30f)] private float alignmentWeight = 1.0f;
    [SerializeField, Range(0f, 30f)] private float cohesionWeight = 1.5f;

    public float SeparationWeight => separationWeight;

    [Header("Perception Sensors")]
    [SerializeField, Range(1f, 30f)] private float hunterPerceptionRadius = 10f;
    [SerializeField, Range(1f, 30f)] private float fruitPerceptionRadius = 10f;
    [SerializeField, Range(0.5f, 30f)] private float fruitWeight = 2.0f;
    [SerializeField, Range(0.5f, 10f)] private float fruitArriveSlowingRadius = 3.0f;

    public float HunterPerceptionRadius => hunterPerceptionRadius;
    public float FruitWeight => fruitWeight;
    public float FruitArriveSlowingRadius => fruitArriveSlowingRadius;

    [Header("Respawn Configuration")]
    [SerializeField, Range(1f, 10f)] private float minRespawnTime = 3f;
    [SerializeField, Range(1f, 10f)] private float maxRespawnTime = 6f;

    public float MinRespawnTime => minRespawnTime;
    public float MaxRespawnTime => maxRespawnTime;

    [Header("Feedback")]
    [SerializeField] private TextMeshPro stateTextUI;
    private MeshRenderer meshRenderer;
    private Animator animator;

    protected override void Start()
    {
        base.Start();
        meshRenderer = GetComponent<MeshRenderer>();
        animator = GetComponent<Animator>();

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterBoid(this);

        InitializeFSM();
        currentHealth = 2;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterBoid(this);
    }

    private void InitializeFSM()
    {
        fsm = new FiniteStateMachine();
        FlockingState = new BoidFlockingState(this);
        EvadeState = new BoidEvadeState(this);
        SeekObjectState = new BoidSeekObjectState(this);
        ConsumeState = new BoidConsumeState(this);
        DeadState = new BoidDeadState(this);
        RespawnState = new BoidRespawnState(this);

        fsm.ChangeState(FlockingState);
    }

    public void Revive()
    {
        IsDead = false;
        IsRespawning = false;
        currentHealth = 2;
        SetColorFeedback(Color.green);

        Velocity = transform.forward * (maxSpeed * 0.5f);
        fsm.ChangeState(FlockingState);
    }

    public void ReceiveDamage(int damage)
    {
        if (IsDead || IsRespawning) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            IsDead = true;
            fsm.ChangeState(DeadState);
        }
    }

    public void TriggerRespawnFlow()
    {
        IsRespawning = true;
        fsm.ChangeState(RespawnState);
    }

    private void Update()
    {
        EvaluateFSMTransitions();
        fsm.Update();
        ApplyPhysics();
        UpdateUIFeedback();
        UpdateAnimator();
    }

    private void EvaluateFSMTransitions()
    {
        if (IsRespawning)
        {
            if (fsm.CurrentState != RespawnState) fsm.ChangeState(RespawnState);
            return;
        }

        if (IsDead)
        {
            if (fsm.CurrentState != DeadState) fsm.ChangeState(DeadState);
            return;
        }

        HunterAI hunter = GameManager.Instance != null ? GameManager.Instance.Hunter : null;
        if (hunter != null && Vector3.Distance(transform.position, hunter.transform.position) < hunterPerceptionRadius)
        {
            if (fsm.CurrentState != EvadeState) fsm.ChangeState(EvadeState);
            return;
        }

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

        if (fsm.CurrentState != FlockingState)
        {
            fsm.ChangeState(FlockingState);
        }
    }

    public void SetColorFeedback(Color color)
    {
        if (meshRenderer != null) meshRenderer.material.color = color;
    }

    private void UpdateUIFeedback()
    {
        if (stateTextUI != null && fsm.CurrentState != null)
        {
            string stateName = fsm.CurrentState.GetType().Name.Replace("Boid", "").Replace("State", "");
            stateTextUI.text = stateName;
        }
    }

    private void UpdateAnimator()
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", Velocity.magnitude);
            animator.SetBool("IsDead", IsDead || IsRespawning);
        }
    }

    public Fruit FindNearestFruit()
    {
        if (GameManager.Instance?.ActiveFruits == null) return null;

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
        if (GameManager.Instance?.ActiveBoids == null) return Vector3.zero;

        Vector3 force = Vector3.zero;
        int count = 0;

        foreach (var other in GameManager.Instance.ActiveBoids)
        {
            if (other == null || other == this || other.IsDead || other.IsRespawning) continue;
            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist > 0f && dist < separationRadius)
            {
                Vector3 repulse = (transform.position - other.transform.position);
                repulse /= (dist * dist);
                force += repulse;
                count++;
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

    public Vector3 GetAlignmentForce()
    {
        if (GameManager.Instance?.ActiveBoids == null) return Vector3.zero;

        Vector3 sumVelocity = Vector3.zero;
        int count = 0;

        foreach (var other in GameManager.Instance.ActiveBoids)
        {
            if (other == null || other == this || other.IsDead || other.IsRespawning) continue;
            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < alignmentRadius)
            {
                sumVelocity += other.Velocity;
                count++;
            }
        }

        if (count > 0)
        {
            sumVelocity /= count;
            sumVelocity = sumVelocity.normalized * maxSpeed;
            Vector3 steer = sumVelocity - Velocity;
            return Vector3.ClampMagnitude(steer, maxForce);
        }
        return Vector3.zero;
    }

    public Vector3 GetCohesionForce()
    {
        if (GameManager.Instance?.ActiveBoids == null) return Vector3.zero;

        Vector3 sumPosition = Vector3.zero;
        int count = 0;

        foreach (var other in GameManager.Instance.ActiveBoids)
        {
            if (other == null || other == this || other.IsDead || other.IsRespawning) continue;
            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < cohesionRadius)
            {
                sumPosition += other.transform.position;
                count++;
            }
        }

        if (count > 0)
        {
            sumPosition /= count;
            return Seek(sumPosition);
        }
        return Vector3.zero;
    }
}