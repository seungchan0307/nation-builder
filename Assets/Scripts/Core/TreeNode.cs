using System.Collections.Generic;

namespace NationBuilder.Core
{
    /// <summary>One node in the shared node tree, parsed from Resources/node-tree.txt.</summary>
    public class TreeNode
    {
        public string Id;
        public string DisplayName;
        public int PointCost;
        public List<string> PrerequisiteIds = new();
        public string UnlocksBuildingId;
        public string Category;
    }
}
