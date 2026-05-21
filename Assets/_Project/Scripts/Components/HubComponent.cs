using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using OceanFactory.Conveyors;
using OceanFactory.Core;
using OceanFactory.Data;

namespace OceanFactory.Components
{
    /// <summary>
    /// Central delivery target. Walks an ordered list of LevelGoalSOs; each level can require
    /// any number of different items. Items not matching a current requirement are still consumed
    /// (so belts don't back up), but do not count toward progress.
    /// </summary>
    public class HubComponent : BuildingComponent, IItemSink
    {
        public readonly struct RequirementProgress
        {
            public readonly ItemTypeSO Item;
            public readonly int Current;
            public readonly int Target;
            public bool IsComplete => Current >= Target;

            public RequirementProgress(ItemTypeSO item, int current, int target)
            {
                Item = item;
                Current = current;
                Target = target;
            }
        }

        [Header("Progression")]
        [SerializeField] private LevelProgressionSO progression;
        [Tooltip("If true, levels loop back to level 1 after the last one is completed.")]
        [SerializeField] private bool loopLevels = false;

        [Header("World-space hub display")]
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private GameObject worldDisplayRoot;

        [Header("Diagnostics")]
        [SerializeField, Tooltip("Period (s) between console status logs.")]
        private float logIntervalSeconds = 10f;
        [SerializeField] private bool enableStatusLog = true;

        private int currentLevelIndex;
        private readonly Dictionary<ItemTypeSO, int> counts = new();
        private float logTimer;

        public int CurrentLevel => currentLevelIndex + 1;
        public bool AllLevelsComplete =>
            progression == null || progression.levels == null || currentLevelIndex >= progression.levels.Count;
        public LevelGoalSO CurrentLevelData => AllLevelsComplete ? null : progression.levels[currentLevelIndex];

        public bool TryAcceptItem(ItemTypeSO item, Vector2Int intoCell, Direction fromDir)
        {
            if (item == null) return false;
            // Hub is multi-cell — accept items entering ANY cell of the footprint.
            if (!IsInsideFootprint(intoCell)) return false;
            EventBus.Publish(new ItemDeliveredEvent(item, Cell));

            var level = CurrentLevelData;
            if (level != null && CountsTowardsLevel(level, item))
            {
                counts.TryGetValue(item, out int c);
                counts[item] = c + 1;

                if (IsLevelComplete(level))
                {
                    int finishedIndex = currentLevelIndex;
                    Debug.Log($"[HUB] Level {finishedIndex + 1} ({LevelDisplayName(level)}) COMPLETE.");
                    EventBus.Publish(new GoalCompletedEvent(finishedIndex, item));
                    currentLevelIndex++;
                    counts.Clear();
                    if (loopLevels && progression != null && currentLevelIndex >= progression.levels.Count)
                    {
                        currentLevelIndex = 0;
                    }
                }
            }
            RefreshDisplay();
            return true;
        }

        /// <summary>Per-requirement progress for the current level. Empty when all levels done.</summary>
        public IReadOnlyList<RequirementProgress> GetCurrentRequirements()
        {
            var list = new List<RequirementProgress>();
            var level = CurrentLevelData;
            if (level == null) return list;
            for (int i = 0; i < level.requirements.Count; i++)
            {
                var req = level.requirements[i];
                if (req.item == null) continue;
                counts.TryGetValue(req.item, out int c);
                list.Add(new RequirementProgress(req.item, Mathf.Min(c, req.targetCount), req.targetCount));
            }
            return list;
        }

        public override void OnPlaced()
        {
            base.OnPlaced();
            currentLevelIndex = 0;
            counts.Clear();
            logTimer = 0f;
            RefreshDisplay();
            Debug.Log($"[HUB] Placed at {Cell}. Starting at Level 1: {DescribeCurrent()}");
        }

        private void Update()
        {
            if (!enableStatusLog) return;
            if (!Services.TryGet<GameStateManager>(out var gsm) || !gsm.IsPlaying) return;
            logTimer += Time.deltaTime;
            if (logTimer < logIntervalSeconds) return;
            logTimer = 0f;
            Debug.Log($"[HUB] {DescribeCurrent()}");
        }

        private string DescribeCurrent()
        {
            if (AllLevelsComplete) return $"Level {CurrentLevel}: ALL LEVELS COMPLETE";
            var level = CurrentLevelData;
            var sb = new StringBuilder();
            sb.Append($"Level {currentLevelIndex + 1} — {LevelDisplayName(level)}: ");
            for (int i = 0; i < level.requirements.Count; i++)
            {
                var r = level.requirements[i];
                if (r.item == null) continue;
                counts.TryGetValue(r.item, out int c);
                if (i > 0) sb.Append(", ");
                sb.Append($"{r.item.displayName} {Mathf.Min(c, r.targetCount)}/{r.targetCount}");
            }
            return sb.ToString();
        }

        private static string LevelDisplayName(LevelGoalSO level) =>
            !string.IsNullOrWhiteSpace(level.title) ? level.title : level.name;

        private static bool CountsTowardsLevel(LevelGoalSO level, ItemTypeSO item)
        {
            for (int i = 0; i < level.requirements.Count; i++)
            {
                if (level.requirements[i].item == item) return true;
            }
            return false;
        }

        private bool IsLevelComplete(LevelGoalSO level)
        {
            for (int i = 0; i < level.requirements.Count; i++)
            {
                var req = level.requirements[i];
                if (req.item == null) continue;
                counts.TryGetValue(req.item, out int c);
                if (c < req.targetCount) return false;
            }
            return true;
        }

        private bool IsInsideFootprint(Vector2Int cell)
        {
            var size = Size;
            return cell.x >= Cell.x && cell.x < Cell.x + size.x &&
                   cell.y >= Cell.y && cell.y < Cell.y + size.y;
        }

        private void RefreshDisplay()
        {
            if (worldDisplayRoot != null) worldDisplayRoot.SetActive(true);
            if (levelLabel != null)
            {
                levelLabel.text = AllLevelsComplete
                    ? "ALL LEVELS\nCOMPLETE"
                    : $"Lv.{CurrentLevel}";
            }
        }
    }
}
