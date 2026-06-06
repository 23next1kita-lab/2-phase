using UnityEngine;
using UnityEditor;
using Fusion;

public static class CreateNetworkPrefab
{
    [MenuItem("Tools/Setup Network Components")]
    public static void SetupNetworkComponents()
    {
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogError("No GameManager found in scene.");
            return;
        }

        var go = gm.gameObject;

        if (go.GetComponent<NetworkObject>() == null)
        {
            var no = go.AddComponent<NetworkObject>();
            Debug.Log("Added NetworkObject to GameManager.");
        }

        if (go.GetComponent<NetworkGameHandler>() == null)
        {
            go.AddComponent<NetworkGameHandler>();
            Debug.Log("Added NetworkGameHandler to GameManager.");
        }

        if (go.GetComponent<NetworkRunner>() == null)
        {
            go.AddComponent<NetworkRunner>();
            Debug.Log("Added NetworkRunner to GameManager.");
        }

        EditorUtility.SetDirty(go);
        Debug.Log("Network components setup complete. GameManager is ready for Fusion.");
    }
}
