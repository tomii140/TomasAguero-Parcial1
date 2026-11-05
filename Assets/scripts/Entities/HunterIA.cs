using UnityEngine;
using TMPro;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
    [SerializeField, Range(1f, 80f)] private float visionRadius = 8f;
    [SerializeField, Range(1f, 80f)] private float rangeAttackRadius = 5f;
    [SerializeField, Range(0.5f, 80f)] private float meleeAttackRadius = 2f;
    [SerializeField, Range(0.5f, 80f)] private float timeBetweenAttacks = 3f;
    [SerializeField, Range(0.5f, 80f)] private float gatheringTime = 2f;
    [SerializeField, Range(0.5f, 80f)] private float captureRadius = 1.5f;

    [Header("State Thresholds")]
    [SerializeField, Range(0.1f, 40f)] private float waypointThreshold = 0.8f;
    [SerializeField, Range(1f, 40f)] private float directSeekFactor = 2.5f;

    [Header("Animation & Speed Boost")]
    [SerializeField] private float runSpeedBoost = 3.0f;
    [SerializeField] private Animator hunterAnimator;
    private float defaultMaxSpeed;

    public int RequiredGathers => requiredGathers;
    public float RespawnTime => respawnTime;
    public float SafeZoneWaitTime => safeZoneWaitTime;
    public int CurrentGathers => currentGathers;

    public float VisionRadius => visionRadius;
    public float RangeAttackRadius => rangeAttackRadius;
    public float MeleeAttackRadius => meleeAttackRadius;
    public float TimeBetweenAttacks => timeBetweenAttacks;
    public float GatheringTime => gatheringTime;
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
    private List<Node> currentPath;
    private int currentPathIndex = 0;
    private MeshRenderer meshRenderer;

    public List<Node> CurrentPath => currentPath;

    protected override void Start()
    {
        base.Start();
        defaultMaxSpeed = maxSpeed;
        TimerAttackCooldown = 0f;
        meshRenderer = GetComponent<MeshRenderer>();

        if (hunterAnimator == null)
        {
            hunterAnimator = GetComponent<Animator>();
            if (hunterAnimator == null)
            {
                hunterAnimator = GetComponentInChildren<Animator>();
            }
        }

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
        if (GameManager.Instance == null || GraphManager.Instance == null) return;

        if (TimerAttackCooldown > 0)
            TimerAttackCooldown -= Time.deltaTime;

        if (CheckWolfDetection() && FSM.CurrentState != FleeState && FSM.CurrentState != DeadState)
        {
            FSM.ChangeState(FleeState);
        }

        if (FSM.CurrentState == AttackState || FSM.CurrentState == FleeState)
        {
            maxSpeed = defaultMaxSpeed + runSpeedBoost;
        }
        else
        {
            maxSpeed = defaultMaxSpeed;
        }

        HandleFruitSpawning();
        FSM.Update();
        ApplyPhysics();
        UpdateStateFeedback();

        if (hunterAnimator != null)
        {
            hunterAnimator.SetFloat("Speed", Velocity.magnitude);
        }
    }

    private bool CheckWolfDetection()
    {
        if (GameManager.Instance == null || GameManager.Instance.ActiveWolves == null) return false;

        foreach (var wolf in GameManager.Instance.ActiveWolves)
        {
            if (wolf != null)
            {
                float dist = Vector3.Distance(transform.position, wolf.transform.position);
                if (dist <= visionRadius)
                {
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
        if (GraphManager.Instance == null || GraphManager.Instance.AllNodes == null) return null;

        Node nearestSafe = null;
        float minCost = float.MaxValue;

        foreach (var node in GraphManager.Instance.AllNodes)
        {
            if (node != null && node.Zone == ZoneType.SafeZone)
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
            if (GameManager.Instance != null && GameManager.Instance.ActiveFruits.Count < maxFruitsInScene && fruitPrefab != null)
            {
                Vector2 randomCircle = Random.insideUnitCircle * fruitSpawnRadius;
                Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
                Instantiate(fruitPrefab, spawnPos, Quaternion.identity);
            }
        }
    }

    public Boid FindBoidInVision(bool lookForDead)
    {
        if (GameManager.Instance == null || GameManager.Instance.ActiveBoids == null) return null;

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

    public void SetTargetBoid(Boid boid) => targetBoid = boid;
    public Boid GetTargetBoid() => targetBoid;

    public void ResetAttackCooldown()
    {
        TimerAttackCooldown = timeBetweenAttacks;
    }

    public void SetColorFeedback(Color color)
    {
        if (agentMeshRenderer != null)
        {
            agentMeshRenderer.material.color = color;
        }
    }

    // --- GIZMOS OBLIGATORIOS (Punto 1.12) ---
    private void OnDrawGizmos()
    {
        // 1. Radio de Visión (Amarillo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        // 2. Radio de Ataque a Distancia (Cian)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);

        // 3. Radio de Ataque Cuerpo a Cuerpo (Rojo)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);

        // 4. Dibujar el Path actual de navegación A*
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                if (currentPath[i] != null && currentPath[i + 1] != null)
                {
                    Gizmos.DrawLine(currentPath[i].transform.position, currentPath[i + 1].transform.position);
                    Gizmos.DrawSphere(currentPath[i].transform.position, 0.2f);
                }
            }
        }
    }
}