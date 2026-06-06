using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardState
{
    public int width;
    public int height;
    private Dictionary<BoardCoord, PieceModel> occupancy;
    private List<PieceModel> allPieces;
    private int nextPieceId;

    public BoardState(int width, int height)
    {
        this.width = width;
        this.height = height;
        occupancy = new Dictionary<BoardCoord, PieceModel>();
        allPieces = new List<PieceModel>();
        nextPieceId = 1;
    }

    public bool IsValidCoord(BoardCoord coord)
    {
        return coord.x >= 0 && coord.x < width && coord.y >= 0 && coord.y < height;
    }

    public bool IsOccupied(BoardCoord coord)
    {
        return occupancy.ContainsKey(coord);
    }

    public PieceModel GetPieceAt(BoardCoord coord)
    {
        occupancy.TryGetValue(coord, out PieceModel piece);
        return piece;
    }

    public List<PieceModel> GetAllPieces()
    {
        return allPieces.ToList();
    }

    public List<PieceModel> GetPiecesOf(PlayerSide side)
    {
        return allPieces.Where(p => p.owner == side).ToList();
    }

    public List<PieceModel> GetPiecesOf(PlayerSide side, PieceType type)
    {
        return allPieces.Where(p => p.owner == side && p.pieceType == type).ToList();
    }

    public List<BoardCoord> GetEmptyCells()
    {
        var empty = new List<BoardCoord>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var coord = new BoardCoord(x, y);
                if (!IsOccupied(coord))
                    empty.Add(coord);
            }
        }
        return empty;
    }

    public int GeneratePieceId()
    {
        return nextPieceId++;
    }

    public void AddPiece(PieceModel piece)
    {
        allPieces.Add(piece);
        if (IsValidCoord(piece.currentPosition))
            occupancy[piece.currentPosition] = piece;
    }

    public PieceModel GetPieceById(int id)
    {
        return allPieces.FirstOrDefault(p => p.pieceId == id);
    }

    public void RemovePiece(PieceModel piece)
    {
        allPieces.Remove(piece);
        occupancy.Remove(piece.currentPosition);
    }

    public void MovePiece(PieceModel piece, BoardCoord newPos)
    {
        if (piece == null) return;
        occupancy.Remove(piece.currentPosition);
        piece.SetPosition(newPos);
        occupancy[newPos] = piece;
    }

    public PieceModel GetOpponentAt(PieceModel mover, BoardCoord target)
    {
        if (!occupancy.TryGetValue(target, out PieceModel occupant)) return null;
        return occupant.owner != mover.owner ? occupant : null;
    }

    public bool HasPieceOfSide(BoardCoord coord, PlayerSide side)
    {
        if (!occupancy.TryGetValue(coord, out PieceModel piece)) return false;
        return piece.owner == side;
    }

    public List<PieceModel> GetActivePiecesOf(PlayerSide side)
    {
        return allPieces.Where(p => p.owner == side && p.canActThisTurn && !p.spawnedThisTurn).ToList();
    }

    public void ClearAllPieces()
    {
        allPieces.Clear();
        occupancy.Clear();
    }

    public void LogState()
    {
        Debug.Log("=== BoardState ===");
        Debug.Log($"Board: {width}x{height}, Pieces: {allPieces.Count}");
        foreach (var p in allPieces)
        {
            Debug.Log(p.ToString());
        }
        Debug.Log("=================");
    }
}
