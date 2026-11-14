using UnityEngine;

public class HunterPatrolState : IState
{
    private readonly HunterAI hunter;
    private int currentWaypointIndex = 0;
    private bool movingForward = true;
    private float fruitSpawnTimer = 0f;

    public HunterPatrolState(HunterAI hunter)
    {
        this.hunter = hunter;
    }

    public void Enter()
    {
        hunter.SetColorFeedback(Color.green);
    }

    public void Update()
    {
        HandleFruitSpawning();

        if (EvaluateTargetTransitions()) return;

        ExecutePatrolMovement();
    }

    private void HandleFruitSpawning()
    {
        fruitSpawnTimer += Time.deltaTime;
        if (fruitSpawnTimer >= hunter.FruitSpawnInterval)
        {
            fruitSpawnTimer = 0f;
            hunter.SpawnFruitIfAllowed();
        }
    }

    private bool EvaluateTargetTransitions()
    {
        // Pasa a Gather con CUALQUIER Boid muerto detectado dentro de VisionRadius (sin restricción extra de distancia)
        Boid deadBoid = hunter.FindBoidInVision(lookForDead: true);
        if (deadBoid != null && deadBoid.IsDead)
        {
            hunter.SetTargetBoid(deadBoid);
            hunter.FSM.ChangeState(hunter.GatherState);
            return true;
        }

        // Pasa a Attack si detecta un Boid vivo dentro de VisionRadius
        if (hunter.TimerAttackCooldown <= 0)
        {
            Boid liveBoid = hunter.FindBoidInVision(lookForDead: false);
            if (liveBoid != null && !liveBoid.IsDead)
            {
                hunter.SetTargetBoid(liveBoid);
                hunter.FSM.ChangeState(hunter.AttackState);
                return true;
            }
        }

        return false;
    }

    private void ExecutePatrolMovement()
    {
        Transform[] waypoints = hunter.Waypoints;
        if (waypoints == null || waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 targetPos = targetWaypoint.position;
        targetPos.y = hunter.transform.position.y;

        Vector3 force = hunter.Arrive(targetPos, hunter.WaypointArriveSlowingRadius);
        hunter.AddForce(force);

        UpdateWaypointProgress(targetPos);
    }

    private void UpdateWaypointProgress(Vector3 targetPos)
    {
        float distanceXZ = Vector3.Distance(
            new Vector3(hunter.transform.position.x, 0f, hunter.transform.position.z),
            new Vector3(targetPos.x, 0f, targetPos.z)
        );

        if (distanceXZ < hunter.WaypointThreshold)
        {
            Transform[] waypoints = hunter.Waypoints;
            if (movingForward)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Length)
                {
                    currentWaypointIndex = Mathf.Max(0, waypoints.Length - 2);
                    movingForward = false;
                }
            }
            else
            {
                currentWaypointIndex--;
                if (currentWaypointIndex < 0)
                {
                    currentWaypointIndex = waypoints.Length > 1 ? 1 : 0;
                    movingForward = true;
                }
            }
        }
    }

    public void Exit() { }
}