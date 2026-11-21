
using UnityEngine;

public class BoidDeadState : IState
{
    private readonly Boid boid;

    public BoidDeadState(Boid boid)
    {
        this.boid = boid;
    }

    public void Enter()
    {
        boid.SetColorFeedback(Color.black);
        boid.ResetVelocity();
    }

    public void Update()
    {
        if (boid.ShouldRespawn)
        {
            boid.FSM.ChangeState(boid.RespawnState);
        }
    }

    public void Exit() { }
}
