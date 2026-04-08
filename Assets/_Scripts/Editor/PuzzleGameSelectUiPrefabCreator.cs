#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Editor
{
    public static class PuzzleGameSelectUiPrefabCreator
    {
        private const string PrefabFolder = "Assets/Resources/UI";
        private const string PrefabPath = PrefabFolder + "/PuzzleGameSelectUI.prefab";

        [MenuItem("Tools/DreamHome/Create Puzzle Game Select UI Prefab")]
        public static void CreatePrefab()
        {
            EnsureFolder(PrefabFolder);

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

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(prefab);
            Debug.Log("Created Puzzle Game Select UI prefab at: " + PrefabPath);
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

            CreateText(root.transform, "Label", label, 30f, TextAlignmentOptions.Center);
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;

            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            return tmp;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string normalized = folderPath.Replace("\\", "/");
            string[] parts = normalized.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif

