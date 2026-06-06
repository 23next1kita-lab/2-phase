using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InitialPieceLayout", menuName = "2-Phase/Initial Piece Layout")]
public class InitialPieceLayoutSO : ScriptableObject
{
    public List<InitialPieceData> pieces = new List<InitialPieceData>();
}
