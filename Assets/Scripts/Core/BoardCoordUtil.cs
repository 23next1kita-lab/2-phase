using System.Collections.Generic;

public static class BoardCoordUtil
{
    public static BoardCoord Offset(Direction dir)
    {
        return dir switch
        {
            Direction.Up => new BoardCoord(0, 1),
            Direction.Down => new BoardCoord(0, -1),
            Direction.Left => new BoardCoord(-1, 0),
            Direction.Right => new BoardCoord(1, 0),
            Direction.UpLeft => new BoardCoord(-1, 1),
            Direction.UpRight => new BoardCoord(1, 1),
            Direction.DownLeft => new BoardCoord(-1, -1),
            Direction.DownRight => new BoardCoord(1, -1),
            _ => new BoardCoord(0, 0)
        };
    }

    public static Direction Opposite(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            Direction.UpLeft => Direction.DownRight,
            Direction.UpRight => Direction.DownLeft,
            Direction.DownLeft => Direction.UpRight,
            Direction.DownRight => Direction.UpLeft,
            _ => Direction.Up
        };
    }

    public static List<Direction> AllDirections() => new List<Direction>
    {
        Direction.Up, Direction.Down, Direction.Left, Direction.Right,
        Direction.UpLeft, Direction.UpRight, Direction.DownLeft, Direction.DownRight
    };
}
