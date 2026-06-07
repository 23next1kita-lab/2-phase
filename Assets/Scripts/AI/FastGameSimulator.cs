using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FastGameSimulator
{
    private const float OnePhaseCapturePriorityBonus = 10000000f;
    private GameRulesSO rules;
    private BoardState board;
    private TurnManager turnManager;
    private MoveResolver moveResolver;
    private CaptureResolver captureResolver;
    private PlacementResolver placementResolver;
    private EvalWeights p1Weights;
    private EvalWeights p2Weights;
    private PlayerSide? winner;
    private RepetitionDetector repDetector;
    private bool isDraw;
    private int captureCount;
    private Dictionary<int, BoardCoord> fastTurnStartPositions;
    private int fastMoveCount;
    private List<string> fastPastStateHashes;

    public int CaptureCount => captureCount;
    public PlayerSide? Winner => winner;

    public PlayerSide? Simulate(EvalWeights w1, EvalWeights w2, int maxTurns = 100)
    {
        p1Weights = w1;
        p2Weights = w2;
        InitializeGame();
        var result = RunLoop(maxTurns);
        return result;
    }

    private void InitializeGame()
    {
        rules = ScriptableObject.CreateInstance<GameRulesSO>();
        rules.boardWidth = 7;
        rules.boardHeight = 7;
        rules.firstTurnMoveCount = 1;
        rules.normalTurnMoveCount = 2;
        rules.allowSamePieceTwicePerTurn = true;
        rules.captureEndsTurnImmediately = true;

        board = new BoardState(rules.boardWidth, rules.boardHeight);
        turnManager = new TurnManager(rules, silent: true);
        moveResolver = new MoveResolver(board, rules, silent: true);
        captureResolver = new CaptureResolver(board, rules, silent: true);
        placementResolver = new PlacementResolver(board, rules, silent: true);
        winner = null;
        isDraw = false;
        captureCount = 0;
        repDetector = new RepetitionDetector();
        fastPastStateHashes = new List<string>();

        PlaceDefaultPieces();

        repDetector.RecordAndCheck(board, turnManager.CurrentPlayer, turnManager.TurnNumber);
    }

    private void PlaceDefaultPieces()
    {
        int h = rules.boardHeight;
        int w = rules.boardWidth;

        var presets = rules.randomPieceDirections
            ? GenerateRandomPresets()
            : GetDefaultPresets();

        for (int y = 0; y < h; y++)
        {
            var p = presets[y];
            var front = p.front;
            var back = p.back;
            int id = board.GeneratePieceId();
            var piece = new PieceModel(id, PlayerSide.Player1, PieceType.TwoPhase,
                new BoardCoord(0, y), front, back, true);
            board.AddPiece(piece);
        }

        for (int y = 0; y < h; y++)
        {
            var p = presets[y];
            int id = board.GeneratePieceId();
            var piece = new PieceModel(id, PlayerSide.Player2, PieceType.TwoPhase,
                new BoardCoord(w - 1, y), MirrorDirs(p.front), MirrorDirs(p.back), true);
            board.AddPiece(piece);
        }
    }

    private (List<Direction> front, List<Direction> back)[] GetDefaultPresets()
    {
        return new (List<Direction> front, List<Direction> back)[]
        {
            (new List<Direction>{ Direction.Right, Direction.UpRight, Direction.UpLeft }, new List<Direction>{ Direction.Up, Direction.DownLeft, Direction.UpLeft }),
            (new List<Direction>{ Direction.DownRight, Direction.Right, Direction.UpRight, Direction.Left }, new List<Direction>{ Direction.DownLeft, Direction.Down, Direction.DownRight, Direction.Up }),
            (new List<Direction>{ Direction.Right, Direction.UpRight, Direction.DownRight, Direction.Up, Direction.DownLeft }, new List<Direction>{ Direction.Left, Direction.UpLeft, Direction.DownLeft, Direction.Up, Direction.DownRight }),
            (new List<Direction>{ Direction.UpRight, Direction.Right, Direction.DownRight, Direction.UpLeft, Direction.Left, Direction.DownLeft }, new List<Direction>{ Direction.UpRight, Direction.Up, Direction.UpLeft, Direction.DownRight, Direction.Down, Direction.DownLeft }),
            (new List<Direction>{ Direction.Right, Direction.DownRight, Direction.UpRight, Direction.Down, Direction.UpLeft }, new List<Direction>{ Direction.Left, Direction.DownLeft, Direction.UpLeft, Direction.Down, Direction.UpRight }),
            (new List<Direction>{ Direction.UpRight, Direction.Right, Direction.DownRight, Direction.Left }, new List<Direction>{ Direction.UpLeft, Direction.Up, Direction.UpRight, Direction.Down }),
            (new List<Direction>{ Direction.Right, Direction.DownRight, Direction.DownLeft }, new List<Direction>{ Direction.Up, Direction.UpLeft, Direction.DownLeft }),
        };
    }

    private (List<Direction> front, List<Direction> back)[] GenerateRandomPresets()
    {
        int count = rules.boardHeight;
        var allDirs = new List<Direction>((Direction[])System.Enum.GetValues(typeof(Direction)));
        var presets = new (List<Direction> front, List<Direction> back)[count];

        for (int i = 0; i < count; i++)
        {
            int dirCount = i switch { 3 => 6, 2 or 4 => 5, _ => 4 };
            var front = PickRandomDirections(allDirs, dirCount);
            var back = PickRandomDirections(allDirs, dirCount);
            presets[i] = (front, back);
        }

        return presets;
    }

    private List<Direction> PickRandomDirections(List<Direction> candidates, int count)
    {
        var shuffled = new List<Direction>(candidates);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, shuffled.Count);
            var tmp = shuffled[i]; shuffled[i] = shuffled[j]; shuffled[j] = tmp;
        }
        return shuffled.GetRange(0, Mathf.Min(count, shuffled.Count));
    }

    private List<Direction> MirrorDirs(List<Direction> dirs)
    {
        return dirs.Select(d =>
        {
            switch (d)
            {
                case Direction.Right: return Direction.Left;
                case Direction.Left: return Direction.Right;
                case Direction.UpRight: return Direction.UpLeft;
                case Direction.UpLeft: return Direction.UpRight;
                case Direction.DownRight: return Direction.DownLeft;
                case Direction.DownLeft: return Direction.DownRight;
                default: return d;
            }
        }).ToList();
    }

    private PlayerSide? RunLoop(int maxTurns)
    {
        for (int t = 0; t < maxTurns; t++)
        {
            if (winner.HasValue) return winner.Value;
            if (isDraw) return null;

            var player = turnManager.CurrentPlayer;
            var weights = player == PlayerSide.Player1 ? p1Weights : p2Weights;

            fastTurnStartPositions = new Dictionary<int, BoardCoord>();
            foreach (var p in board.GetPiecesOf(player))
                fastTurnStartPositions[p.pieceId] = p.currentPosition;
            fastMoveCount = 0;
            fastPastStateHashes.Insert(0, GetCurrentFriendlyStateHashFast(player));
            if (fastPastStateHashes.Count > 5)
                fastPastStateHashes.RemoveAt(fastPastStateHashes.Count - 1);

            int movesAllowed = turnManager.MovesRemaining;
            bool wasCapture = false;

            for (int m = 0; m < movesAllowed; m++)
            {
                if (winner.HasValue) return winner.Value;
                if (isDraw) return null;

                fastMoveCount = m + 1;
                var movable = GetMovablePieces(player);
                if (movable.Count == 0) break;

                var best = PickBestMove(movable, weights);
                if (best == null) break;

                wasCapture = ExecuteMove(best.Value.piece, best.Value.target, player);
                if (wasCapture) break;
                if (winner.HasValue) return winner.Value;
                if (isDraw) return null;

                turnManager.OnMoveCompleted(best.Value.piece);
            }

            if (!winner.HasValue && !wasCapture)
            {
                foreach (var p in board.GetAllPieces())
                {
                    p.spawnedThisTurn = false;
                }
                turnManager.StartNextTurn();
                foreach (var p in board.GetAllPieces())
                {
                    if (p.owner == turnManager.CurrentPlayer)
                        p.canActThisTurn = true;
                }

                if (repDetector.RecordAndCheck(board, turnManager.CurrentPlayer, turnManager.TurnNumber))
                {
                    return null;
                }
            }
        }

        return null;
    }

    private List<(PieceModel piece, BoardCoord target, float score, bool isCapture)> GetAllMoves(PlayerSide player)
    {
        var moves = new List<(PieceModel, BoardCoord, float, bool)>();
        var pieces = board.GetPiecesOf(player);
        foreach (var p in pieces)
        {
            if (!p.canActThisTurn || p.spawnedThisTurn) continue;
            var legals = moveResolver.GetLegalMovesForPiece(p);
            foreach (var t in legals)
            {
                var occ = board.GetPieceAt(t);
                bool isCap = occ != null && occ.owner != p.owner;
                moves.Add((p, t, 0, isCap));
            }
        }
        return moves;
    }

    private (PieceModel piece, BoardCoord target, float score, bool isCapture)? PickBestMove(
        List<PieceModel> pieces, EvalWeights w)
    {
        (PieceModel piece, BoardCoord target, float score, bool isCapture)? best = null;

        foreach (var p in pieces)
        {
            var legals = moveResolver.GetLegalMovesForPiece(p);
            foreach (var t in legals)
            {
                var occ = board.GetPieceAt(t);
                bool isCap = occ != null && occ.owner != p.owner;
                float sc = EvaluateMoveFast(p, t, w);

                if (w.opponentResponseFactor > 0.01f)
                {
                    float threat = GetWorstOpponentResponseFast(p, t);
                    sc -= threat * w.opponentResponseFactor;
                }

                if (best == null || sc > best.Value.score)
                    best = (p, t, sc, isCap);
                else if (sc == best.Value.score && UnityEngine.Random.value < 0.5f)
                    best = (p, t, sc, isCap);
            }
        }

        return best;
    }

    private bool ExecuteMove(PieceModel piece, BoardCoord target, PlayerSide player)
    {
        var occupant = board.GetPieceAt(target);
        bool hasOpponent = occupant != null && occupant.owner != piece.owner;

        if (hasOpponent)
        {
            captureCount++;
            board.RemovePiece(occupant);

            Direction moveDir = GetMoveDirection(piece.currentPosition, target);
            moveResolver.ApplyMove(piece, target);

            if (piece.pieceType == PieceType.TwoPhase)
                moveResolver.ApplyFlip(piece, moveDir);

            var result = captureResolver.ResolveCaptureWithCaptured(occupant, piece.owner);

            if (result.isGameOver)
            {
                winner = result.winner;
                return true;
            }

            if (result.needsSplitPlacement && result.splitPieces != null)
            {
                var splitOwner = result.splitPieces[0].owner;
                var placed = AutoPlaceSplitPieces(result.splitPieces, splitOwner);
                foreach (var sp in placed)
                {
                    sp.spawnedThisTurn = true;
                    placementResolver.PlaceSplitPiece(sp, sp.currentPosition, sp.GetCurrentFaceDirections());
                }
            }

            var nextPlayer = player == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;
            turnManager.StartNextTurn();
            foreach (var p in board.GetAllPieces())
            {
                if (p.owner == nextPlayer && !p.spawnedThisTurn)
                    p.canActThisTurn = true;
            }

            if (repDetector.RecordAndCheck(board, turnManager.CurrentPlayer, turnManager.TurnNumber))
            {
                isDraw = true;
                return true;
            }

            return true;
        }

        Direction moveDir2 = GetMoveDirection(piece.currentPosition, target);
        moveResolver.ApplyMove(piece, target);

        if (piece.pieceType == PieceType.TwoPhase)
            moveResolver.ApplyFlip(piece, moveDir2);

        return false;
    }

    private List<PieceModel> AutoPlaceSplitPieces(List<PieceModel> splitPieces, PlayerSide placingPlayer)
    {
        var placed = new List<PieceModel>();
        var empty = board.GetEmptyCells();
        var w = placingPlayer == PlayerSide.Player1 ? p1Weights : p2Weights;

        int oppSide = placingPlayer == PlayerSide.Player1 ? 0 : rules.boardWidth - 1;

        var scored = empty.OrderByDescending(c =>
        {
            float s = 0;

            int distFromHome = Math.Abs(c.x - oppSide);
            s -= distFromHome * 100;

            int centerY = (rules.boardHeight - 1) / 2;
            s -= Math.Abs(c.y - centerY) * 2;

            var myPieces = board.GetPiecesOf(placingPlayer);
            foreach (var mp in myPieces)
            {
                int dx = Math.Abs(c.x - mp.currentPosition.x);
                int dy = Math.Abs(c.y - mp.currentPosition.y);
                s -= Math.Max(0, 3 - (dx + dy)) * 5;
            }

            var oppPieces = board.GetPiecesOf(placingPlayer == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1);
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

            int fwd = placingPlayer == PlayerSide.Player1 ? c.x : (rules.boardWidth - 1 - c.x);
            s += fwd * (w?.forwardPressure ?? 20f) * 0.3f;

            return s;
        }).ToList();

        for (int i = 0; i < Math.Min(splitPieces.Count, scored.Count); i++)
        {
            var piece = splitPieces[i];
            var coord = scored[i];
            piece.SetPosition(coord);
            placed.Add(piece);
        }

        return placed;
    }

    private List<PieceModel> GetMovablePieces(PlayerSide player)
    {
        var pieces = board.GetPiecesOf(player);
        var result = new List<PieceModel>();
        foreach (var p in pieces)
        {
            if (p.canActThisTurn && !p.spawnedThisTurn && moveResolver.GetLegalMovesForPiece(p).Count > 0)
                result.Add(p);
        }
        return result;
    }

    private float EvaluateMoveFast(PieceModel piece, BoardCoord target, EvalWeights w)
    {
        float score = 0;
        var opponent = piece.owner == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;
        var occupant = board.GetPieceAt(target);
        bool isCapture = occupant != null && occupant.owner != piece.owner;

        if (isCapture)
        {
            score += occupant.pieceType == PieceType.OnePhase ? w.captureOnePhase : w.captureTwoPhase;
            if (occupant.pieceType == PieceType.OnePhase)
                score += OnePhaseCapturePriorityBonus;
        }

        var afterDirs = piece.pieceType == PieceType.TwoPhase
            ? PieceModel.TransformDirections(piece.GetCurrentFaceDirections(),
                GetMoveDirection(piece.currentPosition, target))
            : piece.GetCurrentFaceDirections();

        var oppPieces = board.GetPiecesOf(opponent);
        foreach (var op in oppPieces)
        {
            var opMoves = moveResolver.GetLegalMovesForPiece(op);
            if (opMoves.Contains(target))
            {
                score += piece.pieceType == PieceType.OnePhase ? w.dangerOnePhase : w.dangerTwoPhase;
            }
        }

        int threatenCount = 0;
        foreach (var d in afterDirs)
        {
            var next = new BoardCoord(target.x + BoardCoordUtil.Offset(d).x,
                target.y + BoardCoordUtil.Offset(d).y);
            if (!board.IsValidCoord(next)) continue;
            var nextOcc = board.GetPieceAt(next);
            if (nextOcc != null && nextOcc.owner == opponent && nextOcc.pieceType == PieceType.OnePhase)
                threatenCount++;
        }
        if (threatenCount > 0)
        {
            float bonus = threatenCount * 50000f;
            if (threatenCount >= 2) bonus *= 3f;
            score += bonus;
        }

        if (piece.pieceType == PieceType.OnePhase && occupant != null && occupant.owner == opponent)
        {
            var ourPieces2 = board.GetPiecesOf(piece.owner);
            var ourOnePhase2 = ourPieces2.Where(fp => fp.pieceType == PieceType.OnePhase && fp.pieceId != piece.pieceId).ToList();
            bool anyThreatened = false;
            foreach (var op in oppPieces)
            {
                if (op.pieceId == occupant.pieceId) continue;
                var opMoves2 = moveResolver.GetLegalMovesForPiece(op);
                foreach (var o1p in ourOnePhase2)
                {
                    if (opMoves2.Contains(o1p.currentPosition))
                    { anyThreatened = true; break; }
                }
                if (anyThreatened) break;
            }
            if (anyThreatened)
                score += 100000f;
        }

        var friendlyPieces = board.GetPiecesOf(piece.owner);
        int adjOnePhase = 0, adjFriendly = 0;
        foreach (var fp in friendlyPieces)
        {
            if (fp.pieceId == piece.pieceId) continue;
            int dx = Math.Abs(target.x - fp.currentPosition.x);
            int dy = Math.Abs(target.y - fp.currentPosition.y);
            if (dx + dy == 1)
            {
                adjFriendly++;
                if (fp.pieceType == PieceType.OnePhase) adjOnePhase++;
            }
        }
        if (piece.pieceType == PieceType.TwoPhase)
            score += adjOnePhase * w.surroundByOnePhase + adjFriendly * w.surroundFriendly;
        else
            score += adjFriendly * w.onePhaseSurroundFriendly;

        float avgDist = 0;
        int fCount = 0;
        foreach (var fp in friendlyPieces)
        {
            if (fp.pieceId == piece.pieceId) continue;
            avgDist += Math.Abs(target.x - fp.currentPosition.x) + Math.Abs(target.y - fp.currentPosition.y);
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
            if (board.IsValidCoord(next) && board.GetPieceAt(next) == null)
                mobility++;
        }
        score += (piece.pieceType == PieceType.OnePhase ? w.onePhaseMobility : w.twoPhaseMobility) * mobility;

        int turnNum = turnManager?.TurnNumber ?? 0;
        if (turnNum <= w.earlyTurnCutoff)
        {
            int forward = piece.owner == PlayerSide.Player1 ? 1 : -1;
            score += (target.x - piece.currentPosition.x) * forward * w.forwardPushEarly;
        }

        int midY = (rules.boardHeight - 1) / 2;
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

        int turnNumF = turnManager?.TurnNumber ?? 0;
        if (advance > 0 && turnNumF <= 5)
            score += w.openingAdvanceBonus;

        if (fastTurnStartPositions != null && fastTurnStartPositions.TryGetValue(piece.pieceId, out var startPos) && target.Equals(startPos))
            score += w.backtrackPenalty;

        if (advance < 0 && turnNumF <= 3)
            score += w.earlyBacktrackPenalty;

        if (advance > 0)
        {
            int fewestAdj = int.MaxValue;
            foreach (var fp in friendlyPieces)
            {
                int adjC = 0;
                foreach (var fp2 in friendlyPieces)
                {
                    if (fp2.pieceId == fp.pieceId) continue;
                    int dx = Math.Abs(fp.currentPosition.x - fp2.currentPosition.x);
                    int dy = Math.Abs(fp.currentPosition.y - fp2.currentPosition.y);
                    if (dx <= 1 && dy <= 1 && (dx != 0 || dy != 0)) adjC++;
                }
                if (adjC < fewestAdj) fewestAdj = adjC;
            }
            int thisAdj = 0;
            foreach (var fp in friendlyPieces)
            {
                if (fp.pieceId == piece.pieceId) continue;
                int dx = Math.Abs(piece.currentPosition.x - fp.currentPosition.x);
                int dy = Math.Abs(piece.currentPosition.y - fp.currentPosition.y);
                if (dx <= 1 && dy <= 1 && (dx != 0 || dy != 0)) thisAdj++;
            }
            if (thisAdj == fewestAdj)
                score += w.isolatedAdvanceBonus;
        }

        if (fastPastStateHashes != null && fastPastStateHashes.Count >= 3)
        {
            string projHash = GetProjectedStateHashFast(piece, target, isCapture);
            for (int i = 2; i < fastPastStateHashes.Count && i <= 4; i++)
            {
                if (projHash == fastPastStateHashes[i])
                    score += w.stateRepeatPenalty;
            }
        }

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
            foreach (var op in oppPieces)
            {
                if (moveResolver.GetLegalMovesForPiece(op).Contains(target))
                { canRecapture = true; break; }
            }
            if (!canRecapture)
                score += w.safeTwoPhaseCapture;
        }

        if (isCapture && occupant != null && occupant.pieceType == PieceType.TwoPhase && fastMoveCount >= 2)
            score += w.secondMoveCaptureBonus;

        if (piece.pieceType == PieceType.TwoPhase)
        {
            foreach (var d in new Direction[] { Direction.Up, Direction.Down })
            {
                var adj = new BoardCoord(target.x + BoardCoordUtil.Offset(d).x, target.y + BoardCoordUtil.Offset(d).y);
                if (!board.IsValidCoord(adj)) continue;
                var adjPiece = board.GetPieceAt(adj);
                if (adjPiece != null && adjPiece.owner == piece.owner && adjPiece.pieceType == PieceType.TwoPhase)
                    score += w.twoPhaseWall;
            }
        }

        if (piece.pieceType == PieceType.OnePhase)
        {
            bool threatened = false;
            foreach (var op in oppPieces)
            {
                if (moveResolver.GetLegalMovesForPiece(op).Contains(target))
                { threatened = true; break; }
            }
            if (threatened && adjFriendly == 0)
                score += w.exposedOnePhase;
        }

        if (!isCapture)
        {
            foreach (var d in afterDirs)
            {
                var next = new BoardCoord(target.x + BoardCoordUtil.Offset(d).x,
                    target.y + BoardCoordUtil.Offset(d).y);
                if (!board.IsValidCoord(next)) continue;
                var nextOcc = board.GetPieceAt(next);
                if (nextOcc != null && nextOcc.owner == opponent)
                {
                    bool safe = true;
                    foreach (var op in oppPieces)
                    {
                        if (op.pieceId == nextOcc.pieceId) continue;
                        if (moveResolver.GetLegalMovesForPiece(op).Contains(next))
                        { safe = false; break; }
                    }
                    score += safe ? w.safeForkCapture : w.riskyForkCapture;
                    if (safe && piece.pieceType == PieceType.TwoPhase)
                        score += w.twoPhaseThreat;
                }
            }
        }

        foreach (var op in oppPieces)
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

        float distCenter = Math.Abs(target.x - (rules.boardWidth - 1) / 2)
            + Math.Abs(target.y - (rules.boardHeight - 1) / 2);
        score += distCenter * w.centerWeight;

        if (piece.spawnedThisTurn)
            score += w.spawnedTurnPenalty;

        if (repDetector.WouldRepeatAfterMove(board, turnManager.CurrentPlayer, turnManager.TurnNumber,
            piece, target, isCapture))
            score += w.repetitionPenalty;

        int myReach = 0, oppReach = 0;
        foreach (var fp in friendlyPieces)
            myReach += moveResolver.GetLegalMovesForPiece(fp).Count;
        foreach (var op in oppPieces)
            oppReach += moveResolver.GetLegalMovesForPiece(op).Count;
        score += (myReach - oppReach) * w.territoryControl;

        score += (friendlyPieces.Count - oppPieces.Count) * w.pieceCountAdvantage;

        int isoPenalty = 0;
        foreach (var fp in friendlyPieces)
        {
            int minDist = 999;
            foreach (var fp2 in friendlyPieces)
            {
                if (fp2.pieceId == fp.pieceId) continue;
                int d = Math.Abs(fp.currentPosition.x - fp2.currentPosition.x)
                      + Math.Abs(fp.currentPosition.y - fp2.currentPosition.y);
                if (d < minDist) minDist = d;
            }
            if (minDist < 999)
                isoPenalty += minDist;
        }
        score += isoPenalty * w.isolationPenalty;

        int safeZoneCount = 0;
        int bwF = rules.boardWidth;
        int bhF = rules.boardHeight;
        int homeEdgeX = piece.owner == PlayerSide.Player1 ? -1 : bwF;
        for (int x = 0; x < bwF; x++)
        {
            for (int y = 0; y < bhF; y++)
            {
                var cellOcc = board.GetPieceAt(new BoardCoord(x, y));
                bool isValidCell = cellOcc == null || (cellOcc.owner == piece.owner && cellOcc.pieceType == PieceType.OnePhase);
                if (!isValidCell) continue;
                bool surrounded = true;
                for (int dx = -1; dx <= 1 && surrounded; dx++)
                {
                    for (int dy = -1; dy <= 1 && surrounded; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= bwF || ny < 0 || ny >= bhF)
                        {
                            if (nx == homeEdgeX) continue;
                            else if (ny < 0 || ny >= bhF) continue;
                            else { surrounded = false; break; }
                        }
                        var neighbor = board.GetPieceAt(new BoardCoord(nx, ny));
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

    private string GetCurrentFriendlyStateHashFast(PlayerSide side)
    {
        var pieces = board.GetPiecesOf(side);
        var positions = pieces.Select(p => p.currentPosition)
            .OrderBy(c => c.x * 100 + c.y)
            .Select(c => $"{c.x},{c.y}");
        return string.Join("|", positions);
    }

    private string GetProjectedStateHashFast(PieceModel piece, BoardCoord target, bool isCapture)
    {
        var pieces = board.GetPiecesOf(piece.owner);
        var positions = new List<BoardCoord>();
        foreach (var p in pieces)
        {
            if (p.pieceId == piece.pieceId)
                positions.Add(target);
            else
                positions.Add(p.currentPosition);
        }
        var sorted = positions.OrderBy(c => c.x * 100 + c.y).Select(c => $"{c.x},{c.y}");
        return string.Join("|", sorted);
    }

    private float GetWorstOpponentResponseFast(PieceModel myPiece, BoardCoord myTarget)
    {
        var opponent = myPiece.owner == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;
        float worstScore = float.MinValue;
        var oppPieces = board.GetPiecesOf(opponent);

        foreach (var op in oppPieces)
        {
            if (op.spawnedThisTurn) continue;
            var opMoves = moveResolver.GetLegalMovesForPiece(op);
            foreach (var om in opMoves)
            {
                float threat = 0;
                var occ = board.GetPieceAt(om);
                if (occ != null && occ.owner == myPiece.owner)
                {
                    threat += occ.pieceType == PieceType.OnePhase ? 8000 : 500;
                }
                if (threat > worstScore) worstScore = threat;
            }
        }

        return worstScore > 0 ? worstScore : 0;
    }

    private float EvaluateLookAheadFast(PieceModel piece, BoardCoord target, EvalWeights w)
    {
        var simBoard = board.Clone();
        var simResolver = new MoveResolver(simBoard, rules);

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

        float ourPosScore = EvaluatePositionFast(simBoard, piece.owner, simResolver, w);

        var opponent = piece.owner == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;
        var oppPieces = simBoard.GetPiecesOf(opponent);
        float bestOppScore = float.MinValue;

        foreach (var op in oppPieces)
        {
            var opMoves2 = simResolver.GetLegalMovesForPiece(op);
            foreach (var om in opMoves2)
            {
                var sim2 = simBoard.Clone();
                var sim2Resolver = new MoveResolver(sim2, rules);

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

                float oppPos = EvaluatePositionFast(sim2, piece.owner, sim2Resolver, w);
                if (oppPos > bestOppScore) bestOppScore = oppPos;
            }
        }

        if (bestOppScore == float.MinValue) return ourPosScore;
        return ourPosScore - bestOppScore;
    }

    private float CalculateSafeThreatBonusFast(PieceModel piece, BoardCoord target, EvalWeights w)
    {
        var afterDirs = piece.pieceType == PieceType.TwoPhase
            ? PieceModel.TransformDirections(piece.GetCurrentFaceDirections(),
                GetMoveDirection(piece.currentPosition, target))
            : piece.GetCurrentFaceDirections();
        bool canCaptureNext = false;
        foreach (var d in afterDirs)
        {
            var next = new BoardCoord(target.x + BoardCoordUtil.Offset(d).x,
                target.y + BoardCoordUtil.Offset(d).y);
            if (!board.IsValidCoord(next)) continue;
            var nextOcc = board.GetPieceAt(next);
            if (nextOcc != null && nextOcc.owner != piece.owner)
            { canCaptureNext = true; break; }
        }
        if (!canCaptureNext) return 0;
        var opponent = piece.owner == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;
        var oppPieces = board.GetPiecesOf(opponent);
        foreach (var op in oppPieces)
        {
            if (moveResolver.GetLegalMovesForPiece(op).Contains(target))
                return 0;
        }
        return w.safeThreatBonus;
    }

    private float EvaluatePositionFast(BoardState b, PlayerSide side, MoveResolver resolver, EvalWeights w)
    {
        float score = 0;
        var opponent = side == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;
        var myPieces = b.GetPiecesOf(side);
        var oppPieces = b.GetPiecesOf(opponent);

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

        int bw = rules.boardWidth;
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
                    if (!b.IsValidCoord(adj)) continue;
                    var adjPiece = b.GetPieceAt(adj);
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
                if (!b.IsValidCoord(adj)) continue;
                var adjPiece = b.GetPieceAt(adj);
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

    public PlayerSide? SimulateLevels(EvalWeights w, int p1Level, int p2Level, int maxTurns = 100)
    {
        p1Weights = w;
        p2Weights = w;
        InitializeGame();
        return RunLoopLevels(p1Level, p2Level, maxTurns);
    }

    private PlayerSide? RunLoopLevels(int p1Level, int p2Level, int maxTurns)
    {
        for (int t = 0; t < maxTurns; t++)
        {
            if (winner.HasValue) return winner.Value;
            if (isDraw) return null;

            var player = turnManager.CurrentPlayer;
            int level = player == PlayerSide.Player1 ? p1Level : p2Level;
            var weights = player == PlayerSide.Player1 ? p1Weights : p2Weights;

            fastTurnStartPositions = new Dictionary<int, BoardCoord>();
            foreach (var p in board.GetPiecesOf(player))
                fastTurnStartPositions[p.pieceId] = p.currentPosition;
            fastMoveCount = 0;
            fastPastStateHashes.Insert(0, GetCurrentFriendlyStateHashFast(player));
            if (fastPastStateHashes.Count > 5)
                fastPastStateHashes.RemoveAt(fastPastStateHashes.Count - 1);

            int movesAllowed = turnManager.MovesRemaining;
            bool wasCapture = false;

            for (int m = 0; m < movesAllowed; m++)
            {
                if (winner.HasValue) return winner.Value;
                if (isDraw) return null;

                fastMoveCount = m + 1;
                var movable = GetMovablePieces(player);
                if (movable.Count == 0) break;

                var best = PickMoveByLevel(movable, weights, level);
                if (best == null) break;

                wasCapture = ExecuteMove(best.Value.piece, best.Value.target, player);
                if (wasCapture) break;
                if (winner.HasValue) return winner.Value;
                if (isDraw) return null;

                turnManager.OnMoveCompleted(best.Value.piece);
            }

            if (!winner.HasValue && !wasCapture)
            {
                foreach (var p in board.GetAllPieces()) p.spawnedThisTurn = false;
                turnManager.StartNextTurn();
                foreach (var p in board.GetAllPieces())
                    if (p.owner == turnManager.CurrentPlayer) p.canActThisTurn = true;
                if (repDetector.RecordAndCheck(board, turnManager.CurrentPlayer, turnManager.TurnNumber))
                    return null;
            }
        }
        return null;
    }

    private (PieceModel piece, BoardCoord target, float score, bool isCapture)? PickMoveByLevel(
        List<PieceModel> pieces, EvalWeights w, int level)
    {
        var allMoves = new List<(PieceModel piece, BoardCoord target, bool isCapture)>();
        foreach (var p in pieces)
        {
            var legals = moveResolver.GetLegalMovesForPiece(p);
            foreach (var t in legals)
            {
                var occ = board.GetPieceAt(t);
                bool isCap = occ != null && occ.owner != p.owner;
                allMoves.Add((p, t, isCap));
            }
        }
        if (allMoves.Count == 0) return null;

        if (level == 1)
        {
            var rnd = allMoves[UnityEngine.Random.Range(0, allMoves.Count)];
            return (rnd.piece, rnd.target, 0f, rnd.isCapture);
        }

        if (level == 2)
        {
            var captures = allMoves.Where(m => m.isCapture).ToList();
            if (captures.Count > 0)
            {
                var c = captures[UnityEngine.Random.Range(0, captures.Count)];
                return (c.piece, c.target, 0f, true);
            }
            var r = allMoves[UnityEngine.Random.Range(0, allMoves.Count)];
            return (r.piece, r.target, 0f, false);
        }

        (PieceModel piece, BoardCoord target, float score, bool isCapture)? best = null;
        foreach (var m in allMoves)
        {
            float sc = EvaluateMoveFast(m.piece, m.target, w);

            if (level >= 4)
            {
                float lookAhead = EvaluateLookAheadFast(m.piece, m.target, w);
                sc += lookAhead;
                sc += CalculateSafeThreatBonusFast(m.piece, m.target, w);
            }

            if (best == null || sc > best.Value.score)
                best = (m.piece, m.target, sc, m.isCapture);
            else if (sc == best.Value.score && UnityEngine.Random.value < 0.5f)
                best = (m.piece, m.target, sc, m.isCapture);
        }
        return best;
    }

    public static void TestCpuLevels()
    {
        var w = WeightEvolution.LoadBestWeights();
        if (w == null) { Debug.LogError("[Test] No best_weights.json found"); return; }

        var sim = new FastGameSimulator();
        int gamesPerMatch = 10;

        Debug.Log("[Test] === CPU Level Validation ===");
        Debug.Log($"[Test] Weights loaded: capture1P={w.captureOnePhase:F0} capture2P={w.captureTwoPhase:F0}");

        for (int a = 1; a <= 4; a++)
        {
            for (int b = a + 1; b <= 4; b++)
            {
                int aWins = 0, bWins = 0, draws = 0;
                for (int g = 0; g < gamesPerMatch; g++)
                {
                    var r1 = sim.SimulateLevels(w, a, b);
                    if (r1 == PlayerSide.Player1) aWins++;
                    else if (r1 == PlayerSide.Player2) bWins++;
                    else draws++;

                    var r2 = sim.SimulateLevels(w, b, a);
                    if (r2 == PlayerSide.Player2) aWins++;
                    else if (r2 == PlayerSide.Player1) bWins++;
                    else draws++;
                }
                int total = aWins + bWins + draws;
                Debug.Log($"[Test] Lv{a} vs Lv{b}: Lv{a} wins {aWins}/{total} ({100f*aWins/total:F0}%)  Lv{b} wins {bWins}/{total} ({100f*bWins/total:F0}%)  draws {draws}/{total}");
            }
        }
    }
}
