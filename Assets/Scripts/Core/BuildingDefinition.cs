namespace NationBuilder.Core
{
    /// <summary>One building type, parsed from Resources/buildings.txt.</summary>
    public class BuildingDefinition
    {
        public string Id;
        public string DisplayName;
        public double BuildCostGold;
        public float BuildTimeSeconds;
    }
}
