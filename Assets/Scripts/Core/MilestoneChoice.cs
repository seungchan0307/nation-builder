using System;

namespace NationBuilder.Core
{
    /// <summary>One option in a civilization-style milestone choice screen.</summary>
    public class MilestoneChoice
    {
        public string Title;
        public string Description;
        public Action Apply;
    }
}
