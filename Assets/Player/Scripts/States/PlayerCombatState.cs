using UnityEngine;

public class PlayerCombatState : PlayerState
{
    private float castTimer;

    public PlayerCombatState(Player player, PlayerStateMachine stateMachine) : base(player, stateMachine) { }

    public override void Enter()
    {
        castTimer = player.CastDuration;
        player.Flip(player.MoveInput.x);
        if (!player.TryCastSpell())
            castTimer = 0f;
    }

    public override void Update()
    {
        float xInput = player.MoveInput.x;
        player.Flip(xInput);
        player.Rb.linearVelocity = new Vector2(xInput * player.MoveSpeed, player.Rb.linearVelocity.y);
        castTimer -= Time.deltaTime;

        if (!player.HasWeapon || castTimer <= 0f)
        {
            if (player.IsGrounded())
            {
                player.ResetJumpCount();
                stateMachine.changeState(Mathf.Abs(xInput) > 0.01f ? player.MoveState : player.IdleState);
            }
            else
            {
                // FallState also preserves upward velocity, without granting another jump.
                stateMachine.changeState(player.FallState);
            }
        }
    }
}
