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
        FindNearestWaypointIndex();
        isReversing = false;
        RequestPath();
    }

    public void UpdateState()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform targetWp = waypoints[currentIndex];

        Vector3 wolfPosXZ = new Vector3(wolf.transform.position.x, 0f, wolf.transform.position.z);
        Vector3 targetWpXZ = new Vector3(targetWp.position.x, 0f, targetWp.position.z);
        float distToWaypoint = Vector3.Distance(wolfPosXZ, targetWpXZ);

        if (distToWaypoint < 2.0f)
        {
            UpdateWaypointIndex();
            RequestPath();
            return;
        }

        // Si el path es nulo o está vacío, lo solicita de nuevo en lugar de ir en línea recta
        if (currentPath == null || currentPath.Count == 0)
        {
            RequestPath();
            return;
        }

        // Si llegó al último nodo del path pero aún no al waypoint físico, se dirige al último nodo de forma segura por grafo
        if (pathIndex >= currentPath.Count)
        {
            Vector3 lastNodePos = currentPath[currentPath.Count - 1].transform.position;
            wolf.AddForce(wolf.Arrive(lastNodePos, 1.2f));
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

    private void FindNearestWaypointIndex()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        int nearestIndex = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            float dist = Vector3.Distance(wolf.transform.position, waypoints[i].position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestIndex = i;
            }
        }
        currentIndex = nearestIndex;
    }

    private void RequestPath()
    {
        if (waypoints == null || waypoints.Length == 0 || GraphManager.Instance == null) return;
        Transform targetWp = waypoints[currentIndex];
        currentPath = GraphManager.Instance.FindPath(wolf.transform.position, targetWp.position, excludeSafeZones: false);
        pathIndex = 0;
    }

    private void FollowPath()
    {
        if (currentPath == null || pathIndex >= currentPath.Count) return;

        Vector3 targetPos = currentPath[pathIndex].transform.position;
        Vector3 force = wolf.Arrive(targetPos, 1.2f);
        wolf.AddForce(force);

        Vector3 wolfPosXZ = new Vector3(wolf.transform.position.x, 0f, wolf.transform.position.z);
        Vector3 targetPosXZ = new Vector3(targetPos.x, 0f, targetPos.z);

        // Umbral estricto de 1.0f para doblar correctamente en las esquinas sin cortar camino
        if (Vector3.Distance(wolfPosXZ, targetPosXZ) < 1.0f)
        {
            pathIndex++;
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