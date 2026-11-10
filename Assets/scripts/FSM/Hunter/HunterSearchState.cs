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
        hunter.SetColorFeedback(Color.green); // Color para Patrulla
    }

    public void Update()
    {
        // 1. Generación periódica de objetos de interés (máximo 5 activos)
        HandleFruitSpawning();

        // 2. Transición prioritaria a Gather si detecta un Boid eliminado en su radio de visión
        Boid deadBoid = hunter.FindBoidInVision(lookForDead: true);
        if (deadBoid != null && deadBoid.isDead)
        {
            hunter.SetTargetBoid(deadBoid);
            hunter.FSM.ChangeState(hunter.GatherState);
            return;
        }

        // 3. Transición a Attack si el TBA finalizó y detecta un Boid vivo
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

        // 4. Movimiento por Waypoints (Patrullaje)
        Transform[] waypoints = hunter.Waypoints;
        if (waypoints == null || waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 force = hunter.Arrive(targetWaypoint.position, 2.0f);
        hunter.AddForce(force);

        if (Vector3.Distance(hunter.transform.position, targetWaypoint.position) < hunter.WaypointThreshold)
        {
            if (movingForward)
            {
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Length)
                {
                    currentWaypointIndex = waypoints.Length - 2;
                    movingForward = false;
                }
            }
            else
            {
                currentWaypointIndex--;
                if (currentWaypointIndex < 0)
                {
                    currentWaypointIndex = 1;
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