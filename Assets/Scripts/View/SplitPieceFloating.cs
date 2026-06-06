using System.Collections.Generic;
using UnityEngine;

public class SplitPieceFloating : MonoBehaviour
{
    private PieceModel piece;
    private GameManager gameManager;
    private SplitPlacementController splitController;
    private bool isDragging;
    private Vector3 dragOffset;
    private Vector3 dragStartPosition;
    private Vector3 originalPosition;
    private bool isOnBoard;
    private SpriteRenderer bodySr;
    private GameObject arrowsParent;
    private List<GameObject> arrowInstances = new List<GameObject>();

    public PieceModel Piece => piece;
    public bool IsOnBoard => isOnBoard;

    public void Initialize(PieceModel model, GameManager gm, SplitPlacementController controller, Vector3 worldPos)
    {
        piece = model;
        gameManager = gm;
        splitController = controller;
        transform.position = worldPos;
        originalPosition = worldPos;
        isDragging = false;
        isOnBoard = false;

        Color playerColor = piece.owner == PlayerSide.Player1 ? Color.blue : Color.red;

        bodySr = GetComponent<SpriteRenderer>();
        if (bodySr == null)
        {
            bodySr = gameObject.AddComponent<SpriteRenderer>();
            Sprite s = piece.owner == PlayerSide.Player1 ? SpriteCache.OnePhaseBlue : SpriteCache.OnePhaseRed;
            if (s != null)
            {
                bodySr.sprite = s;
                bodySr.color = Color.white;
            }
            else
            {
                bodySr.sprite = DebugSpriteGenerator.CreateSquareSprite(20, Color.white);
                bodySr.color = playerColor;
            }
            bodySr.sortingOrder = 10;
        }

        if (bodySr.sprite != null)
        {
            Sprite twoPhaseRef = piece.owner == PlayerSide.Player1 ? SpriteCache.TwoPhaseBlue : SpriteCache.TwoPhaseRed;
            if (twoPhaseRef != null)
            {
                float refWorldWidth = twoPhaseRef.rect.width / twoPhaseRef.pixelsPerUnit;
                float thisWorldWidth = bodySr.sprite.rect.width / bodySr.sprite.pixelsPerUnit;
                transform.localScale = Vector3.one * 0.24371f * (refWorldWidth / thisWorldWidth);
            }
            else
            {
                transform.localScale = Vector3.one * 0.24371f;
            }
        }

        var box = GetComponent<BoxCollider2D>();
        if (box == null)
        {
            box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(3.2f, 3.2f);
        }

        arrowsParent = new GameObject("FloatingArrows");
        arrowsParent.transform.SetParent(transform, false);
        arrowsParent.transform.localPosition = Vector3.zero;

        RefreshArrows();
        gameObject.name = $"SplitPiece_{piece.pieceId}";
    }

    void OnMouseDown()
    {
        isDragging = false;
        dragStartPosition = transform.position;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        dragOffset = transform.position - mouseWorld;
    }

    void OnMouseDrag()
    {
        isDragging = true;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;
        transform.position = mouseWorld + dragOffset;
    }

    void OnMouseUp()
    {
        float dist = Vector3.Distance(transform.position, dragStartPosition);
        if (dist > 0.01f)
        {
            if (isOnBoard)
                Unplace();
            TryPlace();
        }
        else
        {
            RotatePiece();
        }
    }

    private void RotatePiece()
    {
        piece.RotateCW();
        RefreshArrows();
    }

    private void TryPlace()
    {
        BoardCoord? coord = WorldToBoardCoord(transform.position);
        if (coord == null)
        {
            transform.position = originalPosition;
            return;
        }

        gameManager.PlaceSplitPieceOnBoard(piece, coord.Value);
        isOnBoard = true;

        transform.position = CellCenterWorld(coord.Value);

        splitController.NotifyPiecePlaced();
    }

    private void Unplace()
    {
        gameManager.UnplaceSplitPieceFromBoard(piece);
        piece.currentPosition = new BoardCoord(-1, -1);
        isOnBoard = false;

        splitController.NotifyPieceRemoved();
    }

    public void CreateBoardPieceView()
    {
        if (!isOnBoard) return;
        if (gameManager.BoardView == null) return;
        gameManager.BoardView.CreatePieceView(piece);
    }

    private BoardCoord? WorldToBoardCoord(Vector3 worldPos)
    {
        int w = gameManager.GameRules.boardWidth;
        int h = gameManager.GameRules.boardHeight;

        float originX = -(w - 1) * 0.5f;
        float originY = -(h - 1) * 0.5f;

        int x = Mathf.RoundToInt((worldPos.x - originX));
        int y = Mathf.RoundToInt((worldPos.y - originY));

        if (x < 0 || x >= w || y < 0 || y >= h) return null;

        var coord = new BoardCoord(x, y);
        var occupant = gameManager.BoardState.GetPieceAt(coord);
        if (occupant != null && occupant.pieceId != piece.pieceId)
            return null;

        return coord;
    }

    private Vector3 CellCenterWorld(BoardCoord coord)
    {
        float posX = coord.x - (gameManager.GameRules.boardWidth - 1) * 0.5f;
        float posY = coord.y - (gameManager.GameRules.boardHeight - 1) * 0.5f;
        return new Vector3(posX, posY, -0.2f);
    }

    private void RefreshArrows()
    {
        ClearArrows();

        var dirs = piece.GetCurrentFaceDirections();
        if (dirs == null) return;

        float distance = 1.1f;
        float arrowSize = 0.3f;

        foreach (var dir in dirs)
        {
            var arrow = new GameObject("Arrow_" + dir);
            arrow.transform.SetParent(arrowsParent.transform, false);

            var sr = arrow.AddComponent<SpriteRenderer>();
            if (SpriteCache.Arrow != null)
                sr.sprite = SpriteCache.Arrow;
            else
                sr.sprite = CreateArrowSprite();
            sr.color = Color.black;
            sr.sortingOrder = 12;

            arrow.transform.localPosition = DirectionToLocalPos(dir) * distance;
            arrow.transform.localRotation = DirectionToRotation(dir);
            arrow.transform.localScale = Vector3.one * arrowSize;

            arrowInstances.Add(arrow);
        }
    }

    private void ClearArrows()
    {
        foreach (var a in arrowInstances)
            if (a != null) Destroy(a);
        arrowInstances.Clear();
    }

    private Vector3 DirectionToLocalPos(Direction dir)
    {
        return dir switch
        {
            Direction.Up => Vector3.up,
            Direction.Down => Vector3.down,
            Direction.Left => Vector3.left,
            Direction.Right => Vector3.right,
            Direction.UpLeft => new Vector3(-1, 1, 0).normalized,
            Direction.UpRight => new Vector3(1, 1, 0).normalized,
            Direction.DownLeft => new Vector3(-1, -1, 0).normalized,
            Direction.DownRight => new Vector3(1, -1, 0).normalized,
            _ => Vector3.up
        };
    }

    private Quaternion DirectionToRotation(Direction dir)
    {
        float angle = dir switch
        {
            Direction.Up => 90f,
            Direction.Down => 270f,
            Direction.Left => 180f,
            Direction.Right => 0f,
            Direction.UpLeft => 135f,
            Direction.UpRight => 45f,
            Direction.DownLeft => 225f,
            Direction.DownRight => -45f,
            _ => 0f
        };
        return Quaternion.Euler(0, 0, angle);
    }

    private Sprite CreateArrowSprite()
    {
        int size = 16;
        var tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float cx = x - size / 2f;
                float cy = y - size / 2f;
                float r = size / 2f;

                bool inside = false;
                Vector2[] tri = {
                    new Vector2(0, r),
                    new Vector2(-r * 0.7f, -r * 0.7f),
                    new Vector2(r * 0.7f, -r * 0.7f)
                };

                if (PointInTriangle(new Vector2(cx, cy), tri[0], tri[1], tri[2]))
                    inside = true;

                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.7f), size);
    }

    private bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        var v0 = c - a;
        var v1 = b - a;
        var v2 = p - a;

        float dot00 = Vector2.Dot(v0, v0);
        float dot01 = Vector2.Dot(v0, v1);
        float dot02 = Vector2.Dot(v0, v2);
        float dot11 = Vector2.Dot(v1, v1);
        float dot12 = Vector2.Dot(v1, v2);

        float inv = 1f / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * inv;
        float v = (dot00 * dot12 - dot01 * dot02) * inv;

        return (u >= 0) && (v >= 0) && (u + v < 1);
    }
}
