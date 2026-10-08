using UnityEngine;

public sealed class WeaponIdleState : WeaponState
{
    public WeaponIdleState(Weapon weapon, WeaponStateMachine stateMachine) : base(weapon, stateMachine) { }

    public override void Enter()
    {
        weapon.transform.localRotation = Quaternion.identity;
        weapon.PlayAnimation(weapon.IdleAnimationName);
    }
}
