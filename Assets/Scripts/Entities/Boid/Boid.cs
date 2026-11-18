
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class Boid : Agent
{
    private static readonly List<Boid> EmptyBoids = new List<Boid>();

    public FiniteStateMachine FSM { get; private set; }

    public BoidFlockingState FlockingState { get; private set; }
    public BoidEvadeState EvadeState { get; private set; }
    public BoidSeekObjectState SeekObjectState { get; private set; }
    public BoidConsumeState ConsumeState { get; private set; }
    public BoidDeadState DeadState { get; private set; }
    public BoidRespawnState RespawnState { get; private set; }

    public bool IsDead { get; private set; }
    public bool IsRespawning { get; private set; }
    public bool ShouldRespawn { get; private set; }

    [Header("Health Configuration")]
    [SerializeField] private int maxHealth = 2;
    private int currentHealth;

    public int CurrentHealth => currentHealth;

    [Header("Flocking Configuration")]
    [SerializeField, Range(0.5f, 50f)] private float separationRadius = 2f;
    [SerializeField, Range(2f, 50f)] private float alignmentRadius = 12f;
    [SerializeField, Range(2f, 50f)] private float cohesionRadius = 12f;

    [SerializeField, Range(0f, 50f)] private float separationWeight = 2f;
    [SerializeField, Range(0f, 50f)] private float alignmentWeight = 1f;
    [SerializeField, Range(0f, 50f)] private float cohesionWeight = 1.5f;

    public float SeparationRadius => separationRadius;
    public float AlignmentRadius => alignmentRadius;
    public float CohesionRadius => cohesionRadius;

    public float SeparationWeight => separationWeight;
    public float AlignmentWeight => alignmentWeight;
    public float CohesionWeight => cohesionWeight;

    [Header("Perception Sensors")]
    [SerializeField, Range(1f, 50f)] private float hunterPerceptionRadius = 10f;
    [SerializeField, Range(1f, 50f)] private float fruitPerceptionRadius = 10f;
    [SerializeField, Range(0.5f, 50f)] private float fruitWeight = 2f;
    [SerializeField, Range(0.5f, 50f)] private float fruitArriveSlowingRadius = 3f;

    [SerializeField, Range(0.02f, 1f)]
    private float perceptionUpdateInterval = 0.2f;

    public float HunterPerceptionRadius => hunterPerceptionRadius;
    public float FruitPerceptionRadius => fruitPerceptionRadius;
    public float FruitWeight => fruitWeight;
    public float FruitArriveSlowingRadius => fruitArriveSlowingRadius;

    [Header("Respawn Configuration")]
    [SerializeField, Range(1f, 10f)] private float minRespawnTime = 3f;
    [SerializeField, Range(1f, 10f)] private float maxRespawnTime = 6f;

    public float MinRespawnTime => minRespawnTime;
    public float MaxRespawnTime => maxRespawnTime;

    [Header("Movement Configuration")]
    [SerializeField, Range(0.1f, 1f)]
    private float recoverySpeedMultiplier = 0.5f;

    public float RecoverySpeedMultiplier => recoverySpeedMultiplier;

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip eatAudioClip;
    [SerializeField, Min(0.05f)] private float eatSoundInterval = 0.5f;

    public AudioClip EatAudioClip => eatAudioClip;
    public float EatSoundInterval => eatSoundInterval;

    [Header("Feedback")]
    [SerializeField] private TextMeshPro stateTextUI;
    [SerializeField] private bool showGizmos = true;

    private Animator animator;

    private Renderer[] cachedRenderers;
    private Collider[] cachedColliders;

    private BoidPerception perception;
    private BoidFlocking flocking;

    private void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedColliders = GetComponentsInChildren<Collider>(true);
    }

    protected override void Start()
    {
        base.Start();

        animator = GetComponent<Animator>();

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterBoid(this);

        currentHealth = maxHealth;

        perception = new BoidPerception(this, perceptionUpdateInterval);
        flocking = new BoidFlocking(this);

        perception.Refresh();

        InitializeFSM();
        RefreshUIFeedback();
    }

    private void OnDestroy()
    {
        if (FSM != null)
            FSM.OnStateChanged -= HandleStateChanged;

        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterBoid(this);

        if (WorldBounds.Instance != null)
            WorldBounds.Instance.UnregisterBoid(this);
    }

    private void InitializeFSM()
    {
        FSM = new FiniteStateMachine();

        FlockingState = new BoidFlockingState(this);
        EvadeState = new BoidEvadeState(this);
        SeekObjectState = new BoidSeekObjectState(this);
        ConsumeState = new BoidConsumeState(this);
        DeadState = new BoidDeadState(this);
        RespawnState = new BoidRespawnState(this);

        FSM.OnStateChanged += HandleStateChanged;
        FSM.ChangeState(FlockingState);
    }

    private void Update()
    {
        if (perception != null)
            perception.Update(Time.deltaTime);

        FSM.Update();

        ApplyPhysics();
        UpdateAnimator();
    }

    public void Revive()
    {
        IsDead = false;
        IsRespawning = false;
        ShouldRespawn = false;

        currentHealth = maxHealth;

        SetColorFeedback(Color.green);

        Velocity = transform.forward * (maxSpeed * recoverySpeedMultiplier);

        if (perception != null)
            perception.Refresh();

        FSM.ChangeState(FlockingState);
        RefreshUIFeedback();
    }

    public void ReceiveDamage(int damage)
    {
        if (IsDead || IsRespawning || damage <= 0)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            IsDead = true;
        }

        RefreshUIFeedback();
    }

    public void TriggerRespawnFlow()
    {
        IsRespawning = true;
        ShouldRespawn = true;
    }

    public HunterAI DetectHunterInRange()
    {
        return perception != null
            ? perception.DetectHunterInRange()
            : null;
    }

    public Fruit FindNearestFruit()
    {
        return perception != null
            ? perception.FindNearestFruit()
            : null;
    }

    public List<Boid> GetNearbyBoids()
    {
        return perception != null
            ? perception.GetNearbyBoids()
            : EmptyBoids;
    }

    public Vector3 CalculateFlocking()
    {
        return flocking != null
            ? flocking.CalculateFlocking()
            : Vector3.zero;
    }

    public Vector3 GetSeparationForce()
    {
        return flocking != null
            ? flocking.GetSeparationForce()
            : Vector3.zero;
    }

    public Vector3 GetAlignmentForce()
    {
        return flocking != null
            ? flocking.GetAlignmentForce()
            : Vector3.zero;
    }

    public Vector3 GetCohesionForce()
    {
        return flocking != null
            ? flocking.GetCohesionForce()
            : Vector3.zero;
    }

    public void RestoreForwardMomentum()
    {
        Velocity = transform.forward * (maxSpeed * recoverySpeedMultiplier);
    }

    public void SetRespawnVisibility(bool visible)
    {
        foreach (Renderer cachedRenderer in cachedRenderers)
        {
            if (cachedRenderer != null)
                cachedRenderer.enabled = visible;
        }

        foreach (Collider cachedCollider in cachedColliders)
        {
            if (cachedCollider != null)
                cachedCollider.enabled = visible;
        }
    }

    public void SetColorFeedback(Color color)
    {
        if (agentMeshRenderer != null)
            agentMeshRenderer.material.color = color;
    }

    private void HandleStateChanged(IState newState)
    {
        string stateName = newState.GetType().Name
            .Replace("Boid", "")
            .Replace("State", "");

        Debug.Log($"[BOID] {name} cambió su comportamiento: {stateName}.");

        RefreshUIFeedback();
    }

    private void RefreshUIFeedback()
    {
        if (stateTextUI == null || FSM == null || FSM.CurrentState == null)
            return;

        string stateName = FSM.CurrentState.GetType().Name
            .Replace("Boid", "")
            .Replace("State", "");

        stateTextUI.text = $"{stateName}\nHP: {currentHealth}/{maxHealth}";
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", Velocity.magnitude);
        animator.SetBool("IsDead", IsDead || IsRespawning);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hunterPerceptionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, fruitPerceptionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, alignmentRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, cohesionRadius);
    }
}
