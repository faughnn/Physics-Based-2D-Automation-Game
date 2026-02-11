using System;

namespace FallingSand
{
    /// <summary>
    /// Coordinates all machine types (pistons, and future machines).
    /// Delegates to individual machine managers for type-specific logic.
    /// </summary>
    public class MachineManager : IDisposable
    {
        public PistonManager Pistons { get; private set; }

        public void Initialize(CellWorld world, ClusterManager clusterManager, TerrainColliderManager terrainColliders)
        {
            Pistons = new PistonManager();
            Pistons.Initialize(world, clusterManager, terrainColliders);
        }

        /// <summary>
        /// Updates all machine motors. Called each physics step.
        /// </summary>
        public void UpdateMotors()
        {
            Pistons.UpdateMotors();
        }

        /// <summary>
        /// Updates all machine visuals. Called each frame.
        /// </summary>
        public void UpdateVisuals()
        {
            Pistons.UpdateVisuals();
        }

        /// <summary>
        /// Checks if any machine occupies the given cell.
        /// </summary>
        public bool HasMachineAt(int cellX, int cellY)
        {
            return Pistons.HasPistonAt(cellX, cellY);
        }

        public void Dispose()
        {
            Pistons?.Dispose();
        }
    }
}
