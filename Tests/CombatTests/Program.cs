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

    Console.WriteLine("PASS: health starts full, applies damage, notifies, and dies once.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"FAIL: {error}");
    return 1;
}
