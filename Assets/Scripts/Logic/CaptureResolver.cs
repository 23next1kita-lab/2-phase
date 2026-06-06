using System.Collections.Generic;
using UnityEngine;

public class CaptureResolver
{
    private BoardState board;
    private GameRulesSO rules;
    private bool silent;

    public CaptureResolver(BoardState board, GameRulesSO rules, bool silent = false)
    {
        this.board = board;
        this.rules = rules;
        this.silent = silent;
    }

    public CaptureResult ResolveCapture(PieceModel mover, BoardCoord target)
    {
        var result = new CaptureResult();
        result.capturingPlayer = mover.owner;

        PieceModel occupant = board.GetPieceAt(target);
        if (occupant == null || occupant.owner == mover.owner)
        {
            result.capturedNothing = true;
            return result;
        }

        result.capturedPiece = occupant;

        if (occupant.pieceType == PieceType.OnePhase)
        {
            result.isGameOver = true;
            result.winner = mover.owner;
            if (!silent) Debug.Log($"[Capture] {mover.owner} captured opponent's OnePhase piece! Game over. Winner: {mover.owner}");
        }
        else if (occupant.pieceType == PieceType.TwoPhase)
        {
            result.needsSplitPlacement = true;
            result.splitPieces = CreateSplitPieces(occupant, mover.owner);
            if (!silent) Debug.Log($"[Capture] {mover.owner} captured opponent's TwoPhase piece! Preparing split placement.");
        }

        board.RemovePiece(occupant);
        if (!silent) Debug.Log($"[Capture] {mover.owner}'s piece {mover.pieceId} captured {occupant.owner}'s piece {occupant.pieceId} at {target}");

        return result;
    }

    public CaptureResult ResolveCaptureWithCaptured(PieceModel capturedPiece, PlayerSide capturingPlayer)
    {
        var result = new CaptureResult();
        result.capturingPlayer = capturingPlayer;
        result.capturedPiece = capturedPiece;

        if (capturedPiece.pieceType == PieceType.OnePhase)
        {
            result.isGameOver = true;
            result.winner = capturingPlayer;
            if (!silent) Debug.Log($"[Capture] {capturingPlayer} captured opponent's OnePhase piece! Game over. Winner: {capturingPlayer}");
        }
        else if (capturedPiece.pieceType == PieceType.TwoPhase)
        {
            result.needsSplitPlacement = true;
            result.splitPieces = CreateSplitPieces(capturedPiece, capturingPlayer);
            if (!silent) Debug.Log($"[Capture] {capturingPlayer} captured opponent's TwoPhase piece! Preparing split placement.");
        }

        return result;
    }

    private List<PieceModel> CreateSplitPieces(PieceModel captured, PlayerSide capturingPlayer)
    {
        var pieces = new List<PieceModel>();
        int id1 = board.GeneratePieceId();
        int id2 = board.GeneratePieceId();

        PlayerSide splitOwner = captured.owner;

        var frontDirs = new List<Direction>(captured.initialFrontDirections);
        var backDirs = new List<Direction>(captured.initialBackDirections);

        var p1 = new PieceModel(id1, splitOwner, PieceType.OnePhase, new BoardCoord(-1, -1),
            frontDirs, frontDirs, true);
        p1.canActThisTurn = true;
        p1.spawnedThisTurn = false;

        var p2 = new PieceModel(id2, splitOwner, PieceType.OnePhase, new BoardCoord(-1, -1),
            backDirs, backDirs, true);
        p2.canActThisTurn = true;
        p2.spawnedThisTurn = false;

        pieces.Add(p1);
        pieces.Add(p2);
        return pieces;
    }
}

public class CaptureResult
{
    public bool capturedNothing;
    public PieceModel capturedPiece;
    public bool isGameOver;
    public PlayerSide winner;
    public bool needsSplitPlacement;
    public List<PieceModel> splitPieces;
    public PlayerSide capturingPlayer;
}
