using UnityEngine;

public class HunterSearchState : IState
{
    private readonly HunterAI hunter;
    private int currentWaypointIndex = 0;
    private bool movingForward = true;
    private float fruitSpawnTimer = 0f;

    public HunterSearchState(HunterAI hunter)
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

        Boid deadBoid = hunter.FindBoidInVision(lookForDead: true);
        if (deadBoid != null && deadBoid.IsDead)
        {
            float distToDead = Vector3.Distance(hunter.transform.position, deadBoid.transform.position);
            if (distToDead <= hunter.CaptureRadius * 1.5f)
            {
                hunter.SetTargetBoid(deadBoid);
                hunter.FSM.ChangeState(hunter.GatherState);
                return;
            }
        }

        if (hunter.TimerAttackCooldown <= 0)
        {
            Boid liveBoid = hunter.FindBoidInVision(lookForDead: false);
            if (liveBoid != null && !liveBoid.IsDead)
            {
                hunter.SetTargetBoid(liveBoid);
                hunter.FSM.ChangeState(hunter.AttackState);
                return;
            }
        }

        Transform[] waypoints = hunter.Waypoints;
        if (waypoints == null || waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];

        Vector3 targetPos = targetWaypoint.position;
        targetPos.y = hunter.transform.position.y;

        Vector3 force = hunter.Arrive(targetPos, 2.0f);
        hunter.AddForce(force);

        float distanceXZ = Vector3.Distance(
            new Vector3(hunter.transform.position.x, 0f, hunter.transform.position.z),
            new Vector3(targetPos.x, 0f, targetPos.z)
        );

        if (distanceXZ < hunter.WaypointThreshold)
        {
            if (movingForward)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Length)
                {
                    currentWaypointIndex = waypoints.Length - 2;
                    if (currentWaypointIndex < 0) currentWaypointIndex = 0;
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

    private void HandleFruitSpawning()
    {
        fruitSpawnTimer += Time.deltaTime;
        if (fruitSpawnTimer >= hunter.FruitSpawnInterval)
        {
            fruitSpawnTimer = 0f;
            hunter.SpawnFruitIfAllowed();
        }
    }

    public void Exit() { }
}