using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum PlayMode { Offline, Online, CPU, CpuVsCpu }

    [SerializeField] private GameRulesSO gameRules;
    [SerializeField] private InitialPieceLayoutSO initialLayout;
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private GameObject boardCellPrefab;

    private BoardState boardState;
    private TurnManager turnManager;
    private MoveResolver moveResolver;
    private CaptureResolver captureResolver;
    private PlacementResolver placementResolver;
    private SplitPlacementController splitController;
    private GamePhase currentPhase;

    private PieceModel selectedPiece;
    private List<BoardCoord> legalMoves;
    private CaptureResult pendingCaptureResult;
    private List<PieceModel> pendingSplitPieces;
    private BoardCoord lastCaptureCell;

    private BoardView boardView;
    private UIManager uiManager;
    private PlayMode playMode;
    private RepetitionDetector repetitionDetector;
    private PlayerSide? gameWinner;
    private float p1TimeRemaining;
    private float p2TimeRemaining;

    public GameRulesSO GameRules => gameRules;
    public BoardState BoardState => boardState;
    public TurnManager TurnManager => turnManager;
    public GamePhase CurrentPhase => currentPhase;
    public PieceModel SelectedPiece => selectedPiece;
    public List<BoardCoord> LegalMoves => legalMoves;
    public List<PieceModel> PendingSplitPieces => pendingSplitPieces;
    public BoardCoord LastCaptureCell => lastCaptureCell;
    public BoardView BoardView => boardView;
    public PlayMode CurrentPlayMode => playMode;
    public bool IsHost { get; set; }
    public PlayerSide LocalPlayerSide { get; set; }
    public int CpuLevel { get; set; } = 1;
    public System.Action OnGameStateChanged;
    public RepetitionDetector RepetitionDetector => repetitionDetector;
    public PlayerSide? GameWinner => gameWinner;
    public float P1TimeRemaining => p1TimeRemaining;
    public float P2TimeRemaining => p2TimeRemaining;
    public bool TimeControlEnabled => gameRules != null && gameRules.timeControlSeconds > 0;

    public bool WouldMoveCauseRepetition(PieceModel piece, BoardCoord target, bool isCapture)
    {
        if (repetitionDetector == null) return false;
        return repetitionDetector.WouldRepeatAfterMove(boardState, turnManager.CurrentPlayer,
            turnManager.TurnNumber, piece, target, isCapture);
    }

    private void Awake()
    {
        if (FindObjectOfType<AudioManager>() == null)
        {
            var audioObj = new GameObject("AudioManager");
            audioObj.AddComponent<AudioManager>();
        }

        if (gameRules == null)
        {
            gameRules = ScriptableObject.CreateInstance<GameRulesSO>();
            Debug.LogWarning("[Game] No GameRulesSO assigned. Using default settings.");
        }
        gameRules.LogSettings();
    }

    private void Start()
    {
        if (FindObjectOfType<MainMenuUI>() == null)
        {
            var menuObj = new GameObject("MainMenuUI");
            menuObj.AddComponent<MainMenuUI>();
        }
    }

    private void Update()
    {
        if (currentPhase == GamePhase.GameOver || gameRules == null || gameRules.timeControlSeconds <= 0)
            return;
        if (turnManager == null) return;

        float dt = Time.deltaTime;
        if (turnManager.CurrentPlayer == PlayerSide.Player1)
        {
            p1TimeRemaining -= dt;
            if (p1TimeRemaining <= 0)
            {
                p1TimeRemaining = 0;
                gameWinner = PlayerSide.Player2;
                currentPhase = GamePhase.GameOver;
                if (uiManager != null) uiManager.ShowGameOver(PlayerSide.Player2);
                OnGameStateChanged?.Invoke();
            }
        }
        else
        {
            p2TimeRemaining -= dt;
            if (p2TimeRemaining <= 0)
            {
                p2TimeRemaining = 0;
                gameWinner = PlayerSide.Player1;
                currentPhase = GamePhase.GameOver;
                if (uiManager != null) uiManager.ShowGameOver(PlayerSide.Player1);
                OnGameStateChanged?.Invoke();
            }
        }

        if (uiManager != null)
            uiManager.UpdateUI();
    }

    public void StartGame(PlayMode mode, int cpuLevel = 1)
    {
        playMode = mode;
        CpuLevel = cpuLevel;
        InitializeGame();

        if (mode == PlayMode.Online)
        {
            var nm = gameObject.AddComponent<NetworkManager>();
            nm.OnConnected += OnNetworkConnected;
            nm.ShowLobby();
        }
        else if (mode == PlayMode.CPU)
        {
            var cpu = gameObject.AddComponent<CpuPlayer>();
            cpu.DifficultyLevel = cpuLevel;
            cpu.cpuSide = PlayerSide.Player2;
        }
        else if (mode == PlayMode.CpuVsCpu)
        {
            var cpu1 = gameObject.AddComponent<CpuPlayer>();
            cpu1.DifficultyLevel = cpuLevel;
            cpu1.cpuSide = PlayerSide.Player1;
            var cpu2 = gameObject.AddComponent<CpuPlayer>();
            cpu2.DifficultyLevel = cpuLevel;
            cpu2.cpuSide = PlayerSide.Player2;
        }

        OnGameStateChanged?.Invoke();
    }

    private void OnNetworkConnected()
    {
        var handler = FindObjectOfType<NetworkGameHandler>();
        if (handler != null)
        {
            IsHost = handler.HasStateAuthority;
            LocalPlayerSide = IsHost ? PlayerSide.Player1 : PlayerSide.Player2;
            if (!IsHost)
            {
                var clickHandler = FindObjectOfType<ClickHandler>();
                if (clickHandler != null)
                    clickHandler.networkHandler = handler;
            }
        }
    }

    public void InitializeGame()
    {
        SpriteCache.Load();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayRandomBGM("battle1", "battle2");

        if (splitController != null)
            splitController.ClearFloatingPieces();

        selectedPiece = null;
        legalMoves = null;
        pendingSplitPieces = null;

        boardState = new BoardState(gameRules.boardWidth, gameRules.boardHeight);
        turnManager = new TurnManager(gameRules);
        moveResolver = new MoveResolver(boardState, gameRules);
        captureResolver = new CaptureResolver(boardState, gameRules);
        placementResolver = new PlacementResolver(boardState, gameRules);
        repetitionDetector = new RepetitionDetector();
        p1TimeRemaining = gameRules.timeControlSeconds;
        p2TimeRemaining = gameRules.timeControlSeconds;

        PlaceInitialPieces();

        if (FindObjectOfType<ClickHandler>() == null)
            gameObject.AddComponent<ClickHandler>();

        boardView = FindObjectOfType<BoardView>();
        if (boardView == null)
        {
            GameObject bvObj = new GameObject("BoardView");
            bvObj.transform.SetParent(transform);
            boardView = bvObj.AddComponent<BoardView>();
        }
        boardView.Initialize(this);

        EnsureEventSystemExists();

        uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            GameObject uiObj = new GameObject("UIManager");
            uiManager = uiObj.AddComponent<UIManager>();
        }
        uiManager.Initialize(this);

        splitController = new SplitPlacementController();
        splitController.Initialize(this, uiManager);
        currentPhase = GamePhase.WaitingForPieceSelect;
        selectedPiece = null;
        legalMoves = null;
        pendingSplitPieces = null;

        boardState.LogState();
        Debug.Log($"[Game] Game started. First turn: {turnManager.CurrentPlayer}, moves: {turnManager.MovesRemaining}");

        repetitionDetector.RecordAndCheck(boardState, turnManager.CurrentPlayer, turnManager.TurnNumber);

        if (uiManager != null)
            uiManager.UpdateUI();
    }

    private void EnsureEventSystemExists()
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    private void PlaceInitialPieces()
    {
        if (initialLayout == null)
        {
            Debug.LogWarning("InitialPieceLayoutSO not assigned. Using default layout.");
            CreateDefaultLayout();
            return;
        }

        foreach (var data in initialLayout.pieces)
        {
            if (!boardState.IsValidCoord(data.position))
            {
                Debug.LogWarning($"Invalid position {data.position} for initial piece, skipping.");
                continue;
            }

            int id = boardState.GeneratePieceId();
            var piece = new PieceModel(id, data.owner, data.pieceType, data.position,
                data.frontDirections, data.backDirections, true);
            boardState.AddPiece(piece);
        }
    }

    private void CreateDefaultLayout()
    {
        int h = gameRules.boardHeight;
        int w = gameRules.boardWidth;

        var presets = gameRules.randomPieceDirections
            ? GenerateRandomPresets(h)
            : GetDefaultPresets();

        for (int y = 0; y < h; y++)
        {
            var p = presets[y];
            int id = boardState.GeneratePieceId();
            var piece = new PieceModel(id, PlayerSide.Player1, PieceType.TwoPhase,
                new BoardCoord(0, y), p.front, p.back, true);
            boardState.AddPiece(piece);
        }

        for (int y = 0; y < h; y++)
        {
            var p = presets[y];
            int id = boardState.GeneratePieceId();
            var frontForP2 = MirrorDirections(p.front);
            var backForP2 = MirrorDirections(p.back);
            var piece = new PieceModel(id, PlayerSide.Player2, PieceType.TwoPhase,
                new BoardCoord(w - 1, y), frontForP2, backForP2, true);
            boardState.AddPiece(piece);
        }

        Debug.Log($"[Game] Default layout: {boardState.GetAllPieces().Count} pieces with 7 direction sets");
    }

    private (List<Direction> front, List<Direction> back)[] GetDefaultPresets()
    {
        var F = Direction.Right; var B = Direction.Left;
        var UR = Direction.UpRight; var UL = Direction.UpLeft;
        var DR = Direction.DownRight; var DL = Direction.DownLeft;
        var U = Direction.Up; var D = Direction.Down;

        return new (List<Direction> front, List<Direction> back)[]
        {
            (new List<Direction>{ F, UR, UL }, new List<Direction>{ U, DL, UL }),
            (new List<Direction>{ DR, F, UR, B }, new List<Direction>{ DL, D, DR, U }),
            (new List<Direction>{ F, UR, DR, U, DL }, new List<Direction>{ B, UL, DL, U, DR }),
            (new List<Direction>{ UR, F, DR, UL, B, DL }, new List<Direction>{ UR, U, UL, DR, D, DL }),
            (new List<Direction>{ F, DR, UR, D, UL }, new List<Direction>{ B, DL, UL, D, UR }),
            (new List<Direction>{ UR, F, DR, B }, new List<Direction>{ UL, U, UR, D }),
            (new List<Direction>{ F, DR, DL }, new List<Direction>{ U, UL, DL }),
        };
    }

    private (List<Direction> front, List<Direction> back)[] GenerateRandomPresets(int count)
    {
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
            int j = Random.Range(i, shuffled.Count);
            var tmp = shuffled[i]; shuffled[i] = shuffled[j]; shuffled[j] = tmp;
        }
        return shuffled.GetRange(0, Mathf.Min(count, shuffled.Count));
    }

    private List<Direction> MirrorDirections(List<Direction> dirs)
    {
        var result = new List<Direction>();
        foreach (var d in dirs)
        {
            result.Add(d switch
            {
                Direction.Right => Direction.Left,
                Direction.Left => Direction.Right,
                Direction.UpRight => Direction.UpLeft,
                Direction.UpLeft => Direction.UpRight,
                Direction.DownRight => Direction.DownLeft,
                Direction.DownLeft => Direction.DownRight,
                _ => d
            });
        }
        return result;
    }

    public void OnPieceClicked(PieceModel piece)
    {
        if (currentPhase == GamePhase.WaitingForDestinationSelect)
        {
            if (piece.owner != turnManager.CurrentPlayer && selectedPiece != null && piece != selectedPiece)
            {
                TryMove(piece.currentPosition);
            }
            else if (piece.owner == turnManager.CurrentPlayer && piece != selectedPiece)
            {
                if (piece.canActThisTurn && !piece.spawnedThisTurn && turnManager.CanMoveThisTurn(piece))
                {
                    selectedPiece = piece;
                    legalMoves = moveResolver.GetLegalMovesForPiece(piece);
                    currentPhase = GamePhase.WaitingForDestinationSelect;
                    Debug.Log($"[Input] Re-selected {selectedPiece.owner}'s piece {selectedPiece.pieceId}.");
                    if (boardView != null) boardView.UpdateHighlights(legalMoves);
                    if (uiManager != null) uiManager.UpdateUI();
                }
            }
            return;
        }

        if (currentPhase != GamePhase.WaitingForPieceSelect) return;

        if (piece.owner != turnManager.CurrentPlayer)
        {
            Debug.Log($"[Input] Cannot select opponent's piece.");
            return;
        }

        if (!piece.canActThisTurn || piece.spawnedThisTurn)
        {
            Debug.Log($"[Input] Piece {piece.pieceId} cannot act this turn.");
            return;
        }

        if (!turnManager.CanMoveThisTurn(piece))
        {
            Debug.Log($"[Input] No moves remaining this turn.");
            return;
        }

        selectedPiece = piece;
        legalMoves = moveResolver.GetLegalMovesForPiece(piece);
        currentPhase = GamePhase.WaitingForDestinationSelect;

        Debug.Log($"[Input] Selected {selectedPiece.owner}'s piece {selectedPiece.pieceId} at {selectedPiece.currentPosition}. Legal moves: {legalMoves.Count}");

        AudioManager.Instance?.PlaySE("piece_select");

        if (boardView != null)
            boardView.UpdateHighlights(legalMoves);
        if (uiManager != null)
            uiManager.UpdateUI();

        OnGameStateChanged?.Invoke();
    }

    public void OnCellClicked(BoardCoord coord)
    {
        if (currentPhase == GamePhase.WaitingForDestinationSelect)
        {
            TryMove(coord);
            OnGameStateChanged?.Invoke();
        }
    }

    private void TryMove(BoardCoord target)
    {
        if (selectedPiece == null) return;

        if (!legalMoves.Contains(target))
        {
            Debug.Log($"[Input] {target} is not a legal move.");
            return;
        }

        PieceModel occupant = boardState.GetPieceAt(target);
        bool hasOpponent = occupant != null && occupant.owner != selectedPiece.owner;

        if (hasOpponent)
        {
            PieceModel captured = occupant;
            boardState.RemovePiece(captured);

            Direction moveDir = GetMoveDirection(selectedPiece.currentPosition, target);
            moveResolver.ApplyMove(selectedPiece, target);

            if (selectedPiece.pieceType == PieceType.TwoPhase)
                moveResolver.ApplyFlip(selectedPiece, moveDir);
            if (boardView != null)
                boardView.UpdatePieceView(selectedPiece.pieceId);

            if (boardView != null)
                boardView.RemovePieceView(captured.pieceId);

            AudioManager.Instance?.PlaySE("capture");

            var captureResult = captureResolver.ResolveCaptureWithCaptured(captured, selectedPiece.owner);

            if (captureResult.isGameOver)
            {
                gameWinner = captureResult.winner;
                if (boardView != null) boardView.UpdatePieceView(selectedPiece.pieceId);
                currentPhase = GamePhase.GameOver;
                Debug.Log($"[Game] Game over! Winner: {captureResult.winner}");
                AudioManager.Instance?.PlaySE("gameover");
                if (uiManager != null)
                {
                    uiManager.ShowGameOver(captureResult.winner);
                    uiManager.UpdateUI();
                }
                if (boardView != null)
                    boardView.UpdateHighlights(null);
                selectedPiece = null;
                legalMoves = null;
                return;
            }

            if (captureResult.needsSplitPlacement)
            {
                turnManager.OnMoveCompleted(selectedPiece);

                pendingSplitPieces = captureResult.splitPieces;
                lastCaptureCell = target;

                if (playMode == PlayMode.Online && captured.owner != LocalPlayerSide)
                {
                    var handler = FindObjectOfType<NetworkGameHandler>();
                    if (handler != null)
                    {
                        var json = handler.SerializeSplitPieces(pendingSplitPieces);
                        handler.RPC_StartClientSplitPlacement(json);
                    }
                    currentPhase = GamePhase.WaitingForOpponentSplit;
                    OnGameStateChanged?.Invoke();
                    if (uiManager != null)
                        uiManager.ShowMessage("相手の分裂配置を待っています...");
                    selectedPiece = null;
                    legalMoves = null;
                    return;
                }

                currentPhase = GamePhase.PlacingSplitPieces;
                OnGameStateChanged?.Invoke();

                Debug.Log($"[Game] Entering split placement phase. Drag-floating split pieces onto the board.");
                if (boardView != null)
                    boardView.UpdateHighlights(null);

                splitController.CreateFloatingPieces(pendingSplitPieces);

                if (uiManager != null)
                {
                    uiManager.ShowSplitPlacementMessage(pendingSplitPieces.Count);
                    uiManager.UpdateUI();
                }
                selectedPiece = null;
                legalMoves = null;
                return;
            }

            if (boardView != null) boardView.UpdatePieceView(selectedPiece.pieceId);

            if (gameRules.captureEndsTurnImmediately)
            {
                turnManager.EndTurnEarly();
            }
        }
        else
        {
            Direction moveDir = GetMoveDirection(selectedPiece.currentPosition, target);
            moveResolver.ApplyMove(selectedPiece, target);

            if (selectedPiece.pieceType == PieceType.TwoPhase)
                moveResolver.ApplyFlip(selectedPiece, moveDir);
            if (boardView != null)
                boardView.UpdatePieceView(selectedPiece.pieceId);

            AudioManager.Instance?.PlaySE("move");
            turnManager.OnMoveCompleted(selectedPiece);
        }

        if (turnManager.IsTurnOver())
        {
            EndTurn();
        }
        else
        {
            selectedPiece = null;
            legalMoves = null;
            currentPhase = GamePhase.WaitingForPieceSelect;
            if (boardView != null)
                boardView.UpdateHighlights(null);
            if (uiManager != null)
                uiManager.UpdateUI();
        }
    }

    public void PlaceSplitPieceOnBoard(PieceModel piece, BoardCoord coord)
    {
        if (piece == null) return;
        placementResolver.PlaceSplitPiece(piece, coord, piece.GetCurrentFaceDirections());
    }

    public void UnplaceSplitPieceFromBoard(PieceModel piece)
    {
        if (piece == null) return;
        boardState.RemovePiece(piece);
    }

    public void FinalizeSplitPlacement(List<PieceModel> placedSplitPieces)
    {
        turnManager.StartNextTurn();

        if (placedSplitPieces != null)
        {
            foreach (var p in placedSplitPieces)
            {
                p.spawnedThisTurn = true;
                p.canActThisTurn = true;
            }
        }

        foreach (var piece in boardState.GetAllPieces())
        {
            if (piece.owner == turnManager.CurrentPlayer)
                piece.canActThisTurn = true;
        }

        if (repetitionDetector.RecordAndCheck(boardState, turnManager.CurrentPlayer, turnManager.TurnNumber))
        {
            gameWinner = null;
            currentPhase = GamePhase.GameOver;
            Debug.Log("[Game] Draw by repetition (after split)!");
            AudioManager.Instance?.PlaySE("gameover");
            if (uiManager != null)
                uiManager.ShowGameOver(null);
            if (boardView != null)
                boardView.UpdateHighlights(null);
            selectedPiece = null;
            legalMoves = null;
            pendingSplitPieces = null;
            OnGameStateChanged?.Invoke();
            return;
        }

        pendingSplitPieces = null;
        currentPhase = GamePhase.WaitingForPieceSelect;

        if (splitController != null)
            splitController.ClearFloatingPieces();

        if (uiManager != null)
        {
            uiManager.ShowConfirmButton(false);
            uiManager.ClearMessage();
        }

        Debug.Log($"[Game] Split placement confirmed. {turnManager.CurrentPlayer} starts turn (split pieces spawned).");

        if (boardView != null)
            boardView.UpdateHighlights(null);
        if (uiManager != null)
            uiManager.UpdateUI();
        OnGameStateChanged?.Invoke();
    }

    private void EndTurn()
    {
        foreach (var piece in boardState.GetAllPieces())
        {
            piece.spawnedThisTurn = false;
        }

        turnManager.StartNextTurn();

        foreach (var piece in boardState.GetAllPieces())
        {
            if (piece.owner == turnManager.CurrentPlayer)
            {
                piece.canActThisTurn = true;
            }
        }

        if (repetitionDetector.RecordAndCheck(boardState, turnManager.CurrentPlayer, turnManager.TurnNumber))
        {
            gameWinner = null;
            currentPhase = GamePhase.GameOver;
            Debug.Log("[Game] Draw by repetition!");
            AudioManager.Instance?.PlaySE("gameover");
            if (uiManager != null)
                uiManager.ShowGameOver(null);
            if (boardView != null)
                boardView.UpdateHighlights(null);
            selectedPiece = null;
            legalMoves = null;
            OnGameStateChanged?.Invoke();
            return;
        }

        selectedPiece = null;
        legalMoves = null;
        currentPhase = GamePhase.WaitingForPieceSelect;

        if (boardView != null)
            boardView.UpdateHighlights(null);
        if (uiManager != null)
            uiManager.UpdateUI();

        boardState.LogState();
        OnGameStateChanged?.Invoke();
    }

    public void ConfirmSplitPlacement()
    {
        if (playMode == PlayMode.Online && turnManager.CurrentPlayer != LocalPlayerSide)
        {
            var handler = FindObjectOfType<NetworkGameHandler>();
            if (handler != null)
            {
                var placed = splitController.ConfirmPlacement();
                if (placed != null && placed.Count >= 2)
                {
                    var json = handler.SerializePlacementResult(placed);
                    handler.RPC_FinishClientSplitPlacement(json);
                    uiManager?.ClearMessage();
                }
            }
            return;
        }

        var placedLocal = splitController.ConfirmPlacement();
        if (placedLocal != null && placedLocal.Count >= 2)
            FinalizeSplitPlacement(placedLocal);
    }

    public void StartRemoteSplitPlacement(List<PieceModel> splitPieces)
    {
        pendingSplitPieces = splitPieces;
        lastCaptureCell = new BoardCoord(-1, -1);
        currentPhase = GamePhase.PlacingSplitPieces;

        splitController.CreateFloatingPieces(splitPieces);

        if (uiManager != null)
        {
            uiManager.ShowSplitPlacementMessage(splitPieces.Count);
            uiManager.UpdateUI();
        }

        if (boardView != null)
            boardView.UpdateHighlights(null);
    }

    public void SyncPhaseFromNetwork(GamePhase phase)
    {
        currentPhase = phase;
        if (uiManager != null)
            uiManager.UpdateUI();
    }

    public PlayerSide GetOpponent(PlayerSide side)
    {
        return side == PlayerSide.Player1 ? PlayerSide.Player2 : PlayerSide.Player1;
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
}
