using UnityEngine;

public class HunterAttackState : IState
{
    private readonly HunterAI hunter;
    private Vector3 lastKnownTargetPosition;
    private bool hasLastKnownPosition = false;

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
        if (HasExceededWorldBounds()) return;

        if (!ProcessEnergyManagement()) return;

        if (TryPrioritizeGathering()) return;

        EvaluateTargetAndSearch();
        ProcessCombatAndAttack();
        ApplyMovement();
    }

    

    private bool HasExceededWorldBounds()
    {
        if (WorldBounds.Instance == null) return false;

        bool hunterOutOfBounds = IsOutOfBounds(hunter.transform.position);
        Boid currentTarget = hunter.GetTargetBoid();
        bool targetOutOfBounds = currentTarget != null && IsOutOfBounds(currentTarget.transform.position);

        if (hunterOutOfBounds || targetOutOfBounds)
        {
            hunter.SetTargetBoid(null);
            hunter.FSM.ChangeState(hunter.PatrolState);
            return true;
        }
        return false;
    }

    private bool IsOutOfBounds(Vector3 pos)
    {
        return pos.x < WorldBounds.Instance.MinX || pos.x > WorldBounds.Instance.MaxX ||
               pos.z < WorldBounds.Instance.MinZ || pos.z > WorldBounds.Instance.MaxZ;
    }

    private bool ProcessEnergyManagement()
    {
        hunter.ConsumeOrRecoverEnergy(-hunter.EnergyConsumptionRate * Time.deltaTime);

        if (hunter.CurrentEnergy <= 0f)
        {
            hunter.FSM.ChangeState(hunter.RestState);
            return false;
        }
        return true;
    }

    private bool TryPrioritizeGathering()
    {
        Boid deadBoidInVision = hunter.FindBoidInVision(lookForDead: true);
        if (deadBoidInVision != null && deadBoidInVision.IsDead)
        {
            hunter.SetTargetBoid(deadBoidInVision);
            hunter.FSM.ChangeState(hunter.GatherState);
            return true;
        }
        return false;
    }

    private void EvaluateTargetAndSearch()
    {
        Boid target = hunter.GetTargetBoid();

        if (target != null && (target.IsDead || target.IsRespawning))
        {
            StoreLastKnownPosition(target.transform.position);
            hunter.SetTargetBoid(null);
            target = null;
        }

        if (target == null)
        {
            Boid nextLiveBoid = hunter.FindBoidInVision(lookForDead: false);
            if (nextLiveBoid != null && !nextLiveBoid.IsDead)
            {
                hunter.SetTargetBoid(nextLiveBoid);
                StoreLastKnownPosition(nextLiveBoid.transform.position);
            }
        }
        else
        {
            StoreLastKnownPosition(target.transform.position);
        }
    }

    private void StoreLastKnownPosition(Vector3 pos)
    {
        lastKnownTargetPosition = pos;
        hasLastKnownPosition = true;
    }

    private void ProcessCombatAndAttack()
    {
        Boid target = hunter.GetTargetBoid();
        if (target == null || target.IsDead) return;

        float distanceToTarget = Vector3.Distance(hunter.transform.position, target.transform.position);
        bool isMelee = distanceToTarget <= hunter.MeleeAttackRadius;

        DrawCombatDebugLines(target.transform.position, isMelee);

        if (distanceToTarget <= hunter.RangeAttackRadius && hunter.TimerAttackCooldown <= 0f)
        {
            ExecuteAttack(isMelee, target);
        }
    }

    private void DrawCombatDebugLines(Vector3 targetPos, bool isMelee)
    {
        Color lineColor = isMelee ? Color.magenta : Color.red;
        Debug.DrawLine(hunter.transform.position, targetPos, lineColor, 0.1f);
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

        if (target != null && !target.IsDead)
        {
            Vector3 pursuitForce = hunter.Pursuit(target);
            hunter.AddForce(pursuitForce);
        }
        else if (hasLastKnownPosition)
        {
            Vector3 seekLastPos = hunter.Seek(lastKnownTargetPosition);
            hunter.AddForce(seekLastPos);
        }
    }

    public void Exit()
    {
        hasLastKnownPosition = false;
    }
}