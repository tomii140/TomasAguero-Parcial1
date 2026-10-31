using UnityEngine;

public class WolfAttackState : IWolfState
{
    private WolfAI wolf;

    public WolfAttackState(WolfAI wolf)
    {
        this.wolf = wolf;
    }

    public void Enter()
    {
        wolf.StopAgent();
        if (wolf.Animator != null) wolf.Animator.SetTrigger("Attack");

        HunterAI hunter = GameManager.Instance.Hunter;
        if (hunter != null && !(hunter.FSM.CurrentState is HunterDeadState))
        {
            hunter.FSM.ChangeState(hunter.DeadState);
        }
    }

    public void UpdateState() { }

    public void Exit() { }
}