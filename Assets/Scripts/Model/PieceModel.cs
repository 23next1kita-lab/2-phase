using System;
using System.Collections.Generic;

public class PieceModel
{
    public int pieceId;
    public PlayerSide owner;
    public PieceType pieceType;
    public BoardCoord currentPosition;
    public bool isFrontFaceActive;
    public bool canActThisTurn;
    public bool spawnedThisTurn;

    public List<Direction> initialFrontDirections;
    public List<Direction> initialBackDirections;
    public List<Direction> currentFaceDirections;
    private List<Direction> hiddenFaceDirections;

    public PieceModel(int pieceId, PlayerSide owner, PieceType pieceType, BoardCoord position,
        List<Direction> frontDirs, List<Direction> backDirs, bool isFrontFaceActive)
    {
        this.pieceId = pieceId;
        this.owner = owner;
        this.pieceType = pieceType;
        this.currentPosition = position;
        this.isFrontFaceActive = isFrontFaceActive;
        this.canActThisTurn = true;
        this.spawnedThisTurn = false;
        this.initialFrontDirections = new List<Direction>(frontDirs ?? new List<Direction>());
        this.initialBackDirections = new List<Direction>(backDirs ?? new List<Direction>());
        this.currentFaceDirections = new List<Direction>(
            isFrontFaceActive ? frontDirs : backDirs ?? new List<Direction>());
        this.hiddenFaceDirections = new List<Direction>(
            isFrontFaceActive ? backDirs : frontDirs ?? new List<Direction>());
    }

    public List<Direction> GetCurrentFaceDirections()
    {
        return currentFaceDirections;
    }

    public List<Direction> GetOppositeFaceDirections()
    {
        if (pieceType != PieceType.TwoPhase)
            return currentFaceDirections;
        return hiddenFaceDirections;
    }

    public void FlipAcross(Direction moveDirection)
    {
        var transformed = TransformDirections(currentFaceDirections, moveDirection);
        currentFaceDirections = TransformDirections(hiddenFaceDirections, moveDirection);
        hiddenFaceDirections = transformed;
        isFrontFaceActive = !isFrontFaceActive;
    }

    public void Flip()
    {
        FlipAcross(Direction.Right);
    }

    public static List<Direction> TransformDirections(List<Direction> dirs, Direction moveDir)
    {
        var result = new List<Direction>();
        foreach (var d in dirs)
            result.Add(TransformDirection(d, moveDir));
        return result;
    }

    public static Direction TransformDirection(Direction dir, Direction moveDir)
    {
        return moveDir switch
        {
            Direction.Right or Direction.Left => RefAcrossVertical(dir),
            Direction.Up or Direction.Down => RefAcrossHorizontal(dir),
            Direction.UpRight or Direction.DownLeft => RotateCCW(RefAcrossVertical(dir)),
            Direction.UpLeft or Direction.DownRight => RotateCW(RefAcrossVertical(dir)),
            _ => dir
        };
    }

    private static Direction RefAcrossVertical(Direction d) => d switch
    {
        Direction.Right => Direction.Left,
        Direction.Left => Direction.Right,
        Direction.UpRight => Direction.UpLeft,
        Direction.UpLeft => Direction.UpRight,
        Direction.DownRight => Direction.DownLeft,
        Direction.DownLeft => Direction.DownRight,
        _ => d
    };

    private static Direction RefAcrossHorizontal(Direction d) => d switch
    {
        Direction.Up => Direction.Down,
        Direction.Down => Direction.Up,
        Direction.UpLeft => Direction.DownLeft,
        Direction.UpRight => Direction.DownRight,
        Direction.DownLeft => Direction.UpLeft,
        Direction.DownRight => Direction.UpRight,
        _ => d
    };

    private static Direction RotateCW(Direction d) => d switch
    {
        Direction.Up => Direction.Right,
        Direction.Right => Direction.Down,
        Direction.Down => Direction.Left,
        Direction.Left => Direction.Up,
        Direction.UpRight => Direction.DownRight,
        Direction.DownRight => Direction.DownLeft,
        Direction.DownLeft => Direction.UpLeft,
        Direction.UpLeft => Direction.UpRight,
        _ => d
    };

    private static Direction RotateCCW(Direction d) => d switch
    {
        Direction.Up => Direction.Left,
        Direction.Left => Direction.Down,
        Direction.Down => Direction.Right,
        Direction.Right => Direction.Up,
        Direction.UpRight => Direction.UpLeft,
        Direction.UpLeft => Direction.DownLeft,
        Direction.DownLeft => Direction.DownRight,
        Direction.DownRight => Direction.UpRight,
        _ => d
    };

    public void RotateCW()
    {
        var rotated = new List<Direction>();
        foreach (var d in currentFaceDirections)
        {
            rotated.Add(d switch
            {
                Direction.Up => Direction.Right,
                Direction.Right => Direction.Down,
                Direction.Down => Direction.Left,
                Direction.Left => Direction.Up,
                Direction.UpRight => Direction.DownRight,
                Direction.DownRight => Direction.DownLeft,
                Direction.DownLeft => Direction.UpLeft,
                Direction.UpLeft => Direction.UpRight,
                _ => d
            });
        }
        currentFaceDirections = rotated;
        initialFrontDirections = rotated;
    }

    public void SetPosition(BoardCoord newPos)
    {
        currentPosition = newPos;
    }

    public PieceModel Clone()
    {
        var clone = new PieceModel(pieceId, owner, pieceType, currentPosition,
            new List<Direction>(initialFrontDirections),
            new List<Direction>(initialBackDirections), isFrontFaceActive);
        clone.canActThisTurn = canActThisTurn;
        clone.spawnedThisTurn = spawnedThisTurn;
        return clone;
    }

    public override string ToString()
    {
        string dirs = string.Join(",", currentFaceDirections);
        return $"[Piece {pieceId}] {owner} at {currentPosition} " +
               $"face={(isFrontFaceActive ? "Front" : "Back")} " +
               $"dirs=[{dirs}] canAct={canActThisTurn}";
    }
}