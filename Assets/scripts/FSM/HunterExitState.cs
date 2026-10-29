using UnityEngine;
using System.Collections.Generic;

public class HunterExitState : IState
{
    private readonly HunterAI _hunter;
    private List<Node> _exitPath;

    public HunterExitState(HunterAI hunter) => _hunter = hunter;

    public void Enter()
    {
        _hunter.SetColorFeedback(Color.blue);
        if (_hunter.ExitPoint != null)
        {
            _exitPath = GraphManager.Instance.FindPath(_hunter.transform.position, _hunter.ExitPoint.position);
            _hunter.FollowPath(_exitPath);
        }
    }

    public void Update()
    {
        if (_exitPath != null)
        {
            _hunter.ExecutePathMovement();
            if (Vector3.Distance(_hunter.transform.position, _hunter.ExitPoint.position) < 1.5f)
            {
                _hunter.ResetGathers();
                _hunter.transform.position = _hunter.InitialSpawnPoint.position;
                _hunter.FSM.ChangeState(_hunter.SearchState);
            }
        }
    }

    public void Exit() { }
}