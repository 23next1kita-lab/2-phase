using System.Collections.Generic;
using UnityEngine;

public class SplitPlacementController
{
    private GameManager gameManager;
    private UIManager uiManager;
    private List<GameObject> floatingPieces;
    private bool confirmButtonShown;
    private int piecesPlaced;
    private Transform parentTransform;

    public void Initialize(GameManager gm, UIManager ui)
    {
        gameManager = gm;
        uiManager = ui;
        floatingPieces = new List<GameObject>();
        piecesPlaced = 0;
        confirmButtonShown = false;
        parentTransform = gm.transform;
    }

    public void CreateFloatingPieces(List<PieceModel> splitPieces)
    {
        ClearFloatingPieces();
        piecesPlaced = 0;
        confirmButtonShown = false;

        int w = gameManager.GameRules.boardWidth;
        int h = gameManager.GameRules.boardHeight;
        float originX = -(w - 1) * 0.5f;
        float originY = -(h - 1) * 0.5f;

        for (int i = 0; i < splitPieces.Count; i++)
        {
            var piece = splitPieces[i];
            float xOff = originX + w + 1f;
            float yOff = originY + (i == 0 ? 1.5f : -1.5f);

            GameObject obj = new GameObject($"FloatingSplit_{i}");
            obj.transform.SetParent(parentTransform);

            obj.transform.position = new Vector3(xOff, yOff, -0.2f);

            var floating = obj.AddComponent<SplitPieceFloating>();
            floating.Initialize(piece, gameManager, this, obj.transform.position);

            floatingPieces.Add(obj);
        }
    }

    public void NotifyPiecePlaced()
    {
        piecesPlaced = Mathf.Min(2, piecesPlaced + 1);
        UpdateConfirmButton();
    }

    public void NotifyPieceRemoved()
    {
        piecesPlaced = Mathf.Max(0, piecesPlaced - 1);
        UpdateConfirmButton();
    }

    private void UpdateConfirmButton()
    {
        if (piecesPlaced >= 2 && !confirmButtonShown)
        {
            confirmButtonShown = true;
            if (uiManager != null)
                uiManager.ShowConfirmButton(true);
        }
        else if (piecesPlaced < 2 && confirmButtonShown)
        {
            confirmButtonShown = false;
            if (uiManager != null)
                uiManager.ShowConfirmButton(false);
        }
    }

    public List<PieceModel> ConfirmPlacement()
    {
        var placed = new List<PieceModel>();
        foreach (var obj in floatingPieces)
        {
            if (obj == null) continue;
            var floating = obj.GetComponent<SplitPieceFloating>();
            if (floating != null && floating.IsOnBoard)
                placed.Add(floating.Piece);
        }

        if (placed.Count < 2)
        {
            return null;
        }

        foreach (var obj in floatingPieces)
        {
            if (obj == null) continue;
            var floating = obj.GetComponent<SplitPieceFloating>();
            if (floating != null)
                floating.CreateBoardPieceView();
        }

        ClearFloatingPieces();
        if (uiManager != null)
        {
            uiManager.ShowConfirmButton(false);
            uiManager.ClearMessage();
        }
        confirmButtonShown = false;

        return placed;
    }

    public void ClearFloatingPieces()
    {
        foreach (var obj in floatingPieces)
            if (obj != null) GameObject.Destroy(obj);
        floatingPieces.Clear();
    }
}
