using UnityEngine;
using TMPro;

public class HunterAI : Agent
{
    public FiniteStateMachine FSM { get; private set; }

    public HunterPatrolState PatrolState { get; private set; }
    public HunterAttackState AttackState { get; private set; }
    public HunterGatherState GatherState { get; private set; }

    [Header("Configuración del Cazador")]
    [SerializeField] private float visionRadius = 8f;
    [SerializeField] private float rangeAttackRadius = 5f;
    [SerializeField] private float meleeAttackRadius = 2f;
    [SerializeField] private float timeBetweenAttacks = 3f;

    public float VisionRadius => visionRadius;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;
    public float TimeBetweenAttacks => timeBetweenAttacks;
    public float TimerAttackCooldown { get; private set; }

    [Header("Fruit Spawning (Máximo 5 en escena)")]
    [SerializeField] private GameObject fruitPrefab;
    [SerializeField] private float fruitSpawnInterval = 3f;
    [SerializeField] private float fruitSpawnRadius = 6f;
    private float fruitTimer = 0f;

    [Header("Waypoints")]
    [SerializeField] private Transform[] waypoints;
    public Transform[] Waypoints => waypoints;

    [Header("Feedback Visual")]
    [SerializeField] private TextMeshPro stateTextUI;

    private Boid targetBoid;
    private Coroutine _gatheringCoroutine; // Guard de Corrutina

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
            if (GameManager.Instance != null && GameManager.Instance.ActiveFruits.Count < 5)
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
        // Si ya hay una corrutina en progreso, bloqueamos cualquier llamada extra desde Update/Estados
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

        Debug.Log($"<color=yellow>[PRINT STRING]</color> Entrando en GatheringRoutine con Target: {(targetBoid != null ? targetBoid.name : "NULL")}");

        yield return new WaitForSeconds(2.0f);

        if (targetBoid != null)
        {
            Debug.Log($"<color=green>[PRINT STRING]</color> Llamando a AddCapturedBoid() para: {targetBoid.name}");
            if (UIManager.Instance) UIManager.Instance.AddCapturedBoid();
            if (GameManager.Instance) GameManager.Instance.NotifyBoidDeath(targetBoid);
        }
        else
        {
            Debug.LogWarning("<color=orange>[PRINT STRING]</color> GatheringRoutine terminó pero targetBoid era NULL!");
        }

        targetBoid = null;
        _gatheringCoroutine = null; // Liberamos el flag para habilitar la próxima recolección
        FSM.ChangeState(PatrolState);
    }

    private void OnDrawGizmos()
    {
        // Radios de detección
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, visionRadius);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);

        // Gizmo entre Waypoints
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

        // Conexiones visuales a los Boids
        if (GameManager.Instance != null && GameManager.Instance.ActiveBoids != null)
        {
            foreach (var boid in GameManager.Instance.ActiveBoids)
            {
                if (boid == null || boid.isDead) continue;

                float dist = Vector2.Distance(transform.position, boid.transform.position);
                if (dist <= visionRadius)
                {
                    Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
                    Gizmos.DrawLine(transform.position, boid.transform.position);
                }
            }
        }

        // Gizmo destacado para el FOCUS (Objetivo actual del Hunter)
        if (targetBoid != null && !targetBoid.isDead)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, targetBoid.transform.position);
            Gizmos.DrawWireSphere(targetBoid.transform.position, 0.4f);
        }
    }
}