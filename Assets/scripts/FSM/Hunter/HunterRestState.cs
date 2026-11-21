using UnityEngine;

public class HunterRestState : IState
{
    private readonly HunterAI hunter;

    public HunterRestState(HunterAI hunter) => this.hunter = hunter;

    public void Enter()
    {
        hunter.ResetVelocity();
        hunter.PlaySound(hunter.RestSound);
    }

    public void Update()
    {
        RecoverEnergyContinuously();
    }

    private void RecoverEnergyContinuously()
    {
        // Regeneración de energía progresiva hasta el tope máximo para reanudar tareas.
        hunter.ConsumeOrRecoverEnergy(hunter.EnergyRecoveryRate * Time.deltaTime);

        if (hunter.CurrentEnergy >= hunter.MaxEnergy)
        {
            hunter.FSM.ChangeState(hunter.PatrolState);
        }
    }

    public void Exit() { }
}