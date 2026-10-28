using UnityEngine;

public class HunterGatherState : IState
{
    private readonly HunterAI _hunter;

    public HunterGatherState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.blue);
    }

    public void Update()
    {
        // Estado pasivo: La Corrutina en HunterAI gestiona la espera y la transición posterior
    }

    public void Exit() { }
}