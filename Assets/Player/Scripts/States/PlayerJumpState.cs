using UnityEngine;

public class PlayerJumpState : PlayerState
{
    private bool hasLeftGround;

    public PlayerJumpState(Player player,PlayerStateMachine stateMachine) : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        hasLeftGround = false;

        player.Animator.CrossFade("Player_Jump", 0.1f);

        player.Rb.linearVelocity = new Vector2(player.Rb.linearVelocity.x,player.JumpForce);

        player.JumpCount++;
        player.ConsumeJumpInput();
    }

    public override void Update()
    {
        base.Update();

        float xInput = player.MoveInput.x;

        player.Flip(xInput);

        player.Rb.linearVelocity = new Vector2(xInput * player.MoveSpeed,player.Rb.linearVelocity.y);

        // Đã thực sự rời khỏi mặt đất
        if (!player.IsGrounded())
        {
            hasLeftGround = true;
        }

        // Double Jump
        if (player.JumpPressed)
        {
            if (player.JumpCount < player.MaxJumpCount)
            {
                player.ConsumeJumpInput();

                player.Rb.linearVelocity = new Vector2(player.Rb.linearVelocity.x,player.JumpForce);

                player.JumpCount++;
            }
            else
            {
                // Space dư -> bỏ
                player.ConsumeJumpInput();
            }
        }

        // Khi bắt đầu rơi
        if (hasLeftGround && player.Rb.linearVelocity.y < 0f)
        {
            stateMachine.changeState(player.FallState);
            return;
        }

        if (player.DashPressed && player.CanDash)
        {
            stateMachine.changeState(player.DashState);
            return;
        }
    }
}