# Player Combat Implementation Plan

> **For agentic workers:** Use executing-plans for inline execution. Steps use checkbox tracking.

**Goal:** Equip a weapon, cast spells with cooldown, damage enemy HP, show configurable normal/boss bars.

**Architecture:** PlayerCombatState follows existing PlayerStateMachine. Weapon equips/picks up; SpellProjectile resolves collisions to Enemy.TakeDamage. Shared Health remains the source of HP.

**Tech Stack:** Unity 6000.3.24f1, C#, Input System, Physics2D, uGUI, .NET 10 test runner.

**Spec:** ../specs/2026-10-06-player-combat-design.md

## Global Constraints

- Edit the shared Unity project in place as authorized in chat.
- Preserve movement, jump, dash and existing player HUD.
- Cooldown survives state changes and weapon swaps.
- No boss AI implementation; reusable boss HP configuration.

## Review Focus

- No weapon or dead player: reject cast without consuming cooldown.
- Repeated input: one cast per cooldown, no queued cast after unequipping.
- Fast projectile: sweep collisions and stop exactly at range.
- Multiple colliders: one damage event per projectile.
- Dead enemy: HP clamps to zero, AI stops, bar updates.

### Task 1: Weapon, cooldown, combat state

- [x] Add failing behavioral tests for CombatCooldown weapon/death gate and elapsed countdown.
- [x] Run tests and observe missing behavior failure.
- [x] Add CombatCooldown; Weapon pickup; integrate PlayerCombatState and Attack input.
- [x] Run .NET tests; compile with Unity references.

### Task 2: Spell damage and enemy bars

- [x] Add failing behavioral tests for ProjectileTravel range clamping and invalid values.
- [x] Run tests and observe missing behavior failure.
- [x] Add SpellProjectile sweep/hit/expiry, Enemy.TakeDamage/death, EnemyHealthBar normal/boss.
- [x] Run full .NET suite and compile Unity scripts.

### Task 3: Scene assets and verification

- [x] Provide spell and weapon prefabs, pickup in SampleScene, wire Attack and enemy bar.
- [x] Add Unity integration tests for actual equip/cast/damage/bar flow.
- [x] Review changes, run available checks and document actual results.
- [x] Write Vietnamese setup guide and final progress.
