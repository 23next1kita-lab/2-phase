using UnityEngine;

public class CellView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private BoardCoord coord;
    private GameManager gameManager;
    private bool isHighlighted;

    public BoardCoord Coord => coord;

    public void Initialize(BoardCoord coord, GameManager gm)
    {
        this.coord = coord;
        this.gameManager = gm;
    }

    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = color;
    }

    public void SetHighlight(bool highlighted)
    {
        isHighlighted = highlighted;
    }

}
