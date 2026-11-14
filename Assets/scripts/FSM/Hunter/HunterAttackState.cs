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

        // 3. Evaluar objetivo actual o búsqueda en última posición conocida (sin abandonar Hunt ni volver a Patrol)
        EvaluateTargetAndSearch();

        // 4. Ejecutar combate si el objetivo sigue vivo y en rango de ataque
        ProcessCombatAndAttack();

        // 5. Perseguir objetivo o avanzar hacia última posición conocida
        ApplyMovement();
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

        // Si el objetivo actual murió (y no se disparó Gather arriba por alguna razón) o se perdió referencia
        if (target != null && (target.IsDead || target.IsRespawning))
        {
            lastKnownTargetPosition = target.transform.position;
            hasLastKnownPosition = true;
            hunter.SetTargetBoid(null);
            target = null;
        }

        // Si no tiene objetivo activo, intenta detectar una nueva presa viva dentro de VisionRadius
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
            // Regla clave: NO abandona la presa aunque salga de VisionRadius; actualiza su posición conocida
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
            // Persecución continua (Pursuit) incluso si sale de VisionRadius
            Vector3 pursuitForce = hunter.Pursuit(target);
            hunter.AddForce(pursuitForce);
        }
        else if (hasLastKnownPosition)
        {
            // Si la presa murió y no hay otra visible, permanece en Hunt desplazándose a la última posición conocida
            Vector3 seekLastPos = hunter.Seek(lastKnownTargetPosition);
            hunter.AddForce(seekLastPos);
        }
    }

    public void Exit()
    {
        hasLastKnownPosition = false;
    }
}