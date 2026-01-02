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
                MaterialType.Rock => 5,    // Heavier than sand/gravel
                MaterialType.Gravel => 3,  // Between sand and gold
                MaterialType.Terrain => int.MaxValue,
                _ => 0
            };
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
