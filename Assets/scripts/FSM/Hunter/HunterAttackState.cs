using UnityEngine;

public class HunterAttackState : IState
{
    private readonly HunterAI hunter;

    public HunterAttackState(HunterAI hunter)
    {
        this.hunter = hunter;
    }

    public void Enter()
    {
        // se ejecuta una sola vez al entrar en ataque (al detectar/fijar al boid)
        hunter.PlaySound(hunter.DetectBoidSound);
    }

    public void Update()
    {
        if (!ProcessEnergyDrain()) return;

        if (!ValidateAndSwitchTarget()) return;

        ProcessCombatAndAttack();
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

    private bool ValidateAndSwitchTarget()
    {
        Boid target = hunter.GetTargetBoid();

        if (target != null && target.IsDead)
        {
            if (TrySwitchToGatherOrPatrol()) return false;
        }

        if (target == null)
        {
            target = hunter.FindBoidInVision(lookForDead: false);

            if (target != null)
            {
                hunter.SetTargetBoid(target);
            }
            else
            {
                hunter.FSM.ChangeState(hunter.PatrolState);
                return false;
            }
        }

        float distanceToTarget = Vector3.Distance(hunter.transform.position, target.transform.position);

        if (distanceToTarget > hunter.VisionRadius)
        {
            hunter.SetTargetBoid(null);
            hunter.FSM.ChangeState(hunter.PatrolState);
            return false;
        }

        return true;
    }

    private bool TrySwitchToGatherOrPatrol()
    {
        Boid deadBoidInVision = hunter.FindBoidInVision(lookForDead: true);

        if (deadBoidInVision != null && deadBoidInVision.IsDead)
        {
            hunter.SetTargetBoid(deadBoidInVision);
            hunter.FSM.ChangeState(hunter.GatherState);
            return true;
        }

        Boid nextLiveBoid = hunter.FindBoidInVision(lookForDead: false);

        if (nextLiveBoid != null && !nextLiveBoid.IsDead)
        {
            hunter.SetTargetBoid(nextLiveBoid);
            return false;
        }

        hunter.SetTargetBoid(null);
        hunter.FSM.ChangeState(hunter.PatrolState);
        return true;
    }

    private void ProcessCombatAndAttack()
    {
        Boid target = hunter.GetTargetBoid();

        if (target == null) return;

        float distanceToTarget = Vector3.Distance(hunter.transform.position, target.transform.position);

        bool isMelee = distanceToTarget <= hunter.MeleeAttackRadius;
        Color lineColor = isMelee ? Color.magenta : Color.red;
        Debug.DrawLine(hunter.transform.position, target.transform.position, lineColor, 0.1f);

        if (distanceToTarget <= hunter.RangeAttackRadius && hunter.TimerAttackCooldown <= 0f)
        {
            int damage = isMelee ? 2 : 1;

            if (isMelee)
            {
                Debug.Log("<color=magenta>[MELEE ATTACK]</color> Daño: 2 | Distancia: " + distanceToTarget.ToString("F2"));
            }
            else
            {
                Debug.Log("<color=red>[RANGED ATTACK]</color> Daño: 1 | Distancia: " + distanceToTarget.ToString("F2"));
            }

            hunter.PlayAttackSound(isMelee);

            target.ReceiveDamage(damage);
            hunter.ResetAttackCooldown();
        }
    }

    private void ApplyMovement()
    {
        Boid target = hunter.GetTargetBoid();

        if (target != null)
        {
            Vector3 pursuitForce = hunter.Pursuit(target);
            hunter.AddForce(pursuitForce);
        }
    }

    public void Exit() { }
}