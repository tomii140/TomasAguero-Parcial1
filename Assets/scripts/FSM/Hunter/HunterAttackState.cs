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
        // 0. Condición estricta: Si la presa o el cazador salen de los límites del mundo, abandona Hunt y vuelve a Patrol
        if (IsOutOfBounds(hunter.transform.position))
        {
            hunter.SetTargetBoid(null);
            hunter.FSM.ChangeState(hunter.PatrolState);
            return;
        }

        Boid currentTarget = hunter.GetTargetBoid();
        if (currentTarget != null && IsOutOfBounds(currentTarget.transform.position))
        {
            hunter.SetTargetBoid(null);
            hunter.FSM.ChangeState(hunter.PatrolState);
            return;
        }

        // 1. Verificar agotamiento de energía
        if (!ProcessEnergyDrain()) return;

        // 2. Prioridad de recolección: si aparece CUALQUIER Boid muerto en VisionRadius, pasa a Gather
        Boid deadBoidInVision = hunter.FindBoidInVision(lookForDead: true);
        if (deadBoidInVision != null && deadBoidInVision.IsDead)
        {
            hunter.SetTargetBoid(deadBoidInVision);
            hunter.FSM.ChangeState(hunter.GatherState);
            return;
        }

        // 3. Evaluar objetivo actual o búsqueda en última posición conocida
        EvaluateTargetAndSearch();

        // 4. Ejecutar combate si el objetivo sigue vivo y en rango de ataque
        ProcessCombatAndAttack();

        // 5. Perseguir objetivo o avanzar hacia última posición conocida
        ApplyMovement();
    }

    private bool IsOutOfBounds(Vector3 pos)
    {
        if (WorldBounds.Instance == null) return false;
        return pos.x < WorldBounds.Instance.MinX || pos.x > WorldBounds.Instance.MaxX ||
               pos.z < WorldBounds.Instance.MinZ || pos.z > WorldBounds.Instance.MaxZ;
    }

    private bool ProcessEnergyDrain()
    {
        hunter.ConsumeOrRecoverEnergy(-hunter.EnergyConsumptionRate * Time.deltaTime);

        if (hunter.CurrentEnergy <= 0f)
        {
            hunter.FSM.ChangeState(hunter.RestState);
            return false;
        }

        return true;
    }

    private void EvaluateTargetAndSearch()
    {
        Boid target = hunter.GetTargetBoid();

        if (target != null && (target.IsDead || target.IsRespawning))
        {
            lastKnownTargetPosition = target.transform.position;
            hasLastKnownPosition = true;
            hunter.SetTargetBoid(null);
            target = null;
        }

        if (target == null)
        {
            Boid nextLiveBoid = hunter.FindBoidInVision(lookForDead: false);
            if (nextLiveBoid != null && !nextLiveBoid.IsDead)
            {
                hunter.SetTargetBoid(nextLiveBoid);
                lastKnownTargetPosition = nextLiveBoid.transform.position;
                hasLastKnownPosition = true;
            }
        }
        else
        {
            lastKnownTargetPosition = target.transform.position;
            hasLastKnownPosition = true;
        }
    }

    private void ProcessCombatAndAttack()
    {
        Boid target = hunter.GetTargetBoid();
        if (target == null || target.IsDead) return;

        float distanceToTarget = Vector3.Distance(hunter.transform.position, target.transform.position);
        bool isMelee = distanceToTarget <= hunter.MeleeAttackRadius;

        Color lineColor = isMelee ? Color.magenta : Color.red;
        Debug.DrawLine(hunter.transform.position, target.transform.position, lineColor, 0.1f);

        if (distanceToTarget <= hunter.RangeAttackRadius && hunter.TimerAttackCooldown <= 0f)
        {
            int damage = isMelee ? 2 : 1;
            hunter.PlayAttackSound(isMelee);
            target.ReceiveDamage(damage);
            hunter.ResetAttackCooldown();
        }
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