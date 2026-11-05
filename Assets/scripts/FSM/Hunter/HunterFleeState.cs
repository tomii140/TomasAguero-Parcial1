using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class HunterFleeState : IState
{
    private readonly HunterAI _hunter;
    private List<Node> _fleePath;
    private int _currentIndex = 0;
    private bool _isInSafeZone = false;

    public HunterFleeState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.magenta);
        _isInSafeZone = false;
        _fleePath = null;
        _currentIndex = 0;

        Node nearestSafe = _hunter.GetNearestSafeZoneNode();
        if (nearestSafe != null && GraphManager.Instance != null)
        {
            _fleePath = GraphManager.Instance.FindPath(_hunter.transform.position, nearestSafe.transform.position);
        }
    }

    public void Update()
    {
        if (_isInSafeZone) return;

        // Ejecuta A* hacia la zona segura y valida llegada estricta al nodo final
        if (_fleePath != null && _fleePath.Count > 0)
        {
            Node targetNode = _fleePath[_fleePath.Count - 1];

            if (_currentIndex < _fleePath.Count)
            {
                Vector3 targetNodePos = _fleePath[_currentIndex].transform.position;
                Vector3 force = _hunter.Arrive(targetNodePos, 1.5f);
                _hunter.AddForce(force);

                Vector3 hunterXZ = new Vector3(_hunter.transform.position.x, 0f, _hunter.transform.position.z);
                Vector3 nodeXZ = new Vector3(targetNodePos.x, 0f, targetNodePos.z);

                if (Vector3.Distance(hunterXZ, nodeXZ) < 1.2f)
                {
                    _currentIndex++;
                }

                // Control exhaustivo para asegurar que entre al callejón sin chocar muros
                if (_currentIndex >= _fleePath.Count)
                {
                    float distToCenter = Vector3.Distance(hunterXZ, new Vector3(targetNode.transform.position.x, 0f, targetNode.transform.position.z));
                    if (distToCenter <= 1.5f)
                    {
                        _isInSafeZone = true;
                        _hunter.StartCoroutine(SafeZoneWaitRoutine());
                    }
                }
                return;
            }
        }
    }

    private IEnumerator SafeZoneWaitRoutine()
    {
        _hunter.ResetVelocity();
        _fleePath = null;
        _currentIndex = 0;

        // Espera obligatoria dentro de la zona segura configurada por rúbrica
        yield return new WaitForSeconds(_hunter.SafeZoneWaitTime);

        if (_hunter.CurrentGathers >= _hunter.RequiredGathers)
        {
            _hunter.FSM.ChangeState(_hunter.ExitState);
        }
        else
        {
            _hunter.FSM.ChangeState(_hunter.SearchState);
        }
    }

    public void Exit()
    {
        _fleePath = null;
        _currentIndex = 0;
        _isInSafeZone = false;
        if (_hunter != null)
        {
            _hunter.FollowPath(null, 0f);
        }
    }
}