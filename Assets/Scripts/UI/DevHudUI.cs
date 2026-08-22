using System;
using System.Linq;
using UnityEngine;
using NationBuilder.Core;

namespace NationBuilder.UI
{
    /// <summary>
    /// Temporary IMGUI (OnGUI) debug HUD for the systems GameBootstrap wires up:
    /// node tree investment, building dex/build/upgrade, town hall upgrade, and
    /// the milestone choice popup. Deliberately unstyled - swap for real
    /// Canvas/TextMeshPro UI once the art direction is settled.
    /// </summary>
    public class DevHudUI : MonoBehaviour
    {
        private GameBootstrap _game;
        private float _offlineBannerHideAt;

        public void Init(GameBootstrap game)
        {
            _game = game;
            if (game.OfflineGoldEarned > 1)
            {
                _offlineBannerHideAt = Time.unscaledTime + 6f;
            }
        }

        private void OnGUI()
        {
            if (_game == null) return;

            DrawOfflineBanner();
            DrawNodeTreePanel();
            DrawBuildingDexPanel();
            DrawTownHallPanel();
            DrawMilestonePopup();
        }

        private void DrawOfflineBanner()
        {
            if (Time.unscaledTime >= _offlineBannerHideAt) return;

            var rect = new Rect(Screen.width / 2f - 200, 10, 400, 30);
            GUI.Box(rect, $"오프라인 동안 골드 {Mathf.FloorToInt((float)_game.OfflineGoldEarned)} 모았습니다");
        }

        private static readonly string[] CategoryOrder = { "경제", "군사", "기반", "문화", "공용" };

        private Vector2 _nodeTreeScroll;

        private void DrawNodeTreePanel()
        {
            GUILayout.BeginArea(new Rect(10, 10, 260, 480), GUI.skin.box);
            GUILayout.Label($"노드 트리 - 포인트: {_game.NodeTree.Points}");

            string leading = _game.NodeTree.LeadingCategory();
            GUILayout.Label(leading == null ? "나라 성향: 아직 없음" : $"나라 성향: {leading} 중심");

            _nodeTreeScroll = GUILayout.BeginScrollView(_nodeTreeScroll, GUILayout.Height(400));

            foreach (string category in CategoryOrder)
            {
                var nodesInCategory = _game.NodeTree.AllNodes.Where(n => n.Category == category).ToList();
                if (nodesInCategory.Count == 0) continue;

                _game.NodeTree.CategoryInvestment.TryGetValue(category, out int invested);
                GUILayout.Label($"-- {category} ({invested}P 투자) --");

                foreach (TreeNode node in nodesInCategory)
                {
                    DrawNodeRow(node);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawNodeRow(TreeNode node)
        {
            bool unlocked = _game.NodeTree.IsUnlocked(node.Id);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{node.DisplayName} ({node.PointCost}P)", GUILayout.Width(150));

            if (unlocked)
            {
                GUILayout.Label("해금됨");
            }
            else
            {
                GUI.enabled = _game.NodeTree.CanUnlock(node);
                if (GUILayout.Button("해금"))
                {
                    _game.NodeTree.TryUnlock(node.Id);
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
        }

        private Vector2 _buildingDexScroll;

        private void DrawBuildingDexPanel()
        {
            GUILayout.BeginArea(new Rect(280, 10, 300, 480), GUI.skin.box);
            GUILayout.Label("건물 도감");

            _buildingDexScroll = GUILayout.BeginScrollView(_buildingDexScroll, GUILayout.Height(430));

            foreach (string buildingId in _game.BuildingDex.RegisteredIds)
            {
                BuildingDefinition def = _game.BuildingDex.AllBuildings[buildingId];
                PlacedBuilding placed = _game.BuildingManager.Find(buildingId);

                GUILayout.BeginHorizontal();
                GUILayout.Label(def.DisplayName, GUILayout.Width(80));

                if (placed == null)
                {
                    if (GUILayout.Button($"건설 ({def.BuildCostGold}G)"))
                    {
                        _game.BuildingManager.TryBuild(buildingId);
                    }
                }
                else if (placed.IsUpgrading)
                {
                    GUILayout.Label($"{FormatTime(placed.RemainingTime)} 남음");
                }
                else
                {
                    double nextCost = def.BuildCostGold * (placed.Level + 1);
                    GUILayout.Label($"Lv.{placed.Level}");
                    if (GUILayout.Button($"업그레이드 ({nextCost}G)"))
                    {
                        _game.BuildingManager.TryUpgrade(buildingId);
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (_game.BuildingDex.RegisteredIds.Count == 0)
            {
                GUILayout.Label("노드 트리에서 건물을 해금하세요.");
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawTownHallPanel()
        {
            TownHallManager townHall = _game.TownHall;
            GUILayout.BeginArea(new Rect(590, 10, 260, 100), GUI.skin.box);
            GUILayout.Label($"마을회관 Lv.{townHall.Level}");

            if (townHall.IsUpgrading)
            {
                GUILayout.Label($"업그레이드 중... {FormatTime(townHall.UpgradeCompletesAtUtc.Value - DateTime.UtcNow)} 남음");
            }
            else
            {
                if (GUILayout.Button($"업그레이드 ({townHall.NextUpgradeCostGold}G, {townHall.NextUpgradeTimeSeconds}초)"))
                {
                    townHall.TryUpgrade();
                }
            }

            GUILayout.EndArea();
        }

        private void DrawMilestonePopup()
        {
            var choices = _game.Milestone.PendingChoices;
            if (choices == null) return;

            var rect = new Rect(Screen.width / 2f - 200, Screen.height / 2f - 120, 400, 240);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("마을회관이 레벨업했습니다! 방향을 선택하세요.");

            for (int i = 0; i < choices.Count; i++)
            {
                if (GUILayout.Button($"{choices[i].Title} - {choices[i].Description}"))
                {
                    _game.Milestone.Choose(i);
                }
            }

            GUILayout.EndArea();
        }

        private static string FormatTime(TimeSpan span)
        {
            if (span < TimeSpan.Zero) span = TimeSpan.Zero;
            return span.TotalHours >= 1
                ? $"{(int)span.TotalHours}h {span.Minutes}m"
                : $"{span.Minutes}m {span.Seconds}s";
        }
    }
}
