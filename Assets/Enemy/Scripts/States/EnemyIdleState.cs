using UnityEngine;

public class EnemyIdleState : EnemyState
{
    public EnemyIdleState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        Debug.Log("ENEMY ENTER IDLE");

        enemy.Rb.linearVelocity = new Vector2(0,enemy.Rb.linearVelocity.y);

        enemy.PlayAnimation("Enemy_Idle", 0.1f);
    }

    public override void Update()
    {
        base.Update();
    }

    public override void Exit()
    {
        base.Exit();
    }
}
