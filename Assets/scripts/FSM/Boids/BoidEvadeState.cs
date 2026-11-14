using UnityEngine;

public class BoidEvadeState : IState
{
    private readonly Boid _boid;

    public BoidEvadeState(Boid boid) => _boid = boid;

    public void Enter() => _boid.SetColorFeedback(Color.red);

    public void Update()
    {
        HunterAI hunter = _boid.DetectHunterInRange();

        if (hunter == null)
        {
            Fruit targetFruit = _boid.FindNearestFruit();
            if (targetFruit != null)
            {
                float distToFruit = Vector3.Distance(_boid.transform.position, targetFruit.transform.position);
                _boid.FSM.ChangeState(distToFruit <= targetFruit.ConsumeRadius ? _boid.ConsumeState : _boid.SeekObjectState);
            }
            else
            {
                _boid.FSM.ChangeState(_boid.FlockingState);
            }
            return;
        }

        Vector3 evadeForce = _boid.Evade(hunter);
        Vector3 separationForce = _boid.GetSeparationForce() * _boid.SeparationWeight;
        _boid.AddForce(evadeForce + separationForce);
    }

    public void Exit() { }
}