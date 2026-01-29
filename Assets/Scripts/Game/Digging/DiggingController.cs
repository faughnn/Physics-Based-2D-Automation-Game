using UnityEngine;
using UnityEngine.InputSystem;

namespace FallingSand
{
    /// <summary>
    /// Handles player digging interaction. Requires shovel to be equipped.
    /// Converts Ground cells to Air and spawns Dirt particles with upward velocity.
    /// </summary>
    public class DiggingController : MonoBehaviour
    {
        [Header("Dig Settings")]
        [SerializeField] private float digRadius = 8f;
        [SerializeField] private float maxDigDistance = 200f;

        [Header("Particle Settings")]
        [SerializeField] private float minUpwardVelocity = 3f;
        [SerializeField] private float maxUpwardVelocity = 8f;
        [SerializeField] private float horizontalSpread = 3f;

        [Header("Repeat Settings")]
        [SerializeField] private float digCooldown = 0.1f;  // 100ms between digs

        private float nextDigTime;
        private PlayerController player;
        private Camera mainCamera;
        private Mouse mouse;

        public float MaxDigDistance => maxDigDistance;

        private SimulationManager Simulation => SimulationManager.Instance;
        private CellWorld World => Simulation?.World;

        private void Start()
        {
            player = GetComponent<PlayerController>();
            mainCamera = Camera.main;
            mouse = Mouse.current;

            if (player == null)
                Debug.LogError("[DiggingController] No PlayerController found on this GameObject!");
        }

        private void Update()
        {
            if (mouse == null || player == null || World == null) return;

            // Left click/hold to dig
            if (mouse.leftButton.isPressed && Time.time >= nextDigTime)
            {
                PerformanceProfiler.StartTiming(TimingSlot.Digging);
                if (TryDig())
                {
                    nextDigTime = Time.time + digCooldown;
                }
                PerformanceProfiler.StopTiming(TimingSlot.Digging);
            }
        }

        private bool TryDig()
        {
            // Check shovel equipped
            if (player.EquippedTool != ToolType.Shovel)
                return false;

            // Get click position in world coordinates
            Vector2 mouseScreen = mouse.position.ReadValue();
            Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(
                new Vector3(mouseScreen.x, mouseScreen.y, 0));

            // Proximity check
            Vector2 playerPos = transform.position;
            Vector2 clickPos = new Vector2(mouseWorld.x, mouseWorld.y);
            float distance = Vector2.Distance(playerPos, clickPos);

            if (distance > maxDigDistance)
                return false;

            // Convert click to cell coordinates using CoordinateUtils
            var world = World;
            Vector2Int centerCell = CoordinateUtils.WorldToCell(clickPos, world.width, world.height);

            // Perform the dig
            DigAt(centerCell);
            return true;
        }

        private void DigAt(Vector2Int center)
        {
            int radius = Mathf.RoundToInt(digRadius);
            int cellsDug = 0;
            int dirtSpawned = 0;

            var world = World;
            var terrainColliders = Simulation.TerrainColliders;

            // Iterate over circular area
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    // Circular check
                    if (dx * dx + dy * dy > radius * radius)
                        continue;

                    int x = center.x + dx;
                    int y = center.y + dy;

                    if (!world.IsInBounds(x, y))
                        continue;

                    byte material = world.GetCell(x, y);

                    // Check if this is Ground material (diggable)
                    if (material != Materials.Ground)
                        continue;

                    // Convert Ground to Air
                    world.SetCell(x, y, Materials.Air);
                    terrainColliders.MarkChunkDirtyAt(x, y);
                    cellsDug++;

                    // Spawn Dirt particle above with upward velocity
                    // Find spawn position (1 cell above the dug cell)
                    int spawnY = y - 1;  // One cell up (lower Y = higher position)
                    if (spawnY >= 0 && world.GetCell(x, spawnY) == Materials.Air)
                    {
                        SpawnDirtWithVelocity(x, spawnY);
                        dirtSpawned++;
                    }
                }
            }

        }

        private void SpawnDirtWithVelocity(int x, int y)
        {
            var world = World;
            int index = y * world.width + x;

            // Create Dirt cell with upward velocity
            Cell cell = world.cells[index];
            cell.materialId = Materials.Dirt;

            // Random upward velocity (negative Y because cell Y increases downward)
            float upwardSpeed = Random.Range(minUpwardVelocity, maxUpwardVelocity);
            float horizontalSpeed = Random.Range(-horizontalSpread, horizontalSpread);

            // Clamp to sbyte range (-128 to 127), typically use -16 to +16
            cell.velocityY = (sbyte)Mathf.Clamp(-upwardSpeed, -16, 16);
            cell.velocityX = (sbyte)Mathf.Clamp(horizontalSpeed, -16, 16);

            world.cells[index] = cell;
            world.MarkDirty(x, y);
        }
    }
}
