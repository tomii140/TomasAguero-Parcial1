
using System;

public class FiniteStateMachine
{
    private IState currentState;
    private IState pendingState;
    private bool isChangingState;

    public IState CurrentState => currentState;

    public event Action<IState> OnStateChanged;

    public void ChangeState(IState newState)
    {
        if (newState == null ||
            newState == currentState ||
            newState == pendingState)
        {
            return;
        }

        pendingState = newState;

        if (isChangingState)
            return;

        isChangingState = true;

        try
        {
            // Procesa los cambios pendientes sin interrumpir una transición en curso.
            while (pendingState != null)
            {
                IState nextState = pendingState;
                pendingState = null;

                if (nextState == currentState)
                    continue;

                currentState?.Exit();

                currentState = nextState;
                currentState.Enter();

                OnStateChanged?.Invoke(currentState);
            }
        }
        finally
        {
            isChangingState = false;
        }
    }

    public void Update()
    {
        currentState?.Update();
    }
}
