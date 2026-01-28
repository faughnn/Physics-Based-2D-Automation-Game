# Bug: Player Lift Force Not Working Correctly

## Status: OPEN

## Symptoms
1. Lift helps player jump higher (bigger/higher jump) - force IS being applied
2. Upward velocity is NOT maintained after the initial jump boost
3. Player does NOT start moving up just by walking into a lift (should float upward)

## Expected Behavior
- Player should experience continuous upward acceleration when inside lift zone
- Standing still in a lift should cause the player to rise (net 20% gravity upward)
- Upward velocity should be maintained/increased while in lift, not just during jump

## Current Implementation
In `PlayerController.FixedUpdate()`:
```csharp
// Check for lift force
var simulation = SimulationManager.Instance;
if (simulation?.LiftManager != null)
{
    isInLift = simulation.LiftManager.ApplyLiftForce(
        rb, simulation.World.width, simulation.World.height);
}
```

`LiftManager.ApplyLiftForce()` uses `rb.AddForce()` to apply 120% of gravity upward.

## Possible Causes

### 1. Ground Collision Preventing Upward Movement
If the player is grounded (standing on terrain), the ground collision prevents upward movement. The lift force pushes up, but the collider is blocked by the floor. Unlike clusters (which are made of cells that can move through lift material), the player is a solid Rigidbody2D that collides with terrain.

### 2. Force vs Velocity Magnitude
The lift force (120% of gravity) may not be strong enough to overcome:
- Ground friction
- The player's mass relative to the force
- Any damping on the Rigidbody2D

### 3. Order of Operations
The velocity is being directly set for horizontal movement after `AddForce()`. While this shouldn't affect Y velocity, there could be subtle physics timing issues.

## Investigation Steps
1. Check if player rises when already in the air (jump into lift from below)
2. Check player's Rigidbody2D settings (mass, drag, gravity scale)
3. Verify the lift force value is appropriate for the player's mass
4. Test if lift works when player is NOT grounded (e.g., falling into lift)
5. Consider if lifts need to be hollow/passable for player (like they are for cells)

## Potential Fixes

### Option A: Increase Lift Force for Player
Use a higher force multiplier specifically for the player (e.g., 2.0x instead of 1.2x).

### Option B: Direct Velocity Modification
Instead of `AddForce()`, directly modify velocity when in lift:
```csharp
if (isInLift && rb.linearVelocity.y < maxLiftSpeed)
{
    velocity.y += liftAcceleration * Time.fixedDeltaTime;
}
```

### Option C: Cancel Gravity in Lift
When in lift, reduce effective gravity on the player rather than fighting against it.

## Files Involved
- `Assets/Scripts/Structures/LiftManager.cs` - `ApplyLiftForce()` method
- `Assets/Scripts/Game/PlayerController.cs` - FixedUpdate lift check

## Related
- Clusters work correctly with lifts (they rise as expected)
- Loose cells work correctly with lifts (simulation applies lift force in job)
