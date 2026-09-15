using UnityEngine;

public class HunterAttackState : IState
{
    private HunterAI hunter;

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

        // 1. Si no hay objetivo asignado o el objetivo ya murió, buscar el Boid más cercano
        if (target == null || target.isDead)
        {
            target = hunter.FindBoidInVision(lookForDead: false);
            if (target != null)
            {
                hunter.SetTargetBoid(target);
            }
            else
            {
                // Si no hay ningún Boid vivo en el rango de visión, volver a Patrulla
                hunter.FSM.ChangeState(hunter.PatrolState);
                return;
            }
        }

        float distanceToTarget = Vector2.Distance(hunter.transform.position, target.transform.position);

        // 2. Si se escapó más allá del radio de visión, perder el foco y patrullar
        if (distanceToTarget > hunter.VisionRadius)
        {
            hunter.SetTargetBoid(null);
            hunter.FSM.ChangeState(hunter.PatrolState);
            return;
        }

        // 3. Captura / Ataque Melee exitoso
        if (distanceToTarget <= hunter.MeleeAttackRadius)
        {
            target.isDead = true;             // Marca el Boid como muerto
            hunter.StartGatheringRoutine();   // Inicia la rutina de recolección de 2 segundos
            hunter.FSM.ChangeState(hunter.GatherState);
            return;
        }

        // 4. Persecución (Ajuste para evitar que orbite o gire en círculos)
        // Si está a menos del doble del radio Melee, usa Seek directo; de lo contrario usa Pursuit
        Vector2 attackForce = (distanceToTarget < hunter.MeleeAttackRadius * 2.5f) 
            ? hunter.Seek(target.transform.position) 
            : hunter.Pursuit(target);

        hunter.AddForce(attackForce);
    }

    public void Exit() { }
}