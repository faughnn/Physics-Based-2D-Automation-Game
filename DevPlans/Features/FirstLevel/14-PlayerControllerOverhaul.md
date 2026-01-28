# Player Controller Overhaul

## Current Issues

The current `PlayerController` is a basic implementation with several problems:

1. **Instant velocity changes** - No acceleration/deceleration, movement feels robotic
2. **No coyote time** - Can't jump briefly after walking off a ledge
3. **No jump buffering** - Pressing jump slightly before landing doesn't register
4. **No variable jump height** - Tap vs hold jump feels the same
5. **No air control tuning** - Same control in air as on ground
6. **Mixed responsibilities** - Tool/inventory logic mixed with movement

## Proposed Changes

### 1. Acceleration-Based Movement
```csharp
[Header("Movement")]
[SerializeField] private float maxSpeed = 80f;
[SerializeField] private float groundAcceleration = 600f;
[SerializeField] private float groundDeceleration = 800f;
[SerializeField] private float airAcceleration = 300f;
[SerializeField] private float airDeceleration = 200f;
```

Replace instant velocity with gradual acceleration:
- Snappy on ground (high accel/decel)
- Floatier in air (lower accel/decel)

### 2. Coyote Time
```csharp
[Header("Jump Assists")]
[SerializeField] private float coyoteTime = 0.1f;  // seconds after leaving ground

private float coyoteTimeCounter;
```

Allow jumping for a brief window after walking off a platform.

### 3. Jump Buffering
```csharp
[SerializeField] private float jumpBufferTime = 0.1f;

private float jumpBufferCounter;
```

If player presses jump just before landing, execute jump on landing.

### 4. Variable Jump Height
```csharp
[SerializeField] private float jumpForce = 400f;
[SerializeField] private float jumpCutMultiplier = 0.5f;  // velocity multiplier when releasing jump early
```

Releasing jump early cuts upward velocity for shorter hops.

### 5. Separate Inventory System
Move tool/inventory logic to a separate `PlayerInventory` component:
- `PlayerController` - Movement only
- `PlayerInventory` - Tool collection, equipping, events

## Implementation

### File to Modify
`Assets/Scripts/Game/PlayerController.cs`

### File to Create
`Assets/Scripts/Game/PlayerInventory.cs` - Extracted inventory logic

### New PlayerController Structure
```csharp
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 80f;
    [SerializeField] private float groundAcceleration = 600f;
    [SerializeField] private float groundDeceleration = 800f;
    [SerializeField] private float airAcceleration = 300f;
    [SerializeField] private float airDeceleration = 200f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 400f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Header("Jump Assists")]
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckDistance = 0.5f;
    [SerializeField] private LayerMask groundLayer = ~0;

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;

    // State
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isJumping;

    // Input
    private float moveInput;
    private bool jumpPressedThisFrame;
    private bool jumpHeld;

    private void FixedUpdate()
    {
        CheckGrounded();
        UpdateTimers();
        HandleMovement();
        HandleJump();
    }

    private void HandleMovement()
    {
        float targetSpeed = moveInput * maxSpeed;
        float accel = isGrounded
            ? (Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration)
            : (Mathf.Abs(targetSpeed) > 0.01f ? airAcceleration : airDeceleration);

        float speedDiff = targetSpeed - rb.linearVelocity.x;
        float movement = speedDiff * accel * Time.fixedDeltaTime;

        // Clamp to not overshoot target
        if (Mathf.Abs(movement) > Mathf.Abs(speedDiff))
            movement = speedDiff;

        rb.linearVelocity += new Vector2(movement, 0);
    }

    private void HandleJump()
    {
        // Buffer jump input
        if (jumpPressedThisFrame)
            jumpBufferCounter = jumpBufferTime;

        // Can jump if: (grounded OR in coyote time) AND (jump buffered)
        bool canJump = (isGrounded || coyoteTimeCounter > 0) && jumpBufferCounter > 0;

        if (canJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isJumping = true;
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
        }

        // Variable jump height - cut velocity when releasing jump
        if (isJumping && !jumpHeld && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            isJumping = false;
        }

        // Reset jumping flag on landing
        if (isGrounded && rb.linearVelocity.y <= 0)
            isJumping = false;
    }

    private void UpdateTimers()
    {
        // Coyote time - start counting when leaving ground
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.fixedDeltaTime;

        // Jump buffer countdown
        jumpBufferCounter -= Time.fixedDeltaTime;
    }
}
```

## Testing Checklist
- [ ] Movement feels responsive on ground
- [ ] Movement feels slightly floaty in air (intentional)
- [ ] Can jump briefly after walking off ledge (coyote time)
- [ ] Jump registers if pressed slightly before landing (buffering)
- [ ] Tap jump = short hop, hold jump = full height
- [ ] Tool collection still works (via PlayerInventory)

## Tuning Notes
These values will need playtesting:
- `coyoteTime`: 0.08-0.15s feels fair without being exploitable
- `jumpBufferTime`: 0.08-0.12s prevents frustration
- `jumpCutMultiplier`: 0.4-0.6 for noticeable but not jarring difference
- Acceleration values depend on world scale and desired feel
