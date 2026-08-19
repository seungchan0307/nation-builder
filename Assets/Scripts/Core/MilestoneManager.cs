using System;
using System.Collections.Generic;
using UnityEngine;

namespace NationBuilder.Core
{
    /// <summary>
    /// Shows a civilization-style choice screen at growth milestones (town hall
    /// level-ups). The 3 options below are prototype placeholders - once a real
    /// differentiation hook is picked (see private-notes/DESIGN-PRIVATE.md), these
    /// should be redesigned around it instead of being generic economy/build/points
    /// bonuses.
    /// </summary>
    public class MilestoneManager : MonoBehaviour
    {
        public IReadOnlyList<MilestoneChoice> PendingChoices { get; private set; }
        public event Action OnChoiceMade;

        public void TriggerMilestone(int townHallLevel, NodeTreeManager nodeTree, BuildingManager buildingManager)
        {
            PendingChoices = new List<MilestoneChoice>
            {
                new()
                {
                    Title = "확장 투자",
                    Description = "노드 트리 포인트 +2",
                    Apply = () => nodeTree.AddPoints(2),
                },
                new()
                {
                    Title = "상업 특화",
                    Description = "골드 생산 속도 +20%",
                    Apply = () => ResourceManager.Instance.MultiplyGoldRate(1.2),
                },
                new()
                {
                    Title = "건설 특화",
                    Description = "건물 건설/업그레이드 시간 -15%",
                    Apply = () => buildingManager.BuildTimeMultiplier *= 0.85f,
                },
            };
        }

        public void Choose(int index)
        {
            if (PendingChoices == null || index < 0 || index >= PendingChoices.Count) return;
            PendingChoices[index].Apply?.Invoke();
            PendingChoices = null;
            OnChoiceMade?.Invoke();
        }
    }
}
