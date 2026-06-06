using System;
using System.Collections.Generic;

[Serializable]
public struct InitialPieceData
{
    public PlayerSide owner;
    public PieceType pieceType;
    public BoardCoord position;
    public List<Direction> frontDirections;
    public List<Direction> backDirections;
}
