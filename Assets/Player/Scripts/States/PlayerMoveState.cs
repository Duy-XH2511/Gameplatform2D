using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(Player player,PlayerStateMachine stateMachine) : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        player.Animator.CrossFade("Player_Walk", 0.1f);

        Debug.Log("ENTER MOVE");
    }

    public override void Update()
    {
        base.Update();

        float xInput = player.MoveInput.x;

        player.Flip(xInput);

        player.Rb.linearVelocity = new Vector2(xInput * player.MoveSpeed,player.Rb.linearVelocity.y);

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

        if (xInput == 0)
        {
            stateMachine.changeState(player.IdleState);
        }
    }
}