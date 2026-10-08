using System;

public sealed class WeaponStateMachine
{
    public WeaponState CurrentState { get; private set; }

    public void Initialize(WeaponState startingState)
    {
        ChangeState(startingState);
    }

    public void ChangeState(WeaponState nextState)
    {
        if (nextState == null)
            throw new ArgumentNullException(nameof(nextState));

        CurrentState?.Exit();
        CurrentState = nextState;
        CurrentState.Enter();
    }
}
