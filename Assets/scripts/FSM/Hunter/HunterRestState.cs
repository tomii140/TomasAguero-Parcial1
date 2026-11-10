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
        _hunter.SetColorFeedback(Color.black); // Feedback visual para Rest
        _hunter.ResetVelocity();
    }

    public void Update()
    {
        // Recarga energía únicamente en este estado a una velocidad configurable
        _hunter.ConsumeOrRecoverEnergy(-_hunter.EnergyRecoveryRate * Time.deltaTime);

        // Al recargar la energía hasta MaxEnergy, vuelve a Patrol (SearchState)
        if (_hunter.CurrentEnergy >= _hunter.MaxEnergy)
        {
            _hunter.FSM.ChangeState(_hunter.SearchState);
        }
    }

    public void Exit() { }
}