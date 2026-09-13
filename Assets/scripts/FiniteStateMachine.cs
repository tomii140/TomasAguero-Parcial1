using UnityEngine;

public class FiniteStateMachine
{
    private IState _currentState;

    public void ChangeState(IState newState)
    {
        if (_currentState != null)
            _currentState.Exit();

        _currentState = newState;

        if (_currentState != null)
            _currentState.Enter();
    }

    public void Update()
    {
        if (_currentState != null)
            _currentState.Update();
    }
}