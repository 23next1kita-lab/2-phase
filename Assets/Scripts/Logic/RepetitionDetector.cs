using System.Collections.Generic;
using UnityEngine;

public class RepetitionDetector
{
    private Dictionary<string, int> stateHistory = new Dictionary<string, int>();
    public int threshold = 20;

    public bool RecordAndCheck(BoardState board, PlayerSide currentPlayer, int turnNumber)
    {
        string hash = ComputeHash(board, currentPlayer);

        if (stateHistory.TryGetValue(hash, out int count))
        {
            count++;
            stateHistory[hash] = count;
            if (count >= threshold)
                return true;
        }
        else
        {
            stateHistory[hash] = 1;
        }

        return false;
    }

    public bool WouldRepeat(BoardState board, PlayerSide currentPlayer, int turnNumber)
    {
        string hash = ComputeHash(board, currentPlayer);

        if (stateHistory.TryGetValue(hash, out int count))
            return count >= threshold - 1;

        return false;
    }

    public bool WouldRepeatAfterMove(BoardState board, PlayerSide currentPlayer, int turnNumber,
        PieceModel piece, BoardCoord target, bool isCapture)
    {
        PlayerSide nextPlayer = currentPlayer == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;
        var pieces = board.GetAllPieces();
        var sb = new System.Text.StringBuilder();
        sb.Append((int)nextPlayer);
        sb.Append('|');
        foreach (var p in pieces)
        {
            int x = p.currentPosition.x, y = p.currentPosition.y;
            if (p.pieceId == piece.pieceId) { x = target.x; y = target.y; }
            if (isCapture && x == target.x && y == target.y && p.pieceId != piece.pieceId)
                continue;
            sb.Append(p.pieceId);
            sb.Append(':');
            sb.Append((int)p.owner);
            sb.Append(',');
            sb.Append((int)p.pieceType);
            sb.Append(',');
            sb.Append(p.isFrontFaceActive ? 1 : 0);
            sb.Append(',');
            sb.Append(x);
            sb.Append(',');
            sb.Append(y);
            sb.Append(';');
        }
        string hash = sb.ToString();
        if (stateHistory.TryGetValue(hash, out int count))
            return count >= threshold - 1;
        return false;
    }

    public void Clear()
    {
        stateHistory.Clear();
    }

    public string ComputeHash(BoardState board, PlayerSide currentPlayer)
    {
        var pieces = board.GetAllPieces();
        var sb = new System.Text.StringBuilder();
        sb.Append((int)currentPlayer);
        sb.Append('|');
        foreach (var p in pieces)
        {
            sb.Append(p.pieceId);
            sb.Append(':');
            sb.Append((int)p.owner);
            sb.Append(',');
            sb.Append((int)p.pieceType);
            sb.Append(',');
            sb.Append(p.isFrontFaceActive ? 1 : 0);
            sb.Append(',');
            sb.Append(p.currentPosition.x);
            sb.Append(',');
            sb.Append(p.currentPosition.y);
            sb.Append(';');
        }
        return sb.ToString();
    }
}
