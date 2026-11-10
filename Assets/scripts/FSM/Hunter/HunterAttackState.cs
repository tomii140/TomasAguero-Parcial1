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
        // 1. Consumo continuo de energía en Hunt
        hunter.ConsumeOrRecoverEnergy(hunter.EnergyConsumptionRate * Time.deltaTime);

        // Salida de Hunt por energía agotada -> Rest
        if (hunter.CurrentEnergy <= 0f)
        {
            hunter.FSM.ChangeState(hunter.RestState);
            return;
        }

        Boid target = hunter.GetTargetBoid();

        // 2. Si la presa actual murió, evaluamos las reglas del parcial:
        if (target != null && target.isDead)
        {
            // ¿Hay un boid muerto en el radio de visión para recolectar? -> Transición a Gather
            Boid deadBoidInVision = hunter.FindBoidInVision(lookForDead: true);
            if (deadBoidInVision != null && deadBoidInVision.isDead)
            {
                hunter.SetTargetBoid(deadBoidInVision);
                hunter.FSM.ChangeState(hunter.GatherState);
                return;
            }

            // Si no hay cadáver cerca, buscamos otro boid vivo para seguir en Hunt (según consigna)
            Boid nextLiveBoid = hunter.FindBoidInVision(lookForDead: false);
            if (nextLiveBoid != null && !nextLiveBoid.isDead)
            {
                hunter.SetTargetBoid(nextLiveBoid);
                target = nextLiveBoid;
            }
            else
            {
                // Si no hay más vivos ni cadáveres cercanos, volvemos a Patrol (SearchState)
                hunter.SetTargetBoid(null);
                hunter.FSM.ChangeState(hunter.SearchState);
                return;
            }
        }

        // Si no hay objetivo inicial, intentamos capturar uno vivo
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

        // Salida de Hunt si la presa sale de los límites / visión
        if (distanceToTarget > hunter.VisionRadius)
        {
            hunter.SetTargetBoid(null);
            hunter.FSM.ChangeState(hunter.SearchState);
            return;
        }

        // Ejecución de ataques (Melee vs Range) respetando el TBA
        if (distanceToTarget <= hunter.MeleeAttackRadius || distanceToTarget <= hunter.RangeAttackRadius)
        {
            if (hunter.TimerAttackCooldown <= 0)
            {
                int damage = (distanceToTarget <= hunter.MeleeAttackRadius) ? 2 : 1;
                target.ReceiveDamage(damage);
                hunter.ResetAttackCooldown();
            }
        }

        // Persecución mediante Pursuit
        Vector3 pursuitForce = hunter.Pursuit(target);
        hunter.AddForce(pursuitForce);
    }

    public void Exit() { }
}