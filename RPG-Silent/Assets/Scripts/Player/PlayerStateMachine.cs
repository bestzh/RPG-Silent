public class PlayerStateMachine
{
    private PlayerState currentState;

    public void ChangeState(PlayerState newState)
    {
        if (newState.GetType() == currentState?.GetType())
        {
            return;
        }
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void Update()
    {
        currentState?.Update();
    }
}
