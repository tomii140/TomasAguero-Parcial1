
using UnityEngine;

public class HunterAttackState : IState
{
    private readonly HunterAI hunter;

    private Vector3 lastKnownTargetPosition;
    private bool hasLastKnownPosition;
    private int targetWrapCount;

    public HunterAttackState(HunterAI hunter)
    {
        this.hunter = hunter;
    }

    public void Enter()
    {
        hunter.PlaySound(hunter.DetectBoidSound);

        Boid initialTarget = hunter.GetTargetBoid();

        if (initialTarget != null)
        {
            lastKnownTargetPosition = initialTarget.transform.position;
            hasLastKnownPosition = true;
            targetWrapCount = GetCurrentWrapCount(initialTarget);
        }
        else
        {
            targetWrapCount = 0;
        }
    }

    public void Update()
    {
        if (HasTargetLeftMap())
            return;

        if (!ProcessEnergyManagement())
            return;

        if (TryPrioritizeGathering())
            return;

        EvaluateTargetAndSearch();

        ProcessCombatAndAttack();

        ApplyMovement();
    }

    private int GetCurrentWrapCount(Boid target)
    {
        if (WorldBounds.Instance == null || target == null)
            return 0;

        return WorldBounds.Instance.GetBoidWrapCount(target);
    }

    private bool HasTargetLeftMap()
    {
        Boid currentTarget = hunter.GetTargetBoid();

        if (currentTarget == null || WorldBounds.Instance == null)
            return false;

        int currentWrapCount = GetCurrentWrapCount(currentTarget);

        if (currentWrapCount > targetWrapCount)
        {
            Debug.Log("[HUNTER] La presa salió del mapa. Regresando a Patrol.");

            hunter.SetTargetBoid(null);
            hunter.FSM.ChangeState(hunter.PatrolState);
            return true;
        }

        return false;
    }

    private bool ProcessEnergyManagement()
    {
        hunter.ConsumeOrRecoverEnergy(
            -hunter.EnergyConsumptionRate * Time.deltaTime
        );

        if (hunter.CurrentEnergy <= 0f)
        {
            hunter.FSM.ChangeState(hunter.RestState);
            return false;
        }

        return true;
    }

    private bool TryPrioritizeGathering()
    {
        Boid deadBoid = hunter.FindBoidInVision(true);

        if (deadBoid != null && deadBoid.IsDead)
        {
            hunter.SetTargetBoid(deadBoid);
            hunter.FSM.ChangeState(hunter.GatherState);
            return true;
        }

        return false;
    }

    private void EvaluateTargetAndSearch()
    {
        Boid currentTarget = hunter.GetTargetBoid();

        if (currentTarget != null &&
            !currentTarget.IsDead &&
            !currentTarget.IsRespawning)
        {
            lastKnownTargetPosition = currentTarget.transform.position;
            hasLastKnownPosition = true;
            return;
        }

        if (currentTarget != null)
            hunter.SetTargetBoid(null);

        Boid visibleTarget = hunter.FindBoidInVision(false);

        if (visibleTarget != null &&
            !visibleTarget.IsDead &&
            !visibleTarget.IsRespawning)
        {
            hunter.SetTargetBoid(visibleTarget);

            lastKnownTargetPosition = visibleTarget.transform.position;
            hasLastKnownPosition = true;
            targetWrapCount = GetCurrentWrapCount(visibleTarget);
        }
    }

    private void ProcessCombatAndAttack()
    {
        Boid target = hunter.GetTargetBoid();

        if (target == null || target.IsDead || target.IsRespawning)
            return;

        float distance = Vector3.Distance(
            hunter.transform.position,
            target.transform.position
        );

        bool isMelee = distance <= hunter.MeleeAttackRadius;

        DrawCombatDebugLines(target.transform.position, isMelee);

        if (distance <= hunter.RangeAttackRadius &&
            hunter.TimerAttackCooldown <= 0f)
        {
            ExecuteAttack(isMelee, target);
        }
    }

    private void DrawCombatDebugLines(Vector3 targetPosition, bool isMelee)
    {
        Color lineColor = isMelee ? Color.magenta : Color.red;

        Debug.DrawLine(
            hunter.transform.position,
            targetPosition,
            lineColor,
            0.1f
        );
    }

    private void ExecuteAttack(bool isMelee, Boid target)
    {
        int damage = isMelee ? 2 : 1;

        hunter.PlayAttackSound(isMelee);
        target.ReceiveDamage(damage);
        hunter.ResetAttackCooldown();
    }

    private void ApplyMovement()
    {
        Boid target = hunter.GetTargetBoid();

        if (target != null &&
            !target.IsDead &&
            !target.IsRespawning)
        {
            hunter.AddForce(hunter.Pursuit(target));
            return;
        }

        if (!hasLastKnownPosition)
        {
            hunter.ResetVelocity();
            return;
        }

        Vector3 hunterPosition = hunter.transform.position;

        float distance = Vector3.Distance(
            new Vector3(hunterPosition.x, 0f, hunterPosition.z),
            new Vector3(
                lastKnownTargetPosition.x,
                0f,
                lastKnownTargetPosition.z
            )
        );

        if (distance <= hunter.WaypointThreshold)
        {
            hunter.ResetVelocity();
            return;
        }

        hunter.AddForce(hunter.Seek(lastKnownTargetPosition));
    }

    public void Exit()
    {
        hasLastKnownPosition = false;
        targetWrapCount = 0;
    }
}
