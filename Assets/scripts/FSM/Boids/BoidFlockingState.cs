
using UnityEngine;

public class BoidFlockingState : IState
{
    private readonly Boid boid;

    public BoidFlockingState(Boid boid)
    {
        this.boid = boid;
    }

    public void Enter()
    {
        boid.SetColorFeedback(Color.green);
    }

    public void Update()
    {
        if (CheckIfDead())
            return;

        if (CheckThreatAndEvade())
            return;

        if (CheckFoodAndSeek())
            return;

        ApplyFlockingBehavior();
    }

    private bool CheckIfDead()
    {
        if (!boid.IsDead)
            return false;

        boid.FSM.ChangeState(boid.DeadState);
        return true;
    }

    private bool CheckThreatAndEvade()
    {
        if (boid.DetectHunterInRange() == null)
            return false;

        boid.FSM.ChangeState(boid.EvadeState);
        return true;
    }

    private bool CheckFoodAndSeek()
    {
        Fruit targetFruit = boid.FindNearestFruit();

        if (targetFruit == null)
            return false;

        float distance = Vector3.Distance(
            boid.transform.position,
            targetFruit.transform.position
        );

        if (distance <= targetFruit.ConsumeRadius)
            boid.FSM.ChangeState(boid.ConsumeState);
        else
            boid.FSM.ChangeState(boid.SeekObjectState);

        return true;
    }

    private void ApplyFlockingBehavior()
    {
        Vector3 flockingForce = boid.CalculateFlocking();

        Vector3 fruitAvoidanceForce =
            boid.GetFruitAvoidanceForce() * boid.FruitAvoidanceWeight;

        boid.AddForce(flockingForce + fruitAvoidanceForce);
    }

    public void Exit() { }
}
