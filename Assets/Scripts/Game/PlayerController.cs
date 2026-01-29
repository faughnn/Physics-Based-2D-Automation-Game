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
        [SerializeField] private float moveSpeed = 250f;       // world units/sec
        [SerializeField] private float jumpForce = 400f;      // impulse force

        [Header("Ground Check")]
        [SerializeField] private float groundCheckDistance = 2f;  // More forgiving for slopes
        [SerializeField] private LayerMask groundLayer = ~0;  // All layers by default
        [SerializeField] private float coyoteTime = 0.15f;  // Grace period after leaving ground

        private Rigidbody2D rb;
        private BoxCollider2D boxCollider;
        private bool isGrounded;
        private float lastGroundedTime;  // For coyote time
        private bool isInLift;

        // Input
        private Keyboard keyboard;
        private float moveInput;
        private bool jumpPressed;

        // Inventory
        private HashSet<ToolType> inventory = new HashSet<ToolType>();
        private ToolType equippedTool = ToolType.None;
        private StructureType equippedStructure = StructureType.None;

        // Events for UI/feedback
        public event Action<ToolType> OnToolEquipped;
        public event Action<ToolType> OnToolCollected;
        public event Action<StructureType> OnStructureEquipped;

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

            // Check for lift force
            var simulation = SimulationManager.Instance;
            if (simulation?.LiftManager != null)
            {
                isInLift = simulation.LiftManager.ApplyLiftForce(
                    rb, simulation.World.width, simulation.World.height);
            }
            else
            {
                isInLift = false;
            }

            // Horizontal movement
            Vector2 velocity = rb.linearVelocity;
            velocity.x = moveInput * moveSpeed;
            rb.linearVelocity = velocity;

            // Jump (with coyote time - can jump shortly after leaving ground)
            bool canJump = isGrounded || (Time.time - lastGroundedTime < coyoteTime);
            if (jumpPressed && canJump)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);  // Reset vertical velocity
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                lastGroundedTime = 0f;  // Prevent double jump during coyote time
            }

            // Reset jump flag
            jumpPressed = false;
        }

        private void CheckGrounded()
        {
            if (boxCollider == null) return;

            // Check for ground using OverlapBox below the player's feet
            Vector2 boxSize = boxCollider.size;
            Vector2 feetPos = (Vector2)transform.position + boxCollider.offset;
            feetPos.y -= boxSize.y * 0.5f + groundCheckDistance * 0.5f;

            // Wider box for better slope detection
            Vector2 checkSize = new Vector2(boxSize.x * 1.1f, groundCheckDistance);

            // Get all overlapping colliders and find first non-player hit
            Collider2D[] hits = Physics2D.OverlapBoxAll(feetPos, checkSize, 0f, groundLayer);

            Collider2D groundHit = null;
            foreach (var hit in hits)
            {
                if (hit.gameObject != gameObject)
                {
                    groundHit = hit;
                    break;
                }
            }

            isGrounded = groundHit != null;

            // Track last grounded time for coyote time
            if (isGrounded)
            {
                lastGroundedTime = Time.time;
            }
        }

        /// <summary>
        /// Returns true if the player is currently on the ground.
        /// </summary>
        public bool IsGrounded => isGrounded;

        /// <summary>
        /// Returns true if the player is currently in a lift zone.
        /// </summary>
        public bool IsInLift => isInLift;

        private void OnDrawGizmos()
        {
            if (boxCollider == null) return;

            // Draw ground check area
            Vector2 boxSize = boxCollider.size;
            Vector2 feetPos = (Vector2)transform.position + boxCollider.offset;
            feetPos.y -= boxSize.y * 0.5f + groundCheckDistance * 0.5f;
            Vector2 checkSize = new Vector2(boxSize.x * 1.1f, groundCheckDistance);

            bool canJump = isGrounded || (Time.time - lastGroundedTime < coyoteTime);
            Gizmos.color = canJump ? Color.green : Color.red;
            Gizmos.DrawWireCube(feetPos, checkSize);

            // Draw player collider bounds
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube((Vector2)transform.position + boxCollider.offset, boxSize);
        }

        /// <summary>
        /// Currently equipped (active) tool.
        /// </summary>
        public ToolType EquippedTool => equippedTool;

        /// <summary>
        /// Currently equipped structure (None if a tool is equipped).
        /// </summary>
        public StructureType EquippedStructure => equippedStructure;

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
        /// Grabber is always available without being in inventory.
        /// </summary>
        public bool EquipTool(ToolType tool)
        {
            if (tool != ToolType.None && tool != ToolType.Grabber && !inventory.Contains(tool))
                return false;

            equippedTool = tool;
            equippedStructure = StructureType.None;
            OnToolEquipped?.Invoke(tool);
            OnStructureEquipped?.Invoke(StructureType.None);
            return true;
        }

        /// <summary>
        /// Equip a structure for placement. Unequips any tool.
        /// </summary>
        public void EquipStructure(StructureType structure)
        {
            equippedTool = ToolType.None;
            equippedStructure = structure;
            OnToolEquipped?.Invoke(ToolType.None);
            OnStructureEquipped?.Invoke(structure);
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
            OnToolEquipped?.Invoke(tool);
        }
    }
}
