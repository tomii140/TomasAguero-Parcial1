using UnityEngine;
using System.Collections.Generic;

public class HunterSearchState : IState
{
    private readonly HunterAI hunter;
    private List<Node> currentPath;
    private int pathIndex = 0;
    private bool reachingCoto = true;

    public HunterSearchState(HunterAI hunter)
    {
        this.hunter = hunter;
    }

    public void Enter()
    {
        hunter.SetColorFeedback(Color.blue);
        reachingCoto = true;

        if (hunter.CotoCenter != null && GraphManager.Instance != null)
        {
            currentPath = GraphManager.Instance.FindPath(hunter.transform.position, hunter.CotoCenter.position, excludeSafeZones: false);
            pathIndex = 0;
        }
    }

    public void Update()
    {
        if (GraphManager.Instance == null) return;

        // Transición prioritaria al estado Gather si ve un boid muerto
        Boid deadBoid = hunter.FindBoidInVision(lookForDead: true);
        if (deadBoid != null && deadBoid.isDead)
        {
            hunter.SetTargetBoid(deadBoid);
            hunter.FSM.ChangeState(hunter.GatherState);
            return;
        }

        // Transición prioritaria al estado Attack si el cooldown finalizó y ve un boid vivo
        if (hunter.TimerAttackCooldown <= 0)
        {
            Boid liveBoid = hunter.FindBoidInVision(lookForDead: false);
            if (liveBoid != null && !liveBoid.isDead)
            {
                hunter.SetTargetBoid(liveBoid);
                hunter.FSM.ChangeState(hunter.AttackState);
                return;
            }
        }

        // Control de llegada inicial al Coto de Caza
        if (reachingCoto && hunter.CotoCenter != null)
        {
            Vector3 hunterXZ = new Vector3(hunter.transform.position.x, 0f, hunter.transform.position.z);
            Vector3 cotoXZ = new Vector3(hunter.CotoCenter.position.x, 0f, hunter.CotoCenter.position.z);

            if (Vector3.Distance(hunterXZ, cotoXZ) < 5.0f || (currentPath != null && pathIndex >= currentPath.Count))
            {
                reachingCoto = false;
                RequestRandomCotoNodePath();
                return;
            }
        }

        if (currentPath == null || pathIndex >= currentPath.Count)
        {
            if (reachingCoto && hunter.CotoCenter != null)
            {
                currentPath = GraphManager.Instance.FindPath(hunter.transform.position, hunter.CotoCenter.position, excludeSafeZones: false);
            }
            else
            {
                RequestRandomCotoNodePath();
            }
            pathIndex = 0;
            return;
        }

        // Navegación secuencial por nodos utilizando Arrive
        Vector3 targetNodePos = currentPath[pathIndex].transform.position;
        Vector3 force = hunter.Arrive(targetNodePos, 2.0f);
        hunter.AddForce(force);

        Vector3 currentHunterXZ = new Vector3(hunter.transform.position.x, 0f, hunter.transform.position.z);
        Vector3 targetNodeXZ = new Vector3(targetNodePos.x, 0f, targetNodePos.z);

        if (Vector3.Distance(currentHunterXZ, targetNodeXZ) < 2.0f)
        {
            pathIndex++;
        }
    }

    private void RequestRandomCotoNodePath()
    {
        if (GraphManager.Instance == null || GraphManager.Instance.AllNodes == null || GraphManager.Instance.AllNodes.Count == 0) return;

        List<Node> validNodes = new List<Node>();
        foreach (var node in GraphManager.Instance.AllNodes)
        {
            if (node != null && hunter.CotoCenter != null)
            {
                float dist = Vector3.Distance(hunter.CotoCenter.position, node.transform.position);
                if (dist <= 15.0f)
                {
                    validNodes.Add(node);
                }
            }
        }

        if (validNodes.Count > 0)
        {
            Node randomNode = validNodes[Random.Range(0, validNodes.Count)];
            currentPath = GraphManager.Instance.FindPath(hunter.transform.position, randomNode.transform.position, excludeSafeZones: false);
            pathIndex = 0;
        }
    }

    public void Exit()
    {
        currentPath = null;
    }
}