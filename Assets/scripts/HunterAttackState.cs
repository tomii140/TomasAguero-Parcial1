using UnityEngine;

public class HunterAttackState : IState
{
    private readonly HunterAI hunter;

    public HunterAttackState(HunterAI hunter) => this.hunter = hunter;

    public void Enter() => hunter.SetColorFeedback(Color.red);

    public void Update()
    {
        Boid target = hunter.GetTargetBoid();

        // 1. Validar si el objetivo existe y sigue vivo
        if (target == null || target.isDead)
        {
            hunter.SetTargetBoid(null);
            hunter.FSM.ChangeState(hunter.PatrolState);
            return;
        }

        float distanceToBoid = Vector2.Distance(hunter.transform.position, target.transform.position);

        // 2. Pérdida de visión
        if (distanceToBoid > hunter.VisionRadius)
        {
            hunter.SetTargetBoid(null);
            hunter.FSM.ChangeState(hunter.PatrolState);
            return;
        }

        // 3. Rango Melee: Aplicar daño y pasar al estado de Recolección (sin iniciar corrutina prematura)
        if (distanceToBoid <= hunter.MeleeAttackRadius)
        {
            target.Die();
            hunter.ResetAttackCooldown();
            hunter.FSM.ChangeState(hunter.GatherState);
            return;
        }

        // 4. Persecución directa a velocidad máxima (Seek para acortar distancia)
        hunter.AddForce(hunter.Seek(target.transform.position));
    }

    public void Exit() { }
}