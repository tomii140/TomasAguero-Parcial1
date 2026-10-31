using UnityEngine;
using System.Collections.Generic;

public class WolfChaseState : IWolfState
{
    private WolfAI wolf;
    private List<Node> currentPath;
    private int pathIndex = 0;
    private float timer = 0f;
    private float interval = 0.5f;

    public WolfChaseState(WolfAI wolf)
    {
        this.wolf = wolf;
    }

    public void Enter()
    {
        RecalculatePath();
    }

    public void UpdateState()
    {
        HunterAI hunter = GameManager.Instance.Hunter;
        if (hunter == null) return;

        float dist = Vector3.Distance(wolf.transform.position, hunter.transform.position);

        if (dist <= wolf.VisionRadius && !Physics.Raycast(wolf.transform.position + Vector3.up * 0.5f, (hunter.transform.position - wolf.transform.position).normalized, dist, wolf.ObstacleMask))
        {
            Vector3 force = wolf.Arrive(hunter.transform.position, 2.0f);
            wolf.AddForce(force);
        }
        else
        {
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

    public void Exit() { currentPath = null; }

    private void RecalculatePath()
    {
        if (GameManager.Instance.Hunter == null) return;
        currentPath = GraphManager.Instance.FindPath(wolf.transform.position, GameManager.Instance.Hunter.transform.position, excludeSafeZones: true);
        pathIndex = 0;
    }

    private void FollowPath()
    {
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
                Vector3 force = wolf.Arrive(GameManager.Instance.Hunter.transform.position, 2.0f);
                wolf.AddForce(force);
            }
        }
        else
        {
            Vector3 force = wolf.Arrive(GameManager.Instance.Hunter.transform.position, 2.0f);
            wolf.AddForce(force);
        }
    }
}