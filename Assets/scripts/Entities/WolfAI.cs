using UnityEngine;
using System.Collections.Generic;

public class WolfAI : Agent
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private bool isReversing = false;
    private List<Node> currentPath;
    private int pathIndex = 0;

    [Header("Perception & Vision")]
    [SerializeField, Range(1f, 25f)] private float visionRadius = 10f;
    [SerializeField, Range(10f, 120f)] private float fieldOfViewAngle = 60f;
    [SerializeField, Range(0.5f, 5f)] private float captureRadius = 1.5f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Alert Settings")]
    private Vector3? lastKnownHunterPosition = null;
    private bool isAlerted = false;

    protected override void Start()
    {
        base.Start();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ActiveWolves.Add(gameObject);
        }
        MoveToNextWaypoint();
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
        CheckHunterPerception();
        ExecuteBehavior();
        ApplyPhysics();
    }

    private void CheckHunterPerception()
    {
        if (GameManager.Instance == null || GameManager.Instance.Hunter == null) return;
        HunterAI hunter = GameManager.Instance.Hunter;

        Node hunterNode = GraphManager.Instance.GetNearestNode(hunter.transform.position);
        if (hunter.FSM.CurrentState is HunterDeadState || (hunterNode != null && hunterNode.Zone == ZoneType.SafeZone))
        {
            if (isAlerted)
            {
                LoseTarget();
            }
            return;
        }

        float distToHunter = Vector3.Distance(transform.position, hunter.transform.position);
        if (distToHunter <= visionRadius)
        {
            Vector3 dirToHunter = (hunter.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToHunter);

            if (angle <= fieldOfViewAngle / 2f)
            {
                if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToHunter, distToHunter, obstacleMask))
                {
                    DetectHunter(hunter.transform.position);
                }
            }
        }
    }

    public void DetectHunter(Vector3 hunterPosition)
    {
        lastKnownHunterPosition = hunterPosition;
        if (!isAlerted)
        {
            isAlerted = true;
            EmitHowl(hunterPosition);
        }

        currentPath = GraphManager.Instance.FindPath(transform.position, hunterPosition, excludeSafeZones: true);
        pathIndex = 0;
    }

    public void ReceiveAlert(Vector3 hunterPosition)
    {
        if (!isAlerted)
        {
            isAlerted = true;
            lastKnownHunterPosition = hunterPosition;
            currentPath = GraphManager.Instance.FindPath(transform.position, hunterPosition, excludeSafeZones: true);
            pathIndex = 0;
        }
    }

    private void EmitHowl(Vector3 hunterPosition)
    {
        if (GameManager.Instance == null) return;
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

    private void LoseTarget()
    {
        isAlerted = false;
        lastKnownHunterPosition = null;
        currentPath = null;
        MoveToNextWaypoint();
    }

    private void ExecuteBehavior()
    {
        if (GameManager.Instance == null || GameManager.Instance.Hunter == null) return;
        HunterAI hunter = GameManager.Instance.Hunter;

        float distToHunter = Vector3.Distance(transform.position, hunter.transform.position);
        if (distToHunter <= captureRadius && !(hunter.FSM.CurrentState is HunterDeadState))
        {
            hunter.FSM.ChangeState(hunter.DeadState);
            return;
        }

        if (isAlerted && lastKnownHunterPosition.HasValue)
        {
            if (distToHunter <= visionRadius)
            {
                AddForce(Seek(hunter.transform.position));
            }
            else
            {
                FollowPathExecution();
            }
        }
        else
        {
            PatrolBehavior();
        }
    }

    private void PatrolBehavior()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform targetWp = waypoints[currentWaypointIndex];
        float dist = Vector3.Distance(transform.position, targetWp.position);

        if (dist < 1f)
        {
            UpdateWaypointIndex();
            MoveToNextWaypoint();
        }
        else
        {
            FollowPathExecution();
        }
    }

    private void MoveToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        Transform targetWp = waypoints[currentWaypointIndex];

        Vector3 dir = (targetWp.position - transform.position);
        float dist = dir.magnitude;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dir.normalized, dist, obstacleMask))
        {
            currentPath = GraphManager.Instance.FindPath(transform.position, targetWp.position, excludeSafeZones: true);
            pathIndex = 0;
        }
        else
        {
            currentPath = null;
        }
    }

    private void FollowPathExecution()
    {
        if (currentPath != null && pathIndex < currentPath.Count)
        {
            Vector3 targetPos = currentPath[pathIndex].transform.position;
            AddForce(Seek(targetPos));

            if (Vector3.Distance(transform.position, targetPos) < 0.8f)
            {
                pathIndex++;
            }
        }
        else if (waypoints != null && waypoints.Length > 0)
        {
            AddForce(Seek(waypoints[currentWaypointIndex].position));
            if (Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position) < 0.8f)
            {
                UpdateWaypointIndex();
                MoveToNextWaypoint();
            }
        }
    }

    private void UpdateWaypointIndex()
    {
        if (!isReversing)
        {
            if (currentWaypointIndex + 1 < waypoints.Length)
            {
                currentWaypointIndex++;
            }
            else
            {
                isReversing = true;
                currentWaypointIndex--;
            }
        }
        else
        {
            if (currentWaypointIndex - 1 >= 0)
            {
                currentWaypointIndex--;
            }
            else
            {
                isReversing = false;
                currentWaypointIndex++;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isAlerted ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        if (waypoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var wp in waypoints)
            {
                if (wp != null) Gizmos.DrawSphere(wp.position, 0.2f);
            }
        }
    }
}