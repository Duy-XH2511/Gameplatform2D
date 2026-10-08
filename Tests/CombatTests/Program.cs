using System.Reflection;

var healthType = typeof(Program).Assembly.GetType("HealthPool");
if (healthType == null)
{
    Console.Error.WriteLine("FAIL: HealthPool is missing.");
    return 1;
}

var constructor = healthType.GetConstructor(new[] { typeof(int) })!;
var currentHealth = healthType.GetProperty("CurrentHealth")!;
var isDead = healthType.GetProperty("IsDead")!;
var takeDamage = healthType.GetMethod("TakeDamage")!;
var died = healthType.GetEvent("Died")!;
var changed = healthType.GetEvent("Changed")!;

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

try
{
    var health = constructor.Invoke(new object[] { 5 });
    Check((int)currentHealth.GetValue(health)! == 5, "Health should start full.");

    var notifications = new List<(int current, int maximum)>();
    changed.AddEventHandler(health, (Action<int, int>)((current, maximum) => notifications.Add((current, maximum))));
    takeDamage.Invoke(health, new object[] { 2 });
    Check((int)currentHealth.GetValue(health)! == 3, "Damage should reduce current health.");
    Check(notifications.Count == 1 && notifications[0] == (3, 5), "Damage should notify observers of the new health.");

    var deaths = 0;
    died.AddEventHandler(health, (Action)(() => deaths++));
    takeDamage.Invoke(health, new object[] { 99 });
    takeDamage.Invoke(health, new object[] { 1 });
    Check((int)currentHealth.GetValue(health)! == 0, "Health should not go below zero.");
    Check((bool)isDead.GetValue(health)!, "Zero health should be dead.");
    Check(deaths == 1, "Death should be raised exactly once.");
    Check(notifications.Count == 2 && notifications[1] == (0, 5), "Death should notify observers once.");

    var other = constructor.Invoke(new object[] { 4 });
    takeDamage.Invoke(other, new object[] { 0 });
    takeDamage.Invoke(other, new object[] { -2 });
    Check((int)currentHealth.GetValue(other)! == 4, "Nonpositive damage should be ignored.");

    var cooldownType = typeof(Program).Assembly.GetType("CombatCooldown");
    Check(cooldownType != null, "CombatCooldown is missing: weapon-gated casting is not implemented.");
    dynamic cooldown = Activator.CreateInstance(cooldownType!, new object[] { 0.5f })!;
    Check(!cooldown.TryStart(false, false), "Unarmed player must not cast.");
    Check(cooldown.Remaining == 0f, "Unarmed attempt must not consume cooldown.");
    Check(!cooldown.TryStart(true, true), "Dead player must not cast.");
    Check(cooldown.TryStart(true, false), "Armed living player must cast immediately.");
    Check(!cooldown.TryStart(true, false), "Repeated attack must not bypass cooldown.");
    cooldown.Tick(0.2f);
    Check(Math.Abs((float)cooldown.Remaining - 0.3f) < 0.0001f, "Countdown must decrease with elapsed time.");
    Check(!cooldown.TryStart(false, false), "Unequipping must block attack.");
    Check(!cooldown.TryStart(true, false), "Re-equipping must retain the existing cooldown.");
    cooldown.Tick(-1f);
    Check(Math.Abs((float)cooldown.Remaining - 0.3f) < 0.0001f, "Negative elapsed time must not extend cooldown.");
    cooldown.Tick(1f);
    Check(cooldown.Remaining == 0f, "Cooldown must clamp to zero.");
    Check(cooldown.TryStart(true, false), "Cast must become available after cooldown.");

    var travelType = typeof(Program).Assembly.GetType("ProjectileTravel");
    Check(travelType != null, "ProjectileTravel is missing: spell range is not implemented.");
    dynamic travel = Activator.CreateInstance(travelType!, new object[] { 8f })!;
    Check(travel.Advance(10f, 0.25f) == 2.5f, "Spell must move by speed times delta.");
    Check(travel.Advance(10f, 1f) == 5.5f, "A long frame must stop exactly at max range.");
    Check(travel.IsComplete, "Spell must expire at configured range.");
    Check(travel.Advance(10f, 1f) == 0f, "Expired spell must not move farther.");
    dynamic paused = Activator.CreateInstance(travelType!, new object[] { 2f })!;
    Check(paused.Advance(10f, -1f) == 0f, "Negative elapsed time must not move the projectile backwards.");
    Check(paused.Advance(-10f, 1f) == 0f, "Negative speed must not move the projectile backwards.");

    var boss = constructor.Invoke(new object[] { 100 });
    takeDamage.Invoke(boss, new object[] { 25 });
    Check((int)currentHealth.GetValue(boss)! == 75, "Boss must support its own HP value.");
    Check((int)currentHealth.GetValue(other)! == 4, "Enemy HP pools must be independent.");

    Console.WriteLine("PASS: health starts full, applies damage, notifies, and dies once.");
    Console.WriteLine("PASS: weapon/death gate, repeated input, countdown and weapon swaps.");
    Console.WriteLine("PASS: spell travel, exact range expiry and invalid movement inputs.");
    Console.WriteLine("PASS: normal and boss HP are independently configurable.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"FAIL: {error}");
    return 1;
}
