using UnityEngine;

public class EnemyPatrolState : EnemyState
{
    public EnemyPatrolState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        Debug.Log("ENEMY ENTER PATROL");

        enemy.PlayAnimation("Enemy_Walk");
    }

    public override void Update()
    {
        base.Update();

        // Sắp rơi khỏi mép
        if (!enemy.IsGroundAhead())
        {
            enemy.StopMovement();
            enemy.StepBack();
            enemy.TurnAround();
            return;
        }

        // Gặp tường
        if (enemy.IsWallAhead())
        {
            enemy.StopMovement();
            enemy.TurnAround();
            return;
        }

        // Di chuyển
        enemy.Rb.linearVelocity = new Vector2(enemy.FacingDirection * enemy.MoveSpeed,enemy.Rb.linearVelocity.y);

        // Phát hiện Player
        if (enemy.CanSeePlayer())
        {
            stateMachine.ChangeState(enemy.ChaseState);
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}
