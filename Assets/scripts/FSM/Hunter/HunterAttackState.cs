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
        hunter.SetColorFeedback(Color.red);
    }

    public void Update()
    {
        hunter.ConsumeOrRecoverEnergy(hunter.EnergyConsumptionRate * Time.deltaTime);

        if (hunter.CurrentEnergy <= 0f)
        {
            hunter.FSM.ChangeState(hunter.RestState);
            return;
        }

        Boid target = hunter.GetTargetBoid();

        if (target != null && target.IsDead)
        {
            Boid deadBoidInVision = hunter.FindBoidInVision(lookForDead: true);
            if (deadBoidInVision != null && deadBoidInVision.IsDead)
            {
                hunter.SetTargetBoid(deadBoidInVision);
                hunter.FSM.ChangeState(hunter.GatherState);
                return;
            }

            Boid nextLiveBoid = hunter.FindBoidInVision(lookForDead: false);
            if (nextLiveBoid != null && !nextLiveBoid.IsDead)
            {
                hunter.SetTargetBoid(nextLiveBoid);
                target = nextLiveBoid;
            }
            else
            {
                hunter.SetTargetBoid(null);
                hunter.FSM.ChangeState(hunter.SearchState);
                return;
            }
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
                hunter.FSM.ChangeState(hunter.SearchState);
                return;
            }
        }

        float distanceToTarget = Vector3.Distance(hunter.transform.position, target.transform.position);

        if (distanceToTarget > hunter.VisionRadius)
        {
            hunter.SetTargetBoid(null);
            hunter.FSM.ChangeState(hunter.SearchState);
            return;
        }

        if (distanceToTarget <= hunter.MeleeAttackRadius || distanceToTarget <= hunter.RangeAttackRadius)
        {
            if (hunter.TimerAttackCooldown <= 0)
            {
                int damage = (distanceToTarget <= hunter.MeleeAttackRadius) ? 2 : 1;
                target.ReceiveDamage(damage);
                hunter.ResetAttackCooldown();
            }
        }

        Vector3 pursuitForce = hunter.Pursuit(target);
        hunter.AddForce(pursuitForce);
    }

    public void Exit() { }
}