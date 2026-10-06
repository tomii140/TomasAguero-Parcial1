using UnityEngine;

public class HunterGatherState : IState
{
    private readonly HunterAI _hunter;

    public HunterGatherState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.blue);
        //_hunter.StartGatheringRoutine(); . lo desactivamos por llamda redundante , porque ya se dispara en HunterAttackState.cs
    }

    public void Update()
    {
        // Estado pasivo: La Corrutina en HunterAI gestiona la espera de 2s y la transición posterior
    }

    public void Exit() { }
}