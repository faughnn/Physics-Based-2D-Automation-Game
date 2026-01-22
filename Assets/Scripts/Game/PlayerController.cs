using System;
using System.Collections.Generic;
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

        // Inventory
        private HashSet<ToolType> inventory = new HashSet<ToolType>();
        private ToolType equippedTool = ToolType.None;

        // Events for UI/feedback
        public event Action<ToolType> OnToolEquipped;
        public event Action<ToolType> OnToolCollected;

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

        /// <summary>
        /// Currently equipped (active) tool.
        /// </summary>
        public ToolType EquippedTool => equippedTool;

        /// <summary>
        /// All tools the player has collected.
        /// </summary>
        public IReadOnlyCollection<ToolType> Inventory => inventory;

        /// <summary>
        /// Check if player has a tool in inventory.
        /// </summary>
        public bool HasTool(ToolType tool)
        {
            return inventory.Contains(tool);
        }

        /// <summary>
        /// Switch to a different tool from inventory.
        /// </summary>
        public bool EquipTool(ToolType tool)
        {
            if (tool == ToolType.None || inventory.Contains(tool))
            {
                equippedTool = tool;
                OnToolEquipped?.Invoke(tool);
                return true;
            }
            return false;  // Don't have this tool
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var item = other.GetComponent<WorldItem>();
            if (item != null)
            {
                CollectTool(item.ToolType);
                item.Collect();
            }
        }

        private void CollectTool(ToolType tool)
        {
            // Add to inventory
            inventory.Add(tool);
            OnToolCollected?.Invoke(tool);

            // Silently equip the new tool (replaces current)
            equippedTool = tool;
            Debug.Log($"[PlayerController] Collected and equipped: {tool}");
            OnToolEquipped?.Invoke(tool);
        }
    }
}
