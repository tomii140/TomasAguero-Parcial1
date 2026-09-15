using UnityEngine;

public class HunterPatrolState : IState
{
    private readonly HunterAI _hunter;
    private int _waypointIndex = 0;
    private bool _isReversing = false;

    public HunterPatrolState(HunterAI hunter) => _hunter = hunter;

    public void Enter() => _hunter.SetColorFeedback(Color.green);

    public void Update()
    {
        // Detectar únicamente Boid vivo para atacar si finalizó el cooldown (TBA)
        if (_hunter.TimerAttackCooldown <= 0)
        {
            Boid aliveBoid = _hunter.FindBoidInVision(false);
            if (aliveBoid != null)
            {
                _hunter.SetTargetBoid(aliveBoid);
                _hunter.FSM.ChangeState(_hunter.AttackState);
                return;
            }
        }

        // Patrullaje por Waypoints
        if (_hunter.Waypoints == null || _hunter.Waypoints.Length == 0) return;

        Vector2 destination = _hunter.Waypoints[_waypointIndex].position;
        _hunter.AddForce(_hunter.Seek(destination));

        if (Vector2.Distance(_hunter.transform.position, destination) < 0.8f)
        {
            if (!_isReversing)
            {
                if (_waypointIndex + 1 < _hunter.Waypoints.Length) _waypointIndex++;
                else { _isReversing = true; _waypointIndex--; }
            }
            else
            {
                if (_waypointIndex - 1 >= 0) _waypointIndex--;
                else { _isReversing = false; _waypointIndex++; }
            }
        }
    }

    public void Exit() { }
}