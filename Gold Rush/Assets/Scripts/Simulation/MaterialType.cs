using UnityEngine;

namespace GoldRush.Simulation
{
    public enum MaterialType : byte
    {
        Air = 0,
        Water = 1,
        Sand = 2,
        WetSand = 3,
        Gold = 4,
        Slag = 5,
        Rock = 6,      // Heavy, falls straight down, no spreading
        Gravel = 7,    // Falls like sand, slight spreading
        Ore = 8,       // Gold-bearing ore, heavy like rock, found in deep veins
        Concentrate = 9, // Refined heavy minerals from crushing ore
        Coal = 10,     // Fuel for smelting, found in terrain veins
        Boulder = 11,  // 8x8 cluster (16x16 pixels), largest rock type
        Terrain = 255  // Static solid, never simulated
    }

    public static class MaterialProperties
    {
        // Density determines what sinks through what
        // Higher density sinks through lower density
        public static int GetDensity(MaterialType type)
        {
            return type switch
            {
                MaterialType.Air => 0,
                MaterialType.Water => 1,
                MaterialType.Sand => 2,
                MaterialType.WetSand => 3,
                MaterialType.Gold => 4,
                MaterialType.Slag => 2,
                MaterialType.Rock => 2,    // Same as sand - stays in place
                MaterialType.Gravel => 2,  // Same as sand - stays in place
                MaterialType.Ore => 2,     // Same as sand - stays in place
                MaterialType.Concentrate => 2, // Same as sand - stays in place
                MaterialType.Coal => 2,    // Same as sand - stays in place
                MaterialType.Boulder => 2, // Same as sand - clusters handle movement
                MaterialType.Terrain => int.MaxValue,
                _ => 0
            };
        }

        // Returns the cluster size for this material (0 = not a cluster type)
        // Sizes are in simulation cells (each cell = 2 pixels)
        public static int GetClusterSize(MaterialType type)
        {
            return type switch
            {
                MaterialType.Boulder => 8,  // 8x8 sim cells = 16x16 pixels (fits 64px big crusher)
                MaterialType.Rock => 4,     // 4x4 sim cells = 8x8 pixels (fits 32px small crusher)
                MaterialType.Gravel => 2,   // 2x2 sim cells = 4x4 pixels (needs grinder)
                _ => 0                      // Not a cluster type
            };
        }

        // Is this material a cluster type that should move as a unit?
        public static bool IsClusterMaterial(MaterialType type)
        {
            return GetClusterSize(type) > 0;
        }

        // Colors for rendering
        public static Color32 GetColor(MaterialType type)
        {
            return type switch
            {
                MaterialType.Air => new Color32(0, 0, 0, 0),               // Transparent (camera bg shows through)
                MaterialType.Water => new Color32(51, 128, 230, 200),    // Blue, semi-transparent
                MaterialType.Sand => new Color32(194, 178, 128, 255),    // Tan/beige
                MaterialType.WetSand => new Color32(102, 77, 51, 255),   // Dark brown
                MaterialType.Gold => new Color32(255, 215, 0, 255),      // Gold yellow
                MaterialType.Slag => new Color32(128, 128, 128, 255),    // Grey
                MaterialType.Rock => new Color32(90, 90, 90, 255),       // Dark grey rock
                MaterialType.Gravel => new Color32(160, 140, 120, 255),  // Light brownish grey
                MaterialType.Ore => new Color32(139, 119, 101, 255),     // Rusty brown with metallic hint
                MaterialType.Concentrate => new Color32(178, 134, 0, 255), // Dark gold/bronze
                MaterialType.Coal => new Color32(40, 40, 40, 255),       // Black
                MaterialType.Boulder => new Color32(70, 70, 75, 255),   // Dark grey-blue
                MaterialType.Terrain => new Color32(139, 90, 43, 255),   // Brown
                _ => new Color32(255, 0, 255, 255)                       // Magenta for debug
            };
        }

        // Does this material fall/flow?
        public static bool IsSimulated(MaterialType type)
        {
            return type != MaterialType.Air && type != MaterialType.Terrain;
        }

        // Does this material spread horizontally like liquid?
        public static bool IsSpreading(MaterialType type)
        {
            return type == MaterialType.Water;
        }

        // How far can this material spread per frame?
        public static int GetSpreadDistance(MaterialType type)
        {
            return type switch
            {
                MaterialType.Water => 4,
                _ => 0
            };
        }

        // Can this material displace the other material? (sink through it)
        public static bool CanDisplace(MaterialType self, MaterialType other)
        {
            if (other == MaterialType.Terrain) return false;
            if (other == MaterialType.Air) return true;
            return GetDensity(self) > GetDensity(other);
        }
    }
}
