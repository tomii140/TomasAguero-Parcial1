using UnityEngine;

public class HunterRestState : IState
{
    private readonly HunterAI _hunter;

    public HunterRestState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.ResetVelocity();
        _hunter.PlaySound(_hunter.RestSound);
    }

    public void Update()
    {
        RecoverEnergyContinuously();
    }

    private void RecoverEnergyContinuously()
    {
        // Regeneración de energía progresiva hasta el tope máximo para reanudar tareas.
        _hunter.ConsumeOrRecoverEnergy(_hunter.EnergyRecoveryRate * Time.deltaTime);

        if (_hunter.CurrentEnergy >= _hunter.MaxEnergy)
        {
            _hunter.FSM.ChangeState(_hunter.PatrolState);
        }
    }

    public void Exit() { }
}