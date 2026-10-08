public abstract class WeaponState
{
    protected readonly Weapon weapon;
    protected readonly WeaponStateMachine stateMachine;

    protected WeaponState(Weapon weapon, WeaponStateMachine stateMachine)
    {
        this.weapon = weapon;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}
