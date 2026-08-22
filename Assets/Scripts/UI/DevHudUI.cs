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
    /// Colors/rounded backgrounds come from DevHudSkin - kept separate so the
    /// layout logic here doesn't get buried in style code.
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

            DevHudSkin.EnsureBuilt();

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

            var rect = new Rect(Screen.width / 2f - 220, 10, 440, 34);
            GUI.backgroundColor = DevHudSkin.PanelTint;
            GUI.Box(rect, string.Empty, DevHudSkin.Panel);
            GUI.backgroundColor = Color.white;

            var labelRect = new Rect(rect.x, rect.y + 6, rect.width, rect.height);
            GUI.Label(labelRect, $"오프라인 동안 골드 {Mathf.FloorToInt((float)_game.OfflineGoldEarned)} 모았습니다",
                CenteredStyle(DevHudSkin.Body));
        }

        private void DrawMenuButtons()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 34));
            GUILayout.BeginHorizontal();

            GUI.backgroundColor = _showNodeTree ? DevHudSkin.ButtonOnTint : DevHudSkin.ButtonTint;
            if (GUILayout.Button(_showNodeTree ? "노드 트리 ▲" : "노드 트리 ▼", DevHudSkin.Chip, GUILayout.Width(140)))
            {
                _showNodeTree = !_showNodeTree;
            }

            GUI.backgroundColor = _showBuildingDex ? DevHudSkin.ButtonOnTint : DevHudSkin.ButtonTint;
            if (GUILayout.Button(_showBuildingDex ? "건물 도감 ▲" : "건물 도감 ▼", DevHudSkin.Chip, GUILayout.Width(140)))
            {
                _showBuildingDex = !_showBuildingDex;
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawPanelHeader(string title, Action onClose)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, DevHudSkin.Header, GUILayout.ExpandWidth(true));

            GUI.backgroundColor = DevHudSkin.ButtonTint;
            if (GUILayout.Button("X", DevHudSkin.Chip, GUILayout.Width(26)))
            {
                onClose();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndHorizontal();
        }

        private void DrawFilterRow(ref int selectedIndex)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < FilterOptions.Length; i++)
            {
                bool selected = i == selectedIndex;
                Color tint = selected
                    ? (FilterOptions[i] == "전체" ? DevHudSkin.ButtonOnTint : DevHudSkin.CategoryColor(FilterOptions[i]))
                    : DevHudSkin.ButtonTint;

                GUI.backgroundColor = tint;
                if (GUILayout.Button(FilterOptions[i], DevHudSkin.Chip))
                {
                    selectedIndex = i;
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }

        private void DrawNodeTreePanel()
        {
            GUI.backgroundColor = DevHudSkin.PanelTint;
            GUILayout.BeginArea(new Rect(10, 50, 280, 470), DevHudSkin.Panel);
            GUI.backgroundColor = Color.white;

            DrawPanelHeader($"노드 트리  ·  {_game.NodeTree.Points}P", () => _showNodeTree = false);

            string leading = _game.NodeTree.LeadingCategory();
            GUI.contentColor = leading == null ? DevHudSkin.Dim : DevHudSkin.CategoryColor(leading);
            GUILayout.Label(leading == null ? "나라 성향: 아직 없음" : $"나라 성향: {leading} 중심", DevHudSkin.SubHeader);
            GUI.contentColor = Color.white;

            DrawFilterRow(ref _nodeFilterIndex);
            string filter = FilterOptions[_nodeFilterIndex];

            _nodeTreeScroll = GUILayout.BeginScrollView(_nodeTreeScroll, GUILayout.Height(350));

            foreach (string category in CategoryOrder)
            {
                if (filter != "전체" && filter != category) continue;

                var nodesInCategory = _game.NodeTree.AllNodes.Where(n => n.Category == category).ToList();
                if (nodesInCategory.Count == 0) continue;

                _game.NodeTree.CategoryInvestment.TryGetValue(category, out int invested);
                GUI.contentColor = DevHudSkin.CategoryColor(category);
                GUILayout.Label($"● {category}  ({invested}P 투자)", DevHudSkin.SubHeader);
                GUI.contentColor = Color.white;

                foreach (TreeNode node in nodesInCategory)
                {
                    DrawNodeRow(node);
                }

                GUILayout.Space(4);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawNodeRow(TreeNode node)
        {
            bool unlocked = _game.NodeTree.IsUnlocked(node.Id);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{node.DisplayName} ({node.PointCost}P)", DevHudSkin.Body, GUILayout.Width(150));

            if (unlocked)
            {
                GUI.contentColor = DevHudSkin.Dim;
                GUILayout.Label("해금됨", DevHudSkin.Body);
                GUI.contentColor = Color.white;
            }
            else
            {
                bool canUnlock = _game.NodeTree.CanUnlock(node);
                GUI.enabled = canUnlock;
                GUI.backgroundColor = canUnlock ? DevHudSkin.ButtonOnTint : DevHudSkin.ButtonTint;
                if (GUILayout.Button("해금", DevHudSkin.Chip))
                {
                    _game.NodeTree.TryUnlock(node.Id);
                }
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawBuildingDexPanel()
        {
            GUI.backgroundColor = DevHudSkin.PanelTint;
            GUILayout.BeginArea(new Rect(300, 50, 320, 470), DevHudSkin.Panel);
            GUI.backgroundColor = Color.white;

            DrawPanelHeader("건물 도감", () => _showBuildingDex = false);

            DrawFilterRow(ref _buildingFilterIndex);
            string filter = FilterOptions[_buildingFilterIndex];

            _buildingDexScroll = GUILayout.BeginScrollView(_buildingDexScroll, GUILayout.Height(390));

            foreach (string buildingId in _game.BuildingDex.RegisteredIds)
            {
                string category = CategoryOfBuilding(buildingId);
                if (filter != "전체" && filter != category) continue;

                BuildingDefinition def = _game.BuildingDex.AllBuildings[buildingId];
                PlacedBuilding placed = _game.BuildingManager.Find(buildingId);

                GUILayout.BeginHorizontal();
                GUI.contentColor = DevHudSkin.CategoryColor(category);
                GUILayout.Label("●", DevHudSkin.Body, GUILayout.Width(14));
                GUI.contentColor = Color.white;
                GUILayout.Label(def.DisplayName, DevHudSkin.Body, GUILayout.Width(72));

                if (placed == null)
                {
                    GUI.backgroundColor = DevHudSkin.ButtonOnTint;
                    if (GUILayout.Button($"건설 ({def.BuildCostGold}G)", DevHudSkin.Chip))
                    {
                        _game.BuildingManager.TryBuild(buildingId);
                    }
                    GUI.backgroundColor = Color.white;
                }
                else if (placed.IsUpgrading)
                {
                    GUI.contentColor = DevHudSkin.Dim;
                    GUILayout.Label($"{FormatTime(placed.RemainingTime)} 남음", DevHudSkin.Body);
                    GUI.contentColor = Color.white;
                }
                else
                {
                    double nextCost = def.BuildCostGold * (placed.Level + 1);
                    GUILayout.Label($"Lv.{placed.Level}", DevHudSkin.Body, GUILayout.Width(36));
                    GUI.backgroundColor = DevHudSkin.ButtonTint;
                    if (GUILayout.Button($"업그레이드 ({nextCost}G)", DevHudSkin.Chip))
                    {
                        _game.BuildingManager.TryUpgrade(buildingId);
                    }
                    GUI.backgroundColor = Color.white;
                }
                GUILayout.EndHorizontal();
            }

            if (_game.BuildingDex.RegisteredIds.Count == 0)
            {
                GUI.contentColor = DevHudSkin.Dim;
                GUILayout.Label("노드 트리에서 건물을 해금하세요.", DevHudSkin.Body);
                GUI.contentColor = Color.white;
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

            GUI.backgroundColor = DevHudSkin.PanelTint;
            GUILayout.BeginArea(new Rect(Screen.width - 270, 50, 260, 104), DevHudSkin.Panel);
            GUI.backgroundColor = Color.white;

            GUILayout.Label($"마을회관 Lv.{townHall.Level}", DevHudSkin.Header);

            if (townHall.IsUpgrading)
            {
                GUI.contentColor = DevHudSkin.Dim;
                GUILayout.Label($"업그레이드 중... {FormatTime(townHall.UpgradeCompletesAtUtc.Value - DateTime.UtcNow)} 남음",
                    DevHudSkin.Body);
                GUI.contentColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = DevHudSkin.ButtonOnTint;
                if (GUILayout.Button($"업그레이드 ({townHall.NextUpgradeCostGold}G, {townHall.NextUpgradeTimeSeconds}초)",
                        DevHudSkin.Chip))
                {
                    townHall.TryUpgrade();
                }
                GUI.backgroundColor = Color.white;
            }

            GUILayout.EndArea();
        }

        private void DrawMilestonePopup()
        {
            var choices = _game.Milestone.PendingChoices;
            if (choices == null) return;

            var rect = new Rect(Screen.width / 2f - 200, Screen.height / 2f - 120, 400, 240);

            GUI.backgroundColor = DevHudSkin.PanelTint;
            GUILayout.BeginArea(rect, DevHudSkin.Panel);
            GUI.backgroundColor = Color.white;

            GUILayout.Label("마을회관이 레벨업했습니다! 방향을 선택하세요.", DevHudSkin.Header);
            GUILayout.Space(6);

            for (int i = 0; i < choices.Count; i++)
            {
                GUI.backgroundColor = DevHudSkin.ButtonOnTint;
                if (GUILayout.Button($"{choices[i].Title} - {choices[i].Description}", DevHudSkin.Chip, GUILayout.Height(32)))
                {
                    _game.Milestone.Choose(i);
                }
                GUI.backgroundColor = Color.white;
                GUILayout.Space(4);
            }

            GUILayout.EndArea();
        }

        private static GUIStyle CenteredStyle(GUIStyle baseStyle)
        {
            var style = new GUIStyle(baseStyle) { alignment = TextAnchor.MiddleCenter };
            return style;
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
