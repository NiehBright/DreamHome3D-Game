#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class FurnitureContextUiPrefabCreator
{
    private const string PrefabFolder = "Assets/_Prefab/UI";
    private const string PrefabPath = PrefabFolder + "/FurnitureContextUI.prefab";

    [MenuItem("Tools/DreamHome/Create Furniture Context UI Prefab")]
    public static void CreatePrefab()
    {
        EnsureFolder(PrefabFolder);

        GameObject panel = new GameObject("FurnitureContextUI", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(300f, 96f);

        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);
        bg.raycastTarget = true;

        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        CreateButton(panel.transform, "RotateButton", "XOAY", new Color(0.18f, 0.54f, 0.96f));
        CreateButton(panel.transform, "DeleteButton", "XOA", new Color(0.95f, 0.3f, 0.3f));
        CreateButton(panel.transform, "CancelButton", "HUY", new Color(0.45f, 0.45f, 0.45f));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(panel, PrefabPath);
        Object.DestroyImmediate(panel);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("Created Furniture Context UI prefab at: " + PrefabPath);
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(88f, 72f);

        Image image = root.GetComponent<Image>();
        image.color = color;

        Button button = root.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
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

        return button;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
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
#endif

