using UnityEngine;

public class HunterGatherState : IState
{
    private readonly HunterAI _hunter;
    private bool _routineStarted;

    public HunterGatherState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.blue);
        _routineStarted = false;
    }

    public void Update()
    {
        Boid target = _hunter.GetTargetBoid();

        if (target == null)
        {
            _hunter.FSM.ChangeState(_hunter.PatrolState);
            return;
        }

        float distanceToTarget = Vector2.Distance(_hunter.transform.position, target.transform.position);

        // Arrive para acercarse al Boid caído de forma natural
        _hunter.AddForce(_hunter.Arrive(target.transform.position, 2.0f));

        // Ejecutar rutina solo una vez alcanzada la posición de recolección
        if (!_routineStarted && distanceToTarget <= 0.8f)
        {
            _routineStarted = true;
            _hunter.StartGatheringRoutine();
        }
    }

    public void Exit() { }
}