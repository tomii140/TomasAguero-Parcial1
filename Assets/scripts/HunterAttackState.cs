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
        Boid target = hunter.GetTargetBoid();

        if (target == null || target.isDead)
        {
            target = hunter.FindBoidInVision(lookForDead: false);
            if (target != null)
            {
                hunter.SetTargetBoid(target);
            }
            else
            {
                hunter.FSM.ChangeState(hunter.PatrolState);
                return;
            }
        }

        float distanceToTarget = Vector2.Distance(hunter.transform.position, target.transform.position);

        if (distanceToTarget > hunter.VisionRadius)
        {
            hunter.SetTargetBoid(null);
            hunter.FSM.ChangeState(hunter.PatrolState);
            return;
        }

        if (distanceToTarget <= hunter.MeleeAttackRadius)
        {
            target.isDead = true;
            hunter.ResetAttackCooldown();
            hunter.StartGatheringRoutine();
            hunter.FSM.ChangeState(hunter.GatherState);
            return;
        }

        Vector2 attackForce = (distanceToTarget < hunter.MeleeAttackRadius * hunter.DirectSeekFactor)
            ? hunter.Seek(target.transform.position)
            : hunter.Pursuit(target);

        hunter.AddForce(attackForce);
    }

    public void Exit() { }
}