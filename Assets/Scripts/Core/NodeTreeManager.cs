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

        public event Action<int> OnPointsChanged;
        public event Action<TreeNode> OnNodeUnlocked;

        private readonly HashSet<string> _unlockedNodeIds = new();

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
            OnNodeUnlocked?.Invoke(node);
            return true;
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
