using UnityEngine;

public class BoidFlockingState : IState
{
    private readonly Boid _boid;

    public BoidFlockingState(Boid boid) => _boid = boid;

    public void Enter() => _boid.SetColorFeedback(Color.green);

    public void Update()
    {
        if (CheckIfDead()) return;

        if (CheckThreatAndEvade()) return;

        if (CheckFoodAndSeek()) return;

        ApplyFlockingBehavior();
    }

    private bool CheckIfDead()
    {
        if (_boid.IsDead)
        {
            _boid.FSM.ChangeState(_boid.DeadState);
            return true;
        }
        return false;
    }

    private bool CheckThreatAndEvade()
    {
        // Prioridad máxima defensiva: si el cazador entra en rango, salto inmediatamente a evasión.
        if (_boid.DetectHunterInRange() != null)
        {
            _boid.FSM.ChangeState(_boid.EvadeState);
            return true;
        }
        return false;
    }

    private bool CheckFoodAndSeek()
    {
        // Prioridad secundaria: búsqueda y consumo de recursos (frutas) en el entorno.
        Fruit targetFruit = _boid.FindNearestFruit();
        if (targetFruit != null)
        {
            float distToFruit = Vector3.Distance(_boid.transform.position, targetFruit.transform.position);
            if (distToFruit <= targetFruit.ConsumeRadius)
                _boid.FSM.ChangeState(_boid.ConsumeState);
            else
                _boid.FSM.ChangeState(_boid.SeekObjectState);
            return true;
        }
        return false;
    }

    private void ApplyFlockingBehavior()
    {
        // Cálculo ponderado de las reglas de floco (Separación + Alineación + Cohesión).
        Vector3 flockingForce = _boid.CalculateFlocking();
        _boid.AddForce(flockingForce);
    }

    public void Exit() { }
}