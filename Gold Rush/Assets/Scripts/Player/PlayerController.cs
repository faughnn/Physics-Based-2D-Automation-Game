using UnityEngine;
using GoldRush.Core;

namespace GoldRush.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        private Rigidbody2D rb;
        private BoxCollider2D col;
        private PlayerInput playerInput;

        private bool isGrounded;
        private float groundCheckDistance = 0.1f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<BoxCollider2D>();
            playerInput = GetComponent<PlayerInput>();
        }

        private void FixedUpdate()
        {
            // Check if grounded
            CheckGrounded();

            // Apply horizontal movement
            ApplyMovement();
        }

        private void Update()
        {
            // Handle jump (in Update for responsive input)
            if (playerInput.JumpPressed && isGrounded)
            {
                Jump();
            }
        }

        private void CheckGrounded()
        {
            // Cast a small box downward to check for ground
            Vector2 boxCenter = (Vector2)transform.position + Vector2.down * (col.size.y / 2);
            Vector2 boxSize = new Vector2(col.size.x * 0.9f, groundCheckDistance);

            RaycastHit2D hit = Physics2D.BoxCast(
                boxCenter,
                boxSize,
                0f,
                Vector2.down,
                groundCheckDistance,
                LayerSetup.GetGroundMask()
            );

            isGrounded = hit.collider != null;
        }

        private void ApplyMovement()
        {
            float horizontalInput = playerInput.HorizontalInput;
            float targetVelocityX = horizontalInput * GameSettings.PlayerMoveSpeed;

            // Apply horizontal velocity directly for responsive movement
            rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
        }

        private void Jump()
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, GameSettings.PlayerJumpForce);
        }

        public bool IsGrounded => isGrounded;
    }
}
