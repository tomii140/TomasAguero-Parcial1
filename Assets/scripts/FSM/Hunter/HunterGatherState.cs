using UnityEngine;

public class HunterGatherState : IState
{
    private readonly HunterAI _hunter;
    private Boid _targetBoid;
    private float _gatherTimer;

    public HunterGatherState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.blue); // Color para Recolección
        _targetBoid = _hunter.GetTargetBoid();

        if (_targetBoid == null || !_targetBoid.isDead)
        {
            _hunter.FSM.ChangeState(_hunter.SearchState);
            return;
        }

        _gatherTimer = 0f;
        _hunter.ResetVelocity();
    }

    public void Update()
    {
        if (_targetBoid == null)
        {
            _hunter.SetTargetBoid(null);
            _hunter.FSM.ChangeState(_hunter.SearchState);
            return;
        }

        float dist = Vector3.Distance(_hunter.transform.position, _targetBoid.transform.position);

        if (dist > _hunter.CaptureRadius)
        {
            Vector3 force = _hunter.Seek(_targetBoid.transform.position);
            _hunter.AddForce(force);
            _gatherTimer = 0f;
        }
        else
        {
            _hunter.ResetVelocity();
            _gatherTimer += Time.deltaTime;

            if (_gatherTimer >= _hunter.GatheringTime)
            {
                _hunter.NotifyBoidCollected(_targetBoid);
                _hunter.SetTargetBoid(null);
                _hunter.FSM.ChangeState(_hunter.SearchState);
            }
        }
    }

    public void Exit()
    {
        _gatherTimer = 0f;
    }
}