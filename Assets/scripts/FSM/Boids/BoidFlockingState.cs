using UnityEngine;

public class BoidFlockingState : IState
{
    private readonly Boid _boid;

    public BoidFlockingState(Boid boid) => _boid = boid;

    public void Enter() => _boid.SetColorFeedback(Color.green);

    public void Update()
    {
        
        if (_boid.DetectHunterInRange() != null)
        {
            _boid.FSM.ChangeState(_boid.EvadeState);
            return;
        }

        Fruit targetFruit = _boid.FindNearestFruit();
        if (targetFruit != null)
        {
            float distToFruit = Vector3.Distance(_boid.transform.position, targetFruit.transform.position);
            if (distToFruit <= targetFruit.ConsumeRadius)
                _boid.FSM.ChangeState(_boid.ConsumeState);
            else
                _boid.FSM.ChangeState(_boid.SeekObjectState);
            return;
        }

        Vector3 flockingForce = _boid.CalculateFlocking();
        _boid.AddForce(flockingForce);
    }
    public void Exit() { }
}