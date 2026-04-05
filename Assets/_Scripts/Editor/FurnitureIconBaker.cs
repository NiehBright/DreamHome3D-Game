#if UNITY_EDITOR
using System.IO;
using Runtime.Build;
using UnityEditor;
using UnityEngine;

public static class FurnitureIconBaker
{
    private const string OutputFolder = "Assets/_Art/ShopIcons";

    [MenuItem("Tools/DreamHome/Build/Bake Shop Icons (Selected Catalog)")]
    private static void BakeIconsForSelectedCatalog()
    {
        if (!(Selection.activeObject is FurnitureCatalogData catalog))
        {
            EditorUtility.DisplayDialog(
                "Bake Shop Icons",
                "Select a FurnitureCatalogData asset first.",
                "OK");
            return;
        }

        if (!EnsureFolder(OutputFolder))
        {
            return;
        }

        int bakedCount = 0;
        int skippedCount = 0;

        foreach (var item in catalog.Items)
        {
            if (!TryBakeIconForItem(item))
            {
                skippedCount++;
                continue;
            }

            bakedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Bake shop icons complete. Baked: {bakedCount}, Skipped: {skippedCount}.");
    }

    [MenuItem("Assets/DreamHome/Build/Bake Shop Icon (Selected Furniture Item)")]
    private static void BakeIconForSelectedItem()
    {
        if (!(Selection.activeObject is FurnitureItemData item))
        {
            return;
        }

        if (!EnsureFolder(OutputFolder))
        {
            return;
        }

        bool success = TryBakeIconForItem(item);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(success
            ? $"Baked shop icon for item '{item.name}'."
            : $"Failed to bake shop icon for item '{item.name}'.");
    }

    [MenuItem("Assets/DreamHome/Build/Bake Shop Icon (Selected Furniture Item)", true)]
    private static bool ValidateBakeIconForSelectedItem()
    {
        return Selection.activeObject is FurnitureItemData;
    }

    private static bool TryBakeIconForItem(FurnitureItemData item)
    {
        if (item == null || item.Prefab == null)
        {
            return false;
        }

        Texture2D preview = GetPrefabPreview(item.Prefab);
        if (preview == null)
        {
            Debug.LogWarning($"Could not generate preview for prefab '{item.Prefab.name}'.");
            return false;
        }

        byte[] pngData = preview.EncodeToPNG();
        if (pngData == null || pngData.Length == 0)
        {
            return false;
        }

        string fileName = SanitizeFileName(string.IsNullOrWhiteSpace(item.ItemId) ? item.name : item.ItemId) + ".png";
        string relativePath = Path.Combine(OutputFolder, fileName).Replace("\\", "/");
        string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

        File.WriteAllBytes(absolutePath, pngData);
        AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);

        if (!ConfigureAsSprite(relativePath))
        {
            return false;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
        if (sprite == null)
        {
            return false;
        }

        SerializedObject serializedItem = new SerializedObject(item);
        SerializedProperty iconProp = serializedItem.FindProperty("shopIcon");
        if (iconProp == null)
        {
            return false;
        }

        iconProp.objectReferenceValue = sprite;
        serializedItem.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        return true;
    }

    private static Texture2D GetPrefabPreview(GameObject prefab)
    {
        Texture2D preview = null;

        // AssetPreview can need a few frames before it becomes available.
        for (int i = 0; i < 40; i++)
        {
            preview = AssetPreview.GetAssetPreview(prefab);
            if (preview != null)
            {
                break;
            }
        }

        if (preview == null)
        {
            preview = AssetPreview.GetMiniThumbnail(prefab) as Texture2D;
        }

        if (preview == null)
        {
            return null;
        }

        return CopyToReadableTexture(preview);
    }

    private static Texture2D CopyToReadableTexture(Texture source)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;

        Graphics.Blit(source, renderTexture);
        RenderTexture.active = renderTexture;

        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);
        return readable;
    }

    private static bool ConfigureAsSprite(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return false;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        return true;
    }

    private static bool EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return true;
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

        return AssetDatabase.IsValidFolder(folderPath);
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "furniture_icon";
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char invalidChar in invalidChars)
        {
            name = name.Replace(invalidChar, '_');
        }

        return name.Trim();
    }
}
#endif

