using UnityEngine;
using System.Collections.Generic;

public class WolfChaseState : IWolfState
{
    private WolfAI wolf;
    private List<Node> currentPath;
    private int pathIndex = 0;
    private float timer = 0f;
    private float interval = 0.5f;
    private Vector3? customTargetPosition = null; // Permite almacenar la última posición conocida por aullido

    public WolfChaseState(WolfAI wolf)
    {
        this.wolf = wolf;
    }

    public void Enter()
    {
        customTargetPosition = null; // Al entrar por visión directa limpia posiciones previas de aullidos
        RecalculatePath();
    }

    public void UpdateState()
    {
        HunterAI hunter = GameManager.Instance.Hunter;
        if (hunter == null) return;

        float dist = Vector3.Distance(wolf.transform.position, hunter.transform.position);

        // Si el lobo tiene línea de visión directa con el cazador, lo persigue directamente
        if (dist <= wolf.VisionRadius && !Physics.Raycast(wolf.transform.position + Vector3.up * 0.5f, (hunter.transform.position - wolf.transform.position).normalized, dist, wolf.ObstacleMask))
        {
            customTargetPosition = null; // Vuelve a rastreo visual directo
            Vector3 force = wolf.Arrive(hunter.transform.position, 2.0f);
            wolf.AddForce(force);
        }
        else
        {
            // Si no lo ve directamente, utiliza A* hacia el objetivo (última posición del aullido o la del cazador)
            timer += Time.deltaTime;
            if (timer >= interval)
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

    // Método requerido para recibir la alerta de los aullidos de otros lobos
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
        if (GameManager.Instance.Hunter == null) return;

        // Si hay una posición de aullido almacenada y no ve al cazador, calcula hacia esa posición; si no, hacia el cazador
        Vector3 targetDestination = customTargetPosition.HasValue ? customTargetPosition.Value : GameManager.Instance.Hunter.transform.position;

        currentPath = GraphManager.Instance.FindPath(wolf.transform.position, targetDestination, excludeSafeZones: true);
        pathIndex = 0;
    }

    private void FollowPath()
    {
        Vector3 finalDestination = customTargetPosition.HasValue ? customTargetPosition.Value : GameManager.Instance.Hunter.transform.position;

        if (currentPath != null && currentPath.Count > 0)
        {
            if (pathIndex < currentPath.Count)
            {
                Vector3 targetPos = currentPath[pathIndex].transform.position;
                Vector3 force = wolf.Arrive(targetPos, 2.0f);
                wolf.AddForce(force);

                if (Vector3.Distance(wolf.transform.position, targetPos) < 1.5f)
                {
                    pathIndex++;
                }
            }
            else
            {
                Vector3 force = wolf.Arrive(finalDestination, 2.0f);
                wolf.AddForce(force);
            }
        }
        else
        {
            Vector3 force = wolf.Arrive(finalDestination, 2.0f);
            wolf.AddForce(force);
        }
    }
}