using UnityEngine;

public class EnemyAttackState : EnemyState
{
    private float elapsed;
    private bool hasHit;

    public EnemyAttackState(Enemy enemy, EnemyStateMachine stateMachine)
        : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        elapsed = 0f;
        hasHit = false;

        enemy.StopMovement();
        enemy.StartAttackCooldown();

        if (enemy.Player != null)
            enemy.Flip(enemy.Player.position.x - enemy.transform.position.x);

        // Khi bạn thêm state Enemy_Attack vào Animator, code sẽ tự phát animation.
        if (enemy.Animator.runtimeAnimatorController != null &&
            enemy.Animator.HasState(0, Animator.StringToHash("Enemy_Attack")))
        {
            enemy.PlayAnimation("Enemy_Attack", 0f);
        }
    }

    public override void Update()
    {
        enemy.StopMovement();
        elapsed += Time.fixedDeltaTime;

        if (!hasHit && elapsed >= Mathf.Min(enemy.AttackWindup, enemy.AttackDuration))
        {
            hasHit = true;
            enemy.TryAttackHit();
        }

        if (elapsed < enemy.AttackDuration)
            return;

        stateMachine.ChangeState(
            enemy.CanSeePlayer() ? enemy.ChaseState : enemy.PatrolState
        );
    }
}