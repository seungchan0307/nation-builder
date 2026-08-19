using System;

namespace NationBuilder.Core
{
    /// <summary>A building the player has actually built (or is building/upgrading).</summary>
    public class PlacedBuilding
    {
        public string BuildingId;

        /// <summary>0 while under initial construction; 1+ once at least one build/upgrade has completed.</summary>
        public int Level;

        public DateTime? UpgradeCompletesAtUtc;

        public bool IsUpgrading => UpgradeCompletesAtUtc.HasValue;

        public TimeSpan RemainingTime =>
            UpgradeCompletesAtUtc.HasValue
                ? UpgradeCompletesAtUtc.Value - DateTime.UtcNow
                : TimeSpan.Zero;
    }
}
