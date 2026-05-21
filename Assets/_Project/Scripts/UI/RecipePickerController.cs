using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OceanFactory.Components;
using OceanFactory.Core;
using OceanFactory.Data;

namespace OceanFactory.UI
{
    /// <summary>
    /// Self-building recipe picker. Either add it under your Canvas manually,
    /// or call OpenFor(assembler) — it will lazy-create a child under the active
    /// screen-space Canvas on first click.
    /// </summary>
    public class RecipePickerController : MonoBehaviour
    {
        [SerializeField] private RecipeBookSO recipeBook;
        [SerializeField] private Vector2 panelSize = new Vector2(620f, 680f);
        [Tooltip("Padding (px) between the picker and the screen edge when pinned to a corner.")]
        [SerializeField] private float cornerMargin = 130f;
        [SerializeField] private bool verbose = true;

        public static RecipePickerController Instance { get; private set; }

        private GameObject panelRoot;
        private RectTransform listContent;
        private TextMeshProUGUI titleLabel;
        private AssemblerComponent currentTarget;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            Instance = this;
            // Force the host GameObject to fully stretch over the canvas regardless of how the
            // scene-authored RectTransform was set up — without this the panel inherits whatever
            // odd anchors/sizes the user happened to drag in the Inspector, and we end up
            // positioned partially off-screen.
            ForceFullStretchOnSelf();
            BuildPanel();
            HidePanel();
            if (verbose) Debug.Log("[RecipePicker] ready (Awake done, panel built)");
        }

        private void ForceFullStretchOnSelf()
        {
            var rt = transform as RectTransform;
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Open(AssemblerComponent assembler)
        {
            if (verbose) Debug.Log($"[RecipePicker] Open() called, assembler={(assembler != null ? assembler.name : "null")}");
            if (assembler == null) return;

            // 1) Try the picker's own field.
            // 2) Try the clicked assembler's serialized RecipeBook.
            // 3) Scan scene for any other assembler that has one.
            // 4) Last resort: any RecipeBookSO loaded anywhere.
            if (recipeBook == null) recipeBook = assembler.RecipeBook;
            if (recipeBook == null) recipeBook = FindAnyRecipeBook();
            if (verbose) Debug.Log($"[RecipePicker] recipeBook resolved to: {(recipeBook != null ? recipeBook.name : "null")}");

            if (panelRoot == null)
            {
                BuildPanel();
                HidePanel();
            }

            currentTarget = assembler;
            PopulateOptions();
            ShowPanel();
            if (verbose) Debug.Log($"[RecipePicker] Open() done. panel active={panelRoot.activeSelf}, panel position={((RectTransform)panelRoot.transform).position}, list children={listContent.childCount}");
        }

        public void Close()
        {
            currentTarget = null;
            HidePanel();
        }

        public static void OpenFor(AssemblerComponent assembler)
        {
            if (assembler == null)
            {
                Debug.LogWarning("[RecipePicker] OpenFor called with null assembler");
                return;
            }
            var picker = Instance;
            if (picker == null)
            {
                Debug.Log("[RecipePicker] Instance was null — creating one under a screen-space Canvas");
                var canvas = FindBestCanvas();
                if (canvas == null)
                {
                    Debug.LogError("[RecipePicker] No screen-space Canvas found in scene — cannot create picker. Add a Canvas (Screen Space - Overlay) to the scene.");
                    return;
                }
                var go = new GameObject("RecipePicker", typeof(RectTransform));
                go.transform.SetParent(canvas.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
                picker = go.AddComponent<RecipePickerController>();
                Debug.Log($"[RecipePicker] Created RecipePicker under canvas '{canvas.name}' (render mode {canvas.renderMode})");
            }
            picker.Open(assembler);
        }

        private static Canvas FindBestCanvas()
        {
            // Prefer ScreenSpaceOverlay/ScreenSpaceCamera. Skip WorldSpace canvases (hub goal panels etc.).
            var all = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Canvas overlay = null, screenCam = null;
            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                if (c == null || !c.isActiveAndEnabled) continue;
                if (c.renderMode == RenderMode.ScreenSpaceOverlay) { overlay = c; break; }
                if (c.renderMode == RenderMode.ScreenSpaceCamera && screenCam == null) screenCam = c;
            }
            return overlay != null ? overlay : screenCam;
        }

        private static RecipeBookSO FindAnyRecipeBook()
        {
            var assemblers = FindObjectsByType<AssemblerComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < assemblers.Length; i++)
            {
                if (assemblers[i] != null && assemblers[i].RecipeBook != null) return assemblers[i].RecipeBook;
            }
            var loaded = Resources.FindObjectsOfTypeAll<RecipeBookSO>();
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null) return loaded[i];
            }
            return null;
        }

        private void ShowPanel()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        private void HidePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Close();
        }

        private void BuildPanel()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[RecipePicker] no Canvas in parents — cannot build panel UI");
                return;
            }

            panelRoot = CreateUIObject("RecipePickerPanel", transform, out var panelRT);
            // Pin to bottom-right corner with a small margin so the recipe picker never overlaps
            // the build hotbar and never gets clipped off-screen, regardless of resolution.
            // (The global recipe list opened with M lives in a separate controller and stays
            // centered — we only style the per-assembler picker here.)
            panelRT.anchorMin = panelRT.anchorMax = new Vector2(1f, 0f);
            panelRT.pivot = new Vector2(1f, 0f);
            panelRT.sizeDelta = panelSize;
            panelRT.anchoredPosition = new Vector2(-cornerMargin, cornerMargin);
            var bgImg = panelRoot.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.10f, 0.16f, 0.96f);
            bgImg.raycastTarget = true;

            // Outline
            var outline = panelRoot.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.6f, 0.9f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Title
            var title = CreateUIObject("Title", panelRoot.transform, out var titleRT);
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.sizeDelta = new Vector2(0f, 52f);
            titleRT.anchoredPosition = new Vector2(0f, 0f);
            titleLabel = title.AddComponent<TextMeshProUGUI>();
            titleLabel.text = "Выбор рецепта";
            titleLabel.fontSize = 28;
            titleLabel.color = Color.white;
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.raycastTarget = false;

            // Close button
            var close = CreateUIObject("CloseButton", panelRoot.transform, out var closeRT);
            closeRT.anchorMin = closeRT.anchorMax = new Vector2(1f, 1f);
            closeRT.pivot = new Vector2(1f, 1f);
            closeRT.sizeDelta = new Vector2(60f, 32f);
            closeRT.anchoredPosition = new Vector2(-8f, -10f);
            var closeImg = close.AddComponent<Image>();
            closeImg.color = new Color(0.5f, 0.15f, 0.15f, 1f);
            var closeBtn = close.AddComponent<Button>();
            closeBtn.onClick.AddListener(Close);
            var closeText = CreateUIObject("Text", close.transform, out var closeTextRT);
            closeTextRT.anchorMin = Vector2.zero; closeTextRT.anchorMax = Vector2.one;
            closeTextRT.offsetMin = closeTextRT.offsetMax = Vector2.zero;
            var closeTMP = closeText.AddComponent<TextMeshProUGUI>();
            closeTMP.text = "X";
            closeTMP.fontSize = 22;
            closeTMP.alignment = TextAlignmentOptions.Center;
            closeTMP.color = Color.white;
            closeTMP.raycastTarget = false;

            // Scroll viewport
            var scrollGO = CreateUIObject("Scroll", panelRoot.transform, out var scrollRT);
            scrollRT.anchorMin = new Vector2(0f, 0f);
            scrollRT.anchorMax = new Vector2(1f, 1f);
            scrollRT.offsetMin = new Vector2(10f, 10f);
            scrollRT.offsetMax = new Vector2(-10f, -60f);
            var scrollImg = scrollGO.AddComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0.35f);
            scrollImg.raycastTarget = true;
            scrollGO.AddComponent<RectMask2D>();
            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var content = CreateUIObject("Content", scrollGO.transform, out var contentRT);
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0f, 0f);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRT;
            scroll.viewport = scrollRT;
            listContent = contentRT;
        }

        private void PopulateOptions()
        {
            if (listContent == null) return;
            for (int i = listContent.childCount - 1; i >= 0; i--) Destroy(listContent.GetChild(i).gameObject);

            if (recipeBook == null || recipeBook.recipes == null || recipeBook.recipes.Count == 0)
            {
                CreateMessageRow(recipeBook == null
                    ? "RecipeBook не назначен. Привяжи RecipeBook к RecipePicker или Assembler.prefab."
                    : "В RecipeBook нет рецептов.");
                return;
            }

            int added = 0;
            for (int i = 0; i < recipeBook.recipes.Count; i++)
            {
                var r = recipeBook.recipes[i];
                if (r == null || r.output == null || r.inputA == null || r.inputB == null) continue;
                CreateOption(r);
                added++;
            }
            if (added == 0) CreateMessageRow("В RecipeBook нет валидных рецептов (нужны inputA, inputB, output).");
            if (verbose) Debug.Log($"[RecipePicker] populated {added} recipes from book '{recipeBook.name}'");
        }

        private void CreateMessageRow(string message)
        {
            var row = CreateUIObject("Msg", listContent, out var rt);
            rt.sizeDelta = new Vector2(0f, 80f);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 80f;
            var tmp = row.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = 16;
            tmp.color = new Color(1f, 0.7f, 0.4f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        private static readonly Color RowColorNormal   = new Color(0.14f, 0.20f, 0.30f, 1f);
        private static readonly Color RowColorSelected = new Color(0.20f, 0.45f, 0.30f, 1f);

        private void CreateOption(RecipeSO recipe)
        {
            bool isSelected = currentTarget != null && currentTarget.SelectedRecipe == recipe;

            var row = CreateUIObject($"Option_{recipe.name}", listContent, out var rowRT);
            rowRT.sizeDelta = new Vector2(0f, 60f);
            var rowImg = row.AddComponent<Image>();
            rowImg.color = isSelected ? RowColorSelected : RowColorNormal;
            var btn = row.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectRecipe(recipe));
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 60f;

            // Highlight selected row with a bright outline so it pops at any zoom level.
            if (isSelected)
            {
                var outline = row.AddComponent<Outline>();
                outline.effectColor = new Color(0.45f, 1f, 0.55f, 1f);
                outline.effectDistance = new Vector2(2f, -2f);
            }

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(10, 10, 6, 6);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            if (isSelected) AddSelectedMarker(row.transform);
            AddItemIcon(row.transform, recipe.inputA);
            AddSymbol(row.transform, "+");
            AddItemIcon(row.transform, recipe.inputB);
            AddSymbol(row.transform, "->");
            AddItemIcon(row.transform, recipe.output);

            var labelGO = CreateUIObject("Label", row.transform, out var labelRT);
            labelRT.sizeDelta = new Vector2(260f, 48f);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{recipe.output.displayName}  ({recipe.craftTime:0.#}s)";
            tmp.fontSize = 18;
            tmp.color = isSelected ? new Color(0.85f, 1f, 0.9f, 1f) : Color.white;
            tmp.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            labelGO.AddComponent<LayoutElement>().preferredWidth = 260f;
        }

        private static void AddSelectedMarker(Transform parent)
        {
            var go = CreateUIObject("SelectedMark", parent, out var rt);
            rt.sizeDelta = new Vector2(18f, 48f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "\u2713"; // check mark
            tmp.fontSize = 28;
            tmp.color = new Color(0.45f, 1f, 0.55f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            go.AddComponent<LayoutElement>().preferredWidth = 18f;
        }

        private static void AddItemIcon(Transform parent, ItemTypeSO item)
        {
            var iconGO = CreateUIObject($"Icon_{item.displayName}", parent, out var iconRT);
            iconRT.sizeDelta = new Vector2(48f, 48f);
            var img = iconGO.AddComponent<Image>();
            img.color = item.color;
            var itemSprite = item.ItemOrFallback;
            if (itemSprite != null) img.sprite = itemSprite;
            img.raycastTarget = false;
            iconGO.AddComponent<LayoutElement>().preferredWidth = 48f;
        }

        private static void AddSymbol(Transform parent, string symbol)
        {
            var go = CreateUIObject(symbol, parent, out var rt);
            rt.sizeDelta = new Vector2(22f, 48f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = symbol;
            tmp.fontSize = 26;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            go.AddComponent<LayoutElement>().preferredWidth = 22f;
        }

        private void SelectRecipe(RecipeSO recipe)
        {
            if (currentTarget != null)
            {
                currentTarget.SetRecipe(recipe);
                if (verbose) Debug.Log($"[RecipePicker] Set recipe '{recipe.name}' on assembler at {currentTarget.Cell}");
            }
            Close();
        }

        private static GameObject CreateUIObject(string name, Transform parent, out RectTransform rt)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            rt = go.GetComponent<RectTransform>();
            return go;
        }
    }
}
