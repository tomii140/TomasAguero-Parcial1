
using UnityEngine;

public class HunterGatherState : IState
{
    private readonly HunterAI _hunter;
    private Boid _targetBoid;
    private float _gatherTimer;

    public HunterGatherState(HunterAI hunter)
    {
        _hunter = hunter;
    }

    public void Enter()
    {
        _targetBoid = _hunter.GetTargetBoid();

        if (_targetBoid == null || !_targetBoid.IsDead || _targetBoid.IsRespawning)
        {
            ReturnToPatrol();
            return;
        }

        _gatherTimer = 0f;
        _hunter.ResetVelocity();
        _hunter.HideGatheringProgress();

        Debug.Log($"[CAZADOR] Comenzó a recolectar a {_targetBoid.name}.");
    }

    public void Update()
    {
        if (!IsTargetValid())
            return;

        ProcessGatheringBehavior();
    }

    private bool IsTargetValid()
    {
        if (_targetBoid == null || !_targetBoid.IsDead || _targetBoid.IsRespawning)
        {
            ReturnToPatrol();
            return false;
        }

        return true;
    }

    private void ProcessGatheringBehavior()
    {
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
            _hunter.HideGatheringProgress();
        }
        else
        {
            _hunter.ResetVelocity();

            _gatherTimer += Time.deltaTime;

            float progress = _hunter.GatheringTime > 0f
                ? _gatherTimer / _hunter.GatheringTime
                : 1f;

            _hunter.ShowGatheringProgress(progress);

            if (_gatherTimer >= _hunter.GatheringTime)
            {
                Debug.Log($"[CAZADOR] Terminó de recolectar a {_targetBoid.name}.");

                _hunter.HideGatheringProgress();

                _hunter.NotifyBoidCollected(_targetBoid);
                ReturnToPatrol();
            }
        }
    }

    private void ReturnToPatrol()
    {
        _hunter.HideGatheringProgress();
        _hunter.SetTargetBoid(null);
        _hunter.FSM.ChangeState(_hunter.PatrolState);
    }

    public void Exit()
    {
        _gatherTimer = 0f;
        _hunter.HideGatheringProgress();
    }
}
