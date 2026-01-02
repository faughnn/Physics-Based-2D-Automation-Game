using UnityEngine;

namespace GoldRush.Core
{
    public static class LayerSetup
    {
        // Layer indices (Unity has layers 0-31, we use 8+ for custom layers)
        // Note: Layers must be defined in Unity Editor or via code at runtime
        // We'll use the following layer assignments:
        public const int DefaultLayer = 0;
        public const int PlayerLayer = 8;
        public const int TerrainLayer = 9;
        public const int SandLayer = 10;
        public const int WetSandLayer = 11;
        public const int GoldLayer = 12;
        public const int SlagLayer = 13;
        public const int WaterLayer = 14;
        public const int InfrastructureLayer = 15;

        // Layer names for reference
        public const string PlayerLayerName = "Player";
        public const string TerrainLayerName = "Terrain";
        public const string SandLayerName = "Sand";
        public const string WetSandLayerName = "WetSand";
        public const string GoldLayerName = "Gold";
        public const string SlagLayerName = "Slag";
        public const string WaterLayerName = "Water";
        public const string InfrastructureLayerName = "Infrastructure";

        public static void Initialize()
        {
            // Set gravity
            Physics2D.gravity = GameSettings.Gravity;

            // Configure collision matrix
            // By default, all layers collide with all layers
            // We need to set up specific ignore rules

            // Reset all collision settings first (enable all collisions)
            for (int i = 0; i < 32; i++)
            {
                for (int j = i; j < 32; j++)
                {
                    Physics2D.IgnoreLayerCollision(i, j, false);
                }
            }

            // Player collides with: Terrain, Infrastructure
            // Player does NOT collide with: Sand, WetSand, Gold, Slag, Water
            Physics2D.IgnoreLayerCollision(PlayerLayer, SandLayer, true);
            Physics2D.IgnoreLayerCollision(PlayerLayer, WetSandLayer, true);
            Physics2D.IgnoreLayerCollision(PlayerLayer, GoldLayer, true);
            Physics2D.IgnoreLayerCollision(PlayerLayer, SlagLayer, true);
            Physics2D.IgnoreLayerCollision(PlayerLayer, WaterLayer, true);

            // Sand collides with: Terrain, Infrastructure, Water
            // Sand does NOT collide with: Player, WetSand, Gold, Slag, other Sand (performance)
            Physics2D.IgnoreLayerCollision(SandLayer, SandLayer, true);  // Prevent O(n²) collisions
            Physics2D.IgnoreLayerCollision(SandLayer, WetSandLayer, true);
            Physics2D.IgnoreLayerCollision(SandLayer, GoldLayer, true);
            Physics2D.IgnoreLayerCollision(SandLayer, SlagLayer, true);

            // WetSand collides with: Terrain, Infrastructure, WetSand
            // WetSand does NOT collide with: Player, Sand, Gold, Slag, Water
            Physics2D.IgnoreLayerCollision(WetSandLayer, GoldLayer, true);
            Physics2D.IgnoreLayerCollision(WetSandLayer, SlagLayer, true);
            Physics2D.IgnoreLayerCollision(WetSandLayer, WaterLayer, true);

            // Gold collides with: Terrain, Infrastructure, GoldStore (via trigger)
            // Gold does NOT collide with: Player, Sand, WetSand, Slag, Water
            Physics2D.IgnoreLayerCollision(GoldLayer, SlagLayer, true);
            Physics2D.IgnoreLayerCollision(GoldLayer, WaterLayer, true);

            // Slag collides with: Terrain, Infrastructure
            // Slag does NOT collide with: Player, Sand, WetSand, Gold, Water
            Physics2D.IgnoreLayerCollision(SlagLayer, WaterLayer, true);

            // Water collides with: Terrain, Water, Sand (for absorption)
            // Water does NOT collide with: Player, WetSand, Gold, Slag, Infrastructure
            Physics2D.IgnoreLayerCollision(WaterLayer, InfrastructureLayer, true);

        }

        // Helper to get layer mask for raycasting
        public static int GetTerrainMask()
        {
            return 1 << TerrainLayer;
        }

        public static int GetInfrastructureMask()
        {
            return 1 << InfrastructureLayer;
        }

        public static int GetGroundMask()
        {
            return (1 << TerrainLayer) | (1 << InfrastructureLayer);
        }
    }
}
