using UnityEngine;
using System.Collections.Generic;

public class WolfPatrolState : IWolfState
{
    private WolfAI wolf;
    private Transform[] waypoints;
    private int currentIndex = 0;
    private bool isReversing = false;
    private List<Node> currentPath;
    private int pathIndex = 0;

    public WolfPatrolState(WolfAI wolf, Transform[] waypoints)
    {
        this.wolf = wolf;
        this.waypoints = waypoints;
    }

    public void Enter()
    {
        currentIndex = 0;
        isReversing = false;
        RequestPath();
    }

    public void UpdateState()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform targetWp = waypoints[currentIndex];

        // Distancia plana en XZ para evitar bloqueos por diferencias en el eje Y
        Vector3 wolfPosXZ = new Vector3(wolf.transform.position.x, 0f, wolf.transform.position.z);
        Vector3 targetWpXZ = new Vector3(targetWp.position.x, 0f, targetWp.position.z);
        float distToWaypoint = Vector3.Distance(wolfPosXZ, targetWpXZ);

        if (distToWaypoint < 2.5f)
        {
            UpdateWaypointIndex();
            RequestPath();
            return;
        }

        if (currentPath == null || currentPath.Count == 0 || pathIndex >= currentPath.Count)
        {
            MoveDirectlyToWaypoint(targetWp.position);
            return;
        }

        FollowPath();

        if (wolf.Animator != null)
        {
            float currentSpeed = wolf.Velocity.magnitude;
            wolf.Animator.SetFloat("Speed", currentSpeed > 0.1f ? 0.5f : 0f);
        }
    }

    public void Exit()
    {
        currentPath = null;
    }

    private void RequestPath()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        Transform targetWp = waypoints[currentIndex];
        currentPath = GraphManager.Instance.FindPath(wolf.transform.position, targetWp.position, excludeSafeZones: false);
        pathIndex = 0;
    }

    private void FollowPath()
    {
        if (currentPath == null || pathIndex >= currentPath.Count) return;

        Vector3 targetPos = currentPath[pathIndex].transform.position;
        Vector3 force = wolf.Arrive(targetPos, 2.0f);
        wolf.AddForce(force);

        // Distancia plana XZ con umbral de 2.0f para avanzar de nodo sin atascarse
        Vector3 wolfPosXZ = new Vector3(wolf.transform.position.x, 0f, wolf.transform.position.z);
        Vector3 targetPosXZ = new Vector3(targetPos.x, 0f, targetPos.z);

        if (Vector3.Distance(wolfPosXZ, targetPosXZ) < 2.0f)
        {
            pathIndex++;
        }
    }

    private void MoveDirectlyToWaypoint(Vector3 targetPos)
    {
        Vector3 directDir = (targetPos - wolf.transform.position);
        directDir.y = 0;

        if (directDir.magnitude > 0.5f)
        {
            Vector3 force = directDir.normalized * wolf.MaxSpeed - wolf.Velocity;
            wolf.AddForce(Vector3.ClampMagnitude(force, wolf.MaxForce));
        }
    }

    private void UpdateWaypointIndex()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (!isReversing)
        {
            if (currentIndex + 1 < waypoints.Length)
            {
                currentIndex++;
            }
            else
            {
                isReversing = true;
                currentIndex = Mathf.Max(0, waypoints.Length - 2);
            }
        }
        else
        {
            if (currentIndex - 1 >= 0)
            {
                currentIndex--;
            }
            else
            {
                isReversing = false;
                currentIndex = Mathf.Min(1, waypoints.Length - 1);
            }
        }
    }
}