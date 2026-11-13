using UnityEngine;

public class BoidConsumeState : IState
{
    private readonly Boid _boid;

    public BoidConsumeState(Boid boid) => _boid = boid;

    public void Enter()
    {
        _boid.SetColorFeedback(new Color(1f, 0.5f, 0f));
        _boid.ResetVelocity();
    }

    public void Update()
    {
        ConsumeTargetFruit();
    }

    private void ConsumeTargetFruit()
    {
        Fruit targetFruit = _boid.FindNearestFruit();
        if (targetFruit != null)
        {
            targetFruit.Consume(targetFruit.DamagePerSecond * Time.deltaTime);
        }
    }

    public void Exit() { }
}