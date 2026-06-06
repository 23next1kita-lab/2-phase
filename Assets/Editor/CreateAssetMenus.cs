using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CreateAssetMenus
{
    [MenuItem("2-Phase/Create GameRules Asset")]
    public static void CreateGameRules()
    {
        var rules = ScriptableObject.CreateInstance<GameRulesSO>();
        string path = "Assets/ScriptableObjects/GameRules.asset";
        System.IO.Directory.CreateDirectory("Assets/ScriptableObjects");
        AssetDatabase.CreateAsset(rules, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created GameRules at {path}");
    }

    [MenuItem("2-Phase/Create Default Layout Asset")]
    public static void CreateDefaultLayout()
    {
        var layout = ScriptableObject.CreateInstance<InitialPieceLayoutSO>();
        System.IO.Directory.CreateDirectory("Assets/ScriptableObjects");

        int w = 7, h = 7;

        for (int y = 0; y < h; y++)
        {
            layout.pieces.Add(new InitialPieceData
            {
                owner = PlayerSide.Player1,
                pieceType = PieceType.TwoPhase,
                position = new BoardCoord(0, y),
                frontDirections = new List<Direction> { Direction.Right },
                backDirections = new List<Direction> { Direction.Left }
            });
        }

        for (int y = 0; y < h; y++)
        {
            layout.pieces.Add(new InitialPieceData
            {
                owner = PlayerSide.Player2,
                pieceType = PieceType.TwoPhase,
                position = new BoardCoord(w - 1, y),
                frontDirections = new List<Direction> { Direction.Left },
                backDirections = new List<Direction> { Direction.Right }
            });
        }

        string path = "Assets/ScriptableObjects/DefaultLayout.asset";
        AssetDatabase.CreateAsset(layout, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created DefaultLayout at {path} (left/right columns)");
    }

    [MenuItem("2-Phase/Full Setup (SO + Prefabs)")]
    public static void FullSetup()
    {
        CreateGameRules();
        CreateDefaultLayout();
        Debug.Log("Full setup complete! Assign assets to GameBootstrap in your scene.");
    }
}
