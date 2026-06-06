using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CpuPlayer : MonoBehaviour
{
    private GameManager gm;
    private MoveResolver moveResolver;
    public PlayerSide cpuSide = PlayerSide.Player2;
    private Dictionary<int, BoardCoord> turnStartPositions = new Dictionary<int, BoardCoord>();

    public int DifficultyLevel { get; set; } = 1;
    public EvalWeights Weights { get; set; }

    private void Awake()
    {
        gm = FindObjectOfType<GameManager>();
        if (gm == null)
        {
            Debug.LogError("[CpuPlayer] GameManager not found.");
            enabled = false;
            return;
        }
        moveResolver = new MoveResolver(gm.BoardState, gm.GameRules);
        gm.OnGameStateChanged += OnGameStateChanged;

        Debug.Log($"[CpuPlayer] Awake: gm={gm != null}, boardState={gm?.BoardState}, gameRules={gm?.GameRules}, cpuSide={cpuSide}");

        if (Weights == null)
        {
            var loaded = WeightEvolution.LoadBestWeights();
            if (loaded != null)
            {
                Weights = loaded;
                Debug.Log("[CpuPlayer] Loaded evolved weights.");
            }
        }
    }

    private void OnGameStateChanged()
    {
        Debug.Log($"[CpuPlayer] OnGameStateChanged: phase={gm.CurrentPhase}, playMode={gm.CurrentPlayMode}, turn={gm.TurnManager?.CurrentPlayer}, cpuSide={cpuSide}");
        if (gm.CurrentPlayMode != GameManager.PlayMode.CPU &&
            gm.CurrentPlayMode != GameManager.PlayMode.CpuVsCpu) return;
        if (gm.TurnManager == null) return;
        if (gm.CurrentPhase == GamePhase.GameOver) return;

        if (gm.CurrentPhase == GamePhase.PlacingSplitPieces)
        {
            Debug.Log($"[CpuPlayer] Split phase: pendingPieces={gm.PendingSplitPieces?.Count}, owner={(gm.PendingSplitPieces != null && gm.PendingSplitPieces.Count > 0 ? gm.PendingSplitPieces[0].owner.ToString() : "n/a")}");
            if (gm.PendingSplitPieces != null && gm.PendingSplitPieces.Count > 0 &&
                gm.PendingSplitPieces[0].owner == cpuSide)
            {
                Debug.Log("[CpuPlayer] Starting split placement coroutine");
                StartCoroutine(DoCpuSplitPlacement());
            }
            else
            {
                Debug.Log("[CpuPlayer] Split pieces not owned by CPU, skipping");
            }
        }
        else if (gm.CurrentPhase == GamePhase.WaitingForPieceSelect)
        {
            if (gm.TurnManager.CurrentPlayer != cpuSide) return;
            turnStartPositions.Clear();
            foreach (var p in gm.BoardState.GetPiecesOf(cpuSide))
                turnStartPositions[p.pieceId] = p.currentPosition;
            StartCoroutine(DoCpuTurn());
        }
    }

    private IEnumerator DoCpuTurn()
    {
        yield return new WaitForSeconds(0.5f);

        if (gm.CurrentPhase != GamePhase.WaitingForPieceSelect) yield break;
        if (gm.TurnManager.CurrentPlayer != cpuSide) yield break;

        var movablePieces = GetMovablePieces();
        if (movablePieces.Count == 0)
        {
            Debug.Log("[CpuPlayer] No movable pieces. Skipping turn.");
            yield break;
        }

        PieceModel chosen;
        BoardCoord target;

        switch (DifficultyLevel)
        {
            case 1:
                (chosen, target) = PickRandom(movablePieces);
                break;
            case 2:
                (chosen, target) = PickGreedy(movablePieces);
                break;
            case 3:
                (chosen, target) = PickEvaluated(movablePieces);
                break;
            default:
                (chosen, target) = PickLookAhead(movablePieces);
                break;
        }

        gm.OnPieceClicked(chosen);
        yield return new WaitForSeconds(0.3f);

        if (gm.CurrentPhase != GamePhase.WaitingForDestinationSelect) yield break;
        if (gm.LegalMoves == null || gm.LegalMoves.Count == 0) yield break;

        gm.OnCellClicked(target);
    }

    private (PieceModel, BoardCoord) PickRandom(List<PieceModel> pieces)
    {
        var chosen = pieces[Random.Range(0, pieces.Count)];
        var moves = moveResolver.GetLegalMovesForPiece(chosen);
        var target = moves[Random.Range(0, moves.Count)];
        return (chosen, target);
    }

    private (PieceModel, BoardCoord) PickGreedy(List<PieceModel> pieces)
    {
        var candidates = new List<(PieceModel, BoardCoord, int)>();

        foreach (var p in pieces)
        {
            var moves = moveResolver.GetLegalMovesForPiece(p);
            foreach (var m in moves)
            {
                int score = 0;
                var occupant = gm.BoardState.GetPieceAt(m);
                if (occupant != null && occupant.owner != p.owner)
                    score += 100;
                if (occupant == null)
                    score += 1;
                candidates.Add((p, m, score));
            }
        }

        var best = candidates.OrderByDescending(c => c.Item3).First();
        return (best.Item1, best.Item2);
    }

    private (PieceModel, BoardCoord) PickEvaluated(List<PieceModel> pieces)
    {
        var candidates = new List<(PieceModel, BoardCoord, float)>();

        foreach (var p in pieces)
        {
            var moves = moveResolver.GetLegalMovesForPiece(p);
            foreach (var m in moves)
            {
                float score = EvaluateMoveV4(p, m);
                candidates.Add((p, m, score));
            }
        }

        var best = candidates.OrderByDescending(c => c.Item3).First();
        return (best.Item1, best.Item2);
    }

    private (PieceModel, BoardCoord) PickLookAhead(List<PieceModel> pieces)
    {
        var w = Weights ?? new EvalWeights();
        var candidates = new List<(PieceModel, BoardCoord, float)>();

        foreach (var p in pieces)
        {
            var moves = moveResolver.GetLegalMovesForPiece(p);
            foreach (var m in moves)
            {
                float baseScore = EvaluateMoveV4(p, m);
                float lookAheadScore = EvaluateLookAhead(p, m, w);
                candidates.Add((p, m, baseScore + lookAheadScore));
            }
        }

        var best = candidates.OrderByDescending(c => c.Item3).First();
        return (best.Item1, best.Item2);
    }

    private float EvaluateLookAhead(PieceModel piece, BoardCoord target, EvalWeights w)
    {
        var simBoard = gm.BoardState.Clone();
        var simResolver = new MoveResolver(simBoard, gm.GameRules);

        var myPiece = simBoard.GetPieceById(piece.pieceId);
        if (myPiece == null) return 0;

        var occupant = simBoard.GetPieceAt(target);
        bool isCapture = occupant != null && occupant.owner != piece.owner;

        simBoard.MovePiece(myPiece, target);
        if (isCapture)
        {
            var captured = simBoard.GetPieceById(occupant.pieceId);
            if (captured != null) simBoard.RemovePiece(captured);
        }
        if (myPiece.pieceType == PieceType.TwoPhase)
        {
            var moveDir = GetMoveDirection(piece.currentPosition, target);
            myPiece.FlipAcross(moveDir);
        }

        float ourPosScore = EvaluatePosition(simBoard, piece.owner, simResolver, w);

        var opponent = gm.GetOpponent(piece.owner);
        var oppPieces = simBoard.GetPiecesOf(opponent);
        float bestOppScore = float.MinValue;

        foreach (var op in oppPieces)
        {
            var opMoves2 = simResolver.GetLegalMovesForPiece(op);
            foreach (var om in opMoves2)
            {
                var sim2 = simBoard.Clone();
                var sim2Resolver = new MoveResolver(sim2, gm.GameRules);

                var op2 = sim2.GetPieceById(op.pieceId);
                if (op2 == null) continue;
                var occ2 = sim2.GetPieceAt(om);
                bool opCapture = occ2 != null && occ2.owner == piece.owner;

                sim2.MovePiece(op2, om);
                if (opCapture)
                {
                    var captured2 = sim2.GetPieceById(occ2.pieceId);
                    if (captured2 != null) sim2.RemovePiece(captured2);
                }
                if (op2.pieceType == PieceType.TwoPhase)
                {
                    var opMoveDir = GetMoveDirection(op.currentPosition, om);
                    op2.FlipAcross(opMoveDir);
                }

                float oppPos = EvaluatePosition(sim2, piece.owner, sim2Resolver, w);
                if (oppPos > bestOppScore) bestOppScore = oppPos;
            }
        }

        if (bestOppScore == float.MinValue) return ourPosScore;
        return ourPosScore - bestOppScore;
    }

    private float EvaluatePosition(BoardState board, PlayerSide side, MoveResolver resolver, EvalWeights w)
    {
        float score = 0;
        var opponent = side == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;
        var myPieces = board.GetPiecesOf(side);
        var oppPieces = board.GetPiecesOf(opponent);

        int myReach = 0, oppReach = 0;
        foreach (var p in myPieces)
        {
            myReach += resolver.GetLegalMovesForPiece(p).Count;
            score += p.pieceType == PieceType.OnePhase ? 1 : 3;
        }
        foreach (var p in oppPieces)
        {
            oppReach += resolver.GetLegalMovesForPiece(p).Count;
            score -= p.pieceType == PieceType.OnePhase ? 1 : 3;
        }

        score += (myReach - oppReach) * 0.1f;
        score += (myPieces.Count - oppPieces.Count) * 0.5f;

        int bw = gm.GameRules.boardWidth;
        foreach (var p in myPieces)
        {
            int fwd = side == PlayerSide.Player1 ? p.currentPosition.x : (bw - 1 - p.currentPosition.x);
            score += fwd * w.forwardPressure * 0.05f;

            if (p.pieceType == PieceType.OnePhase)
            {
                foreach (var dir in BoardCoordUtil.AllDirections())
                {
                    var adj = new BoardCoord(p.currentPosition.x + BoardCoordUtil.Offset(dir).x,
                        p.currentPosition.y + BoardCoordUtil.Offset(dir).y);
                    if (!board.IsValidCoord(adj)) continue;
                    var adjPiece = board.GetPieceAt(adj);
                    if (adjPiece != null && adjPiece.owner == side && adjPiece.pieceType == PieceType.TwoPhase)
                    { score += w.shelterBonus * 0.1f; break; }
                }
            }
        }

        foreach (var p in oppPieces)
        {
            int fwd = side == PlayerSide.Player1 ? (bw - 1 - p.currentPosition.x) : p.currentPosition.x;
            score -= fwd * w.forwardPressure * 0.05f;
        }

        int wallCount = 0;
        foreach (var p in myPieces)
        {
            if (p.pieceType != PieceType.TwoPhase) continue;
            foreach (var d in new Direction[] { Direction.Up, Direction.Down })
            {
                var adj = new BoardCoord(p.currentPosition.x + BoardCoordUtil.Offset(d).x,
                    p.currentPosition.y + BoardCoordUtil.Offset(d).y);
                if (!board.IsValidCoord(adj)) continue;
                var adjPiece = board.GetPieceAt(adj);
                if (adjPiece != null && adjPiece.owner == side && adjPiece.pieceType == PieceType.TwoPhase)
                { wallCount++; break; }
            }
        }
        score += wallCount * w.twoPhaseWall * 0.2f;

        if (myPieces.Count > 0)
        {
            float sx = 0, sy = 0;
            foreach (var p in myPieces) { sx += p.currentPosition.x; sy += p.currentPosition.y; }
            float mx = sx / myPieces.Count, my2 = sy / myPieces.Count;
            float var = 0;
            foreach (var p in myPieces)
                var += (p.currentPosition.x - mx) * (p.currentPosition.x - mx)
                     + (p.currentPosition.y - my2) * (p.currentPosition.y - my2);
            score += var * w.dispersionPenalty * 0.2f;
        }

        return score;
    }

    private float EvaluateMoveV4(PieceModel piece, BoardCoord target)
    {
        var w = Weights ?? new EvalWeights();
        float score = 0;
        var opponent = gm.GetOpponent(piece.owner);
        var occupant = gm.BoardState.GetPieceAt(target);
        bool isCapture = occupant != null && occupant.owner != piece.owner;

        if (isCapture)
        {
            score += occupant.pieceType == PieceType.OnePhase ? w.captureOnePhase : w.captureTwoPhase;
        }

        var afterDirs = piece.pieceType == PieceType.TwoPhase
            ? PieceModel.TransformDirections(piece.GetCurrentFaceDirections(),
                GetMoveDirection(piece.currentPosition, target))
            : piece.GetCurrentFaceDirections();

        var opponentPieces = gm.BoardState.GetPiecesOf(opponent);
        foreach (var op in opponentPieces)
        {
            var opMoves = moveResolver.GetLegalMovesForPiece(op);
            if (opMoves.Contains(target))
            {
                score += piece.pieceType == PieceType.OnePhase ? w.dangerOnePhase : w.dangerTwoPhase;
            }
        }

        var friendlyPieces = gm.BoardState.GetPiecesOf(piece.owner);
        int adjacentOnePhase = 0, adjacentFriendlies = 0;
        foreach (var fp in friendlyPieces)
        {
            if (fp.pieceId == piece.pieceId) continue;
            int dx = Mathf.Abs(target.x - fp.currentPosition.x);
            int dy = Mathf.Abs(target.y - fp.currentPosition.y);
            if (dx + dy == 1)
            {
                adjacentFriendlies++;
                if (fp.pieceType == PieceType.OnePhase)
                    adjacentOnePhase++;
            }
        }
        if (piece.pieceType == PieceType.TwoPhase)
            score += adjacentOnePhase * w.surroundByOnePhase + adjacentFriendlies * w.surroundFriendly;
        else
            score += adjacentFriendlies * w.onePhaseSurroundFriendly;

        float avgDist = 0;
        int fCount = 0;
        foreach (var fp in friendlyPieces)
        {
            if (fp.pieceId == piece.pieceId) continue;
            avgDist += Mathf.Abs(target.x - fp.currentPosition.x)
                + Mathf.Abs(target.y - fp.currentPosition.y);
            fCount++;
        }
        if (fCount > 0)
        {
            avgDist /= fCount;
            score += avgDist * w.groupCohesion;
        }

        int mobility = 0;
        foreach (var d in afterDirs)
        {
            var next = new BoardCoord(target.x + BoardCoordUtil.Offset(d).x,
                target.y + BoardCoordUtil.Offset(d).y);
            if (gm.BoardState.IsValidCoord(next) && gm.BoardState.GetPieceAt(next) == null)
                mobility++;
        }
        score += (piece.pieceType == PieceType.OnePhase ? w.onePhaseMobility : w.twoPhaseMobility) * mobility;

        int turnNum = gm.TurnManager?.TurnNumber ?? 0;
        if (turnNum <= w.earlyTurnCutoff)
        {
            int forward = piece.owner == PlayerSide.Player1 ? 1 : -1;
            score += (target.x - piece.currentPosition.x) * forward * w.forwardPushEarly;
        }

        int midY = (gm.GameRules.boardHeight - 1) / 2;
        int topCount = 0, botCount = 0;
        foreach (var fp in friendlyPieces)
        {
            if (fp.currentPosition.y > midY) topCount++;
            else if (fp.currentPosition.y < midY) botCount++;
        }
        if (topCount > botCount && target.y > midY) score += w.biasSideStrength;
        else if (botCount > topCount && target.y < midY) score += w.biasSideStrength;

        int forwardDir = piece.owner == PlayerSide.Player1 ? 1 : -1;
        int advance = (target.x - piece.currentPosition.x) * forwardDir;
        if (advance > 0)
            score += advance * w.forwardPressure;
        else if (advance < 0)
            score += w.retreatPenalty;

        if (turnStartPositions.TryGetValue(piece.pieceId, out var startPos) && target.Equals(startPos))
            score += w.backtrackPenalty;

        float sumX = 0, sumY = 0;
        foreach (var fp in friendlyPieces) { sumX += fp.currentPosition.x; sumY += fp.currentPosition.y; }
        float meanX = sumX / friendlyPieces.Count;
        float meanY = sumY / friendlyPieces.Count;
        float variance = 0;
        foreach (var fp in friendlyPieces)
            variance += (fp.currentPosition.x - meanX) * (fp.currentPosition.x - meanX)
                      + (fp.currentPosition.y - meanY) * (fp.currentPosition.y - meanY);
        score += variance * w.dispersionPenalty;

        if (isCapture && occupant != null && occupant.pieceType == PieceType.TwoPhase)
        {
            bool canRecapture = false;
            foreach (var op in opponentPieces)
            {
                if (moveResolver.GetLegalMovesForPiece(op).Contains(target))
                { canRecapture = true; break; }
            }
            if (!canRecapture)
                score += w.safeTwoPhaseCapture;
        }

        if (piece.pieceType == PieceType.TwoPhase)
        {
            foreach (var d in new Direction[] { Direction.Up, Direction.Down })
            {
                var adj = new BoardCoord(target.x + BoardCoordUtil.Offset(d).x, target.y + BoardCoordUtil.Offset(d).y);
                if (!gm.BoardState.IsValidCoord(adj)) continue;
                var adjPiece = gm.BoardState.GetPieceAt(adj);
                if (adjPiece != null && adjPiece.owner == piece.owner && adjPiece.pieceType == PieceType.TwoPhase)
                    score += w.twoPhaseWall;
            }
        }

        if (piece.pieceType == PieceType.OnePhase)
        {
            bool threatened = false;
            foreach (var op in opponentPieces)
            {
                if (moveResolver.GetLegalMovesForPiece(op).Contains(target))
                { threatened = true; break; }
            }
            if (threatened && adjacentFriendlies == 0)
                score += w.exposedOnePhase;
        }

        if (!isCapture)
        {
            foreach (var d in afterDirs)
            {
                var next = new BoardCoord(target.x + BoardCoordUtil.Offset(d).x,
                    target.y + BoardCoordUtil.Offset(d).y);
                if (!gm.BoardState.IsValidCoord(next)) continue;
                var nextOcc = gm.BoardState.GetPieceAt(next);
                if (nextOcc != null && nextOcc.owner == opponent)
                {
                    bool safe = true;
                    foreach (var op in opponentPieces)
                    {
                        if (op.pieceId == nextOcc.pieceId) continue;
                        if (moveResolver.GetLegalMovesForPiece(op).Contains(next))
                        { safe = false; break; }
                    }
                    score += safe ? w.safeForkCapture : w.riskyForkCapture;
                }
            }
        }

        foreach (var op in opponentPieces)
        {
            if (op.pieceType != PieceType.TwoPhase) continue;
            var hidden = op.GetOppositeFaceDirections();
            foreach (var d in hidden)
            {
                var ht = new BoardCoord(op.currentPosition.x + BoardCoordUtil.Offset(d).x,
                    op.currentPosition.y + BoardCoordUtil.Offset(d).y);
                if (ht.Equals(target))
                    score += piece.pieceType == PieceType.OnePhase ? w.hiddenFaceOnePhase : w.hiddenFaceTwoPhase;
            }
        }

        int w2 = gm.GameRules.boardWidth;
        int h = gm.GameRules.boardHeight;
        float distToCenter = Mathf.Abs(target.x - (w2 - 1) / 2) + Mathf.Abs(target.y - (h - 1) / 2);
        score += distToCenter * w.centerWeight;

        if (piece.spawnedThisTurn)
            score += w.spawnedTurnPenalty;

        if (gm.WouldMoveCauseRepetition(piece, target, isCapture))
            score += w.repetitionPenalty;

        var oppPieces2 = gm.BoardState.GetPiecesOf(opponent);
        int myReach = 0, oppReach = 0;
        foreach (var fp in friendlyPieces)
            myReach += moveResolver.GetLegalMovesForPiece(fp).Count;
        foreach (var op in oppPieces2)
            oppReach += moveResolver.GetLegalMovesForPiece(op).Count;
        score += (myReach - oppReach) * w.territoryControl;

        score += (friendlyPieces.Count - oppPieces2.Count) * w.pieceCountAdvantage;

        int isoPenalty = 0;
        foreach (var fp in friendlyPieces)
        {
            int minDist = 999;
            foreach (var fp2 in friendlyPieces)
            {
                if (fp2.pieceId == fp.pieceId) continue;
                int d = Mathf.Abs(fp.currentPosition.x - fp2.currentPosition.x)
                      + Mathf.Abs(fp.currentPosition.y - fp2.currentPosition.y);
                if (d < minDist) minDist = d;
            }
            if (minDist < 999)
                isoPenalty += minDist;
        }
        score += isoPenalty * w.isolationPenalty;

        int safeZoneCount = 0;
        int bw = gm.GameRules.boardWidth;
        int bh = gm.GameRules.boardHeight;
        int homeEdgeX = piece.owner == PlayerSide.Player1 ? -1 : bw;
        for (int x = 0; x < bw; x++)
        {
            for (int y = 0; y < bh; y++)
            {
                var cellOcc = gm.BoardState.GetPieceAt(new BoardCoord(x, y));
                bool isValidCell = cellOcc == null || (cellOcc.owner == piece.owner && cellOcc.pieceType == PieceType.OnePhase);
                if (!isValidCell) continue;
                bool surrounded = true;
                for (int dx = -1; dx <= 1 && surrounded; dx++)
                {
                    for (int dy = -1; dy <= 1 && surrounded; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= bw || ny < 0 || ny >= bh)
                        {
                            if (nx == homeEdgeX) continue;
                            else { surrounded = false; break; }
                        }
                        var neighbor = gm.BoardState.GetPieceAt(new BoardCoord(nx, ny));
                        if (neighbor == null || neighbor.owner != piece.owner || neighbor.pieceType != PieceType.TwoPhase)
                            surrounded = false;
                    }
                }
                if (surrounded) safeZoneCount++;
            }
        }
        score += safeZoneCount * w.safeZoneBonus;

        return score;
    }

    private float GetWorstOpponentResponse(PieceModel myPiece, BoardCoord myTarget)
    {
        var opponent = gm.GetOpponent(myPiece.owner);
        float worstScore = float.MinValue;
        var oppPieces = gm.BoardState.GetPiecesOf(opponent);
        BoardCoord myOrigin = myPiece.currentPosition;

        foreach (var op in oppPieces)
        {
            if (op.spawnedThisTurn) continue;
            var opMoves = moveResolver.GetLegalMovesForPiece(op);

            foreach (var om in opMoves)
            {
                float threatScore = 0;
                var opOcc = gm.BoardState.GetPieceAt(om);

                if (opOcc != null && opOcc.owner == myPiece.owner)
                {
                    if (opOcc.pieceType == PieceType.OnePhase)
                        threatScore += 8000;
                    else
                        threatScore += 500;
                }

                var myAfterDirs = PieceModel.TransformDirections(myPiece.GetCurrentFaceDirections(),
                    GetMoveDirection(myOrigin, myTarget));
                foreach (var d in myAfterDirs)
                {
                    var next = new BoardCoord(myTarget.x + BoardCoordUtil.Offset(d).x,
                        myTarget.y + BoardCoordUtil.Offset(d).y);
                    if (gm.BoardState.IsValidCoord(next) && next.Equals(om))
                        threatScore -= 200;
                }

                if (threatScore > worstScore)
                    worstScore = threatScore;
            }
        }

        return worstScore > 0 ? worstScore : 0;
    }

    private Direction GetMoveDirection(BoardCoord from, BoardCoord to)
    {
        int dx = to.x - from.x;
        int dy = to.y - from.y;
        foreach (var d in BoardCoordUtil.AllDirections())
        {
            var off = BoardCoordUtil.Offset(d);
            if (off.x == dx && off.y == dy) return d;
        }
        return Direction.Right;
    }

    private IEnumerator DoCpuSplitPlacement()
    {
        Debug.Log("[CpuPlayer] DoCpuSplitPlacement started");
        yield return new WaitForSeconds(0.5f);

        if (gm.CurrentPhase != GamePhase.PlacingSplitPieces) { Debug.Log("[CpuPlayer] Split placement aborted: phase changed"); yield break; }
        if (gm.PendingSplitPieces == null || gm.PendingSplitPieces.Count == 0) { Debug.Log("[CpuPlayer] Split placement aborted: no pending pieces"); yield break; }

        var emptyCells = gm.BoardState.GetEmptyCells();
        if (emptyCells.Count < gm.PendingSplitPieces.Count)
        {
            Debug.LogWarning("[CpuPlayer] Not enough empty cells for split placement.");
            yield break;
        }

        var placed = new List<PieceModel>();

        Debug.Log($"[CpuPlayer] Placing {gm.PendingSplitPieces.Count} split pieces on {emptyCells.Count} empty cells");

        if (DifficultyLevel <= 1)
        {
            var shuffledCells = emptyCells.OrderBy(_ => Random.value).ToList();
            for (int i = 0; i < gm.PendingSplitPieces.Count; i++)
            {
                var piece = gm.PendingSplitPieces[i];
                var coord = shuffledCells[i];
                Debug.Log($"[CpuPlayer] Placing split piece {piece.pieceId} at {coord}");
                gm.PlaceSplitPieceOnBoard(piece, coord);
                if (gm.BoardView != null)
                    gm.BoardView.CreatePieceView(piece);
                placed.Add(piece);
            }
        }
        else if (DifficultyLevel == 2)
        {
            var sortedCells = emptyCells.OrderBy(c =>
            {
                int distFromCenter = Mathf.Abs(c.x - (gm.GameRules.boardWidth - 1) / 2)
                    + Mathf.Abs(c.y - (gm.GameRules.boardHeight - 1) / 2);
                return distFromCenter;
            }).ToList();

            for (int i = 0; i < gm.PendingSplitPieces.Count; i++)
            {
                var piece = gm.PendingSplitPieces[i];
                var coord = sortedCells[i];
                Debug.Log($"[CpuPlayer] Placing split piece {piece.pieceId} at {coord}");
                gm.PlaceSplitPieceOnBoard(piece, coord);
                if (gm.BoardView != null)
                    gm.BoardView.CreatePieceView(piece);
                placed.Add(piece);
            }
        }
        else
        {
            int oppSide = cpuSide == PlayerSide.Player1 ? 0 : gm.GameRules.boardWidth - 1;
            var captureCell = gm.LastCaptureCell;
            var scoredCells = emptyCells.OrderByDescending(c =>
            {
                float s = 0;

                int distFromOpp = Mathf.Abs(c.x - oppSide);
                s -= distFromOpp;

                int centerY = (gm.GameRules.boardHeight - 1) / 2;
                s -= Mathf.Abs(c.y - centerY) * 2;

                if (captureCell.x >= 0)
                {
                    int dx = Mathf.Abs(c.x - captureCell.x);
                    int dy = Mathf.Abs(c.y - captureCell.y);
                    if (dx + dy == 1)
                        s += 80;
                }

                var myPieces = gm.BoardState.GetPiecesOf(cpuSide);
                foreach (var mp in myPieces)
                {
                    int dx = Mathf.Abs(c.x - mp.currentPosition.x);
                    int dy = Mathf.Abs(c.y - mp.currentPosition.y);
                    s -= Mathf.Max(0, 3 - (dx + dy)) * 5;
                }

                var oppPieces = gm.BoardState.GetPiecesOf(gm.GetOpponent(cpuSide));
                foreach (var op in oppPieces)
                {
                    if (op.pieceType != PieceType.TwoPhase) continue;
                    var backDirs = op.GetOppositeFaceDirections();
                    foreach (var d in backDirs)
                    {
                        var ht = new BoardCoord(op.currentPosition.x + BoardCoordUtil.Offset(d).x,
                            op.currentPosition.y + BoardCoordUtil.Offset(d).y);
                        if (ht.Equals(c)) s -= 60;
                    }
                }

                return s;
            }).ToList();

            for (int i = 0; i < gm.PendingSplitPieces.Count; i++)
            {
                var piece = gm.PendingSplitPieces[i];
                var coord = scoredCells[i];
                Debug.Log($"[CpuPlayer] Placing split piece {piece.pieceId} at {coord}");
                gm.PlaceSplitPieceOnBoard(piece, coord);
                if (gm.BoardView != null)
                    gm.BoardView.CreatePieceView(piece);
                placed.Add(piece);
            }
        }

        Debug.Log($"[CpuPlayer] Finalizing split placement with {placed.Count} pieces");
        yield return new WaitForSeconds(0.2f);
        gm.FinalizeSplitPlacement(placed);
    }

    private List<PieceModel> GetMovablePieces()
    {
        var pieces = gm.BoardState.GetPiecesOf(cpuSide);
        var result = new List<PieceModel>();
        foreach (var p in pieces)
        {
            if (p.canActThisTurn && !p.spawnedThisTurn && moveResolver.GetLegalMovesForPiece(p).Count > 0)
                result.Add(p);
        }
        return result;
    }

    private void OnDestroy()
    {
        if (gm != null)
            gm.OnGameStateChanged -= OnGameStateChanged;
    }
}
