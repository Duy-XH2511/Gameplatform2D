using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        player.Animator.CrossFade("Player_Idle", 0.1f);

        player.Rb.linearVelocity = new UnityEngine.Vector2(
            0,
            player.Rb.linearVelocity.y
        );

        Debug.Log("ENTER IDLE");
    }

    public override void Update()
    {
        base.Update();

        float xInput = player.MoveInput.x;

        if (player.JumpPressed)
        {
            player.ConsumeJumpInput();
            stateMachine.changeState(player.JumpState);
            return;
        }
        if (player.DashPressed && player.CanDash)
        {
            stateMachine.changeState(player.DashState);
            return;
        }

        if (xInput != 0)
        {
            stateMachine.changeState(player.MoveState);
        }
    }
}