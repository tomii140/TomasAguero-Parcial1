using UnityEngine;

public class BoidFlockingState : IState
{
    private readonly Boid _boid;

    public BoidFlockingState(Boid boid) => _boid = boid;

    public void Enter() => _boid.SetColorFeedback(Color.green);

    public void Update()
    {
        Vector3 flockingForce = _boid.CalculateFlocking();
        _boid.AddForce(flockingForce);
    }

    public void Exit() { }
}