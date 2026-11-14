using UnityEngine;

public class BoidDeadState : IState
{
    private readonly Boid _boid;

    public BoidDeadState(Boid boid) => _boid = boid;

    public void Enter()
    {
        _boid.SetColorFeedback(Color.black);
        _boid.ResetVelocity();
    }

    public void Update()
    {
        // Estado estático de espera hasta que el cazador complete la recolección y ordene el respawn.
        if (_boid.ShouldRespawn)
        {
            _boid.FSM.ChangeState(_boid.RespawnState);
            return;
        }

        _boid.ResetVelocity();
    }

    public void Exit() { }
}