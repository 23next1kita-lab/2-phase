using UnityEngine;

[CreateAssetMenu(menuName = "2-Phase/Game Rules", fileName = "GameRules")]
public class GameRulesSO : ScriptableObject
{
    [Header("Board")]
    public int boardWidth = 7;
    public int boardHeight = 7;

    [Header("Turn")]
    public int firstTurnMoveCount = 1;
    public int normalTurnMoveCount = 2;
    public bool allowSamePieceTwicePerTurn = true;

    [Header("Capture")]
    public bool captureEndsTurnImmediately = true;

    [Header("Two-Phase Flip Timing")]
    public TwoPhaseFlipTiming twoPhaseFlipTiming = TwoPhaseFlipTiming.AfterMove;

    [Header("Time Control")]
    public int timeControlSeconds = 0;

    [Header("Random Direction")]
    public bool randomPieceDirections = false;

    [Header("Split Placement")]
    public SplitPlacementOwner splitPlacementOwner = SplitPlacementOwner.CapturingOwner;
    public SplitPlacementTiming splitPlacementTiming = SplitPlacementTiming.ImmediateAfterCapture;
    public SplitPlacementArea splitPlacementArea = SplitPlacementArea.AnyEmptyCell;
    public SplitDirectionMode splitDirectionMode = SplitDirectionMode.FreeChoice;
    public bool splitPlacedPiecesCanActThisTurn = false;

    public void LogSettings()
    {
        Debug.Log("=== GameRules Settings ===");
        Debug.Log($"Board: {boardWidth}x{boardHeight}");
        Debug.Log($"FirstTurnMoveCount: {firstTurnMoveCount}");
        Debug.Log($"NormalTurnMoveCount: {normalTurnMoveCount}");
        Debug.Log($"AllowSamePieceTwicePerTurn: {allowSamePieceTwicePerTurn}");
        Debug.Log($"CaptureEndsTurnImmediately: {captureEndsTurnImmediately}");
        Debug.Log($"TwoPhaseFlipTiming: {twoPhaseFlipTiming}");
        Debug.Log($"SplitPlacementOwner: {splitPlacementOwner}");
        Debug.Log($"SplitPlacementTiming: {splitPlacementTiming}");
        Debug.Log($"SplitPlacementArea: {splitPlacementArea}");
        Debug.Log($"SplitDirectionMode: {splitDirectionMode}");
        Debug.Log($"SplitPlacedPiecesCanActThisTurn: {splitPlacedPiecesCanActThisTurn}");
        Debug.Log("===========================");
    }
}
