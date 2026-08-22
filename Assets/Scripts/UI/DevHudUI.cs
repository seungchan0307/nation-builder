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
    ///
    /// Node tree / building dex are togglable panels (buttons top-left open them,
    /// X closes them) so the 3D town underneath is visible when they're closed.
    /// </summary>
    public class DevHudUI : MonoBehaviour
    {
        private static readonly string[] CategoryOrder = { "경제", "군사", "기반", "문화", "공용" };
        private static readonly string[] FilterOptions = { "전체", "경제", "군사", "기반", "문화", "공용" };

        private GameBootstrap _game;
        private float _offlineBannerHideAt;

        private bool _showNodeTree;
        private bool _showBuildingDex;
        private int _nodeFilterIndex;
        private int _buildingFilterIndex;
        private Vector2 _nodeTreeScroll;
        private Vector2 _buildingDexScroll;

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
            DrawMenuButtons();
            DrawTownHallPanel();
            if (_showNodeTree) DrawNodeTreePanel();
            if (_showBuildingDex) DrawBuildingDexPanel();
            DrawMilestonePopup();
        }

        private void DrawOfflineBanner()
        {
            if (Time.unscaledTime >= _offlineBannerHideAt) return;

            var rect = new Rect(Screen.width / 2f - 200, 10, 400, 30);
            GUI.Box(rect, $"오프라인 동안 골드 {Mathf.FloorToInt((float)_game.OfflineGoldEarned)} 모았습니다");
        }

        private void DrawMenuButtons()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 30));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_showNodeTree ? "노드 트리 ▲" : "노드 트리 ▼", GUILayout.Width(140)))
            {
                _showNodeTree = !_showNodeTree;
            }
            if (GUILayout.Button(_showBuildingDex ? "건물 도감 ▲" : "건물 도감 ▼", GUILayout.Width(140)))
            {
                _showBuildingDex = !_showBuildingDex;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawPanelHeader(string title, Action onClose)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                onClose();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawFilterRow(ref int selectedIndex)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < FilterOptions.Length; i++)
            {
                string label = i == selectedIndex ? $"[{FilterOptions[i]}]" : FilterOptions[i];
                if (GUILayout.Button(label))
                {
                    selectedIndex = i;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawNodeTreePanel()
        {
            GUILayout.BeginArea(new Rect(10, 50, 280, 470), GUI.skin.box);
            DrawPanelHeader($"노드 트리 - 포인트: {_game.NodeTree.Points}", () => _showNodeTree = false);

            string leading = _game.NodeTree.LeadingCategory();
            GUILayout.Label(leading == null ? "나라 성향: 아직 없음" : $"나라 성향: {leading} 중심");

            DrawFilterRow(ref _nodeFilterIndex);
            string filter = FilterOptions[_nodeFilterIndex];

            _nodeTreeScroll = GUILayout.BeginScrollView(_nodeTreeScroll, GUILayout.Height(360));

            foreach (string category in CategoryOrder)
            {
                if (filter != "전체" && filter != category) continue;

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

        private void DrawBuildingDexPanel()
        {
            GUILayout.BeginArea(new Rect(300, 50, 320, 470), GUI.skin.box);
            DrawPanelHeader("건물 도감", () => _showBuildingDex = false);

            DrawFilterRow(ref _buildingFilterIndex);
            string filter = FilterOptions[_buildingFilterIndex];

            _buildingDexScroll = GUILayout.BeginScrollView(_buildingDexScroll, GUILayout.Height(400));

            foreach (string buildingId in _game.BuildingDex.RegisteredIds)
            {
                if (filter != "전체" && filter != CategoryOfBuilding(buildingId)) continue;

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

        private string CategoryOfBuilding(string buildingId)
        {
            foreach (TreeNode node in _game.NodeTree.AllNodes)
            {
                if (node.UnlocksBuildingId == buildingId) return node.Category;
            }
            return null;
        }

        private void DrawTownHallPanel()
        {
            TownHallManager townHall = _game.TownHall;
            GUILayout.BeginArea(new Rect(Screen.width - 270, 50, 260, 100), GUI.skin.box);
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
