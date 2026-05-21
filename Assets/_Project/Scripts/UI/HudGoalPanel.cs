using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OceanFactory.Components;
using OceanFactory.Core;

namespace OceanFactory.UI
{
    /// <summary>
    /// Screen-space HUD panel showing the current Hub level title and one row per requirement:
    ///   [icon]    current / target
    /// Rows are built at runtime from HubComponent.GetCurrentRequirements(), so adding/removing
    /// requirements to a level needs no UI rewiring.
    /// </summary>
    public class HudGoalPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleLabel;
        [Tooltip("Container for requirement rows. If null, an auto-built VerticalLayoutGroup is created under panelRoot.")]
        [SerializeField] private RectTransform rowsContainer;

        [Header("Row styling")]
        [SerializeField] private float rowHeight = 56f;
        [SerializeField] private int countFontSize = 24;
        [SerializeField] private Color completeColor = new Color(0.40f, 0.95f, 0.55f, 1f);
        [SerializeField] private Color pendingColor = new Color(1f, 1f, 1f, 1f);

        private HubComponent currentHub;
        private readonly List<RequirementRow> rows = new();

        private struct RequirementRow
        {
            public GameObject root;
            public Image icon;
            public TMP_Text countLabel;
        }

        private void Awake()
        {
            if (titleLabel != null)
            {
#pragma warning disable CS0618
                titleLabel.enableWordWrapping = false;
#pragma warning restore CS0618
                titleLabel.overflowMode = TextOverflowModes.Overflow;
            }
            EnsureRowsContainer();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ItemDeliveredEvent>(OnDelivered);
            EventBus.Subscribe<BuildingPlacedEvent>(OnBuilt);
            EventBus.Subscribe<BuildingRemovedEvent>(OnRemoved);
            EventBus.Subscribe<GoalCompletedEvent>(OnLevelCompleted);
            TryFindHub();
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ItemDeliveredEvent>(OnDelivered);
            EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuilt);
            EventBus.Unsubscribe<BuildingRemovedEvent>(OnRemoved);
            EventBus.Unsubscribe<GoalCompletedEvent>(OnLevelCompleted);
        }

        private void OnBuilt(BuildingPlacedEvent _) { if (currentHub == null) TryFindHub(); Refresh(); }
        private void OnRemoved(BuildingRemovedEvent _) { if (currentHub == null) TryFindHub(); Refresh(); }
        private void OnDelivered(ItemDeliveredEvent _) => Refresh();
        private void OnLevelCompleted(GoalCompletedEvent _) => Refresh();

        private void TryFindHub() => currentHub = FindFirstObjectByType<HubComponent>();

        private void EnsureRowsContainer()
        {
            if (panelRoot == null) panelRoot = gameObject;

            // Auto-create the container if it wasn't wired in the Inspector.
            if (rowsContainer == null)
            {
                var go = new GameObject("Rows", typeof(RectTransform));
                go.transform.SetParent(panelRoot.transform, false);
                rowsContainer = go.GetComponent<RectTransform>();
                rowsContainer.anchorMin = new Vector2(0f, 0f);
                rowsContainer.anchorMax = new Vector2(1f, 1f);
                // Leave room at top for the title label; assumes a ~44 px title bar.
                rowsContainer.offsetMin = new Vector2(8f, 8f);
                rowsContainer.offsetMax = new Vector2(-8f, -44f);
            }

            // Layout requirements horizontally in a single row so multi-item levels (e.g. 3
            // shards) stay inside the HUD panel rectangle instead of stacking vertically. If a
            // stale VerticalLayoutGroup was added by a previous version of this script, strip it.
            var stale = rowsContainer.GetComponent<VerticalLayoutGroup>();
            if (stale != null) Destroy(stale);

            var hlg = rowsContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = rowsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14f;
            hlg.padding = new RectOffset(4, 4, 4, 4);
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            // Anchor the container as a top-stretched strip directly below the title — fixed
            // height (rowHeight + padding), full width minus side padding. Horizontal layout
            // keeps every requirement in this strip.
            rowsContainer.anchorMin = new Vector2(0f, 1f);
            rowsContainer.anchorMax = new Vector2(1f, 1f);
            rowsContainer.pivot = new Vector2(0.5f, 1f);
            rowsContainer.anchoredPosition = new Vector2(0f, -44f);
            rowsContainer.sizeDelta = new Vector2(-16f, rowHeight + 8f);

            var fitter = rowsContainer.GetComponent<ContentSizeFitter>();
            if (fitter != null) Destroy(fitter);
        }

        private void Refresh()
        {
            if (panelRoot != null) panelRoot.SetActive(true);

            if (currentHub == null)
            {
                SetEmpty("No Hub placed");
                return;
            }

            var level = currentHub.CurrentLevelData;
            if (currentHub.AllLevelsComplete || level == null)
            {
                SetEmpty($"Level {currentHub.CurrentLevel}: ALL LEVELS COMPLETE");
                return;
            }

            if (titleLabel != null)
            {
                string t = string.IsNullOrWhiteSpace(level.title) ? level.name : level.title;
                titleLabel.text = $"Level {currentHub.CurrentLevel}: {t}";
            }

            var reqs = currentHub.GetCurrentRequirements();
            EnsureRowCount(reqs.Count);
            for (int i = 0; i < reqs.Count; i++)
            {
                var r = reqs[i];
                var row = rows[i];
                if (row.icon != null)
                {
                    row.icon.enabled = true;
                    var sprite = r.Item != null ? r.Item.ItemOrFallback : null;
                    if (sprite != null) row.icon.sprite = sprite;
                    row.icon.color = Color.white;
                }
                if (row.countLabel != null)
                {
                    bool done = r.Current >= r.Target;
                    row.countLabel.text = $"{r.Current} / {r.Target}";
                    row.countLabel.color = done ? completeColor : pendingColor;
                }
            }
        }

        private void EnsureRowCount(int needed)
        {
            while (rows.Count < needed) rows.Add(CreateRow());
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].root != null) rows[i].root.SetActive(i < needed);
            }
        }

        private RequirementRow CreateRow()
        {
            // The outer rowsContainer is a HorizontalLayoutGroup. Each "row" is one
            // requirement cell that itself lays out its icon + count text horizontally.
            float iconSize = rowHeight * 0.7f;
            var rowGO = new GameObject("Row", typeof(RectTransform));
            rowGO.transform.SetParent(rowsContainer, false);
            var le = rowGO.AddComponent<LayoutElement>();
            le.preferredHeight = rowHeight;
            le.minHeight = rowHeight;

            var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.padding = new RectOffset(2, 6, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            // Icon
            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(rowGO.transform, false);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;
            var iconLE = iconGO.AddComponent<LayoutElement>();
            iconLE.preferredWidth = iconSize;
            iconLE.preferredHeight = iconSize;
            iconLE.minWidth = iconSize;

            // Count label — fixed compact width so multiple requirement cells fit side by side
            // inside the panel rectangle without one of them eating all the slack.
            var countGO = new GameObject("Count", typeof(RectTransform));
            countGO.transform.SetParent(rowGO.transform, false);
            var tmp = countGO.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = countFontSize;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
#pragma warning disable CS0618
            tmp.enableWordWrapping = false;
#pragma warning restore CS0618
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.text = "0 / 0";
            var countLE = countGO.AddComponent<LayoutElement>();
            countLE.preferredWidth = 80f;
            countLE.minWidth = 60f;
            countLE.flexibleWidth = 0f;

            return new RequirementRow { root = rowGO, icon = iconImg, countLabel = tmp };
        }

        private void SetEmpty(string text)
        {
            EnsureRowCount(0);
            if (titleLabel != null) titleLabel.text = text;
        }
    }
}
