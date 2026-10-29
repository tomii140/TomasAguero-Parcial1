using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class HunterAI : Agent
{
    public FiniteStateMachine FSM { get; private set; }

    public HunterSearchState SearchState { get; private set; }
    public HunterAttackState AttackState { get; private set; }
    public HunterGatherState GatherState { get; private set; }
    public HunterFleeState FleeState { get; private set; }
    public HunterExitState ExitState { get; private set; }
    public HunterDeadState DeadState { get; private set; }

    [Header("Mandatory Parcial 2 Variables")]
    [SerializeField] private int requiredGathers = 3;
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private float safeZoneWaitTime = 3f;
    private int currentGathers = 0;

    [Header("Detection & Combat Settings")]
    [SerializeField, Range(1f, 20f)] private float visionRadius = 8f;
    [SerializeField, Range(1f, 15f)] private float rangeAttackRadius = 5f;
    [SerializeField, Range(0.5f, 5f)] private float meleeAttackRadius = 2f;
    [SerializeField, Range(0.5f, 10f)] private float timeBetweenAttacks = 3f;
    [SerializeField, Range(0.5f, 5f)] private float gatheringTime = 2f;
    [SerializeField, Range(0.5f, 5f)] private float captureRadius = 1.5f;

    [Header("State Thresholds")]
    [SerializeField, Range(0.1f, 3f)] private float waypointThreshold = 0.8f;
    [SerializeField, Range(1f, 5f)] private float directSeekFactor = 2.5f;

    public int RequiredGathers => requiredGathers;
    public float RespawnTime => respawnTime;
    public float SafeZoneWaitTime => safeZoneWaitTime;
    public int CurrentGathers => currentGathers;

    public float VisionRadius => visionRadius;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;
    public float TimeBetweenAttacks => timeBetweenAttacks;
    public float CaptureRadius => captureRadius;
    public float WaypointThreshold => waypointThreshold;
    public float DirectSeekFactor => directSeekFactor;
    public float TimerAttackCooldown { get; private set; }

    [Header("Fruit Spawning")]
    [SerializeField] private GameObject fruitPrefab;
    [SerializeField, Range(1f, 10f)] private float fruitSpawnInterval = 3f;
    [SerializeField, Range(1f, 15f)] private float fruitSpawnRadius = 6f;
    [SerializeField, Range(1, 20)] private int maxFruitsInScene = 5;
    private float fruitTimer = 0f;

    [Header("Waypoints & Zones")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private Transform cotoCenter;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform initialSpawnPoint;
    public Transform[] Waypoints => waypoints;
    public Transform CotoCenter => cotoCenter;
    public Transform ExitPoint => exitPoint;
    public Transform InitialSpawnPoint => initialSpawnPoint;

    [Header("UI Feedback")]
    [SerializeField] private TextMeshPro stateTextUI;

    private Boid targetBoid;
    private Coroutine _gatheringCoroutine;
    private List<Node> currentPath;
    private int currentPathIndex = 0;

    protected override void Start()
    {
        base.Start();
        TimerAttackCooldown = 0f;

        SearchState = new HunterSearchState(this);
        AttackState = new HunterAttackState(this);
        GatherState = new HunterGatherState(this);
        FleeState = new HunterFleeState(this);
        ExitState = new HunterExitState(this);
        DeadState = new HunterDeadState(this);

        FSM = new FiniteStateMachine();
        FSM.ChangeState(SearchState);
    }

    private void Update()
    {
        if (TimerAttackCooldown > 0)
            TimerAttackCooldown -= Time.deltaTime;

        // Prioridad máxima: Detección de lobos (Flee)
        if (CheckWolfDetection() && FSM.CurrentState != FleeState && FSM.CurrentState != DeadState)
        {
            FSM.ChangeState(FleeState);
        }

        HandleFruitSpawning();
        FSM.Update();
        ApplyPhysics();
        UpdateStateFeedback();
    }

    private bool CheckWolfDetection()
    {
        if (GameManager.Instance == null) return false;
        // Asumiendo que GameManager expone una lista de lobos activos
        foreach (var wolf in GameManager.Instance.ActiveWolves)
        {
            if (wolf != null)
            {
                float dist = Vector3.Distance(transform.position, wolf.transform.position);
                if (dist <= visionRadius)
                {
                    // Chequear línea de visión por Raycast
                    Vector3 dir = (wolf.transform.position - transform.position).normalized;
                    if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, dist, LayerMask.GetMask("Obstacles")))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void AddGather()
    {
        currentGathers++;
        if (UIManager.Instance) UIManager.Instance.AddCapturedBoid();
    }

    public void ResetGathers()
    {
        currentGathers = 0;
    }

    public void FollowPath(List<Node> path, float threshold = 0.8f)
    {
        if (path == null || path.Count == 0) return;

        currentPath = path;
        currentPathIndex = 0;
    }

    public void ExecutePathMovement()
    {
        if (currentPath == null || currentPathIndex >= currentPath.Count) return;

        Vector3 targetPos = currentPath[currentPathIndex].transform.position;
        AddForce(Seek(targetPos));

        if (Vector3.Distance(transform.position, targetPos) < waypointThreshold)
        {
            currentPathIndex++;
        }
    }

    public Node GetNearestSafeZoneNode()
    {
        Node nearestSafe = null;
        float minCost = float.MaxValue;

        foreach (var node in GraphManager.Instance.AllNodes)
        {
            if (node.Zone == ZoneType.SafeZone)
            {
                float dist = Vector3.Distance(transform.position, node.transform.position);
                if (dist < minCost)
                {
                    minCost = dist;
                    nearestSafe = node;
                }
            }
        }
        return nearestSafe;
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
                Vector2 randomCircle = Random.insideUnitCircle * fruitSpawnRadius;
                Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
                Instantiate(fruitPrefab, spawnPosition, Quaternion.identity);
            }
        }
    }

    public void SetColorFeedback(Color color)
    {
        if (agentMeshRenderer != null && agentMeshRenderer.material != null)
        {
            agentMeshRenderer.material.color = color;
        }
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
                float distanceToBoid = Vector3.Distance(transform.position, boid.transform.position);
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
        Velocity = Vector3.zero;
        acceleration = Vector3.zero;

        yield return new WaitForSeconds(gatheringTime);

        if (targetBoid != null)
        {
            AddGather();
            if (GameManager.Instance) GameManager.Instance.NotifyBoidDeath(targetBoid);
        }

        targetBoid = null;
        _gatheringCoroutine = null;

        if (CurrentGathers >= requiredGathers)
        {
            FSM.ChangeState(ExitState);
        }
        else
        {
            FSM.ChangeState(SearchState);
        }
    }
}