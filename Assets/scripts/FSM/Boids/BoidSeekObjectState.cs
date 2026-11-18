
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

        float distance = GetHorizontalDistance(targetFruit);

        float stoppingDistance = GetStoppingDistance(targetFruit);

        if (distance <= stoppingDistance)
        {
            boid.ResetVelocity();
            boid.FSM.ChangeState(boid.ConsumeState);
            return;
        }

        ApplyApproachForces(targetFruit, stoppingDistance);
    }

    private float GetStoppingDistance(Fruit fruit)
    {
        return fruit.ConsumeRadius +
               boid.HorizontalCollisionRadius +
               fruit.HorizontalCollisionRadius;
    }

    private float GetHorizontalDistance(Fruit fruit)
    {
        Vector3 difference =
            boid.transform.position - fruit.transform.position;

        difference.y = 0f;

        return difference.magnitude;
    }

    private void ApplyApproachForces(
        Fruit targetFruit,
        float stoppingDistance)
    {
        Vector3 targetPosition = targetFruit.transform.position;

        Vector3 direction = targetPosition - boid.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= boid.DirectionEpsilon)
            return;

        Vector3 safeTargetPosition =
            targetPosition - direction.normalized * stoppingDistance;

        Vector3 arriveForce = boid.Arrive(
            safeTargetPosition,
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
