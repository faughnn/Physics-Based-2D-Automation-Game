using System.Collections.Generic;
using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Common interface for structure managers.
    /// Enables registration-based iteration in SimulationManager and GhostStructureRenderer.
    /// </summary>
    public interface IStructureManager
    {
        /// <summary>
        /// Update ghost block states (activate blocks where terrain has cleared).
        /// </summary>
        void UpdateGhostStates();

        /// <summary>
        /// Populate the list with cell positions of ghost blocks for rendering.
        /// </summary>
        void GetGhostBlockPositions(List<Vector2Int> positions);

        /// <summary>
        /// Check if this manager has a structure tile at the given cell position.
        /// </summary>
        bool HasStructureAt(int x, int y);

        /// <summary>
        /// Color used for ghost block overlay rendering.
        /// </summary>
        Color GhostColor { get; }
    }
}
