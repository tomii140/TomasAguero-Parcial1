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

        if (_targetBoid == null)
        {
            _hunter.FSM.ChangeState(_hunter.SearchState);
            return;
        }

        // Asegura que el boid quede congelado inmediatamente
        _targetBoid.isDead = true;
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

        // Si la distancia es mayor al radio de captura, se desplaza hacia el objetivo
        if (dist > _hunter.CaptureRadius)
        {
            Vector3 force = _hunter.Arrive(_targetBoid.transform.position, 2f);
            _hunter.AddForce(force);
            _gatherTimer = 0f;
        }
        else
        {
            // Detención total y ejecución del temporizador de recolección
            _hunter.ResetVelocity();
            _gatherTimer += Time.deltaTime;

            if (_gatherTimer >= _hunter.GatheringTime)
            {
                Object.Destroy(_targetBoid.gameObject);
                _hunter.AddGather();
                _hunter.SetTargetBoid(null);

                if (_hunter.CurrentGathers >= _hunter.RequiredGathers)
                {
                    _hunter.FSM.ChangeState(_hunter.ExitState);
                }
                else
                {
                    _hunter.FSM.ChangeState(_hunter.SearchState);
                }
            }
        }
    }

    public void Exit()
    {
        _gatherTimer = 0f;
    }
}