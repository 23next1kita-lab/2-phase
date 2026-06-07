using System.Collections.Generic;
using UnityEngine;

public class BoardView : MonoBehaviour
{
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private Transform boardParent;
    [SerializeField] private Color cellColor1 = Color.white;
    [SerializeField] private Color cellColor2 = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color highlightColor = Color.green;
    [SerializeField] private Color placementHighlightColor = Color.yellow;

    private GameManager gameManager;
    private CellView[,] cellViews;
    private List<PieceView> pieceViews;
    private List<CellView> highlightedCells;

    public void Initialize(GameManager gm)
    {
        if (pieceViews != null)
        {
            foreach (var pv in pieceViews)
                if (pv != null) Destroy(pv.gameObject);
        }
        if (cellViews != null)
        {
            foreach (var cv in cellViews)
                if (cv != null && cv.gameObject != null) Destroy(cv.gameObject);
        }
        if (highlightedCells != null)
            highlightedCells.Clear();

        gameManager = gm;
        pieceViews = new List<PieceView>();
        highlightedCells = new List<CellView>();

        EnsureCamera();
        CreateBackground();
        CreateBoard();
        CreateAllPieceViews();
    }

    private void EnsureCamera()
    {
        if (Camera.main != null) return;

        GameObject camObj = new GameObject("Main Camera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        camObj.transform.position = new Vector3(0, 0, -10);
        camObj.tag = "MainCamera";
    }

    private void CreateBackground()
    {
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(boardParent != null ? boardParent : transform);
        var sr = bgObj.AddComponent<SpriteRenderer>();
        if (SpriteCache.Background != null)
            sr.sprite = SpriteCache.Background;
        else
            sr.sprite = DebugSpriteGenerator.CreateRectangleSprite(64, 64, new Color(0.2f, 0.2f, 0.2f), Color.black);
        sr.sortingOrder = -2;
        float boardExtent = Mathf.Max(gameManager.GameRules.boardWidth, gameManager.GameRules.boardHeight) * 0.5f + 0.5f;
        bgObj.transform.localScale = Vector3.one * boardExtent * 0.5f;
        bgObj.transform.localPosition = new Vector3(0, 0, 0);
        bgObj.transform.SetSiblingIndex(0);
    }

    private void CreateBoard()
    {
        if (boardParent == null)
            boardParent = transform;

        cellViews = new CellView[gameManager.GameRules.boardWidth, gameManager.GameRules.boardHeight];

        for (int x = 0; x < gameManager.GameRules.boardWidth; x++)
        {
            for (int y = 0; y < gameManager.GameRules.boardHeight; y++)
            {
                GameObject cellObj;
                if (cellPrefab != null)
                {
                    cellObj = Instantiate(cellPrefab, boardParent);
                }
                else
                {
                    cellObj = new GameObject($"Cell_{x}_{y}");
                    cellObj.transform.SetParent(boardParent);
                    var sr = cellObj.AddComponent<SpriteRenderer>();
                    sr.sprite = DebugSpriteGenerator.CreateRectangleSprite(32, 32, new Color(0.9f, 0.9f, 0.9f), Color.gray);
                    sr.sortingOrder = -1;
                    var collider = cellObj.AddComponent<BoxCollider2D>();
                    collider.size = new Vector2(0.95f, 0.95f);
                    cellObj.AddComponent<CellView>();
                }

                cellObj.name = $"Cell_{x}_{y}";
                float posX = x - (gameManager.GameRules.boardWidth - 1) * 0.5f;
                float posY = y - (gameManager.GameRules.boardHeight - 1) * 0.5f;
                cellObj.transform.localPosition = new Vector3(posX, posY, 0);

                var cellView = cellObj.GetComponent<CellView>();
                if (cellView == null)
                    cellView = cellObj.AddComponent<CellView>();

                cellView.Initialize(new BoardCoord(x, y), gameManager);

                bool isDark = (x + y) % 2 == 1;
                cellView.SetColor(isDark ? cellColor2 : cellColor1);

                cellViews[x, y] = cellView;
            }
        }

        float camSize = Mathf.Max(gameManager.GameRules.boardWidth, gameManager.GameRules.boardHeight) * 0.6f + 1f;
        if (Application.isMobilePlatform)
            camSize /= 1.5f;
        if (Camera.main != null)
            Camera.main.orthographicSize = camSize;
    }

    private void CreateAllPieceViews()
    {
        foreach (var existing in pieceViews)
        {
            if (existing != null) Destroy(existing.gameObject);
        }
        pieceViews.Clear();

        foreach (var piece in gameManager.BoardState.GetAllPieces())
        {
            CreatePieceView(piece);
        }
    }

    public void CreatePieceView(PieceModel piece)
    {
        float posX = piece.currentPosition.x - (gameManager.GameRules.boardWidth - 1) * 0.5f;
        float posY = piece.currentPosition.y - (gameManager.GameRules.boardHeight - 1) * 0.5f;

        GameObject pieceObj;
        if (piecePrefab != null)
        {
            pieceObj = Instantiate(piecePrefab, boardParent);
        }
        else
        {
            pieceObj = new GameObject($"Piece_{piece.pieceId}");
            pieceObj.transform.SetParent(boardParent);

            SpriteRenderer sr = pieceObj.AddComponent<SpriteRenderer>();

            if (piece.pieceType == PieceType.TwoPhase)
            {
                Sprite s = piece.owner == PlayerSide.Player1 ? SpriteCache.TwoPhaseBlue : SpriteCache.TwoPhaseRed;
                if (s != null)
                    sr.sprite = s;
                else
                    sr.sprite = DebugSpriteGenerator.CreateCircleSprite(28, Color.white);
                sr.sortingOrder = 1;
            }
            else
            {
                Sprite s = piece.owner == PlayerSide.Player1 ? SpriteCache.OnePhaseBlue : SpriteCache.OnePhaseRed;
                if (s != null)
                    sr.sprite = s;
                else
                    sr.sprite = DebugSpriteGenerator.CreateSquareSprite(24, Color.white);
                sr.sortingOrder = 1;
            }
            var collider = pieceObj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(3.2f, 3.2f);
            if (sr.sprite != null)
            {
                if (piece.pieceType == PieceType.TwoPhase)
                {
                    pieceObj.transform.localScale = Vector3.one * 0.24371f;
                }
                else
                {
                    Sprite twoPhaseRef = piece.owner == PlayerSide.Player1 ? SpriteCache.TwoPhaseBlue : SpriteCache.TwoPhaseRed;
                    if (twoPhaseRef != null)
                    {
                        float refWorldWidth = twoPhaseRef.rect.width / twoPhaseRef.pixelsPerUnit;
                        float thisWorldWidth = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                        pieceObj.transform.localScale = Vector3.one * 0.24371f * (refWorldWidth / thisWorldWidth);
                    }
                    else
                    {
                        pieceObj.transform.localScale = Vector3.one * 0.24371f;
                    }
                }
            }
            else
            {
                pieceObj.transform.localScale = Vector3.one * 0.224f;
            }

            pieceObj.AddComponent<PieceView>();
        }

        pieceObj.transform.localPosition = new Vector3(posX, posY, -0.1f);

        var pieceView = pieceObj.GetComponent<PieceView>();
        if (pieceView == null)
            pieceView = pieceObj.AddComponent<PieceView>();
        pieceView.Initialize(piece, gameManager);

        pieceViews.Add(pieceView);
    }

    public void UpdateHighlights(List<BoardCoord> moves)
    {
        foreach (var cell in highlightedCells)
        {
            bool isDark = (cell.Coord.x + cell.Coord.y) % 2 == 1;
            cell.SetColor(isDark ? cellColor2 : cellColor1);
            cell.SetHighlight(false);
        }
        highlightedCells.Clear();

        if (moves == null) return;

        foreach (var coord in moves)
        {
            if (coord.x >= 0 && coord.x < cellViews.GetLength(0) &&
                coord.y >= 0 && coord.y < cellViews.GetLength(1))
            {
                var cell = cellViews[coord.x, coord.y];
                cell.SetColor(highlightColor);
                cell.SetHighlight(true);
                highlightedCells.Add(cell);
            }
        }
    }

    public void UpdateSplitPlacementHighlights(List<BoardCoord> cells)
    {
        foreach (var cell in highlightedCells)
        {
            bool isDark = (cell.Coord.x + cell.Coord.y) % 2 == 1;
            cell.SetColor(isDark ? cellColor2 : cellColor1);
            cell.SetHighlight(false);
        }
        highlightedCells.Clear();

        if (cells == null) return;

        foreach (var coord in cells)
        {
            if (coord.x >= 0 && coord.x < cellViews.GetLength(0) &&
                coord.y >= 0 && coord.y < cellViews.GetLength(1))
            {
                var cell = cellViews[coord.x, coord.y];
                cell.SetColor(placementHighlightColor);
                cell.SetHighlight(true);
                highlightedCells.Add(cell);
            }
        }
    }

    public void RefreshAllPieces()
    {
        foreach (var pv in pieceViews)
        {
            if (pv != null) Destroy(pv.gameObject);
        }
        pieceViews.Clear();
        CreateAllPieceViews();
    }

    public void UpdatePieceView(int pieceId)
    {
        for (int i = 0; i < pieceViews.Count; i++)
        {
            if (pieceViews[i] != null && pieceViews[i].PieceModel != null && pieceViews[i].PieceModel.pieceId == pieceId)
            {
                pieceViews[i].UpdateVisual();
                return;
            }
        }
    }

    public void RemovePieceView(int pieceId)
    {
        for (int i = pieceViews.Count - 1; i >= 0; i--)
        {
            if (pieceViews[i] != null && pieceViews[i].PieceModel != null && pieceViews[i].PieceModel.pieceId == pieceId)
            {
                Destroy(pieceViews[i].gameObject);
                pieceViews.RemoveAt(i);
                return;
            }
        }
    }

    private void Update()
    {
        if (gameManager == null) return;

        for (int i = pieceViews.Count - 1; i >= 0; i--)
        {
            var pv = pieceViews[i];
            if (pv == null) { pieceViews.RemoveAt(i); continue; }
            if (pv.PieceModel == null) { Destroy(pv.gameObject); pieceViews.RemoveAt(i); continue; }

            float posX = pv.PieceModel.currentPosition.x - (gameManager.GameRules.boardWidth - 1) * 0.5f;
            float posY = pv.PieceModel.currentPosition.y - (gameManager.GameRules.boardHeight - 1) * 0.5f;
            pv.transform.localPosition = Vector3.Lerp(pv.transform.localPosition, new Vector3(posX, posY, -0.1f), 0.3f);
        }
    }

    public void ClearAllPieces()
    {
        foreach (var pv in pieceViews)
        {
            if (pv != null && pv.gameObject != null)
                Destroy(pv.gameObject);
        }
        pieceViews.Clear();
    }
}
