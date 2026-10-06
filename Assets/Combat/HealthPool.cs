#nullable enable
using System;

public sealed class HealthPool
{
    public int MaxHealth { get; }
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth == 0;

    public event Action<int, int>? Changed;
    public event Action? Died;

    public HealthPool(int maxHealth)
    {
        if (maxHealth < 1)
            throw new ArgumentOutOfRangeException(nameof(maxHealth));

        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead)
            return;

        CurrentHealth = Math.Max(0, CurrentHealth - amount);
        Changed?.Invoke(CurrentHealth, MaxHealth);

        if (IsDead)
            Died?.Invoke();
    }
}
