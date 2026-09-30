using UnityEngine;

public class PlayerFallState : PlayerState
{
    public PlayerFallState(Player player,PlayerStateMachine stateMachine) : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        Debug.Log("ENTER FALL");

        player.Animator.CrossFade("Player_Jump", 0.1f);
    }

    public override void Update()
    {
        base.Update();

        float xInput = player.MoveInput.x;

        player.Flip(xInput);

        player.Rb.linearVelocity = new Vector2(xInput * player.MoveSpeed,player.Rb.linearVelocity.y);

        // Kiểm tra chạm đất
        if (player.IsGrounded())
        {
            player.ResetJumpCount();
            player.ConsumeJumpInput();

            if (Mathf.Abs(xInput) > 0.01f)
                stateMachine.changeState(player.MoveState);
            else
                stateMachine.changeState(player.IdleState);

            return;
        }

        // Double Jump
        if (player.JumpPressed)
        {
            if (player.JumpCount < player.MaxJumpCount)
            {
                stateMachine.changeState(player.JumpState);
                return;
            }

            // Đã hết lượt -> bỏ input Space dư
            player.ConsumeJumpInput();
        }

        // Dash
        if (player.DashPressed)
        {
            stateMachine.changeState(player.DashState);
            return;
        }
    }
}