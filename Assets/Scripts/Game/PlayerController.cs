using UnityEngine;
using UnityEngine.InputSystem;

namespace FallingSand
{
    /// <summary>
    /// Platformer-style player controller with WASD/arrow movement and jumping.
    /// Uses Unity's physics system with ground detection via BoxCast.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 200f;      // world units/sec
        [SerializeField] private float jumpForce = 400f;      // impulse force

        [Header("Ground Check")]
        [SerializeField] private float groundCheckDistance = 0.5f;
        [SerializeField] private LayerMask groundLayer = ~0;  // All layers by default

        private Rigidbody2D rb;
        private BoxCollider2D boxCollider;
        private bool isGrounded;

        // Input
        private Keyboard keyboard;
        private float moveInput;
        private bool jumpPressed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            boxCollider = GetComponent<BoxCollider2D>();
        }

        private void Start()
        {
            keyboard = Keyboard.current;

            if (rb == null)
            {
                Debug.LogError("[PlayerController] No Rigidbody2D found!");
                return;
            }

            Debug.Log($"[PlayerController] Initialized. MoveSpeed: {moveSpeed}, JumpForce: {jumpForce}");
        }

        private void Update()
        {
            if (keyboard == null) return;

            // Read horizontal input (A/D or Left/Right arrows)
            moveInput = 0f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                moveInput = -1f;
            else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                moveInput = 1f;

            // Read jump input (Space)
            if (keyboard.spaceKey.wasPressedThisFrame)
                jumpPressed = true;
        }

        private void FixedUpdate()
        {
            if (rb == null) return;

            // Ground check using BoxCast
            CheckGrounded();

            // Horizontal movement
            Vector2 velocity = rb.linearVelocity;
            velocity.x = moveInput * moveSpeed;
            rb.linearVelocity = velocity;

            // Jump (only when grounded)
            if (jumpPressed && isGrounded)
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }

            // Reset jump flag
            jumpPressed = false;
        }

        private void CheckGrounded()
        {
            if (boxCollider == null) return;

            // BoxCast downward from the bottom of the collider
            Vector2 boxSize = boxCollider.size;
            Vector2 origin = (Vector2)transform.position + boxCollider.offset;
            origin.y -= boxSize.y * 0.5f;  // Start from bottom of collider

            // Use a slightly smaller box for the cast
            Vector2 castSize = new Vector2(boxSize.x * 0.9f, 0.1f);

            RaycastHit2D hit = Physics2D.BoxCast(
                origin,
                castSize,
                0f,
                Vector2.down,
                groundCheckDistance,
                groundLayer
            );

            // Grounded if we hit something that isn't ourselves
            isGrounded = hit.collider != null && hit.collider.gameObject != gameObject;
        }

        /// <summary>
        /// Returns true if the player is currently on the ground.
        /// </summary>
        public bool IsGrounded => isGrounded;
    }
}
