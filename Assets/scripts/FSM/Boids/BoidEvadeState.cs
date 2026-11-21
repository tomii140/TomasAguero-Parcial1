
using UnityEngine;

public class BoidEvadeState : IState
{
    private readonly Boid boid;

    public BoidEvadeState(Boid boid)
    {
        this.boid = boid;
    }

    public void Enter()
    {
        boid.SetColorFeedback(Color.red);
    }

    public void Update()
    {
        if (boid.IsDead)
        {
            boid.FSM.ChangeState(boid.DeadState);
            return;
        }

        HunterAI hunter = boid.DetectHunterInRange();

        if (hunter == null)
        {
            EvaluatePostEvacuationTransition();
            return;
        }

        ExecuteEvadeManeuver(hunter);
    }

    private void EvaluatePostEvacuationTransition()
    {
        Fruit targetFruit = boid.FindNearestFruit();

        if (targetFruit != null)
        {
            float distance = Vector3.Distance(
                boid.transform.position,
                targetFruit.transform.position
            );

            boid.FSM.ChangeState(
                distance <= targetFruit.ConsumeRadius
                    ? boid.ConsumeState
                    : boid.SeekObjectState
            );
        }
        else
        {
            boid.FSM.ChangeState(boid.FlockingState);
        }
    }

    private void ExecuteEvadeManeuver(HunterAI hunter)
    {
        Vector3 evadeForce = boid.Evade(hunter);

        Vector3 separationForce =
            boid.GetSeparationForce() * boid.SeparationWeight;

        Vector3 fruitAvoidanceForce =
            boid.GetFruitAvoidanceForce() * boid.FruitAvoidanceWeight;

        boid.AddForce(
            evadeForce +
            separationForce +
            fruitAvoidanceForce
        );
    }

    public void Exit() { }
}
