using UnityEngine;

public class PlayerDashState : PlayerState
{
    private float dashTimer;

    public PlayerDashState( Player player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {

    }

    public override void Enter()
    {
        base.Enter();

        Debug.Log("ENTER DASH");

        player.Animator.CrossFade("Player_Dash", 0.05f);

        // QUAN TRỌNG
        player.ConsumeDashInput();

        player.StartDashCooldown();

        dashTimer = player.DashDuration;

        player.Rb.gravityScale = 0f;

        player.Rb.linearVelocity = new Vector2(player.FacingDirection * player.DashSpeed,0f);
    }

    public override void Update()
    {
        base.Update();

        dashTimer -= Time.deltaTime;

        player.Rb.linearVelocity = new Vector2( player.FacingDirection * player.DashSpeed, 0f);

        if (dashTimer <= 0)
        {
            if (player.IsGrounded())
            {
                if (player.MoveInput.x != 0)
                    stateMachine.changeState(player.MoveState);
                else
                    stateMachine.changeState(player.IdleState);
            }
            else
            {
                stateMachine.changeState(player.FallState);
            }
        }
    }

    public override void Exit()
    {
        base.Exit();

        Debug.Log("EXIT DASH");

        player.Rb.gravityScale = player.NormalGravityScale;
    }
}