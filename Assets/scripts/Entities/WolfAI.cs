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
    [SerializeField, Range(1f, 200f)] private float visionRadius = 10f;
    [SerializeField, Range(10f, 200f)] private float fieldOfViewAngle = 60f;
    [SerializeField, Range(0.5f, 5f)] private float captureRadius = 1.5f;
    [SerializeField] private LayerMask obstacleMask;

    public LayerMask ObstacleMask => obstacleMask;
    public float VisionRadius => visionRadius;
    public Animator Animator { get; private set; }
    public WolfAudioController AudioController { get; private set; }

    private IWolfState currentState;
    public WolfPatrolState PatrolState { get; private set; }
    public WolfChaseState ChaseState { get; private set; }
    public WolfAttackState AttackState { get; private set; }
    public WolfHowlState HowlState { get; private set; }

    public string CurrentStateName => currentState != null ? currentState.GetType().Name : "None";

    protected override void Start()
    {
        base.Start();
        Animator = GetComponentInChildren<Animator>();
        AudioController = GetComponentInChildren<WolfAudioController>();

        if (GameManager.Instance != null && !GameManager.Instance.ActiveWolves.Contains(gameObject))
        {
            GameManager.Instance.ActiveWolves.Add(gameObject);
        }

        PatrolState = new WolfPatrolState(this, waypoints);
        ChaseState = new WolfChaseState(this);
        AttackState = new WolfAttackState(this);
        HowlState = new WolfHowlState(this, transform.position);

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

        Node hunterNode = GraphManager.Instance != null ? GraphManager.Instance.GetNearestNode(hunter.transform.position) : null;
        if (hunter.FSM.CurrentState is HunterDeadState || (hunterNode != null && hunterNode.Zone == ZoneType.SafeZone))
        {
            if (currentState == ChaseState || currentState == HowlState) ChangeState(PatrolState);
            return;
        }

        float distToHunter = Vector3.Distance(transform.position, hunter.transform.position);

        // 1. Revisar si entra en rango de captura
        if (distToHunter <= captureRadius)
        {
            Debug.Log($"<color=green>[DEBUG-CAPTURE]</color> {gameObject.name} alcanzó al cazador. Distancia: {distToHunter:F2}");
            if (currentState != AttackState) ChangeState(AttackState);
            return;
        }

        // 2. Revisar si entra en el radio de visión
        if (distToHunter <= visionRadius)
        {
            Vector3 dirToHunter = (hunter.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToHunter);
            float maxAngle = fieldOfViewAngle / 2f;

            // DEBUG: El cazador está dentro del radio, imprimimos ángulo y distancia constantemente
            Debug.Log($"<color=yellow>[DEBUG-VISION]</color> {gameObject.name} tiene al cazador a {distToHunter:F2}m (Radio máx: {visionRadius}). Ángulo actual: {angle:F2}° (Máx permitido: {maxAngle}°)");

            if (angle <= maxAngle)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 1.0f;
                bool hitObstacle = Physics.Raycast(rayOrigin, dirToHunter, distToHunter, obstacleMask);

                // Dibuja una línea en la escena de Unity: Verde si tiene visión libre, Roja si hay un obstáculo en medio
                Debug.DrawLine(rayOrigin, hunter.transform.position + Vector3.up * 1.0f, hitObstacle ? Color.red : Color.green, 0.1f);

                if (!hitObstacle)
                {
                    if (currentState == PatrolState)
                    {
                        Debug.Log($"<color=cyan>[¡DETECTADO EXITOSAMENTE!]</color> {gameObject.name} vio al cazador de frente. Disparando aullido.");
                        EmitHowlAlert(hunter.transform.position);
                        ChangeState(new WolfHowlState(this, hunter.transform.position));
                    }
                }
                else
                {
                    Debug.Log($"<color=orange>[DEBUG-BLOQUEADO]</color> El raycast chocó con un obstáculo en la máscara '{LayerMask.LayerToName(obstacleMask)}' entre el lobo y el cazador.");
                }
            }
            else
            {
                Debug.Log($"<color=magenta>[DEBUG-FUERA DE CONO]</color> El cazador está en el radio pero el lobo está mirando hacia otro lado (Ángulo: {angle:F2}° > {maxAngle}°).");
            }
        }
    }

    private void EmitHowlAlert(Vector3 hunterPosition)
    {
        Debug.Log($"[{gameObject.name}] ¡Aullido de alerta emitido! Cazador localizado en: {hunterPosition}");

        // Reproducir sonido de aullido en el emisor
        if (AudioController != null)
        {
            AudioController.PlayHowl();
        }

        if (GameManager.Instance != null && GameManager.Instance.ActiveWolves != null)
        {
            foreach (var wolfObj in GameManager.Instance.ActiveWolves)
            {
                if (wolfObj != null && wolfObj != gameObject)
                {
                    WolfAI otherWolf = wolfObj.GetComponent<WolfAI>();
                    if (otherWolf != null)
                    {
                        otherWolf.ReceiveAlert(hunterPosition);
                    }
                }
            }
        }
    }

    public void ReceiveAlert(Vector3 hunterPosition)
    {
        // Si el lobo está patrullando, recibe el aviso, aúlla y va a la posición
        if (currentState == PatrolState)
        {
            Debug.Log($"[{gameObject.name}] Alerta de aullido recibida de otro lobo. Acudiendo.");
            ChangeState(new WolfHowlState(this, hunterPosition));
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