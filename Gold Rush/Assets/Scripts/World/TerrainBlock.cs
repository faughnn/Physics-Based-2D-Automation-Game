using UnityEngine;
using GoldRush.Core;

namespace GoldRush.World
{
    public class TerrainBlock : MonoBehaviour
    {
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public bool IsDug { get; private set; }

        public void Initialize(int gridX, int gridY)
        {
            GridX = gridX;
            GridY = gridY;
            IsDug = false;
        }

        public void Dig()
        {
            if (IsDug) return;

            IsDug = true;

            // Sand spawning is now handled by SimulationWorld.DigAtWorld()
            // The simulation grid converts terrain cells to sand cells

            // Destroy this visual block
            Destroy(gameObject);
        }
    }
}
