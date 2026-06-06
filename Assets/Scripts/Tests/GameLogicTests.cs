using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GameLogicTests
{
    private BoardState board;
    private GameRulesSO rules;
    private MoveResolver moveResolver;
    private CaptureResolver captureResolver;
    private PlacementResolver placementResolver;
    private TurnManager turnManager;

    [SetUp]
    public void Setup()
    {
        rules = ScriptableObject.CreateInstance<GameRulesSO>();
        rules.boardWidth = 5;
        rules.boardHeight = 5;
        rules.firstTurnMoveCount = 1;
        rules.normalTurnMoveCount = 2;
        rules.allowSamePieceTwicePerTurn = true;
        rules.captureEndsTurnImmediately = true;
        rules.splitPlacementOwner = SplitPlacementOwner.CapturedOwner;
        rules.splitPlacementTiming = SplitPlacementTiming.ImmediateAfterCapture;
        rules.splitPlacementArea = SplitPlacementArea.AnyEmptyCell;
        rules.splitDirectionMode = SplitDirectionMode.FreeChoice;
        rules.splitPlacedPiecesCanActThisTurn = false;

        board = new BoardState(rules.boardWidth, rules.boardHeight);
        moveResolver = new MoveResolver(board, rules);
        captureResolver = new CaptureResolver(board, rules);
        placementResolver = new PlacementResolver(board, rules);
        turnManager = new TurnManager(rules);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(rules);
    }

    private PieceModel CreatePiece(PlayerSide owner, PieceType type, BoardCoord pos,
        List<Direction> frontDirs = null, List<Direction> backDirs = null, bool frontActive = true)
    {
        if (frontDirs == null) frontDirs = new List<Direction> { Direction.Right };
        if (backDirs == null) backDirs = new List<Direction> { Direction.Left };
        int id = board.GeneratePieceId();
        var piece = new PieceModel(id, owner, type, pos, frontDirs, backDirs, frontActive);
        board.AddPiece(piece);
        return piece;
    }

    [Test]
    public void OnePhasePiece_MovesOneStepInArrowDirection()
    {
        var dirs = new List<Direction> { Direction.Up };
        var piece = CreatePiece(PlayerSide.Player1, PieceType.OnePhase, new BoardCoord(2, 2), dirs, dirs);
        var moves = moveResolver.GetLegalMovesForPiece(piece);
        Assert.AreEqual(1, moves.Count);
        Assert.AreEqual(new BoardCoord(2, 3), moves[0]);
    }

    [Test]
    public void TwoPhasePiece_MovesOneStepInCurrentFaceDirection()
    {
        var piece = CreatePiece(PlayerSide.Player1, PieceType.TwoPhase, new BoardCoord(2, 2),
            new List<Direction> { Direction.Right }, new List<Direction> { Direction.Left });
        var moves = moveResolver.GetLegalMovesForPiece(piece);
        Assert.AreEqual(1, moves.Count);
        Assert.AreEqual(new BoardCoord(3, 2), moves[0]);
    }

    [Test]
    public void TwoPhasePiece_FlipsAfterMove()
    {
        var piece = CreatePiece(PlayerSide.Player1, PieceType.TwoPhase, new BoardCoord(2, 2),
            new List<Direction> { Direction.Right }, new List<Direction> { Direction.Left });
        CollectionAssert.AreEqual(new[] { Direction.Right }, piece.GetCurrentFaceDirections());
        piece.Flip();
        CollectionAssert.AreEqual(new[] { Direction.Left }, piece.GetCurrentFaceDirections());
        piece.Flip();
        CollectionAssert.AreEqual(new[] { Direction.Right }, piece.GetCurrentFaceDirections());
    }

    [Test]
    public void CannotMoveOutsideBoard()
    {
        var dirs = new List<Direction> { Direction.Down };
        var piece = CreatePiece(PlayerSide.Player1, PieceType.OnePhase, new BoardCoord(0, 0), dirs, dirs);
        var moves = moveResolver.GetLegalMovesForPiece(piece);
        Assert.AreEqual(0, moves.Count);
    }

    [Test]
    public void CannotMoveToCellOccupiedByOwnPiece()
    {
        var p1 = CreatePiece(PlayerSide.Player1, PieceType.TwoPhase, new BoardCoord(2, 2),
            new List<Direction> { Direction.Right }, new List<Direction> { Direction.Left });
        var dirs = new List<Direction> { Direction.Right };
        CreatePiece(PlayerSide.Player1, PieceType.OnePhase, new BoardCoord(3, 2), dirs, dirs);
        var moves = moveResolver.GetLegalMovesForPiece(p1);
        Assert.AreEqual(0, moves.Count);
    }

    [Test]
    public void CanMoveToCellOccupiedByOpponent()
    {
        var p1 = CreatePiece(PlayerSide.Player1, PieceType.TwoPhase, new BoardCoord(2, 2),
            new List<Direction> { Direction.Right }, new List<Direction> { Direction.Left });
        var dirs = new List<Direction> { Direction.Left };
        CreatePiece(PlayerSide.Player2, PieceType.OnePhase, new BoardCoord(3, 2), dirs, dirs);
        var moves = moveResolver.GetLegalMovesForPiece(p1);
        Assert.AreEqual(1, moves.Count);
        Assert.AreEqual(new BoardCoord(3, 2), moves[0]);
    }

    [Test]
    public void FirstTurn_HasOneMove()
    {
        Assert.AreEqual(1, turnManager.MovesRemaining);
    }

    [Test]
    public void NormalTurn_HasTwoMoves()
    {
        turnManager.StartNextTurn();
        Assert.AreEqual(2, turnManager.MovesRemaining);
    }

    [Test]
    public void SamePieceCanMoveTwice_WhenAllowed()
    {
        var piece = CreatePiece(PlayerSide.Player1, PieceType.TwoPhase, new BoardCoord(2, 2),
            new List<Direction> { Direction.Right }, new List<Direction> { Direction.Left });
        turnManager.OnMoveCompleted(piece);
        Assert.IsTrue(turnManager.CanMoveSamePieceAgain(piece));
    }

    [Test]
    public void CaptureEndsTurnImmediately()
    {
        var p1 = CreatePiece(PlayerSide.Player1, PieceType.TwoPhase, new BoardCoord(2, 2),
            new List<Direction> { Direction.Right }, new List<Direction> { Direction.Left });
        var p2 = CreatePiece(PlayerSide.Player2, PieceType.TwoPhase, new BoardCoord(3, 2),
            new List<Direction> { Direction.Left }, new List<Direction> { Direction.Right });

        var result = captureResolver.ResolveCapture(p1, new BoardCoord(3, 2));
        Assert.IsFalse(result.capturedNothing);
        Assert.IsTrue(result.needsSplitPlacement);
        Assert.IsNotNull(result.splitPieces);
        Assert.AreEqual(2, result.splitPieces.Count);
    }

    [Test]
    public void CapturingOnePhasePiece_EndsGame()
    {
        var dirs = new List<Direction> { Direction.Left };
        var p1 = CreatePiece(PlayerSide.Player1, PieceType.TwoPhase, new BoardCoord(2, 2),
            new List<Direction> { Direction.Right }, new List<Direction> { Direction.Left });
        var p2 = CreatePiece(PlayerSide.Player2, PieceType.OnePhase, new BoardCoord(3, 2), dirs, dirs);

        var result = captureResolver.ResolveCapture(p1, new BoardCoord(3, 2));
        Assert.IsTrue(result.isGameOver);
        Assert.AreEqual(PlayerSide.Player1, result.winner);
    }

    [Test]
    public void CapturingTwoPhasePiece_EntersSplitPlacement()
    {
        var p1 = CreatePiece(PlayerSide.Player1, PieceType.TwoPhase, new BoardCoord(2, 2),
            new List<Direction> { Direction.Right }, new List<Direction> { Direction.Left });
        var p2 = CreatePiece(PlayerSide.Player2, PieceType.TwoPhase, new BoardCoord(3, 2),
            new List<Direction> { Direction.Left }, new List<Direction> { Direction.Right });

        var result = captureResolver.ResolveCapture(p1, new BoardCoord(3, 2));
        Assert.IsTrue(result.needsSplitPlacement);
        Assert.AreEqual(2, result.splitPieces.Count);
        Assert.AreEqual(PieceType.OnePhase, result.splitPieces[0].pieceType);
        Assert.AreEqual(PieceType.OnePhase, result.splitPieces[1].pieceType);
    }

    [Test]
    public void SplitPieces_OwnedByCapturedOwner_ByDefault()
    {
        var p1 = CreatePiece(PlayerSide.Player1, PieceType.TwoPhase, new BoardCoord(2, 2),
            new List<Direction> { Direction.Right }, new List<Direction> { Direction.Left });
        var p2 = CreatePiece(PlayerSide.Player2, PieceType.TwoPhase, new BoardCoord(3, 2),
            new List<Direction> { Direction.Left }, new List<Direction> { Direction.Right });

        var result = captureResolver.ResolveCapture(p1, new BoardCoord(3, 2));
        Assert.AreEqual(PlayerSide.Player2, result.splitPieces[0].owner);
        Assert.AreEqual(PlayerSide.Player2, result.splitPieces[1].owner);
    }

    [Test]
    public void SplitPieces_CannotActOnPlacementTurn_ByDefault()
    {
        var p1 = CreatePiece(PlayerSide.Player1, PieceType.TwoPhase, new BoardCoord(2, 2),
            new List<Direction> { Direction.Right }, new List<Direction> { Direction.Left });
        var p2 = CreatePiece(PlayerSide.Player2, PieceType.TwoPhase, new BoardCoord(3, 2),
            new List<Direction> { Direction.Left }, new List<Direction> { Direction.Right });

        var result = captureResolver.ResolveCapture(p1, new BoardCoord(3, 2));
        Assert.IsFalse(result.splitPieces[0].canActThisTurn);
        Assert.IsTrue(result.splitPieces[0].spawnedThisTurn);
    }

    [Test]
    public void SplitPlacement_ValidCellsAreEmpty()
    {
        var p1 = CreatePiece(PlayerSide.Player1, PieceType.TwoPhase, new BoardCoord(2, 2),
            new List<Direction> { Direction.Right }, new List<Direction> { Direction.Left });
        var p2 = CreatePiece(PlayerSide.Player2, PieceType.TwoPhase, new BoardCoord(3, 2),
            new List<Direction> { Direction.Left }, new List<Direction> { Direction.Right });

        var result = captureResolver.ResolveCapture(p1, new BoardCoord(3, 2));
        var validCells = placementResolver.GetValidPlacementCells(result.splitPieces[0], new BoardCoord(3, 2));

        Assert.IsFalse(validCells.Contains(new BoardCoord(2, 2)));
        Assert.IsFalse(validCells.Contains(new BoardCoord(3, 2)));
    }

    [Test]
    public void GamePhase_TransitionsCorrectly()
    {
        Assert.AreEqual(GamePhase.WaitingForPieceSelect, GamePhase.WaitingForPieceSelect);
    }

    [Test]
    public void BoardCoord_Equals_Works()
    {
        var a = new BoardCoord(1, 2);
        var b = new BoardCoord(1, 2);
        var c = new BoardCoord(2, 1);
        Assert.IsTrue(a.Equals(b));
        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, c);
    }

    [Test]
    public void BoardCoord_Offset_ComputesCorrectDirection()
    {
        Assert.AreEqual(new BoardCoord(0, 1), BoardCoordUtil.Offset(Direction.Up));
        Assert.AreEqual(new BoardCoord(0, -1), BoardCoordUtil.Offset(Direction.Down));
        Assert.AreEqual(new BoardCoord(-1, 0), BoardCoordUtil.Offset(Direction.Left));
        Assert.AreEqual(new BoardCoord(1, 0), BoardCoordUtil.Offset(Direction.Right));
    }

    [Test]
    public void BoardCoord_Opposite_ReturnsOppositeDirection()
    {
        Assert.AreEqual(Direction.Down, BoardCoordUtil.Opposite(Direction.Up));
        Assert.AreEqual(Direction.Right, BoardCoordUtil.Opposite(Direction.Left));
    }

    [Test]
    public void SplitPieces_CanBePlacedOnEmptyCells()
    {
        var p1 = CreatePiece(PlayerSide.Player1, PieceType.TwoPhase, new BoardCoord(2, 2),
            new List<Direction> { Direction.Right }, new List<Direction> { Direction.Left });
        var p2 = CreatePiece(PlayerSide.Player2, PieceType.TwoPhase, new BoardCoord(3, 2),
            new List<Direction> { Direction.Left }, new List<Direction> { Direction.Right });

        var result = captureResolver.ResolveCapture(p1, new BoardCoord(3, 2));
        var splitPiece = result.splitPieces[0];
        var validCells = placementResolver.GetValidPlacementCells(splitPiece, new BoardCoord(3, 2));
        Assert.IsTrue(validCells.Count > 0);

        var target = validCells[0];
        placementResolver.PlaceSplitPiece(splitPiece, target, new List<Direction> { Direction.Up });
        Assert.AreEqual(target, splitPiece.currentPosition);
        Assert.IsTrue(board.IsOccupied(target));
    }

    [Test]
    public void Player1OnePhaseCaptured_GameOver()
    {
        var dirs = new List<Direction> { Direction.Right };
        var p1 = CreatePiece(PlayerSide.Player1, PieceType.OnePhase, new BoardCoord(2, 2), dirs, dirs);
        var p2 = CreatePiece(PlayerSide.Player2, PieceType.TwoPhase, new BoardCoord(1, 2),
            new List<Direction> { Direction.Right }, new List<Direction> { Direction.Left });

        var result = captureResolver.ResolveCapture(p2, new BoardCoord(2, 2));
        Assert.IsTrue(result.isGameOver);
        Assert.AreEqual(PlayerSide.Player2, result.winner);
    }

    [Test]
    public void Player2OnePhaseCaptured_GameOver()
    {
        var dirs = new List<Direction> { Direction.Left };
        var p1 = CreatePiece(PlayerSide.Player1, PieceType.TwoPhase, new BoardCoord(2, 2),
            new List<Direction> { Direction.Right }, new List<Direction> { Direction.Left });
        var p2 = CreatePiece(PlayerSide.Player2, PieceType.OnePhase, new BoardCoord(3, 2), dirs, dirs);

        var result = captureResolver.ResolveCapture(p1, new BoardCoord(3, 2));
        Assert.IsTrue(result.isGameOver);
        Assert.AreEqual(PlayerSide.Player1, result.winner);
    }
}
