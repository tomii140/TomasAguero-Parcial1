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
    private Coroutine _gatheringCoroutine;
    private List<Node> currentPath;
    private int currentPathIndex = 0;
    private MeshRenderer meshRenderer;

    protected override void Start()
    {
        base.Start();
        TimerAttackCooldown = 0f;
        meshRenderer = GetComponent<MeshRenderer>();

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

        HandleFruitSpawning();
        FSM.Update();
        ApplyPhysics();
        UpdateStateFeedback();
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
            if (GameManager.Instance != null && GameManager.Instance.ActiveFruits != null && GameManager.Instance.ActiveFruits.Count < maxFruitsInScene)
            {
                if (fruitPrefab != null)
                {
                    Vector2 randomCircle = Random.insideUnitCircle * fruitSpawnRadius;
                    Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
                    GameObject fruitObj = Instantiate(fruitPrefab, spawnPos, Quaternion.identity);

                    Fruit fruitComponent = fruitObj.GetComponent<Fruit>();
                    if (fruitComponent != null)
                    {
                        GameManager.Instance.ActiveFruits.Add(fruitComponent);
                    }
                }
            }
        }
    }

    public void SetColorFeedback(Color newColor)
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = newColor;
        }
    }

    public Boid GetTargetBoid() => targetBoid;

    public void SetTargetBoid(Boid boid) => targetBoid = boid;

    public Boid FindBoidInVision(bool lookForDead = false)
    {
        if (GameManager.Instance == null || GameManager.Instance.ActiveBoids == null) return null;

        Boid nearestBoid = null;
        float minDist = float.MaxValue;

        foreach (var boid in GameManager.Instance.ActiveBoids)
        {
            if (boid != null && boid.isDead == lookForDead)
            {
                float dist = Vector3.Distance(transform.position, boid.transform.position);
                if (dist <= visionRadius && dist < minDist)
                {
                    minDist = dist;
                    nearestBoid = boid;
                }
            }
        }
        return nearestBoid;
    }

    public void ResetAttackCooldown()
    {
        TimerAttackCooldown = timeBetweenAttacks;
    }

    public void StartGatheringRoutine(Boid boid)
    {
        if (_gatheringCoroutine != null)
        {
            StopCoroutine(_gatheringCoroutine);
        }
        _gatheringCoroutine = StartCoroutine(GatheringRoutine(boid));
    }

    private System.Collections.IEnumerator GatheringRoutine(Boid boid)
    {
        float timer = 0f;
        while (timer < gatheringTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (boid != null)
        {
            Destroy(boid.gameObject);
            AddGather();
        }
        targetBoid = null;
        _gatheringCoroutine = null;
    }

    private void OnDrawGizmos()
    {
        // 1. Radio de visión general (Azul tenue)
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        // 2. Rango de ataque a distancia (Amarillo)
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);

        // 3. Rango de ataque cuerpo a cuerpo (Rojo)
        Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);

        // 4. Radio de captura o proximidad (Naranja)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, captureRadius);

        // 5. Línea de velocidad actual (Magenta)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.up * 0.5f + Velocity);

            // 6. Visualización de Pathfinding (Nodos y camino activo con A*)
            if (currentPath != null && currentPath.Count > 0)
            {
                Gizmos.color = Color.cyan;
                for (int i = currentPathIndex; i < currentPath.Count; i++)
                {
                    if (currentPath[i] != null)
                    {
                        Gizmos.DrawSphere(currentPath[i].transform.position, 0.3f);

                        if (i < currentPath.Count - 1 && currentPath[i + 1] != null)
                        {
                            Gizmos.DrawLine(currentPath[i].transform.position, currentPath[i + 1].transform.position);
                        }
                    }
                }

                // Línea brillante hacia el nodo objetivo actual al que se dirige
                if (currentPathIndex < currentPath.Count && currentPath[currentPathIndex] != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, currentPath[currentPathIndex].transform.position);
                }
            }
        }

#if UNITY_EDITOR
        // 7. Feedback textual en pantalla sobre el estado, recolecciones y velocidad
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.cyan;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;

        string currentStateName = FSM != null && FSM.CurrentState != null ? FSM.CurrentState.GetType().Name.Replace("Hunter", "").Replace("State", "") : "None";

        Vector3 textPos = transform.position + Vector3.up * 2.5f;
        Handles.Label(textPos, $"[Hunter]\nState: {currentStateName}\nGathers: {currentGathers}/{requiredGathers}\nVel: {Velocity.magnitude:F2}", style);
#endif
    }
}