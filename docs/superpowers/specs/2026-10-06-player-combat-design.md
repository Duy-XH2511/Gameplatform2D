# Player combat

Approved in chat: pickup weapon, attack to enter CombatState, configurable spell, enemy damage and health bars.

- Unity 6000.3.24f1; existing Player/Enemy state machines and Input System.
- Weapon is a scene pickup, reparented to a player hand anchor. Player cannot cast without an equipped active weapon.
- Left mouse Attack enters PlayerCombatState from Idle/Move/Jump/Fall, excluding Dash. Cast once on entry, then return to locomotion after 0.15 seconds. Movement continues while casting.
- Player has spellCooldown (default 0.5 seconds). Countdown persists across state transitions and weapon changes.
- SpellProjectile prefab has speed 10, maxDistance 8, damage 1, editable SpriteRenderer sprite and scale. Move horizontally in facing direction; ignore player/pickups, hit one enemy once, expire at range or blocking ground/wall.
- Enemy.TakeDamage delegates to existing Health. Zero HP stops enemy AI and movement. Existing Health.maxHealth is editable per enemy/boss.
- EnemyHealthBar displays world bar for Normal and screen bar for Boss; Inspector exposes kind, size, offset and colors.
- SampleScene receives a pickup and enemy HP bar. Prefabs and Vietnamese setup instructions make boss configuration reusable without adding boss AI.
- Verify cooldown, weapon gate, range and Health through .NET behavioral tests; compile all scripts against installed Unity assemblies; add Unity integration verification where executable.

Execution ruling: user's “ok làm đi” authorizes direct implementation in the shared Unity project. Keep changes reviewable in the current checkout; avoid creating another Unity checkout and additional approval stages.
