using System.Collections.Generic;
using System.Linq;

namespace FallingSand
{
    public static class ItemRegistry
    {
        public static readonly ItemDefinition Grabber = new ItemDefinition
        {
            Id = "grabber",
            DisplayName = "Grabber",
            Description = "Grab and carry loose materials",
            Category = ItemCategory.Tool,
            ToolType = ToolType.Grabber,
        };

        public static readonly ItemDefinition Shovel = new ItemDefinition
        {
            Id = "shovel",
            DisplayName = "Shovel",
            Description = "Dig terrain and carry loose dirt",
            Category = ItemCategory.Tool,
            ToolType = ToolType.Shovel,
        };

        public static readonly ItemDefinition Belt = new ItemDefinition
        {
            Id = "belt",
            DisplayName = "Conveyor Belt",
            Description = "Automatically moves materials horizontally",
            Category = ItemCategory.Structure,
            StructureType = StructureType.Belt,
        };

        public static readonly ItemDefinition Lift = new ItemDefinition
        {
            Id = "lift",
            DisplayName = "Lift",
            Description = "Moves materials vertically",
            Category = ItemCategory.Structure,
            StructureType = StructureType.Lift,
        };

        public static readonly ItemDefinition Wall = new ItemDefinition
        {
            Id = "wall",
            DisplayName = "Wall",
            Description = "Blocks material flow",
            Category = ItemCategory.Structure,
            StructureType = StructureType.Wall,
        };

        public static readonly List<ItemDefinition> All = new List<ItemDefinition>
            { Grabber, Shovel, Belt, Lift, Wall };

        public static IEnumerable<ItemDefinition> Tools =>
            All.Where(i => i.Category == ItemCategory.Tool);

        public static IEnumerable<ItemDefinition> Structures =>
            All.Where(i => i.Category == ItemCategory.Structure);
    }
}
