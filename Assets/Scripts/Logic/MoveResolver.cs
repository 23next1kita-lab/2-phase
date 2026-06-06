using System.Collections.Generic;
using UnityEngine;

public class MoveResolver
{
    private BoardState board;
    private GameRulesSO rules;
    private bool silent;

    public MoveResolver(BoardState board, GameRulesSO rules, bool silent = false)
    {
        this.board = board;
        this.rules = rules;
        this.silent = silent;
    }

    public List<BoardCoord> GetLegalMovesForPiece(PieceModel piece)
    {
        var moves = new List<BoardCoord>();
        if (piece == null || !piece.canActThisTurn || piece.spawnedThisTurn)
            return moves;

        foreach (var dir in piece.GetCurrentFaceDirections())
        {
            BoardCoord offset = BoardCoordUtil.Offset(dir);
            BoardCoord target = new BoardCoord(
                piece.currentPosition.x + offset.x,
                piece.currentPosition.y + offset.y);

            if (!board.IsValidCoord(target))
                continue;

            if (board.HasPieceOfSide(target, piece.owner))
                continue;

            if (!moves.Contains(target))
                moves.Add(target);
        }

        return moves;
    }

    public bool IsMoveValid(PieceModel piece, BoardCoord target)
    {
        var legal = GetLegalMovesForPiece(piece);
        return legal.Contains(target);
    }

    public void ApplyMove(PieceModel piece, BoardCoord target)
    {
        board.MovePiece(piece, target);
        if (!silent) Debug.Log($"[Move] {piece.owner} piece {piece.pieceId} moved to {target} " +
                  $"face={(piece.isFrontFaceActive ? "Front" : "Back")}");
    }

    public void ApplyFlip(PieceModel piece, Direction moveDirection)
    {
        if (piece.pieceType != PieceType.TwoPhase) return;
        piece.FlipAcross(moveDirection);
        if (!silent) Debug.Log($"[Flip] Piece {piece.pieceId} flipped across {moveDirection} " +
                  $"dirs=[{string.Join(",", piece.GetCurrentFaceDirections())}]");
    }
}
