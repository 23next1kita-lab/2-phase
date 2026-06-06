using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color player1Color = Color.blue;
    [SerializeField] private Color player2Color = Color.red;

    private PieceModel pieceModel;
    private GameManager gameManager;
    private GameObject arrowsParent;
    private List<GameObject> arrowInstances = new List<GameObject>();
    private bool showOppositeFace;
    private Coroutine longPressCoroutine;

    public PieceModel PieceModel => pieceModel;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(PieceModel model, GameManager gm)
    {
        pieceModel = model;
        gameManager = gm;

        arrowsParent = new GameObject("Arrows");
        arrowsParent.transform.SetParent(transform, false);
        arrowsParent.transform.localPosition = Vector3.zero;

        UpdateVisual();
    }

    void OnMouseDown()
    {
        if (!CanShowOppositeFace()) return;
        if (longPressCoroutine != null) StopCoroutine(longPressCoroutine);
        longPressCoroutine = StartCoroutine(LongPressRoutine());
    }

    void OnMouseUp()
    {
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
        if (showOppositeFace)
        {
            showOppositeFace = false;
            RefreshArrows();
        }
    }

    void OnMouseExit()
    {
        if (showOppositeFace)
        {
            showOppositeFace = false;
            RefreshArrows();
        }
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }
    }

    private bool CanShowOppositeFace()
    {
        if (gameManager == null) return false;
        return pieceModel != null && pieceModel.owner == gameManager.TurnManager.CurrentPlayer;
    }

    private IEnumerator LongPressRoutine()
    {
        yield return new WaitForSeconds(0.4f);
        showOppositeFace = true;
        RefreshArrows();
    }

    public void UpdateVisual()
    {
        if (pieceModel == null) return;

        if (spriteRenderer != null)
        {
            if (!pieceModel.canActThisTurn || pieceModel.spawnedThisTurn)
                spriteRenderer.color = Color.Lerp(Color.white, Color.gray, 0.5f);
            else
                spriteRenderer.color = Color.white;
        }

        RefreshArrows();
        gameObject.name = $"{pieceModel.owner}_Piece_{pieceModel.pieceId}";
    }

    private void RefreshArrows()
    {
        ClearArrows();

        if (pieceModel == null) return;

        var dirs = showOppositeFace
            ? pieceModel.GetOppositeFaceDirections()
            : pieceModel.GetCurrentFaceDirections();

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
            sr.sortingOrder = 3;

            arrow.transform.localPosition = DirectionToLocalPos(dir) * distance;
            arrow.transform.localRotation = DirectionToRotation(dir);
            arrow.transform.localScale = Vector3.one * arrowSize;

            arrowInstances.Add(arrow);
        }
    }

    private void ClearArrows()
    {
        foreach (var arrow in arrowInstances)
        {
            if (arrow != null)
                Destroy(arrow);
        }
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
