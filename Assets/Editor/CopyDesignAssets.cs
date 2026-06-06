using UnityEngine;
using UnityEditor;

public static class CopyDesignAssets
{
    private static readonly string[] Files = {
        "2-phase blue.png",
        "2-phase red.png",
        "1-phase blue.png",
        "1-phase red.png",
        "arrow.png",
        "2-phase background.png"
    };

    [MenuItem("Tools/Copy Design Assets to Resources")]
    public static void Copy()
    {
        string srcDir = "Assets/Design";
        string dstDir = "Assets/Resources/Design";

        if (!AssetDatabase.IsValidFolder(dstDir))
        {
            string parent = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateFolder(parent, "Design");
        }

        int count = 0;
        foreach (var file in Files)
        {
            string src = $"{srcDir}/{file}";
            string dst = $"{dstDir}/{file}";
            if (AssetDatabase.LoadAssetAtPath<Object>(src) != null)
            {
                if (AssetDatabase.CopyAsset(src, dst))
                {
                    Debug.Log($"Copied: {src} -> {dst}");
                    count++;
                }
            }
            else
            {
                Debug.LogWarning($"Source not found: {src}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Design asset copy complete. {count}/{Files.Length} files copied.");
    }
}
