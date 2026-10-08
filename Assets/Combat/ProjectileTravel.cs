using System;

public sealed class ProjectileTravel
{
    public float RemainingDistance { get; private set; }
    public bool IsComplete => RemainingDistance <= 0f;

    public ProjectileTravel(float maxDistance)
    {
        RemainingDistance = Math.Max(0f, maxDistance);
    }

    public float Advance(float speed, float deltaTime)
    {
        float distance = Math.Min(RemainingDistance, Math.Max(0f, speed) * Math.Max(0f, deltaTime));
        RemainingDistance = Math.Max(0f, RemainingDistance - distance);
        return distance;
    }
}
