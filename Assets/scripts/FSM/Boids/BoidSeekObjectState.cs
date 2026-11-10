using UnityEngine;

public class BoidSeekObjectState : IState
{
    private readonly Boid _boid;

    public BoidSeekObjectState(Boid boid) => _boid = boid;

    public void Enter() => _boid.SetColorFeedback(Color.yellow);

    public void Update()
    {
        Fruit targetFruit = _boid.FindNearestFruit();
        if (targetFruit != null)
        {
            Vector3 arriveForce = _boid.Arrive(targetFruit.transform.position, 3.0f) * _boid.FruitWeight;
            Vector3 separationForce = _boid.GetSeparationForce() * _boid.SeparationWeight;
            _boid.AddForce(arriveForce + separationForce);
        }
    }

    public void Exit() { }
}