using UnityEngine;
using System.Collections.Generic;

public class HunterSearchState : IState
{
    private readonly HunterAI _hunter;
    private List<Node> _path;
    private bool _reachedCoto = false;

    public HunterSearchState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.green);
        _reachedCoto = false;

        if (_hunter.CotoCenter != null)
        {
            _path = GraphManager.Instance.FindPath(_hunter.transform.position, _hunter.CotoCenter.position);
            _hunter.FollowPath(_path);
        }
    }

    public void Update()
    {
        // 1. Chequear Boid muerto para recolectar (Gather)
        Boid deadBoid = _hunter.FindBoidInVision(lookForDead: true);
        if (deadBoid != null)
        {
            _hunter.SetTargetBoid(deadBoid);
            _hunter.FSM.ChangeState(_hunter.GatherState);
            return;
        }

        // 2. Chequear Boid vivo para atacar (Attack)
        if (_hunter.TimerAttackCooldown <= 0)
        {
            Boid aliveBoid = _hunter.FindBoidInVision(lookForDead: false);
            if (aliveBoid != null)
            {
                _hunter.SetTargetBoid(aliveBoid);
                _hunter.FSM.ChangeState(_hunter.AttackState);
                return;
            }
        }

        // 3. Navegación hacia el Coto o patrulla interna
        if (_path != null && _path.Count > 0)
        {
            _hunter.ExecutePathMovement();
            if (Vector3.Distance(_hunter.transform.position, _hunter.CotoCenter.position) < 2f)
            {
                _reachedCoto = true;
            }
        }
        else if (_reachedCoto && _hunter.Waypoints != null && _hunter.Waypoints.Length > 0)
        {
            // Movimiento aleatorio por nodos dentro del Coto
            Transform targetWp = _hunter.Waypoints[Random.Range(0, _hunter.Waypoints.Length)];
            _path = GraphManager.Instance.FindPath(_hunter.transform.position, targetWp.position);
            _hunter.FollowPath(_path);
        }
    }

    public void Exit() { }
}