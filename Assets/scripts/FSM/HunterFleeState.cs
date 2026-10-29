using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class HunterFleeState : IState
{
    private readonly HunterAI _hunter;
    private List<Node> _fleePath;
    private bool _isInSafeZone = false;

    public HunterFleeState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.magenta);
        _isInSafeZone = false;

        Node nearestSafe = _hunter.GetNearestSafeZoneNode();
        if (nearestSafe != null)
        {
            _fleePath = GraphManager.Instance.FindPath(_hunter.transform.position, nearestSafe.transform.position);
            _hunter.FollowPath(_fleePath);
        }
    }

    public void Update()
    {
        if (_isInSafeZone) return;

        if (_fleePath != null)
        {
            _hunter.ExecutePathMovement();
        }

        Node currentNode = GraphManager.Instance.GetNearestNode(_hunter.transform.position);
        if (currentNode != null && currentNode.Zone == ZoneType.SafeZone)
        {
            _isInSafeZone = true;
            _hunter.StartCoroutine(SafeZoneWaitRoutine());
        }
    }

    private IEnumerator SafeZoneWaitRoutine()
    {
        _hunter.ResetVelocity();
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
        _isInSafeZone = false;
    }
}