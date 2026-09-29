using UnityEngine;
using TMPro;

public class HunterAI : Agent
{
    public FiniteStateMachine FSM { get; private set; }

    public HunterPatrolState PatrolState { get; private set; }
    public HunterAttackState AttackState { get; private set; }
    public HunterGatherState GatherState { get; private set; }

    [Header("Detection & Combat Settings")]
    [SerializeField, Range(1f, 20f)] private float visionRadius = 8f;
    [SerializeField, Range(1f, 15f)] private float rangeAttackRadius = 5f;
    [SerializeField, Range(0.5f, 5f)] private float meleeAttackRadius = 2f;
    [SerializeField, Range(0.5f, 10f)] private float timeBetweenAttacks = 3f;
    [SerializeField, Range(0.5f, 5f)] private float gatheringTime = 2f;

    [Header("State Thresholds")]
    [SerializeField, Range(0.1f, 3f)] private float waypointThreshold = 0.8f;
    [SerializeField, Range(1f, 5f)] private float directSeekFactor = 2.5f;

    public float VisionRadius => visionRadius;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;
    public float TimeBetweenAttacks => timeBetweenAttacks;
    public float WaypointThreshold => waypointThreshold;
    public float DirectSeekFactor => directSeekFactor;
    public float TimerAttackCooldown { get; private set; }

    [Header("Fruit Spawning")]
    [SerializeField] private GameObject fruitPrefab;
    [SerializeField, Range(1f, 10f)] private float fruitSpawnInterval = 3f;
    [SerializeField, Range(1f, 15f)] private float fruitSpawnRadius = 6f;
    [SerializeField, Range(1, 20)] private int maxFruitsInScene = 5;
    private float fruitTimer = 0f;

    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;
    public Transform[] Waypoints => waypoints;

    [Header("UI Feedback")]
    [SerializeField] private TextMeshPro stateTextUI;

    private Boid targetBoid;
    private Coroutine _gatheringCoroutine;

    protected override void Start()
    {
        base.Start();
        TimerAttackCooldown = 0f;

        PatrolState = new HunterPatrolState(this);
        AttackState = new HunterAttackState(this);
        GatherState = new HunterGatherState(this);

        FSM = new FiniteStateMachine();
        FSM.ChangeState(PatrolState);
    }

    private void Update()
    {
        if (TimerAttackCooldown > 0)
            TimerAttackCooldown -= Time.deltaTime;

        HandleFruitSpawning();
        FSM.Update();
        ApplyPhysics();

        UpdateStateFeedback();
    }

    private void UpdateStateFeedback()
    {
        if (stateTextUI != null && FSM.CurrentState != null)
        {
            stateTextUI.text = FSM.CurrentState.GetType().Name.Replace("Hunter", "").Replace("State", "");
        }
    }

    private void HandleFruitSpawning()
    {
        fruitTimer += Time.deltaTime;
        if (fruitTimer >= fruitSpawnInterval)
        {
            fruitTimer = 0f;
            if (GameManager.Instance != null && GameManager.Instance.ActiveFruits.Count < maxFruitsInScene)
            {
                Vector2 spawnPosition = (Vector2)transform.position + Random.insideUnitCircle * fruitSpawnRadius;
                Instantiate(fruitPrefab, spawnPosition, Quaternion.identity);
            }
        }
    }

    public void SetColorFeedback(Color color)
    {
        if (agentRenderer != null) agentRenderer.color = color;
    }

    public void SetTargetBoid(Boid boid) => targetBoid = boid;
    public Boid GetTargetBoid() => targetBoid;
    public void ResetAttackCooldown() => TimerAttackCooldown = timeBetweenAttacks;

    public void StartGatheringRoutine()
    {
        if (_gatheringCoroutine != null) return;
        _gatheringCoroutine = StartCoroutine(GatheringRoutine());
    }

    public Boid FindBoidInVision(bool lookForDead)
    {
        Boid nearestBoid = null;
        float minimumDistance = float.MaxValue;

        if (GameManager.Instance == null) return null;

        foreach (var boid in GameManager.Instance.ActiveBoids)
        {
            if (boid != null && boid.isDead == lookForDead)
            {
                float distanceToBoid = Vector2.Distance(transform.position, boid.transform.position);
                if (distanceToBoid < visionRadius && distanceToBoid < minimumDistance)
                {
                    minimumDistance = distanceToBoid;
                    nearestBoid = boid;
                }
            }
        }
        return nearestBoid;
    }

    private System.Collections.IEnumerator GatheringRoutine()
    {
        Velocity = Vector2.zero;
        acceleration = Vector2.zero;

        yield return new WaitForSeconds(gatheringTime);

        if (targetBoid != null)
        {
            if (UIManager.Instance) UIManager.Instance.AddCapturedBoid();
            if (GameManager.Instance) GameManager.Instance.NotifyBoidDeath(targetBoid);
        }

        targetBoid = null;
        _gatheringCoroutine = null;
        FSM.ChangeState(PatrolState);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, visionRadius);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);

        // Feedback visual del Boid targeteado
        if (targetBoid != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetBoid.transform.position);
            Gizmos.DrawWireSphere(targetBoid.transform.position, 0.6f);
        }

        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;

                Gizmos.DrawSphere(waypoints[i].position, 0.3f);
                Transform nextWaypoint = waypoints[(i + 1) % waypoints.Length];
                if (nextWaypoint != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, nextWaypoint.position);
                }
            }
        }
    }
}