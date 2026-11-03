using UnityEngine;

public class WolfAttackState : IWolfState
{
    private WolfAI wolf;
    private float attackTimer = 0f;
    private float attackDuration = 1.5f; // Duración del ataque antes de liberar al agente para que no quede estático

    public WolfAttackState(WolfAI wolf)
    {
        this.wolf = wolf;
    }

    public void Enter()
    {
        attackTimer = 0f;
        wolf.StopAgent();

        if (wolf.Animator != null)
        {
            wolf.Animator.SetFloat("Speed", 0f);
            wolf.Animator.SetTrigger("Attack");
        }

        HunterAI hunter = GameManager.Instance.Hunter;
        if (hunter != null && !(hunter.FSM.CurrentState is HunterDeadState))
        {
            hunter.FSM.ChangeState(hunter.DeadState);
        }

        Debug.Log($"[{wolf.gameObject.name}] ¡Atacando al cazador!");
    }

    public void UpdateState()
    {
        attackTimer += Time.deltaTime;

        // Evita que el lobo quede estático tras el ataque: si pasa el tiempo, retoma la persecución o patrulla
        if (attackTimer >= attackDuration)
        {
            HunterAI hunter = GameManager.Instance.Hunter;
            if (hunter == null || hunter.FSM.CurrentState is HunterDeadState)
            {
                wolf.ChangeState(wolf.PatrolState);
            }
            else
            {
                wolf.ChangeState(wolf.ChaseState);
            }
        }
    }

    public void Exit()
    {
        attackTimer = 0f;
    }
}