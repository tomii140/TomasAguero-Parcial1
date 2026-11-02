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

        // Si es un mapa grande, el ataque real (matar al boid) solo debe suceder 
        // cuando esté lo suficientemente cerca para recolectarlo, o usa el rango configurado pero acércate.
        // Aquí exigimos que esté dentro del rango de ataque pero validamos la transición limpia:
        if (distanceToTarget <= hunter.MeleeAttackRadius || distanceToTarget <= hunter.RangeAttackRadius)
        {
            // Solo marca como muerto si ya está a una distancia donde puede iniciar la recogida, 
            // o si es ataque a distancia, disparamos pero nos acercamos a recoger.
            target.isDead = true;
            hunter.ResetAttackCooldown();
            hunter.SetTargetBoid(target);
            hunter.FSM.ChangeState(hunter.GatherState);
            return;
        }

        // Persecución para mapas grandes
        Vector3 attackForce = (distanceToTarget < hunter.MeleeAttackRadius * hunter.DirectSeekFactor)
            ? hunter.Seek(target.transform.position)
            : hunter.Pursuit(target);

        hunter.AddForce(attackForce);
    }

    public void Exit() { }
}