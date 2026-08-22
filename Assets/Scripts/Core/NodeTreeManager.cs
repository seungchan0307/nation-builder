using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NationBuilder.Core
{
    /// <summary>
    /// The single shared node tree every player invests points into (PoE-style).
    /// Which region gets points decides the resulting nation's style.
    /// </summary>
    public class NodeTreeManager : MonoBehaviour
    {
        public IReadOnlyList<TreeNode> AllNodes { get; private set; } = new List<TreeNode>();

        public int Points { get; private set; }
        public IReadOnlyCollection<string> UnlockedNodeIds => _unlockedNodeIds;

        /// <summary>Points spent so far per node category (경제/군사/기반/문화/공용) - which
        /// region a player has leaned into is what decides their nation's resulting style.</summary>
        public IReadOnlyDictionary<string, int> CategoryInvestment => _categoryInvestment;

        public event Action<int> OnPointsChanged;
        public event Action<TreeNode> OnNodeUnlocked;

        private readonly HashSet<string> _unlockedNodeIds = new();
        private readonly Dictionary<string, int> _categoryInvestment = new();

        private void Awake()
        {
            AllNodes = LoadNodes();
        }

        public void AddPoints(int amount)
        {
            if (amount <= 0) return;
            Points += amount;
            OnPointsChanged?.Invoke(Points);
        }

        public bool IsUnlocked(string nodeId) => _unlockedNodeIds.Contains(nodeId);

        public bool CanUnlock(TreeNode node)
        {
            if (node == null || IsUnlocked(node.Id)) return false;
            if (Points < node.PointCost) return false;
            return node.PrerequisiteIds.All(IsUnlocked);
        }

        public bool TryUnlock(string nodeId)
        {
            TreeNode node = AllNodes.FirstOrDefault(n => n.Id == nodeId);
            if (!CanUnlock(node)) return false;

            Points -= node.PointCost;
            OnPointsChanged?.Invoke(Points);
            _unlockedNodeIds.Add(node.Id);
            TrackCategoryInvestment(node);
            OnNodeUnlocked?.Invoke(node);
            return true;
        }

        /// <summary>Category with the most points invested so far, or null if nothing unlocked yet.</summary>
        public string LeadingCategory()
        {
            string best = null;
            int bestPoints = 0;
            foreach (var kv in _categoryInvestment)
            {
                if (kv.Value <= bestPoints) continue;
                best = kv.Key;
                bestPoints = kv.Value;
            }
            return best;
        }

        private void TrackCategoryInvestment(TreeNode node)
        {
            if (string.IsNullOrEmpty(node.Category)) return;
            _categoryInvestment.TryGetValue(node.Category, out int current);
            _categoryInvestment[node.Category] = current + node.PointCost;
        }

        /// <summary>Used by SaveSystem on load. Skips cost/prereq checks but still notifies listeners
        /// (e.g. BuildingDex) so restored nodes re-register their building.</summary>
        public void RestoreState(int points, IEnumerable<string> unlockedNodeIds)
        {
            Points = points;
            OnPointsChanged?.Invoke(Points);

            foreach (string id in unlockedNodeIds)
            {
                TreeNode node = AllNodes.FirstOrDefault(n => n.Id == id);
                if (node == null || !_unlockedNodeIds.Add(node.Id)) continue;
                TrackCategoryInvestment(node);
                OnNodeUnlocked?.Invoke(node);
            }
        }

        private static List<TreeNode> LoadNodes()
        {
            var nodes = new List<TreeNode>();
            TextAsset asset = Resources.Load<TextAsset>("node-tree");
            if (asset == null)
            {
                Debug.LogError("Resources/node-tree.txt를 찾을 수 없습니다.");
                return nodes;
            }

            foreach (string rawLine in asset.text.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                string[] parts = line.Split(',');
                if (parts.Length < 5) continue;

                var node = new TreeNode
                {
                    Id = parts[0].Trim(),
                    DisplayName = parts[1].Trim(),
                    PointCost = int.Parse(parts[2].Trim()),
                    UnlocksBuildingId = parts[4].Trim(),
                    Category = parts.Length > 5 ? parts[5].Trim() : "",
                };

                string prereq = parts[3].Trim();
                if (prereq != "-" && prereq.Length > 0)
                {
                    node.PrerequisiteIds = prereq.Split('|').Select(p => p.Trim()).ToList();
                }

                nodes.Add(node);
            }

            return nodes;
        }
    }
}
