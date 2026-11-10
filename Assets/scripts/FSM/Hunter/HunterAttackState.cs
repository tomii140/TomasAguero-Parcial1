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
        hunter.SetColorFeedback(Color.red); // Color para Ataque/Caza
    }

    public void Update()
    {
        Boid target = hunter.GetTargetBoid();

        // Si la presa desaparece o muere, busca otro vivo o vuelve a Patrol
        if (target == null || target.isDead)
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

        // Si la presa sale del radio de visión, vuelve a Patrol sin reiniciar TBA
        if (distanceToTarget > hunter.VisionRadius)
        {
            hunter.SetTargetBoid(null);
            hunter.FSM.ChangeState(hunter.SearchState);
            return;
        }

        // Evaluación de radios de ataque (Melee vs Range)
        if (distanceToTarget <= hunter.MeleeAttackRadius || distanceToTarget <= hunter.RangeAttackRadius)
        {
            if (hunter.TimerAttackCooldown <= 0)
            {
                // Aplica daño (1 golpe a distancia, 2 cuerpo a cuerpo según requerimiento)
                int damage = (distanceToTarget <= hunter.MeleeAttackRadius) ? 2 : 1;
                target.ReceiveDamage(damage);

                hunter.ResetAttackCooldown();
                hunter.FSM.ChangeState(hunter.SearchState); // Sale del estado Attack tras un ataque exitoso
                return;
            }
        }

        // Persecución mediante Pursuit si está fuera de rango
        Vector3 pursuitForce = hunter.Pursuit(target);
        hunter.AddForce(pursuitForce);
    }

    public void Exit() { }
}