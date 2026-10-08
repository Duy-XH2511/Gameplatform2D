using System;

public sealed class CombatCooldown
{
    private readonly float duration;
    public float Remaining { get; private set; }

    public CombatCooldown(float duration)
    {
        this.duration = Math.Max(0f, duration);
    }

    public bool CanCast(bool hasWeapon, bool isDead)
    {
        return hasWeapon && !isDead && Remaining <= 0f;
    }

    public bool TryStart(bool hasWeapon, bool isDead)
    {
        if (!CanCast(hasWeapon, isDead))
            return false;

        Remaining = duration;
        return true;
    }

    public void Tick(float deltaTime)
    {
        Remaining = Math.Max(0f, Remaining - Math.Max(0f, deltaTime));
    }
}
