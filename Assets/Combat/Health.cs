using System;
using UnityEngine;

public sealed class Health : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 5;

    private HealthPool pool;

    public int MaxHealth => pool != null ? pool.MaxHealth : maxHealth;
    public int CurrentHealth => pool != null ? pool.CurrentHealth : maxHealth;
    public bool IsDead => pool != null && pool.IsDead;

    public event Action<int, int> Changed;
    public event Action Died;

    private void Awake()
    {
        pool = new HealthPool(Mathf.Max(1, maxHealth));
        pool.Changed += (current, maximum) => Changed?.Invoke(current, maximum);
        pool.Died += () => Died?.Invoke();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        pool.TakeDamage(amount);
    }
}
