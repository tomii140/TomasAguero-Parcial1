
using UnityEngine;

public class HunterGatherState : IState
{
    private readonly HunterAI hunter;
    private Boid targetBoid;
    private float gatherTimer;

    public HunterGatherState(HunterAI hunter)
    {
        this.hunter = hunter;
    }

    public void Enter()
    {
        targetBoid = hunter.GetTargetBoid();

        if (targetBoid == null || !targetBoid.IsDead || targetBoid.IsRespawning)
        {
            ReturnToPatrol();
            return;
        }

        gatherTimer = 0f;
        hunter.ResetVelocity();
        hunter.HideGatheringProgress();

        Debug.Log($"[CAZADOR] Comenzó a recolectar a {targetBoid.name}.");
    }

    public void Update()
    {
        if (!IsTargetValid())
            return;

        ProcessGatheringBehavior();
    }

    private bool IsTargetValid()
    {
        if (targetBoid == null || !targetBoid.IsDead || targetBoid.IsRespawning)
        {
            ReturnToPatrol();
            return false;
        }

        return true;
    }

    private void ProcessGatheringBehavior()
    {
        float distanceToTarget = Vector3.Distance(
            new Vector3(hunter.transform.position.x, 0f, hunter.transform.position.z),
            new Vector3(targetBoid.transform.position.x, 0f, targetBoid.transform.position.z)
        );

        if (distanceToTarget > hunter.CaptureRadius)
        {
            Vector3 targetPosition = targetBoid.transform.position;
            targetPosition.y = hunter.transform.position.y;

            Vector3 steeringForce = hunter.Seek(targetPosition);
            hunter.AddForce(steeringForce);

            // La recolección solo avanza mientras permanece dentro del radio.
            gatherTimer = 0f;
            hunter.HideGatheringProgress();
        }
        else
        {
            hunter.ResetVelocity();

            gatherTimer += Time.deltaTime;

            float progress = hunter.GatheringTime > 0f
                ? gatherTimer / hunter.GatheringTime
                : 1f;

            hunter.ShowGatheringProgress(progress);

            if (gatherTimer >= hunter.GatheringTime)
            {
                Debug.Log($"[CAZADOR] Terminó de recolectar a {targetBoid.name}.");

                hunter.HideGatheringProgress();

                // Registra el Boid capturado antes de iniciar su respawn.
                UIManager.Instance?.AddCapturedBoid();

                hunter.NotifyBoidCollected(targetBoid);
                ReturnToPatrol();
            }
        }
    }

    private void ReturnToPatrol()
    {
        hunter.HideGatheringProgress();
        hunter.SetTargetBoid(null);
        hunter.FSM.ChangeState(hunter.PatrolState);
    }

    public void Exit()
    {
        gatherTimer = 0f;
        hunter.HideGatheringProgress();
    }
}
