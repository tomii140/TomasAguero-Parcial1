using UnityEngine;
using System.Collections.Generic;

public class WolfChaseState : IWolfState
{
    private WolfAI wolf;
    private List<Node> currentPath;
    private int pathIndex = 0;
    private float timer = 0f;
    private float interval = 0.5f;
    private Vector3? customTargetPosition = null;

    public WolfChaseState(WolfAI wolf)
    {
        this.wolf = wolf;
    }

    public void Enter()
    {
        customTargetPosition = null;
        RecalculatePath();
    }

    public void UpdateState()
    {
        HunterAI hunter = GameManager.Instance.Hunter;
        if (hunter == null) return;

        float dist = Vector3.Distance(wolf.transform.position, hunter.transform.position);

        // Vector y dirección hacia el cazador
        Vector3 rayOrigin = wolf.transform.position + Vector3.up * 1.0f;
        Vector3 dirToHunter = (hunter.transform.position - wolf.transform.position).normalized;

        // Validamos si hay obstáculo real (asegúrate de que obstacleMask incluya paredes y vallas)
        bool lineOfSightClear = !Physics.Raycast(rayOrigin, dirToHunter, dist, wolf.ObstacleMask);

        // Solo persecución directa en línea recta si lo ve DIRECTAMENTE, está muy cerca Y no hay obstáculos físicos
        if (dist <= (wolf.VisionRadius * 0.5f) && lineOfSightClear)
        {
            customTargetPosition = null;
            Vector3 force = wolf.Arrive(hunter.transform.position, 2.0f);
            wolf.AddForce(force);
        }
        else
        {
            // Si hay obstáculos o está lejos, OBLIGATORIAMENTE sigue la ruta de nodos A* sin cortar camino
            timer += Time.deltaTime;
            if (timer >= interval || currentPath == null || currentPath.Count == 0)
            {
                timer = 0f;
                RecalculatePath();
            }
            FollowPath();
        }

        if (wolf.Animator != null)
        {
            float currentSpeed = wolf.Velocity.magnitude;
            wolf.Animator.SetFloat("Speed", currentSpeed > 0.1f ? 1.5f : 0f);
        }
    }

    public void Exit()
    {
        currentPath = null;
        customTargetPosition = null;
    }

    public void SetLastKnownPosition(Vector3 position)
    {
        customTargetPosition = position;
        if (wolf != null && GraphManager.Instance != null)
        {
            currentPath = GraphManager.Instance.FindPath(wolf.transform.position, customTargetPosition.Value, excludeSafeZones: true);
            pathIndex = 0;
        }
    }

    private void RecalculatePath()
    {
        if (GameManager.Instance.Hunter == null || GraphManager.Instance == null) return;

        Vector3 targetDestination = customTargetPosition.HasValue ? customTargetPosition.Value : GameManager.Instance.Hunter.transform.position;

        currentPath = GraphManager.Instance.FindPath(wolf.transform.position, targetDestination, excludeSafeZones: true);
        pathIndex = 0;
    }

    private void FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0)
        {
            Vector3 fallbackDest = customTargetPosition.HasValue ? customTargetPosition.Value : GameManager.Instance.Hunter.transform.position;
            wolf.AddForce(wolf.Arrive(fallbackDest, 2.0f));
            return;
        }

        if (pathIndex < currentPath.Count)
        {
            if (currentPath[pathIndex] == null)
            {
                pathIndex++;
                return;
            }

            Vector3 targetPos = currentPath[pathIndex].transform.position;

            // Fuerza de Arrive optimizada para giros cerrados
            Vector3 force = wolf.Arrive(targetPos, 1.2f);
            wolf.AddForce(force);

            // Distancia plana XZ con umbral estricto de 1.0f para evitar cortar camino en las esquinas
            Vector3 wolfPosXZ = new Vector3(wolf.transform.position.x, 0f, wolf.transform.position.z);
            Vector3 targetPosXZ = new Vector3(targetPos.x, 0f, targetPos.z);

            if (Vector3.Distance(wolfPosXZ, targetPosXZ) < 1.0f)
            {
                pathIndex++;
            }
        }
        else
        {
            Vector3 finalDestination = customTargetPosition.HasValue ? customTargetPosition.Value : GameManager.Instance.Hunter.transform.position;
            wolf.AddForce(wolf.Arrive(finalDestination, 2.0f));
        }
    }
}