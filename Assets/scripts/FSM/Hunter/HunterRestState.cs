using UnityEngine;

public class HunterRestState : IState
{
    private readonly HunterAI _hunter;

    public HunterRestState(HunterAI hunter)
    {
        _hunter = hunter;
    }

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.black);
        _hunter.ResetVelocity();
    }

    public void Update()
    {
        _hunter.ConsumeOrRecoverEnergy(_hunter.EnergyRecoveryRate * Time.deltaTime);

        if (_hunter.CurrentEnergy >= _hunter.MaxEnergy)
        {
            _hunter.FSM.ChangeState(_hunter.PatrolState);
        }
    }

    public void Exit() { }
}