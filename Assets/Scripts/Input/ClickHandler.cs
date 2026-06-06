using UnityEngine;

public class ClickHandler : MonoBehaviour
{
    public NetworkGameHandler networkHandler;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Camera.main == null) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider == null) return;

        var pieceView = hit.collider.GetComponent<PieceView>();
        if (pieceView != null && pieceView.PieceModel != null)
        {
            var gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                if (gm.CurrentPlayMode == GameManager.PlayMode.Online && !gm.IsHost && networkHandler != null)
                {
                    networkHandler.RPC_MakeMove("select", pieceView.PieceModel.pieceId, 0, 0, default);
                }
                else
                {
                    gm.OnPieceClicked(pieceView.PieceModel);
                }
                return;
            }
        }

        var cellView = hit.collider.GetComponent<CellView>();
        if (cellView != null)
        {
            var gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                if (gm.CurrentPlayMode == GameManager.PlayMode.Online && !gm.IsHost && networkHandler != null)
                {
                    networkHandler.RPC_MakeMove("cell", 0, cellView.Coord.x, cellView.Coord.y, default);
                }
                else
                {
                    gm.OnCellClicked(cellView.Coord);
                }
            }
        }
    }
}
