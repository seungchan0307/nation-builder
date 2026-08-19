using System;
using System.Collections.Generic;
using UnityEngine;

namespace NationBuilder.Core
{
    /// <summary>
    /// All building types that exist in the game (loaded from Resources/buildings.txt),
    /// plus which of them have been unlocked (registered) via the node tree so far.
    /// </summary>
    public class BuildingDex : MonoBehaviour
    {
        public IReadOnlyDictionary<string, BuildingDefinition> AllBuildings { get; private set; }
            = new Dictionary<string, BuildingDefinition>();

        public IReadOnlyCollection<string> RegisteredIds => _registeredIds;
        public event Action<BuildingDefinition> OnBuildingRegistered;

        private readonly HashSet<string> _registeredIds = new();

        private void Awake()
        {
            AllBuildings = LoadBuildings();
        }

        public bool IsRegistered(string buildingId) => _registeredIds.Contains(buildingId);

        public void Register(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId) || !AllBuildings.ContainsKey(buildingId)) return;
            if (!_registeredIds.Add(buildingId)) return;
            OnBuildingRegistered?.Invoke(AllBuildings[buildingId]);
        }

        private static Dictionary<string, BuildingDefinition> LoadBuildings()
        {
            var buildings = new Dictionary<string, BuildingDefinition>();
            TextAsset asset = Resources.Load<TextAsset>("buildings");
            if (asset == null)
            {
                Debug.LogError("Resources/buildings.txt를 찾을 수 없습니다.");
                return buildings;
            }

            foreach (string rawLine in asset.text.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                string[] parts = line.Split(',');
                if (parts.Length < 4) continue;

                var def = new BuildingDefinition
                {
                    Id = parts[0].Trim(),
                    DisplayName = parts[1].Trim(),
                    BuildCostGold = double.Parse(parts[2].Trim()),
                    BuildTimeSeconds = float.Parse(parts[3].Trim()),
                };
                buildings[def.Id] = def;
            }

            return buildings;
        }
    }
}
