
using UnityEngine;

public class HunterAttackState : IState
{
    private readonly HunterAI hunter;

    private Vector3 lastKnownTargetPosition;
    private bool hasLastKnownPosition;

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
        }
    }

    public void Update()
    {
        if (HasExceededWorldBounds())
            return;

        if (!ProcessEnergyManagement())
            return;

        if (TryPrioritizeGathering())
            return;

        EvaluateTargetAndSearch();

        ProcessCombatAndAttack();

        ApplyMovement();
    }

    private bool HasExceededWorldBounds()
    {
        if (WorldBounds.Instance == null)
            return false;

        bool hunterOutOfBounds = IsOutOfBounds(hunter.transform.position);

        Boid currentTarget = hunter.GetTargetBoid();

        bool targetOutOfBounds =
            currentTarget != null &&
            IsOutOfBounds(currentTarget.transform.position);

        if (hunterOutOfBounds || targetOutOfBounds)
        {
            hunter.SetTargetBoid(null);
            hunter.FSM.ChangeState(hunter.PatrolState);
            return true;
        }

        return false;
    }

    private bool IsOutOfBounds(Vector3 position)
    {
        return position.x < WorldBounds.Instance.MinX ||
               position.x > WorldBounds.Instance.MaxX ||
               position.z < WorldBounds.Instance.MinZ ||
               position.z > WorldBounds.Instance.MaxZ;
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
            (currentTarget.IsDead || currentTarget.IsRespawning))
        {
            hunter.SetTargetBoid(null);
            currentTarget = null;
        }

        Boid visibleTarget = hunter.FindBoidInVision(false);

        if (visibleTarget != null)
        {
            hunter.SetTargetBoid(visibleTarget);

            lastKnownTargetPosition =
                visibleTarget.transform.position;

            hasLastKnownPosition = true;
        }
        else if (currentTarget != null)
        {
            // Perdió la visión: conserva solamente la última posición conocida.
            hunter.SetTargetBoid(null);
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

        if (target != null)
        {
            hunter.AddForce(hunter.Pursuit(target));
            return;
        }

        if (!hasLastKnownPosition)
            return;

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
            hasLastKnownPosition = false;
            hunter.FSM.ChangeState(hunter.PatrolState);
            return;
        }

        hunter.AddForce(hunter.Seek(lastKnownTargetPosition));
    }

    public void Exit()
    {
        hasLastKnownPosition = false;
    }
}
