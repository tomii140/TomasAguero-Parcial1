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
        hunter.SetColorFeedback(Color.green); // Color verde para Patrulla
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

        // 4. Movimiento por Waypoints (Patrullaje en plano 3D XZ)
        Transform[] waypoints = hunter.Waypoints;
        if (waypoints == null || waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];

        // Forzamos la misma altura Y para evitar bloqueos por diferencias de terreno
        Vector3 targetPos = targetWaypoint.position;
        targetPos.y = hunter.transform.position.y;

        Vector3 force = hunter.Arrive(targetPos, 2.0f);
        hunter.AddForce(force);

        // Validación de distancia en el plano horizontal XZ
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
                    // Si llega al final, invierte el sentido (rebote / ida y vuelta exigido por rúbrica)
                    currentWaypointIndex = Mathf.Max(0, waypoints.Length - 2);
                    movingForward = false;
                }
            }
            else
            {
                currentWaypointIndex--;
                if (currentWaypointIndex < 0)
                {
                    currentWaypointIndex = Mathf.Min(1, waypoints.Length - 1);
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