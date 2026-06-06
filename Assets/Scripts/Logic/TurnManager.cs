using UnityEngine;

public class TurnManager
{
    private GameRulesSO rules;
    private PlayerSide currentPlayer;
    private int turnNumber;
    private int movesRemaining;
    private int maxMovesThisTurn;
    private PieceModel lastMovedPiece;
    private bool silent;

    public PlayerSide CurrentPlayer => currentPlayer;
    public int TurnNumber => turnNumber;
    public int MovesRemaining => movesRemaining;
    public int MaxMovesThisTurn => maxMovesThisTurn;
    public PieceModel LastMovedPiece => lastMovedPiece;

    public TurnManager(GameRulesSO rules, bool silent = false)
    {
        this.rules = rules;
        this.silent = silent;
        currentPlayer = PlayerSide.Player1;
        turnNumber = 1;
        maxMovesThisTurn = rules.firstTurnMoveCount;
        movesRemaining = maxMovesThisTurn;
        lastMovedPiece = null;
    }

    public void OnMoveCompleted(PieceModel movedPiece)
    {
        lastMovedPiece = movedPiece;
        movesRemaining--;
        if (!silent) Debug.Log($"[Turn] {currentPlayer} moved. Remaining moves this turn: {movesRemaining}");
    }

    public bool CanMoveSamePieceAgain(PieceModel piece)
    {
        if (!rules.allowSamePieceTwicePerTurn) return false;
        return movesRemaining > 0 && piece.canActThisTurn && !piece.spawnedThisTurn;
    }

    public bool IsTurnOver()
    {
        return movesRemaining <= 0;
    }

    public void EndTurnEarly()
    {
        movesRemaining = 0;
        if (!silent) Debug.Log($"[Turn] Turn ended early for {currentPlayer}");
    }

    public void StartNextTurn()
    {
        turnNumber++;
        currentPlayer = currentPlayer == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;

        if (turnNumber == 2)
        {
            maxMovesThisTurn = rules.normalTurnMoveCount;
        }
        else
        {
            maxMovesThisTurn = rules.normalTurnMoveCount;
        }

        movesRemaining = maxMovesThisTurn;
        lastMovedPiece = null;
        if (!silent) Debug.Log($"[Turn] === Turn {turnNumber} / {currentPlayer} starts ({movesRemaining} moves) ===");
    }

    public void SetMovesRemaining(int count)
    {
        movesRemaining = count;
    }

    public bool CanMoveThisTurn(PieceModel piece)
    {
        if (!piece.canActThisTurn || piece.spawnedThisTurn) return false;
        if (movesRemaining <= 0) return false;
        return true;
    }
}
