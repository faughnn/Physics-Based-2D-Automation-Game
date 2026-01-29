namespace FallingSand
{
    public enum ItemCategory
    {
        Tool,
        Structure,
    }

    public class ItemDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public ItemCategory Category;
        public ToolType ToolType;
        public StructureType StructureType;
    }
}
