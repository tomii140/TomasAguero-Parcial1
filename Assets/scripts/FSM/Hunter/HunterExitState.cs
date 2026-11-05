using UnityEngine;
using System.Collections.Generic;

public class HunterExitState : IState
{
    private readonly HunterAI _hunter;
    private List<Node> _exitPath;
    private int _currentIndex = 0;
    private bool _hasReachedExit = false;

    public HunterExitState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(new Color(1f, 0.5f, 0f));
        _hunter.ResetVelocity();
        _hasReachedExit = false;
        _exitPath = null;
        _currentIndex = 0;

        if (_hunter.ExitPoint != null && GraphManager.Instance != null)
        {
            Node startNode = GraphManager.Instance.GetNearestNode(_hunter.transform.position);
            Node targetNode = GraphManager.Instance.GetNearestNode(_hunter.ExitPoint.position);

            if (startNode != null && targetNode != null)
            {
                _exitPath = GraphManager.Instance.FindPath(startNode.transform.position, targetNode.transform.position);
            }
        }
    }

    public void Update()
    {
        if (_hasReachedExit || _hunter.ExitPoint == null) return;

        // Valida la distancia física directa a la salida
        float distToExit = Vector3.Distance(_hunter.transform.position, _hunter.ExitPoint.position);
        if (distToExit <= 2.0f)
        {
            _hasReachedExit = true;
            CompleteExitRoutine();
            return;
        }

        if (_exitPath != null && _exitPath.Count > 0)
        {
            if (_currentIndex < _exitPath.Count)
            {
                Vector3 targetNodePos = _exitPath[_currentIndex].transform.position;
                Vector3 force = _hunter.Arrive(targetNodePos, 2.0f);
                _hunter.AddForce(force);

                Vector3 hunterXZ = new Vector3(_hunter.transform.position.x, 0f, _hunter.transform.position.z);
                Vector3 nodeXZ = new Vector3(targetNodePos.x, 0f, targetNodePos.z);

                if (Vector3.Distance(hunterXZ, nodeXZ) < 2.0f)
                {
                    _currentIndex++;
                }
                return;
            }
        }

        // Tramo final hacia el objetivo si se agotaron los nodos del path
        Vector3 finalForce = _hunter.Arrive(_hunter.ExitPoint.position, 3.0f);
        _hunter.AddForce(finalForce);
    }

    private void CompleteExitRoutine()
    {
        _hunter.ResetGathers();
        _hunter.ResetVelocity();
        _exitPath = null;
        _currentIndex = 0;

        if (_hunter.InitialSpawnPoint != null)
        {
            _hunter.transform.position = _hunter.InitialSpawnPoint.position;
        }

        _hunter.FSM.ChangeState(_hunter.SearchState);
    }

    public void Exit()
    {
        _exitPath = null;
        _currentIndex = 0;
        _hasReachedExit = false;
    }
}