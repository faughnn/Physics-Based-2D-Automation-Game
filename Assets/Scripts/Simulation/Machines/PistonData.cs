using UnityEngine;

namespace FallingSand
{
    /// <summary>
    /// Data for a single 16x16 piston instance.
    /// The base bar is a permanent strip of PistonBase cells.
    /// The plate is a kinematic cluster that moves within the block.
    /// Fill area grows behind the plate to seal the chamber.
    /// </summary>
    public class PistonData
    {
        public int baseCellX;              // Grid-snapped origin of 16x16 block
        public int baseCellY;
        public PistonDirection direction;

        public GameObject anchorObject;    // Kinematic RB with base + fill colliders
        public ClusterData armCluster;     // The plate cluster (kinematic, 2x16 or 16x2)

        public Vector2 retractedWorldPos;  // Plate world pos when fully retracted (strokeT=0)
        public Vector2 extendedWorldPos;   // Plate world pos when fully extended (strokeT=1)
        public float currentStrokeT;       // Actual 0..1 position, may lag phase if stalled
        public int lastFillExtent;         // Cells of fill behind plate (for delta updates)

        public BoxCollider2D fillCollider; // Dynamic collider covering fill area behind plate

        // Visual rod connecting base center to plate
        public GameObject rodObject;
        public SpriteRenderer rodRenderer;
    }
}
