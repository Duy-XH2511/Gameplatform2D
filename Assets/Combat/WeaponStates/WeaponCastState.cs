using UnityEngine;

public sealed class WeaponCastState : WeaponState
{
    private float duration;
    public float RemainingTime { get; private set; }

    public WeaponCastState(Weapon weapon, WeaponStateMachine stateMachine) : base(weapon, stateMachine) { }

    public void SetDuration(float castDuration)
    {
        duration = Mathf.Max(0f, castDuration);
    }

    public override void Enter()
    {
        RemainingTime = duration;
        weapon.PlayAnimation(weapon.CastAnimationName);
    }

    public override void Update()
    {
        RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
        if (RemainingTime <= 0f)
            stateMachine.ChangeState(weapon.IdleState);
    }
}
