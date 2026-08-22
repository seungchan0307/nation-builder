using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NationBuilder.Core
{
    /// <summary>
    /// Real-time build/upgrade timers (Clash of Clans style), keyed on absolute UTC
    /// completion timestamps so progress made while the game is closed resolves itself
    /// the moment Update() runs again - no special-cased offline logic needed here.
    /// </summary>
    public class BuildingManager : MonoBehaviour
    {
        public IReadOnlyList<PlacedBuilding> Placed => _placed;
        public event Action<PlacedBuilding> OnBuildingLevelUp;

        /// <summary>Fired only for newly-built buildings (not for ones restored from a save -
        /// those are already in <see cref="Placed"/> by the time listeners subscribe).</summary>
        public event Action<PlacedBuilding> OnBuildingPlaced;

        /// <summary>1 = normal speed, &lt;1 = faster. Set by milestone choices.</summary>
        public float BuildTimeMultiplier { get; set; } = 1f;

        private readonly List<PlacedBuilding> _placed = new();
        private BuildingDex _dex;

        public void Init(BuildingDex dex)
        {
            _dex = dex;
        }

        private void Update()
        {
            foreach (PlacedBuilding building in _placed)
            {
                if (!building.IsUpgrading) continue;
                if (DateTime.UtcNow < building.UpgradeCompletesAtUtc.Value) continue;

                building.Level++;
                building.UpgradeCompletesAtUtc = null;
                OnBuildingLevelUp?.Invoke(building);
            }
        }

        public PlacedBuilding Find(string buildingId) => _placed.FirstOrDefault(b => b.BuildingId == buildingId);

        public bool TryBuild(string buildingId)
        {
            if (Find(buildingId) != null) return false;
            if (!_dex.AllBuildings.TryGetValue(buildingId, out BuildingDefinition def)) return false;
            if (!ResourceManager.Instance.TrySpendGold(def.BuildCostGold)) return false;

            var building = new PlacedBuilding
            {
                BuildingId = buildingId,
                Level = 0,
                UpgradeCompletesAtUtc = DateTime.UtcNow.AddSeconds(def.BuildTimeSeconds * BuildTimeMultiplier),
            };
            _placed.Add(building);
            OnBuildingPlaced?.Invoke(building);
            return true;
        }

        public bool TryUpgrade(string buildingId)
        {
            PlacedBuilding building = Find(buildingId);
            if (building == null || building.IsUpgrading || building.Level < 1) return false;
            if (!_dex.AllBuildings.TryGetValue(buildingId, out BuildingDefinition def)) return false;

            int nextLevel = building.Level + 1;
            double cost = def.BuildCostGold * nextLevel;
            float seconds = def.BuildTimeSeconds * nextLevel * BuildTimeMultiplier;
            if (!ResourceManager.Instance.TrySpendGold(cost)) return false;

            building.UpgradeCompletesAtUtc = DateTime.UtcNow.AddSeconds(seconds);
            return true;
        }

        /// <summary>Used by SaveSystem on load. Bypasses cost checks.</summary>
        public void RestoreBuilding(string buildingId, int level, DateTime? upgradeCompletesAtUtc)
        {
            _placed.Add(new PlacedBuilding
            {
                BuildingId = buildingId,
                Level = level,
                UpgradeCompletesAtUtc = upgradeCompletesAtUtc,
            });
        }
    }
}
