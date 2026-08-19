using System;
using UnityEngine;

namespace NationBuilder.Core
{
    /// <summary>
    /// Town hall (capital) level gate. Leveling up is the game's main progression
    /// milestone: it grants a node tree point and triggers a civilization-style
    /// choice event (see MilestoneManager). Cost/time numbers are rough placeholders.
    /// </summary>
    public class TownHallManager : MonoBehaviour
    {
        private const double BaseCostGold = 300;
        private const float BaseTimeSeconds = 40f;

        public int Level { get; private set; } = 1;
        public DateTime? UpgradeCompletesAtUtc { get; private set; }
        public bool IsUpgrading => UpgradeCompletesAtUtc.HasValue;

        public event Action<int> OnLevelUp;

        public double NextUpgradeCostGold => BaseCostGold * Level;
        public float NextUpgradeTimeSeconds => BaseTimeSeconds * Level;

        private void Update()
        {
            if (!IsUpgrading || DateTime.UtcNow < UpgradeCompletesAtUtc.Value) return;

            Level++;
            UpgradeCompletesAtUtc = null;
            OnLevelUp?.Invoke(Level);
        }

        public bool TryUpgrade()
        {
            if (IsUpgrading) return false;
            if (!ResourceManager.Instance.TrySpendGold(NextUpgradeCostGold)) return false;

            UpgradeCompletesAtUtc = DateTime.UtcNow.AddSeconds(NextUpgradeTimeSeconds);
            return true;
        }

        /// <summary>Used by SaveSystem on load. Bypasses cost checks.</summary>
        public void RestoreState(int level, DateTime? upgradeCompletesAtUtc)
        {
            Level = Math.Max(1, level);
            UpgradeCompletesAtUtc = upgradeCompletesAtUtc;
        }
    }
}
