using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WolfAI : Agent
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform[] waypoints;

    [Header("Perception & Vision")]
    [SerializeField, Range(1f, 25f)] private float visionRadius = 10f;
    [SerializeField, Range(10f, 120f)] private float fieldOfViewAngle = 60f;
    [SerializeField, Range(0.5f, 5f)] private float captureRadius = 1.5f;
    [SerializeField] private LayerMask obstacleMask;

    public LayerMask ObstacleMask => obstacleMask;
    public float VisionRadius => visionRadius;
    public Animator Animator { get; private set; }

    private IWolfState currentState;
    public WolfPatrolState PatrolState { get; private set; }
    public WolfChaseState ChaseState { get; private set; }
    public WolfAttackState AttackState { get; private set; }

    public string CurrentStateName => currentState != null ? currentState.GetType().Name : "None";

    protected override void Start()
    {
        base.Start();
        Animator = GetComponentInChildren<Animator>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ActiveWolves.Add(gameObject);
        }

        PatrolState = new WolfPatrolState(this, waypoints);
        ChaseState = new WolfChaseState(this);
        AttackState = new WolfAttackState(this);

        ChangeState(PatrolState);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ActiveWolves.Remove(gameObject);
        }
    }

    private void Update()
    {
        CheckPerception();
        currentState?.UpdateState();
        ApplyPhysics();

        if (Animator != null)
        {
            Animator.SetFloat("Speed", Velocity.magnitude);
        }
    }

    public void ChangeState(IWolfState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public void AddForceToAgent(Vector3 targetPosition)
    {
        AddForce(Seek(targetPosition));
    }

    public void StopAgent()
    {
        Velocity = Vector3.zero;
        acceleration = Vector3.zero;
    }

    private void CheckPerception()
    {
        if (GameManager.Instance == null || GameManager.Instance.Hunter == null) return;
        HunterAI hunter = GameManager.Instance.Hunter;

        Node hunterNode = GraphManager.Instance.GetNearestNode(hunter.transform.position);
        if (hunter.FSM.CurrentState is HunterDeadState || (hunterNode != null && hunterNode.Zone == ZoneType.SafeZone))
        {
            if (currentState == ChaseState) ChangeState(PatrolState);
            return;
        }

        float distToHunter = Vector3.Distance(transform.position, hunter.transform.position);

        if (distToHunter <= captureRadius)
        {
            if (currentState != AttackState) ChangeState(AttackState);
            return;
        }

        if (distToHunter <= visionRadius)
        {
            Vector3 dirToHunter = (hunter.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToHunter);

            if (angle <= fieldOfViewAngle / 2f)
            {
                if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToHunter, distToHunter, obstacleMask))
                {
                    if (currentState == PatrolState) ChangeState(ChaseState);
                }
            }
        }
    }

    public void ReceiveAlert(Vector3 hunterPosition)
    {
        if (currentState == PatrolState)
        {
            ChangeState(ChaseState);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, captureRadius);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.up * 0.5f + Velocity);
        }

#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        Vector3 textPos = transform.position + Vector3.up * 2f;
        Handles.Label(textPos, $"[{gameObject.name}]\nState: {CurrentStateName}\nVel: {Velocity.magnitude:F2}", style);
#endif
    }
}