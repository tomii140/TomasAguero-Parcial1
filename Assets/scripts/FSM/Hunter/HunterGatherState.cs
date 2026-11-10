using UnityEngine;

public class HunterGatherState : IState
{
    private readonly HunterAI _hunter;
    private Boid _targetBoid;
    private float _gatherTimer;

    public HunterGatherState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.blue);
        _targetBoid = _hunter.GetTargetBoid();

        if (_targetBoid == null || !_targetBoid.isDead)
        {
            _hunter.SetTargetBoid(null);
            _hunter.FSM.ChangeState(_hunter.SearchState);
            return;
        }

        _gatherTimer = 0f;
        _hunter.ResetVelocity();
    }

    public void Update()
    {
        if (_targetBoid == null || !_targetBoid.isDead)
        {
            _hunter.SetTargetBoid(null);
            _hunter.FSM.ChangeState(_hunter.SearchState);
            return;
        }

        float distXZ = Vector3.Distance(
            new Vector3(_hunter.transform.position.x, 0f, _hunter.transform.position.z),
            new Vector3(_targetBoid.transform.position.x, 0f, _targetBoid.transform.position.z)
        );

        if (distXZ > _hunter.CaptureRadius)
        {
            Vector3 targetPos = _targetBoid.transform.position;
            targetPos.y = _hunter.transform.position.y;

            Vector3 force = _hunter.Seek(targetPos);
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