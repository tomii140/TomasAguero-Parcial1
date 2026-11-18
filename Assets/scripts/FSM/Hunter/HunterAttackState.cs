
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

        if (initialTarget == null)
            initialTarget = hunter.FindBoidInVision(false);

        if (initialTarget != null)
        {
            hunter.SetTargetBoid(initialTarget);

            lastKnownTargetPosition = initialTarget.transform.position;
            hasLastKnownPosition = true;
            targetWrapCount = GetCurrentWrapCount(initialTarget);
        }
        else
        {
            hasLastKnownPosition = false;
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

        if (deadBoid == null || !deadBoid.IsDead)
            return false;

        hunter.SetTargetBoid(deadBoid);
        hunter.FSM.ChangeState(hunter.GatherState);

        return true;
    }

    private void EvaluateTargetAndSearch()
    {
        Boid currentTarget = hunter.GetTargetBoid();

        // Conserva la presa mientras siga viva, aunque salga de visión.
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

        if (visibleTarget == null)
            return;

        if (visibleTarget.IsDead || visibleTarget.IsRespawning)
            return;

        hunter.SetTargetBoid(visibleTarget);

        lastKnownTargetPosition = visibleTarget.transform.position;
        hasLastKnownPosition = true;
        targetWrapCount = GetCurrentWrapCount(visibleTarget);
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

        Debug.DrawLine(
      hunter.transform.position,
      target.transform.position,
      isMelee ? Color.magenta : Color.red,
      hunter.AttackDebugLineDuration
  );

        if (distance > hunter.RangeAttackRadius)
            return;

        if (hunter.TimerAttackCooldown > 0f)
            return;

        ExecuteAttack(isMelee, target);
    }

    private void ExecuteAttack(bool isMelee, Boid target)
    {
        int damage = isMelee
            ? hunter.MeleeDamage
            : hunter.RangedDamage;

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
            float distance = Vector3.Distance(
                hunter.transform.position,
                target.transform.position
            );

            if (distance > hunter.RangeAttackRadius)
                hunter.AddForce(hunter.Pursuit(target));
            else
                hunter.ResetVelocity();

            return;
        }

        if (!hasLastKnownPosition)
        {
            hunter.ResetVelocity();
            return;
        }

        float distanceToLastPosition = Vector3.Distance(
            hunter.transform.position,
            lastKnownTargetPosition
        );

        if (distanceToLastPosition <= hunter.WaypointThreshold)
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
