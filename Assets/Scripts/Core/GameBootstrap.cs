using System;
using System.Linq;
using UnityEngine;

namespace NationBuilder.Core
{
    /// <summary>
    /// Creates and wires every progression system (node tree, building dex,
    /// building timers, town hall, milestones) at startup, with no scene setup
    /// required - everything lives on one runtime-created "SystemsRoot" object,
    /// separate from the hand-placed GameManager/ResourceManager object.
    /// Also owns save/load, including offline gold accrual.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private const float SaveIntervalSeconds = 30f;
        private const float MaxOfflineSeconds = 12 * 3600f; // cap to avoid absurd numbers before balance is tuned

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<GameBootstrap>() != null) return;

            var root = new GameObject("SystemsRoot");
            DontDestroyOnLoad(root);
            root.AddComponent<GameBootstrap>();
        }

        public NodeTreeManager NodeTree { get; private set; }
        public BuildingDex BuildingDex { get; private set; }
        public BuildingManager BuildingManager { get; private set; }
        public TownHallManager TownHall { get; private set; }
        public MilestoneManager Milestone { get; private set; }

        public double OfflineGoldEarned { get; private set; }

        private void Awake()
        {
            if (ResourceManager.Instance == null)
            {
                Debug.LogError("ResourceManager가 없어서 GameBootstrap을 초기화할 수 없습니다. " +
                                "씬에 GameManager(ResourceManager 컴포넌트) 오브젝트가 있는지 확인하세요.");
                Destroy(gameObject);
                return;
            }

            NodeTree = gameObject.AddComponent<NodeTreeManager>();
            BuildingDex = gameObject.AddComponent<BuildingDex>();
            BuildingManager = gameObject.AddComponent<BuildingManager>();
            TownHall = gameObject.AddComponent<TownHallManager>();
            Milestone = gameObject.AddComponent<MilestoneManager>();

            BuildingManager.Init(BuildingDex);

            NodeTree.OnNodeUnlocked += node => BuildingDex.Register(node.UnlocksBuildingId);
            TownHall.OnLevelUp += level =>
            {
                NodeTree.AddPoints(1);
                Milestone.TriggerMilestone(level, NodeTree, BuildingManager);
            };

            LoadAndApplySave();

            gameObject.AddComponent<DevHudUI>().Init(this);

            InvokeRepeating(nameof(SaveNow), SaveIntervalSeconds, SaveIntervalSeconds);
        }

        private void OnApplicationQuit() => SaveNow();

        private void LoadAndApplySave()
        {
            NationSaveData save = SaveSystem.Load();
            if (save == null) return;

            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long offlineSeconds = Math.Clamp(nowUnix - save.lastActiveUnix, 0, (long)MaxOfflineSeconds);
            OfflineGoldEarned = ResourceManager.Instance.GoldPerSecond * offlineSeconds;
            ResourceManager.Instance.SetGold(save.gold + OfflineGoldEarned);

            NodeTree.RestoreState(save.nodePoints, save.unlockedNodeIds);

            DateTime? townHallCompletesAt = save.townHallUpgradeCompletesAtUnix > 0
                ? DateTimeOffset.FromUnixTimeSeconds(save.townHallUpgradeCompletesAtUnix).UtcDateTime
                : null;
            TownHall.RestoreState(save.townHallLevel, townHallCompletesAt);

            foreach (PlacedBuildingSave b in save.placedBuildings)
            {
                DateTime? completesAt = b.upgradeCompletesAtUnix > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(b.upgradeCompletesAtUnix).UtcDateTime
                    : null;
                BuildingManager.RestoreBuilding(b.buildingId, b.level, completesAt);
            }
        }

        private void SaveNow()
        {
            var save = new NationSaveData
            {
                gold = ResourceManager.Instance.Gold,
                lastActiveUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                nodePoints = NodeTree.Points,
                unlockedNodeIds = NodeTree.UnlockedNodeIds.ToList(),
                townHallLevel = TownHall.Level,
                townHallUpgradeCompletesAtUnix = TownHall.UpgradeCompletesAtUtc.HasValue
                    ? new DateTimeOffset(TownHall.UpgradeCompletesAtUtc.Value).ToUnixTimeSeconds()
                    : 0,
                placedBuildings = BuildingManager.Placed.Select(b => new PlacedBuildingSave
                {
                    buildingId = b.BuildingId,
                    level = b.Level,
                    upgradeCompletesAtUnix = b.UpgradeCompletesAtUtc.HasValue
                        ? new DateTimeOffset(b.UpgradeCompletesAtUtc.Value).ToUnixTimeSeconds()
                        : 0,
                }).ToList(),
            };

            SaveSystem.Save(save);
        }
    }
}
