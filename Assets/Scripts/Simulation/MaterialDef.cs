using System.Runtime.InteropServices;
using UnityEngine;

namespace FallingSand
{
    public enum BehaviourType : byte
    {
        Static = 0,    // Never moves (stone, structure)
        Powder = 1,    // Falls, piles (sand, ore)
        Liquid = 2,    // Falls, spreads horizontally (water, oil)
        Gas = 3,       // Rises, disperses (steam, smoke)
    }

    public static class MaterialFlags
    {
        public const byte None         = 0;
        public const byte ConductsHeat = 1 << 0;
        public const byte Flammable    = 1 << 1;
        public const byte Conductive   = 1 << 2;  // Electricity
        public const byte Corrodes     = 1 << 3;  // Acid-like
        public const byte Diggable     = 1 << 4;  // Can be excavated by player
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialDef
    {
        public byte density;            // For displacement (0-255, higher sinks)
        public byte slideResistance;    // Powder only: resistance to diagonal sliding (0-255, higher = steeper piles)
        public BehaviourType behaviour; // Powder, Liquid, Gas, Static
        public byte flags;              // MaterialFlags

        public byte ignitionTemp;       // Temperature to catch fire (0 = won't burn)
        public byte meltTemp;           // Temperature to melt (0 = won't melt)
        public byte freezeTemp;         // Temperature to solidify (255 = won't freeze)
        public byte boilTemp;           // Temperature to evaporate (0 = won't boil)

        public byte materialOnMelt;     // Material ID when melted
        public byte materialOnFreeze;   // Material ID when frozen
        public byte materialOnBurn;     // Material ID when burned (ash, smoke)
        public byte materialOnBoil;     // Material ID when boiled (steam)

        public Color32 baseColour;      // RGBA for rendering
        public byte colourVariation;    // Random variation amount
        public byte dispersionRate;     // Base horizontal spread for liquids (0-255)
        public byte emission;           // Glow intensity for emissive materials (0-255)
        public byte padding3;
    }

    public static class Materials
    {
        public const byte Air = 0;
        public const byte Stone = 1;
        public const byte Sand = 2;
        public const byte Water = 3;
        public const byte Oil = 4;
        public const byte Steam = 5;
        public const byte IronOre = 6;
        public const byte MoltenIron = 7;
        public const byte Iron = 8;
        public const byte Coal = 9;
        public const byte Ash = 10;
        public const byte Smoke = 11;
        public const byte Belt = 12;       // Belt structure tile (base, unused directly)
        public const byte BeltLeft = 13;   // Belt moving left (dark stripe)
        public const byte BeltRight = 14;  // Belt moving right (dark stripe)
        public const byte BeltLeftLight = 15;   // Belt moving left (light stripe)
        public const byte BeltRightLight = 16;  // Belt moving right (light stripe)
        public const byte Dirt = 17;            // Heavy powder that piles steeply
        public const byte Ground = 18;          // Static diggable terrain

        public const int Count = 256;  // Maximum materials

        /// <summary>
        /// Checks if a material ID represents any type of belt.
        /// </summary>
        public static bool IsBelt(byte materialId)
        {
            return materialId == Belt ||
                   materialId == BeltLeft ||
                   materialId == BeltRight ||
                   materialId == BeltLeftLight ||
                   materialId == BeltRightLight;
        }

        public static bool IsDiggable(MaterialDef mat)
        {
            return (mat.flags & MaterialFlags.Diggable) != 0;
        }

        public static MaterialDef[] CreateDefaults()
        {
            var defs = new MaterialDef[Count];

            // Air - empty space
            defs[Air] = new MaterialDef
            {
                density = 0,
                slideResistance = 0,
                behaviour = BehaviourType.Static,
                flags = MaterialFlags.None,
                baseColour = new Color32(20, 20, 30, 255),  // Dark background
                colourVariation = 0,
            };

            // Stone - immovable solid
            defs[Stone] = new MaterialDef
            {
                density = 255,
                slideResistance = 0,
                behaviour = BehaviourType.Static,
                flags = MaterialFlags.ConductsHeat,
                baseColour = new Color32(100, 100, 105, 255),
                colourVariation = 10,
            };

            // Sand - falling powder
            defs[Sand] = new MaterialDef
            {
                density = 128,
                slideResistance = 0,  // No resistance - always tries to slide (preserves original behavior)
                behaviour = BehaviourType.Powder,
                flags = MaterialFlags.None,
                baseColour = new Color32(194, 178, 128, 255),
                colourVariation = 15,
            };

            // Water - flowing liquid
            defs[Water] = new MaterialDef
            {
                density = 64,
                slideResistance = 5,
                behaviour = BehaviourType.Liquid,
                flags = MaterialFlags.ConductsHeat,
                boilTemp = 100,
                materialOnBoil = Steam,
                baseColour = new Color32(32, 64, 192, 255),
                colourVariation = 10,
                dispersionRate = 5,  // How far water spreads horizontally
            };

            // Oil - heavier liquid
            defs[Oil] = new MaterialDef
            {
                density = 48,
                slideResistance = 15,
                behaviour = BehaviourType.Liquid,
                flags = MaterialFlags.Flammable,
                ignitionTemp = 80,
                materialOnBurn = Smoke,
                baseColour = new Color32(80, 60, 20, 255),
                colourVariation = 5,
                dispersionRate = 4,  // Oil spreads less than water
            };

            // Steam - rising gas
            defs[Steam] = new MaterialDef
            {
                density = 4,
                slideResistance = 2,
                behaviour = BehaviourType.Gas,
                flags = MaterialFlags.ConductsHeat,
                freezeTemp = 50,
                materialOnFreeze = Water,
                baseColour = new Color32(200, 200, 220, 255),
                colourVariation = 20,
            };

            // Belt - conveyor belt structure tile (base, not used directly)
            defs[Belt] = new MaterialDef
            {
                density = 255,
                slideResistance = 255,
                behaviour = BehaviourType.Static,
                flags = MaterialFlags.None,
                baseColour = new Color32(60, 60, 70, 255),  // Dark gray
                colourVariation = 0,
            };

            // Belt moving left - dark stripe (chevron pattern)
            defs[BeltLeft] = new MaterialDef
            {
                density = 255,
                slideResistance = 255,
                behaviour = BehaviourType.Static,
                flags = MaterialFlags.None,
                baseColour = new Color32(50, 50, 60, 255),  // Darker gray
                colourVariation = 0,
            };

            // Belt moving right - dark stripe (chevron pattern)
            defs[BeltRight] = new MaterialDef
            {
                density = 255,
                slideResistance = 255,
                behaviour = BehaviourType.Static,
                flags = MaterialFlags.None,
                baseColour = new Color32(50, 50, 60, 255),  // Darker gray
                colourVariation = 0,
            };

            // Belt moving left - light stripe (chevron pattern)
            defs[BeltLeftLight] = new MaterialDef
            {
                density = 255,
                slideResistance = 255,
                behaviour = BehaviourType.Static,
                flags = MaterialFlags.None,
                baseColour = new Color32(80, 80, 95, 255),  // Lighter gray
                colourVariation = 0,
            };

            // Belt moving right - light stripe (chevron pattern)
            defs[BeltRightLight] = new MaterialDef
            {
                density = 255,
                slideResistance = 255,
                behaviour = BehaviourType.Static,
                flags = MaterialFlags.None,
                baseColour = new Color32(80, 80, 95, 255),  // Lighter gray
                colourVariation = 0,
            };

            // Dirt - heavy powder that piles steeply
            defs[Dirt] = new MaterialDef
            {
                density = 140,                              // Heavier than sand (128)
                slideResistance = 200,                      // High resistance - piles steeply
                behaviour = BehaviourType.Powder,
                flags = MaterialFlags.None,
                baseColour = new Color32(139, 90, 43, 255), // Brown
                colourVariation = 12,
            };

            // Ground - static diggable terrain
            defs[Ground] = new MaterialDef
            {
                density = 255,
                behaviour = BehaviourType.Static,
                flags = MaterialFlags.ConductsHeat | MaterialFlags.Diggable,
                baseColour = new Color32(92, 64, 51, 255),  // Dark brown
                colourVariation = 8,
            };

            return defs;
        }
    }
}
