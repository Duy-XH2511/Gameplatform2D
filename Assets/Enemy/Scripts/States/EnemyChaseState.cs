using UnityEngine;

public class EnemyChaseState : EnemyState
{
    public EnemyChaseState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        Debug.Log("ENEMY ENTER CHASE");

        enemy.PlayAnimation("Enemy_Walk");
    }

    public override void Update()
    {
        base.Update();

        if (enemy.Player == null)
            return;

        float direction =
            enemy.Player.position.x > enemy.transform.position.x
                ? 1f
                : -1f;

        enemy.Flip(direction);

        if (!enemy.IsGroundAhead())
        {
            enemy.StopMovement();
            stateMachine.ChangeState(enemy.PatrolState);
            return;
        }

        if (enemy.IsWallAhead())
        {
            enemy.StopMovement();
            return;
        }

        enemy.Rb.linearVelocity = new Vector2(direction * enemy.MoveSpeed, enemy.Rb.linearVelocity.y);

        // Player chạy ra khỏi phạm vi
        if (!enemy.CanSeePlayer())
        {
            stateMachine.ChangeState(enemy.PatrolState);
            return;
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}
