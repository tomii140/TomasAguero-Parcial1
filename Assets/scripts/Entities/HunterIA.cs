using UnityEngine;
using TMPro;

public class HunterAI : Agent
{
    public FiniteStateMachine FSM { get; private set; }

    public HunterSearchState SearchState { get; private set; }
    public HunterAttackState AttackState { get; private set; }
    public HunterGatherState GatherState { get; private set; }
    public HunterRestState RestState { get; private set; }

    [Header("Detection & Combat Settings (Parcial 1)")]
    [SerializeField, Range(1f, 80f)] private float visionRadius = 15f;
    [SerializeField, Range(1f, 50f)] private float rangeAttackRadius = 10f;
    [SerializeField, Range(0.5f, 25f)] private float meleeAttackRadius = 3f;
    [SerializeField, Range(0.1f, 10f)] private float timeBetweenAttacks = 2f;
    [SerializeField, Range(0.5f, 10f)] private float gatheringTime = 2f;
    [SerializeField, Range(0.5f, 5f)] private float captureRadius = 1.5f;

    [Header("Energy & Rest Settings (Parcial 1)")]
    [SerializeField, Range(10f, 200f)] private float maxEnergy = 100f;
    [SerializeField, Range(1f, 50f)] private float energyConsumptionRate = 15f;
    [SerializeField, Range(1f, 50f)] private float energyRecoveryRate = 25f;
    private float currentEnergy;

    [Header("Patrol Settings")]
    [SerializeField, Range(0.1f, 5f)] private float waypointThreshold = 1.0f;
    [SerializeField] private Transform[] waypoints;
    public Transform[] Waypoints => waypoints;

    [Header("Fruit Spawning Settings")]
    [SerializeField] private GameObject fruitPrefab;
    [SerializeField, Range(1f, 10f)] private float fruitSpawnInterval = 4f;
    [SerializeField, Range(1f, 20f)] private float fruitSpawnRadius = 8f;
    [SerializeField, Range(1, 10)] private int maxFruitsInScene = 5;

    [Header("UI & Animation Feedback")]
    [SerializeField] private TextMeshPro stateTextUI;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private bool showGizmos = true;
    private Animator animator;

    private Boid targetBoid;
    public float TimerAttackCooldown { get; private set; } = 0f;

    private Vector3 lastPosition;

    public float VisionRadius => visionRadius;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;
    public float TimeBetweenAttacks => timeBetweenAttacks;
    public float GatheringTime => gatheringTime;
    public float CaptureRadius => captureRadius;
    public float WaypointThreshold => waypointThreshold;
    public float FruitSpawnInterval => fruitSpawnInterval;

    public float MaxEnergy => maxEnergy;
    public float CurrentEnergy => currentEnergy;
    public float EnergyConsumptionRate => energyConsumptionRate;
    public float EnergyRecoveryRate => energyRecoveryRate;

    protected override void Start()
    {
        base.Start();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        animator = GetComponent<Animator>();

        currentEnergy = maxEnergy;
        lastPosition = transform.position;

        SearchState = new HunterSearchState(this);
        AttackState = new HunterAttackState(this);
        GatherState = new HunterGatherState(this);
        RestState = new HunterRestState(this);

        FSM = new FiniteStateMachine();
        FSM.ChangeState(SearchState);
    }

    private void Update()
    {
        if (TimerAttackCooldown > 0f)
            TimerAttackCooldown -= Time.deltaTime;

        FSM.Update();
        ApplyPhysics();
        UpdateUIFeedback();
        UpdateAnimator();
    }

    public void ResetAttackCooldown()
    {
        TimerAttackCooldown = timeBetweenAttacks;
    }

    public Boid GetTargetBoid() => targetBoid;
    public void SetTargetBoid(Boid boid) => targetBoid = boid;

    public void ConsumeOrRecoverEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, maxEnergy);
    }

    public Boid FindBoidInVision(bool lookForDead)
    {
        if (GameManager.Instance == null || GameManager.Instance.ActiveBoids == null) return null;

        if (!lookForDead && targetBoid != null && !targetBoid.isDead)
        {
            float distToCurrent = Vector3.Distance(transform.position, targetBoid.transform.position);
            if (distToCurrent <= visionRadius)
            {
                return targetBoid;
            }
        }

        Boid nearest = null;
        float minDistance = float.MaxValue;

        foreach (var boid in GameManager.Instance.ActiveBoids)
        {
            if (boid == null) continue;
            if (boid.isDead != lookForDead) continue;

            float dist = Vector3.Distance(transform.position, boid.transform.position);
            if (dist <= visionRadius && dist < minDistance)
            {
                minDistance = dist;
                nearest = boid;
            }
        }
        return nearest;
    }

    public void SpawnFruitIfAllowed()
    {
        if (fruitPrefab == null || GameManager.Instance == null) return;
        if (GameManager.Instance.ActiveFruits.Count >= maxFruitsInScene) return;

        Vector2 randomCircle = Random.insideUnitCircle * fruitSpawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        Instantiate(fruitPrefab, spawnPos, Quaternion.identity);
    }

    public void NotifyBoidCollected(Boid boid)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.NotifyBoidDeath(boid);
        }
    }

    public void SetColorFeedback(Color color)
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = color;
        }
    }

    private void UpdateUIFeedback()
    {
        if (stateTextUI != null && FSM.CurrentState != null)
        {
            string currentStateName = FSM.CurrentState.GetType().Name.Replace("Hunter", "").Replace("State", "");
            stateTextUI.text = $"State: {currentStateName}\nEnergy: {Mathf.Round(currentEnergy)}/{maxEnergy}";
        }
    }

    private void UpdateAnimator()
    {
        if (animator != null)
        {
            float speed = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z),
                                           new Vector3(lastPosition.x, 0f, lastPosition.z)) / Time.deltaTime;
            lastPosition = transform.position;

            animator.SetFloat("Speed", speed);

            bool isResting = (FSM.CurrentState == RestState);
            animator.SetBool("IsResting", isResting);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, captureRadius);
    }
}