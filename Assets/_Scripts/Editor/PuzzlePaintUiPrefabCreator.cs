#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.Editor
{
    public static class PuzzlePaintUiPrefabCreator
    {
        private const string PrefabFolder = "Assets/Resources/UI";
        private const string PrefabPath = PrefabFolder + "/PuzzlePaintUI.prefab";

        [MenuItem("Tools/DreamHome/Create Puzzle Paint UI Prefab")]
        public static void CreatePrefab()
        {
            EnsureFolder(PrefabFolder);

            GameObject root = new GameObject("PuzzlePaintUI", typeof(RectTransform), typeof(Image));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            StretchFull(rootRect);

            Image rootBg = root.GetComponent<Image>();
            rootBg.color = new Color(0f, 0f, 0f, 0.01f);
            rootBg.raycastTarget = false;

            GameObject hud = new GameObject("HUD", typeof(RectTransform), typeof(Image));
            hud.transform.SetParent(root.transform, false);
            RectTransform hudRect = hud.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 1f);
            hudRect.anchorMax = new Vector2(0f, 1f);
            hudRect.pivot = new Vector2(0f, 1f);
            hudRect.anchoredPosition = new Vector2(20f, -20f);
            hudRect.sizeDelta = new Vector2(380f, 150f);

            Image hudBg = hud.GetComponent<Image>();
            hudBg.color = new Color(0f, 0f, 0f, 0.35f);

            CreateText(hud.transform, "LevelText", "Level 1", 34f, TextAlignmentOptions.Left, new Vector2(16f, -16f), new Vector2(320f, 44f));
            CreateText(hud.transform, "ProgressText", "Painted 0/0", 26f, TextAlignmentOptions.Left, new Vector2(16f, -66f), new Vector2(320f, 36f));
            CreateButton(hud.transform, "ResetButton", "Reset", new Color(0.24f, 0.58f, 0.96f), new Vector2(260f, -96f), new Vector2(100f, 54f));

            GameObject resultPanel = new GameObject("ResultPanel", typeof(RectTransform), typeof(Image));
            resultPanel.transform.SetParent(root.transform, false);
            RectTransform resultRect = resultPanel.GetComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0.5f, 0.5f);
            resultRect.anchorMax = new Vector2(0.5f, 0.5f);
            resultRect.pivot = new Vector2(0.5f, 0.5f);
            resultRect.anchoredPosition = Vector2.zero;
            resultRect.sizeDelta = new Vector2(520f, 300f);

            Image resultBg = resultPanel.GetComponent<Image>();
            resultBg.color = new Color(0f, 0f, 0f, 0.82f);

            CreateText(resultPanel.transform, "ResultText", "Level Complete!", 38f, TextAlignmentOptions.Center, new Vector2(0f, -28f), new Vector2(440f, 100f));
            CreateButton(resultPanel.transform, "NextButton", "Next Level", new Color(0.46f, 0.2f, 0.74f), new Vector2(0f, 54f), new Vector2(220f, 62f));
            CreateButton(resultPanel.transform, "ReplayButton", "Main", new Color(0.42f, 0.42f, 0.42f), new Vector2(0f, -20f), new Vector2(220f, 62f));

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(prefab);
            Debug.Log("Created Puzzle Paint UI prefab at: " + PrefabPath);
        }

        private static void CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;

            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }
        }

        private static void CreateButton(Transform parent, string name, string label, Color color, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = root.GetComponent<Image>();
            image.color = color;

            Button button = root.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.5f);
            button.colors = colors;

            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(root.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 28f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }
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

