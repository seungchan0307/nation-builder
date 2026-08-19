using System;
using System.Collections.Generic;

namespace NationBuilder.Core
{
    [Serializable]
    public class PlacedBuildingSave
    {
        public string buildingId;
        public int level;
        public long upgradeCompletesAtUnix; // 0 = not upgrading
    }

    [Serializable]
    public class NationSaveData
    {
        public double gold;
        public long lastActiveUnix;

        public int nodePoints;
        public List<string> unlockedNodeIds = new();

        public int townHallLevel = 1;
        public long townHallUpgradeCompletesAtUnix; // 0 = not upgrading

        public List<PlacedBuildingSave> placedBuildings = new();
    }
}
