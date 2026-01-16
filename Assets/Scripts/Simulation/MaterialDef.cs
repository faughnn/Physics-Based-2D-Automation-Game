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
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialDef
    {
        public byte density;            // For displacement (0-255, higher sinks)
        public byte friction;           // Affects horizontal spread (0-255)
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
        public byte padding2;
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

        public const int Count = 256;  // Maximum materials

        public static MaterialDef[] CreateDefaults()
        {
            var defs = new MaterialDef[Count];

            // Air - empty space
            defs[Air] = new MaterialDef
            {
                density = 0,
                friction = 0,
                behaviour = BehaviourType.Static,
                flags = MaterialFlags.None,
                baseColour = new Color32(20, 20, 30, 255),  // Dark background
                colourVariation = 0,
            };

            // Stone - immovable solid
            defs[Stone] = new MaterialDef
            {
                density = 255,
                friction = 255,
                behaviour = BehaviourType.Static,
                flags = MaterialFlags.ConductsHeat,
                baseColour = new Color32(100, 100, 105, 255),
                colourVariation = 10,
            };

            // Sand - falling powder
            defs[Sand] = new MaterialDef
            {
                density = 128,
                friction = 20,
                behaviour = BehaviourType.Powder,
                flags = MaterialFlags.None,
                baseColour = new Color32(194, 178, 128, 255),
                colourVariation = 15,
            };

            // Water - flowing liquid
            defs[Water] = new MaterialDef
            {
                density = 64,
                friction = 5,
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
                friction = 15,
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
                friction = 2,
                behaviour = BehaviourType.Gas,
                flags = MaterialFlags.ConductsHeat,
                freezeTemp = 50,
                materialOnFreeze = Water,
                baseColour = new Color32(200, 200, 220, 255),
                colourVariation = 20,
            };

            return defs;
        }
    }
}
