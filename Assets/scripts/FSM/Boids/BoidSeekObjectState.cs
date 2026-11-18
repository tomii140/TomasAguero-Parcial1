
using UnityEngine;

public class BoidSeekObjectState : IState
{
    private readonly Boid boid;

    public BoidSeekObjectState(Boid boid)
    {
        this.boid = boid;
    }

    public void Enter()
    {
        boid.SetColorFeedback(Color.yellow);
    }

    public void Update()
    {
        if (boid.IsDead)
        {
            boid.FSM.ChangeState(boid.DeadState);
            return;
        }

        if (boid.DetectHunterInRange() != null)
        {
            boid.FSM.ChangeState(boid.EvadeState);
            return;
        }

        Fruit targetFruit = boid.FindNearestFruit();

        if (targetFruit == null)
        {
            boid.FSM.ChangeState(boid.FlockingState);
            return;
        }

        float distance = Vector3.Distance(
            boid.transform.position,
            targetFruit.transform.position
        );

        if (distance <= targetFruit.ConsumeRadius)
        {
            boid.FSM.ChangeState(boid.ConsumeState);
            return;
        }

        ApplyApproachForces(targetFruit);
    }

    private void ApplyApproachForces(Fruit targetFruit)
    {
        Vector3 arriveForce = boid.Arrive(
            targetFruit.transform.position,
            boid.FruitArriveSlowingRadius
        ) * boid.FruitWeight;

        Vector3 separationForce =
            boid.GetSeparationForce() * boid.SeparationWeight;

        Vector3 fruitAvoidanceForce =
            boid.GetFruitAvoidanceForce(targetFruit) *
            boid.FruitAvoidanceWeight;

        boid.AddForce(
            arriveForce +
            separationForce +
            fruitAvoidanceForce
        );
    }

    public void Exit() { }
}
