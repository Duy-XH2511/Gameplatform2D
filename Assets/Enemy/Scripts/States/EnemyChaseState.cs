using UnityEngine;

public class EnemyChaseState : EnemyState
{
    public EnemyChaseState(Enemy enemy, EnemyStateMachine stateMachine)
        : base(enemy, stateMachine)
    {
    }

    public override void Enter()
    {
        enemy.PlayAnimation("Enemy_Walk");
    }

    public override void Update()
    {
        if (!enemy.CanSeePlayer())
        {
            enemy.StopMovement();
            stateMachine.ChangeState(enemy.PatrolState);
            return;
        }

        float direction = enemy.Player.position.x > enemy.transform.position.x
            ? 1f
            : -1f;

        enemy.Flip(direction);

        if (enemy.CanAttack && enemy.IsPlayerInAttackRange())
        {
            stateMachine.ChangeState(enemy.AttackState);
            return;
        }

        if (!enemy.IsGroundAhead())
        {
            enemy.StopMovement();
            stateMachine.ChangeState(enemy.PatrolState);
            return;
        }

        if (enemy.IsPlayerInAttackRange())
        {
            enemy.StopMovement();

            if (enemy.CanAttack)
                stateMachine.ChangeState(enemy.AttackState);

            return;
        }

        enemy.Rb.linearVelocity = new Vector2(
            direction * enemy.MoveSpeed,
            enemy.Rb.linearVelocity.y
        );
    }
}