using TMPro;
using Runtime.Build;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Runtime.Gameplay
{
    public class PuzzleGameSelectController : MonoBehaviour
    {
        private const string DefaultResourcePath = "UI/PuzzleGameSelectUI";

        [Header("References")]
        [SerializeField] private ModeController modeController;
        [SerializeField] private PuzzleLevelSelectController classicPuzzleSelectController;

        [Header("UI")]
        [SerializeField] private Transform panelParent;
        [SerializeField] private string resourcePath = DefaultResourcePath;

        private GameObject _panelInstance;
        private Button _paintButton;
        private Button _classicButton;
        private Button _closeButton;

        public bool IsOpen => _panelInstance != null && _panelInstance.activeSelf;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnDisable()
        {
            CloseSelector();
        }

        public void OpenSelector()
        {
            EnsurePanel();

            if (_panelInstance == null)
            {
                return;
            }

            _panelInstance.SetActive(true);
            _panelInstance.transform.SetAsLastSibling();
        }

        public void CloseSelector()
        {
            if (_panelInstance != null)
            {
                _panelInstance.SetActive(false);
            }
        }

        private void EnsurePanel()
        {
            if (_panelInstance != null)
            {
                return;
            }

            ResolveReferences();

            if (panelParent == null)
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    panelParent = canvas.transform;
                }
            }

            GameObject prefab = string.IsNullOrWhiteSpace(resourcePath) ? null : Resources.Load<GameObject>(resourcePath);
            _panelInstance = prefab != null
                ? Instantiate(prefab, panelParent, false)
                : CreateFallbackPanel();

            _panelInstance.name = "PuzzleGameSelectUI";
            BindButtons(_panelInstance.transform);
            _panelInstance.SetActive(false);
        }

        private void ResolveReferences()
        {
            if (modeController == null)
            {
                modeController = FindFirstObjectByType<ModeController>();
            }

            if (classicPuzzleSelectController == null)
            {
                classicPuzzleSelectController = FindFirstObjectByType<PuzzleLevelSelectController>();
            }
        }

        private void BindButtons(Transform root)
        {
            _paintButton = FindButton(root, "PaintButton");
            _classicButton = FindButton(root, "ClassicButton");
            _closeButton = FindButton(root, "CloseButton");

            AddListener(_paintButton, HandlePaintClicked);
            AddListener(_classicButton, HandleClassicClicked);
            AddListener(_closeButton, CloseSelector);
        }

        private void HandleClassicClicked()
        {
            CloseSelector();

            if (classicPuzzleSelectController != null)
            {
                classicPuzzleSelectController.EnterPuzzleAndOpenSelector();
                return;
            }

            modeController?.EnterPuzzleMode();
        }

        private void HandlePaintClicked()
        {
            if (modeController != null && modeController.TryEnterPuzzlePaintMode())
            {
                CloseSelector();
                return;
            }

            Debug.LogWarning("Puzzle Paint mode is not configured yet. Add puzzlePaintRoot or uiPuzzlePaintRoot to ModeController.");
        }

        private GameObject CreateFallbackPanel()
        {
            GameObject root = new GameObject("PuzzleGameSelectUI", typeof(RectTransform), typeof(Image));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            StretchFull(rootRect);

            Image rootBg = root.GetComponent<Image>();
            rootBg.color = new Color(0f, 0f, 0f, 0.42f);
            rootBg.raycastTarget = true;

            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            card.transform.SetParent(root.transform, false);

            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(560f, 400f);

            Image cardBg = card.GetComponent<Image>();
            cardBg.color = new Color(0.12f, 0.12f, 0.14f, 0.96f);

            VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = card.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            TMP_Text title = CreateText(card.transform, "TitleText", "Chọn game", 42f, TextAlignmentOptions.Center);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(480f, 72f);

            CreateButton(card.transform, "PaintButton", "Puzzle Paint", new Color(0.46f, 0.2f, 0.74f));
            CreateButton(card.transform, "ClassicButton", "Puzzle Cũ", new Color(0.2f, 0.58f, 0.92f));
            CreateButton(card.transform, "CloseButton", "Đóng", new Color(0.45f, 0.45f, 0.45f));

            root.transform.SetParent(panelParent != null ? panelParent : transform, false);
            root.transform.SetAsLastSibling();
            return root;
        }

        private static void CreateButton(Transform parent, string name, string label, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(420f, 84f);

            Image image = root.GetComponent<Image>();
            image.color = color;

            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.preferredWidth = 420f;
            layout.preferredHeight = 84f;

            Button button = root.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.5f);
            button.colors = colors;

            TMP_Text text = CreateText(root.transform, "Label", label, 30f, TextAlignmentOptions.Center);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            return tmp;
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static Button FindButton(Transform root, string buttonName)
        {
            Transform found = FindDeepChild(root, buttonName);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private static Transform FindDeepChild(Transform parent, string targetName)
        {
            if (parent == null)
            {
                return null;
            }

            foreach (Transform child in parent)
            {
                if (child.name == targetName)
                {
                    return child;
                }

                Transform result = FindDeepChild(child, targetName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
