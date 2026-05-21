using TMPro;
using UnityEngine;
using UnityEngine.UI;
using OceanFactory.Components;
using OceanFactory.Data;

namespace OceanFactory.UI
{
    /// <summary>
    /// Read-only browser for the full recipe book. Toggled by the M hotkey
    /// (see InputReader). Self-builds its UI under the first screen-space Canvas
    /// the first time it is opened, so no manual scene setup is required.
    /// </summary>
    public class RecipeListController : MonoBehaviour
    {
        [SerializeField] private RecipeBookSO recipeBook;
        [SerializeField] private Vector2 panelSize = new Vector2(620f, 680f);
        [SerializeField] private bool verbose = false;

        public static RecipeListController Instance { get; private set; }

        private GameObject panelRoot;
        private RectTransform listContent;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            Instance = this;
            BuildPanel();
            HidePanel();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Toggle()
        {
            var inst = EnsureInstance();
            if (inst == null) return;
            if (inst.IsOpen) inst.Close();
            else inst.Open();
        }

        public void Open()
        {
            if (recipeBook == null) recipeBook = FindAnyRecipeBook();
            if (panelRoot == null) BuildPanel();
            PopulateRows();
            ShowPanel();
            if (verbose) Debug.Log($"[RecipeList] open, recipeBook={(recipeBook != null ? recipeBook.name : "null")}");
        }

        public void Close() => HidePanel();

        private static RecipeListController EnsureInstance()
        {
            if (Instance != null) return Instance;
            var canvas = FindBestCanvas();
            if (canvas == null)
            {
                Debug.LogError("[RecipeList] No screen-space Canvas found — cannot create panel.");
                return null;
            }
            var go = new GameObject("RecipeList", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            return go.AddComponent<RecipeListController>();
        }

        private static Canvas FindBestCanvas()
        {
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

        private void ShowPanel() { if (panelRoot != null) panelRoot.SetActive(true); }
        private void HidePanel() { if (panelRoot != null) panelRoot.SetActive(false); }

        private void Update()
        {
            if (!IsOpen) return;
            // Esc also closes us (mirrors RecipePicker behaviour). InputReader will
            // route Esc here before any other handler when we're open.
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Close();
        }

        private void BuildPanel()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[RecipeList] no Canvas in parents — cannot build panel UI");
                return;
            }

            panelRoot = CreateUIObject("RecipeListPanel", transform, out var panelRT);
            panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = panelSize;
            panelRT.anchoredPosition = Vector2.zero;
            var bgImg = panelRoot.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.10f, 0.16f, 0.96f);
            bgImg.raycastTarget = true;

            var outline = panelRoot.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.6f, 0.9f, 1f);
            outline.effectDistance = new Vector2(2f, -2f);

            var title = CreateUIObject("Title", panelRoot.transform, out var titleRT);
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.sizeDelta = new Vector2(0f, 52f);
            titleRT.anchoredPosition = new Vector2(0f, 0f);
            var titleLabel = title.AddComponent<TextMeshProUGUI>();
            titleLabel.text = "Рецепты";
            titleLabel.fontSize = 28;
            titleLabel.color = Color.white;
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.raycastTarget = false;

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

        private void PopulateRows()
        {
            if (listContent == null) return;
            for (int i = listContent.childCount - 1; i >= 0; i--) Destroy(listContent.GetChild(i).gameObject);

            if (recipeBook == null || recipeBook.recipes == null || recipeBook.recipes.Count == 0)
            {
                CreateMessageRow(recipeBook == null
                    ? "RecipeBook не найден. Положи RecipeBook в сцену или на Assembler.prefab."
                    : "В RecipeBook нет рецептов.");
                return;
            }

            int added = 0;
            for (int i = 0; i < recipeBook.recipes.Count; i++)
            {
                var r = recipeBook.recipes[i];
                if (r == null || r.output == null || r.inputA == null || r.inputB == null) continue;
                CreateRow(r);
                added++;
            }
            if (added == 0) CreateMessageRow("В RecipeBook нет валидных рецептов (нужны inputA, inputB, output).");
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

        private static readonly Color RowColor = new Color(0.14f, 0.20f, 0.30f, 1f);

        private void CreateRow(RecipeSO recipe)
        {
            var row = CreateUIObject($"Recipe_{recipe.name}", listContent, out var rowRT);
            rowRT.sizeDelta = new Vector2(0f, 60f);
            var rowImg = row.AddComponent<Image>();
            rowImg.color = RowColor;
            rowImg.raycastTarget = false;
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 60f;

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.padding = new RectOffset(10, 10, 6, 6);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

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
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            labelGO.AddComponent<LayoutElement>().preferredWidth = 260f;
        }

        private static void AddItemIcon(Transform parent, ItemTypeSO item)
        {
            var iconGO = CreateUIObject($"Icon_{item.displayName}", parent, out var iconRT);
            iconRT.sizeDelta = new Vector2(48f, 48f);
            var img = iconGO.AddComponent<Image>();
            img.color = item.color;
            var sprite = item.ItemOrFallback;
            if (sprite != null) img.sprite = sprite;
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

        private static GameObject CreateUIObject(string name, Transform parent, out RectTransform rt)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            rt = go.GetComponent<RectTransform>();
            return go;
        }
    }
}
