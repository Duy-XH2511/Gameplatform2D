using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Copied into the temporary verification project's Assets/Editor by Run-CombatVerification.ps1.
[InitializeOnLoad]
public static class CombatIntegrationVerification
{
    private const string Pending = "CombatVerification.Pending";
    private static readonly List<string> results = new List<string>();
    private static double started;

    static CombatIntegrationVerification()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                Application.runInBackground = true;
                EditorApplication.isPaused = false;
                started = EditorApplication.timeSinceStartup;
                EditorApplication.update += PumpPlayerLoop;
                new GameObject("VerificationDriver").AddComponent<CombatVerificationDriver>().Run(Verify(), Finish);
            }
        };
    }

    public static void Run()
    {
        CombatAssetGenerator.CreateAssets();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PlayerSettings.runInBackground = true;
        Application.runInBackground = true;
        Type console = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
        console.GetMethod("SetConsoleErrorPause", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, new object[] { false });
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void PumpPlayerLoop()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        if (EditorApplication.timeSinceStartup - started > 30d)
            Finish(1, "FAIL: Play Mode verification did not complete within 30 seconds.");
    }

    private static void Finish(int code, string message)
    {
        EditorApplication.update -= PumpPlayerLoop;
        SessionState.SetBool(Pending, false);
        results.Add(message);
        File.WriteAllLines("combat-verification-results.txt", results);
        Debug.Log(message);
        EditorApplication.Exit(code);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        results.Add("PASS: " + message);
        File.WriteAllLines("combat-verification-results.txt", results);
    }

    private static IEnumerator Verify()
    {
        Physics2D.simulationMode = SimulationMode2D.Script;
        GameObject playerObject = new GameObject("TestPlayer", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator));
        playerObject.SetActive(false);
        playerObject.tag = "Player";
        Player player = playerObject.AddComponent<Player>();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/TestPlayer.controller");
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath("Assets/TestPlayer.controller");
            foreach (string state in new[] { "Player_Idle", "Player_Walk", "Player_Jump", "Player_Dash" })
                controller.layers[0].stateMachine.AddState(state);
        }
        playerObject.GetComponent<Animator>().runtimeAnimatorController = controller;
        Transform groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(playerObject.transform, false);
        groundCheck.localPosition = Vector3.down * 0.6f;
        Set(player, "groundCheck", groundCheck);
        playerObject.GetComponent<Rigidbody2D>().gravityScale = 3f;
        playerObject.SetActive(true);
        player.Rb.gravityScale = 0f;
        // Initialize a neutral locomotion state without requiring animation assets.
        player.StateMachine.Initialize(new PlayerState(player, player.StateMachine));
        player.enabled = false;
        Check(!player.TryEnterCombat(), "Unarmed player cannot enter CombatState.");

        Weapon prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Combat/Prefabs/WeaponPickup.prefab").GetComponent<Weapon>();
        Weapon pickup = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        Physics2D.SyncTransforms();
        Physics2D.Simulate(Time.fixedDeltaTime);
        yield return null;
        Check(player.HasWeapon, "Scene pickup equips through a real Physics2D trigger.");
        Check(pickup.transform.IsChildOf(player.transform), "Equipped weapon is attached to the player.");
        Check(!pickup.GetComponent<Collider2D>().enabled, "Equipped weapon disables its pickup collider.");
        Check(WeaponStateName(pickup) == "WeaponIdleState", "Equipped weapon starts in WeaponIdleState.");
        Animator weaponAnimator = pickup.GetComponent<Animator>();
        Check(weaponAnimator != null && weaponAnimator.runtimeAnimatorController != null &&
              weaponAnimator.HasState(0, Animator.StringToHash("Weapon_Idle")) &&
              weaponAnimator.HasState(0, Animator.StringToHash("Weapon_Cast")), "Weapon prefab provides idle and cast Animator states.");

        int before = Object.FindObjectsByType<SpellProjectile>(FindObjectsSortMode.None).Length;
        Check(player.TryEnterCombat(), "Armed player enters CombatState.");
        Check(player.StateMachine.CurrentState == player.CombatState, "Combat state is the active PlayerState.");
        Check(WeaponStateName(pickup) == "WeaponCastState", "Successful player cast enters WeaponCastState.");
        SpellProjectile[] spells = Object.FindObjectsByType<SpellProjectile>(FindObjectsSortMode.None);
        Check(spells.Length == before + 1, "Entering CombatState spawns exactly one spell.");
        SpellProjectile spell = spells[spells.Length - 1];
        Check(!player.TryCastSpell(), "Repeated cast is blocked by cooldown.");
        player.UnequipWeapon();
        Check(WeaponStateName(pickup) == "WeaponIdleState", "Dropping during cast immediately restores WeaponIdleState.");
        Check(!player.TryEnterCombat() && !player.TryCastSpell(), "Unequipping blocks combat and casting.");
        player.EquipWeapon(pickup);
        Check(!player.TryCastSpell(), "Re-equipping cannot bypass cooldown.");

        GameObject enemyObject = new GameObject("TestEnemy", typeof(SpriteRenderer), typeof(BoxCollider2D));
        enemyObject.transform.position = new Vector3(1.2f, 0.1f, 0f);
        enemyObject.GetComponent<BoxCollider2D>().size = Vector2.one;
        Enemy enemy = enemyObject.AddComponent<Enemy>();
        enemy.enabled = false;
        enemy.Rb.gravityScale = 0f;
        EnemyHealthBar bar = enemyObject.AddComponent<EnemyHealthBar>();
        int initialHP = enemy.Health.CurrentHealth;
        Physics2D.SyncTransforms();
        Invoke(spell, "FixedUpdate");
        yield return null;
        Check(enemy.Health.CurrentHealth == initialHP - 1, "Actual projectile sweep calls Enemy.TakeDamage once.");
        RectTransform fill = Get<RectTransform>(bar, "fill");
        Check(Mathf.Approximately(fill.anchorMax.x, (float)(initialHP - 1) / initialHP), "Normal health bar updates after a spell hit.");
        Check(bar.Kind == EnemyHealthBarKind.Normal, "Enemy bar defaults to normal world display.");

        enemyObject.transform.localScale = new Vector3(-5f, 4f, 1f);
        Invoke(bar, "LateUpdate");
        GameObject root = Get<GameObject>(bar, "barRoot");
        Check(root.transform.localScale == Vector3.one * 0.01f && root.transform.rotation == Quaternion.identity, "Enemy facing and scale do not flip or enlarge the HP bar.");

        GameObject bossObject = new GameObject("TestBoss", typeof(SpriteRenderer));
        bossObject.SetActive(false);
        bossObject.transform.position = new Vector3(100f, 0f, 0f);
        Health bossHealth = bossObject.AddComponent<Health>();
        Set(bossHealth, "maxHealth", 100);
        EnemyHealthBar bossBar = bossObject.AddComponent<EnemyHealthBar>();
        Set(bossBar, "kind", EnemyHealthBarKind.Boss);
        bossObject.SetActive(true);
        bossHealth.TakeDamage(25);
        Check(bossHealth.CurrentHealth == 75, "Boss supports independently configured 100 HP.");
        Check(Get<GameObject>(bossBar, "barRoot").GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay, "Boss uses a screen health bar.");
        Check(Mathf.Approximately(Get<RectTransform>(bossBar, "fill").anchorMax.x, 0.75f), "Boss bar displays the correct HP ratio.");

        enemy.TakeDamage(999);
        Check(enemy.Health.IsDead && !enemy.Rb.simulated && !enemy.GetComponent<Collider2D>().enabled, "Dead enemy stops physics and AI and cannot block further spells.");
        Check(fill.anchorMax.x == 0f, "Dead enemy health bar is empty.");

        SpellProjectile leftSpell = Object.Instantiate(prefab.SpellPrefab, new Vector3(-20f, 0f, 0f), Quaternion.identity);
        Set(leftSpell, "maxDistance", 0.1f);
        leftSpell.Launch(player, -1);
        Invoke(leftSpell, "FixedUpdate");
        Check(Mathf.Abs(leftSpell.GetComponent<Rigidbody2D>().position.x - (-20.1f)) < 0.001f, "Left-facing spell clamps to exact configured range.");
        float deadline = Time.realtimeSinceStartup + 2f;
        while (leftSpell != null && Time.realtimeSinceStartup < deadline)
            yield return null;
        Check(leftSpell == null, "Spell is destroyed at max range.");

        GameObject dashVictimObject = new GameObject("DashVictim", typeof(Rigidbody2D), typeof(Animator));
        dashVictimObject.SetActive(false);
        dashVictimObject.transform.position = new Vector3(100f, 100f, 0f);
        dashVictimObject.GetComponent<Rigidbody2D>().gravityScale = 3f;
        dashVictimObject.GetComponent<Animator>().runtimeAnimatorController = controller;
        Player dashVictim = dashVictimObject.AddComponent<Player>();
        dashVictimObject.SetActive(true);
        dashVictim.enabled = false;
        dashVictim.StateMachine.Initialize(dashVictim.DashState);
        dashVictim.Health.TakeDamage(999);
        Check(dashVictim.Rb.gravityScale == 3f && dashVictim.StateMachine.CurrentState != dashVictim.DashState, "Lethal hit during dash restores gravity and exits DashState.");

        GameObject wall = new GameObject("ThinWall", typeof(BoxCollider2D));
        wall.layer = 6;
        wall.transform.position = new Vector3(0.4f, 0.1f, 0f);
        wall.GetComponent<BoxCollider2D>().size = new Vector2(0.05f, 2f);
        Physics2D.SyncTransforms();
        SpellProjectile spawnBlocked = Object.Instantiate(prefab.SpellPrefab, new Vector3(0.65f, 0.1f, 0f), Quaternion.identity);
        spawnBlocked.Launch(player, 1);
        deadline = Time.realtimeSinceStartup + 0.2f;
        while (spawnBlocked != null && Time.realtimeSinceStartup < deadline)
            yield return null;
        Check(spawnBlocked == null, "Spell cannot spawn through a thin wall between player and muzzle.");
        Object.Destroy(wall);
        yield return null;

        // Exercise the real Mouse binding, PlayerInput UnityEvent and Player.Update path.
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        PlayerInput input = playerObject.AddComponent<PlayerInput>();
        input.notificationBehavior = PlayerNotifications.InvokeUnityEvents;
        input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Player/Input/Player.inputactions");
        input.defaultActionMap = "Player";
        PlayerInput.ActionEvent attackEvent = new PlayerInput.ActionEvent(input.actions.FindAction("Player/Attack"));
        attackEvent.AddListener(player.OnAttack);
        input.actionEvents = new[] { attackEvent };
        input.SwitchCurrentControlScheme("Keyboard", keyboard, mouse);
        input.ActivateInput();
        input.SwitchCurrentActionMap("Player");
        Get<CombatCooldown>(player, "combatCooldown").Tick(1f);
        player.StateMachine.Initialize(new PlayerState(player, player.StateMachine));
        Set(player, "castDuration", 10f);
        before = Object.FindObjectsByType<SpellProjectile>(FindObjectsSortMode.None).Length;
        InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
        InputSystem.Update();
        Invoke(player, "Update");
        Check(Object.FindObjectsByType<SpellProjectile>(FindObjectsSortMode.None).Length == before + 1, "Mouse click through PlayerInput invokes combat and casts once.");
        Invoke(player, "Update");
        Check(Object.FindObjectsByType<SpellProjectile>(FindObjectsSortMode.None).Length == before + 1, "Holding the mouse does not generate duplicate casts.");
        Check(WeaponStateName(pickup) == "WeaponCastState", "Mouse attack also activates the weapon cast state.");
        Vector3 heldPosition = pickup.transform.localPosition;
        Vector3 heldScale = pickup.transform.localScale;
        weaponAnimator.Update(0f);
        weaponAnimator.Update(0.07f);
        Check(weaponAnimator.GetCurrentAnimatorStateInfo(0).IsName("Weapon_Cast") &&
              Mathf.Abs(Mathf.DeltaAngle(pickup.transform.localEulerAngles.z, -55f)) < 1f,
              "Cast animation visibly tilts the wand. Angle=" + pickup.transform.localEulerAngles.z +
              ", state=" + weaponAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash +
              ", time=" + weaponAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime +
              ", transitioning=" + weaponAnimator.IsInTransition(0));
        Check(pickup.transform.localPosition == heldPosition && pickup.transform.localScale == heldScale,
              "Weapon animation preserves the configured held position and scale.");
        typeof(Weapon).GetMethod("BeginCast").Invoke(pickup, new object[] { 0.03f });
        deadline = Time.realtimeSinceStartup + 1f;
        while (WeaponStateName(pickup) != "WeaponIdleState" && Time.realtimeSinceStartup < deadline)
            yield return null;
        Check(WeaponStateName(pickup) == "WeaponIdleState", "Weapon cast duration expires automatically back to Idle.");
        weaponAnimator.Update(0f);
        weaponAnimator.Update(0.05f);
        Check(weaponAnimator.GetCurrentAnimatorStateInfo(0).IsName("Weapon_Idle") &&
              Mathf.Abs(Mathf.DeltaAngle(pickup.transform.localEulerAngles.z, 0f)) < 1f,
              "Idle animation restores the upright wand pose.");
        typeof(Weapon).GetMethod("BeginCast").Invoke(pickup, new object[] { 1f });
        InputSystem.RemoveDevice(mouse);
        InputSystem.RemoveDevice(keyboard);

        player.Health.TakeDamage(999);
        Check(WeaponStateName(pickup) == "WeaponIdleState", "Player death cancels the weapon cast state.");
        Check(!player.TryCastSpell() && !player.TryEnterCombat(), "Dead player cannot cast or enter combat.");
    }

    private static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static string WeaponStateName(Weapon weapon)
    {
        PropertyInfo property = typeof(Weapon).GetProperty("StateMachine");
        if (property == null) throw new Exception("Weapon state machine is missing: Idle and Cast are not implemented.");
        object machine = property.GetValue(weapon);
        return machine.GetType().GetProperty("CurrentState").GetValue(machine).GetType().Name;
    }
    private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Invoke(object target, string method) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
}
