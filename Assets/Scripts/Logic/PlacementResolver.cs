using System.Collections.Generic;
using UnityEngine;

public class PlacementResolver
{
    private BoardState board;
    private GameRulesSO rules;
    private bool silent;

    public PlacementResolver(BoardState board, GameRulesSO rules, bool silent = false)
    {
        this.board = board;
        this.rules = rules;
        this.silent = silent;
    }

    public List<BoardCoord> GetValidPlacementCells(PieceModel piece, BoardCoord captureCell)
    {
        var cells = new List<BoardCoord>();

        for (int x = 0; x < board.width; x++)
        {
            for (int y = 0; y < board.height; y++)
            {
                var coord = new BoardCoord(x, y);
                if (board.IsOccupied(coord)) continue;
                cells.Add(coord);
            }
        }

        return cells;
    }

    public void PlaceSplitPiece(PieceModel piece, BoardCoord position, List<Direction> directions)
    {
        piece.currentPosition = position;
        piece.currentFaceDirections = directions;
        piece.initialFrontDirections = new List<Direction>(directions);
        board.AddPiece(piece);
        if (!silent) Debug.Log($"[Placement] Split OnePhase piece {piece.pieceId} placed at {position} facing [{string.Join(",", directions)}]");
    }
}
