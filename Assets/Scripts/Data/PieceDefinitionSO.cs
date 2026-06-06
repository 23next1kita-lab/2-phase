using UnityEngine;

[CreateAssetMenu(menuName = "2-Phase/Piece Definition", fileName = "PieceDefinition")]
public class PieceDefinitionSO : ScriptableObject
{
    public PieceType pieceType;
    public string pieceName;
    public Color uiColor = Color.white;
}
