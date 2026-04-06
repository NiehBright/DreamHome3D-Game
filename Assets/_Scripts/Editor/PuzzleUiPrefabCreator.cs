#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PuzzleUiPrefabCreator
{
    private const string PrefabFolder = "Assets/_Prefab/UI";
    private const string PrefabPath = PrefabFolder + "/PuzzleUI.prefab";

    [MenuItem("Tools/DreamHome/Create Puzzle UI Prefab (TMP)")]
    public static void CreatePuzzleUiPrefab()
    {
        EnsureFolder(PrefabFolder);

        GameObject root = new GameObject("PuzzleUI", typeof(RectTransform));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        StretchFull(rootRect);

        // HUD root (top-left)
        GameObject hud = new GameObject("HUD_StepsStars", typeof(RectTransform), typeof(Image), typeof(StepCounterUI));
        hud.transform.SetParent(root.transform, false);
        RectTransform hudRect = hud.GetComponent<RectTransform>();
        SetTopLeft(hudRect, new Vector2(20f, -20f), new Vector2(340f, 140f));

        Image hudBg = hud.GetComponent<Image>();
        hudBg.color = new Color(0f, 0f, 0f, 0.35f);

        TMP_Text stepText = CreateText(hud.transform, "StepCountText", "Steps: 0", 42f, TextAlignmentOptions.Left);
        RectTransform stepRect = stepText.GetComponent<RectTransform>();
        stepRect.anchorMin = new Vector2(0f, 1f);
        stepRect.anchorMax = new Vector2(0f, 1f);
        stepRect.pivot = new Vector2(0f, 1f);
        stepRect.anchoredPosition = new Vector2(16f, -16f);
        stepRect.sizeDelta = new Vector2(280f, 56f);

        TMP_Text optimalText = CreateText(hud.transform, "OptimalStepsText", "", 24f, TextAlignmentOptions.Left);
        RectTransform optimalRect = optimalText.GetComponent<RectTransform>();
        optimalRect.anchorMin = new Vector2(0f, 1f);
        optimalRect.anchorMax = new Vector2(0f, 1f);
        optimalRect.pivot = new Vector2(0f, 1f);
        optimalRect.anchoredPosition = new Vector2(16f, -70f);
        optimalRect.sizeDelta = new Vector2(280f, 40f);
        optimalText.gameObject.SetActive(false);

        GameObject starsHudRoot = new GameObject("StarsHUD", typeof(RectTransform));
        starsHudRoot.transform.SetParent(hud.transform, false);
        RectTransform starsHudRect = starsHudRoot.GetComponent<RectTransform>();
        starsHudRect.anchorMin = new Vector2(0f, 0f);
        starsHudRect.anchorMax = new Vector2(0f, 0f);
        starsHudRect.pivot = new Vector2(0f, 0f);
        starsHudRect.anchoredPosition = new Vector2(16f, 12f);
        starsHudRect.sizeDelta = new Vector2(240f, 40f);

        GameObject[] hudStars = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            Image star = CreateStarImage(starsHudRoot.transform, "Star" + (i + 1));
            RectTransform starRect = star.GetComponent<RectTransform>();
            starRect.anchorMin = new Vector2(0f, 0.5f);
            starRect.anchorMax = new Vector2(0f, 0.5f);
            starRect.pivot = new Vector2(0f, 0.5f);
            starRect.anchoredPosition = new Vector2(i * 52f, 0f);
            hudStars[i] = star.gameObject;
        }

        // Result panel (center)
        GameObject resultPanel = new GameObject("ResultPanel", typeof(RectTransform), typeof(Image));
        resultPanel.transform.SetParent(root.transform, false);
        RectTransform resultRect = resultPanel.GetComponent<RectTransform>();
        SetCenter(resultRect, new Vector2(520f, 360f));

        Image resultBg = resultPanel.GetComponent<Image>();
        resultBg.color = new Color(0f, 0f, 0f, 0.8f);

        TMP_Text resultText = CreateText(resultPanel.transform, "ResultText", "Stars: 3/3\nLevel Coins: +0\nStar Bonus: +0\nCoins Earned: +0", 38f, TextAlignmentOptions.Center);
        RectTransform resultTextRect = resultText.GetComponent<RectTransform>();
        resultTextRect.anchorMin = new Vector2(0.5f, 1f);
        resultTextRect.anchorMax = new Vector2(0.5f, 1f);
        resultTextRect.pivot = new Vector2(0.5f, 1f);
        resultTextRect.anchoredPosition = new Vector2(0f, -36f);
        resultTextRect.sizeDelta = new Vector2(480f, 180f);
        resultText.lineSpacing = 12f;

        GameObject starsResultRoot = new GameObject("StarsResult", typeof(RectTransform));
        starsResultRoot.transform.SetParent(resultPanel.transform, false);
        RectTransform starsResultRect = starsResultRoot.GetComponent<RectTransform>();
        starsResultRect.anchorMin = new Vector2(0.5f, 0f);
        starsResultRect.anchorMax = new Vector2(0.5f, 0f);
        starsResultRect.pivot = new Vector2(0.5f, 0f);
        starsResultRect.anchoredPosition = new Vector2(0f, 30f);
        starsResultRect.sizeDelta = new Vector2(260f, 70f);

        GameObject[] resultStars = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            Image star = CreateStarImage(starsResultRoot.transform, "Star" + (i + 1));
            RectTransform starRect = star.GetComponent<RectTransform>();
            starRect.anchorMin = new Vector2(0.5f, 0.5f);
            starRect.anchorMax = new Vector2(0.5f, 0.5f);
            starRect.pivot = new Vector2(0.5f, 0.5f);
            starRect.anchoredPosition = new Vector2((i - 1) * 76f, 0f);
            starRect.sizeDelta = new Vector2(64f, 64f);
            resultStars[i] = star.gameObject;
        }

        // Wire StepCounterUI fields
        StepCounterUI stepCounterUI = hud.GetComponent<StepCounterUI>();
        SerializedObject stepSo = new SerializedObject(stepCounterUI);
        stepSo.FindProperty("stepCountText").objectReferenceValue = stepText;
        stepSo.FindProperty("optimalStepsText").objectReferenceValue = optimalText;
        stepSo.FindProperty("stepCountBackground").objectReferenceValue = hudBg;
        SerializedProperty hudStarsProp = stepSo.FindProperty("stars");
        hudStarsProp.arraySize = hudStars.Length;
        for (int i = 0; i < hudStars.Length; i++)
        {
            hudStarsProp.GetArrayElementAtIndex(i).objectReferenceValue = hudStars[i];
        }
        stepSo.ApplyModifiedPropertiesWithoutUndo();

        // Wire LevelResultUI fields
        LevelResultUI levelResultUI = root.AddComponent<LevelResultUI>();
        SerializedObject resultSo = new SerializedObject(levelResultUI);
        resultSo.FindProperty("resultRoot").objectReferenceValue = resultPanel;
        resultSo.FindProperty("resultText").objectReferenceValue = resultText;
        SerializedProperty resultStarsProp = resultSo.FindProperty("stars");
        resultStarsProp.arraySize = resultStars.Length;
        for (int i = 0; i < resultStars.Length; i++)
        {
            resultStarsProp.GetArrayElementAtIndex(i).objectReferenceValue = resultStars[i];
        }
        resultSo.ApplyModifiedPropertiesWithoutUndo();

        // Hidden by default; LevelResultUI will show on win.
        resultPanel.SetActive(false);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("Created Puzzle UI prefab at: " + PrefabPath);
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

    private static Image CreateStarImage(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = Color.yellow;
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(40f, 40f);
        return image;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetTopLeft(RectTransform rect, Vector2 anchoredPos, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
    }

    private static void SetCenter(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
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
#endif

