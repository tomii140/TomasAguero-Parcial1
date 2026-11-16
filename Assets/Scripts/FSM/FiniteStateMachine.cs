
using UnityEngine;
using System;

public class FiniteStateMachine
{
    private IState _currentState;

    public IState CurrentState => _currentState;

    public event Action<IState> OnStateChanged;

    public void ChangeState(IState newState)
    {
        if (newState == null || _currentState == newState) return;

        _currentState?.Exit();

        _currentState = newState;

        _currentState.Enter();

        OnStateChanged?.Invoke(_currentState);
    }

    public void Update()
    {
        _currentState?.Update();
    }
}
