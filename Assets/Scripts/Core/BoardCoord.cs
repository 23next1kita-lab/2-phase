using System;

[Serializable]
public struct BoardCoord : IEquatable<BoardCoord>
{
    public int x;
    public int y;

    public BoardCoord(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public bool Equals(BoardCoord other) => x == other.x && y == other.y;
    public override bool Equals(object obj) => obj is BoardCoord other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(x, y);
    public static bool operator ==(BoardCoord a, BoardCoord b) => a.Equals(b);
    public static bool operator !=(BoardCoord a, BoardCoord b) => !a.Equals(b);
    public override string ToString() => $"({x}, {y})";
}
