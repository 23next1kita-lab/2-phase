using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class NetworkGameHandler : NetworkBehaviour
{
    private GameManager gm;

    [Networked] public NetworkString<_512> SerializedStatePart1 { get; set; }
    [Networked] public NetworkString<_512> SerializedStatePart2 { get; set; }
    [Networked] public NetworkString<_512> SerializedStatePart3 { get; set; }
    [Networked] public int NetworkedPhase { get; set; }
    [Networked] public int NetworkedWinner { get; set; }
    [Networked] public NetworkString<_512> SplitData { get; set; }

    public void Init(GameManager manager, bool host)
    {
        gm = manager;
    }

    public override void Spawned()
    {
        if (gm == null)
            gm = FindAnyObjectByType<GameManager>();
        if (gm != null)
            gm.IsHost = HasStateAuthority;
    }

    public void SetupOnlineSync()
    {
        if (gm == null) gm = FindAnyObjectByType<GameManager>();
        if (gm == null) return;

        var clickHandler = FindAnyObjectByType<ClickHandler>();
        if (clickHandler != null)
            clickHandler.networkHandler = this;

        if (HasStateAuthority)
        {
            gm.OnGameStateChanged += SyncState;
            SyncState();
        }
        else
        {
            ApplyState();
        }
    }

    public void SyncState()
    {
        if (!HasStateAuthority) return;
        NetworkedPhase = (int)gm.CurrentPhase;
        if (gm.CurrentPhase == GamePhase.GameOver)
            NetworkedWinner = gm.GameWinner.HasValue ? (int)gm.GameWinner.Value : -1;
        else
            NetworkedWinner = -1;

        var pieces = gm.BoardState.GetAllPieces();
        var data = new NetworkGameState
        {
            pieces = new NetworkPieceState[pieces.Count]
        };

        for (int i = 0; i < pieces.Count; i++)
        {
            var p = pieces[i];
            data.pieces[i] = new NetworkPieceState
            {
                id = p.pieceId,
                type = (int)p.pieceType,
                owner = (int)p.owner,
                x = p.currentPosition.x,
                y = p.currentPosition.y,
                isFrontActive = p.isFrontFaceActive,
                frontDirs = StringifyDirections(p.GetCurrentFaceDirections()),
                backDirs = StringifyDirections(p.GetOppositeFaceDirections())
            };
        }

        var json = JsonUtility.ToJson(data);
        int partSize = 512;
        SerializedStatePart1 = json.Length > 0 ? json.Substring(0, Mathf.Min(partSize, json.Length)) : "";
        SerializedStatePart2 = json.Length > partSize ? json.Substring(partSize, Mathf.Min(partSize, json.Length - partSize)) : "";
        SerializedStatePart3 = json.Length > partSize * 2 ? json.Substring(partSize * 2) : "";

        RPC_StateUpdated();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StateUpdated()
    {
        if (HasStateAuthority) return;
        ApplyState();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartClientSplitPlacement(string splitDataJson)
    {
        if (HasStateAuthority) return;
        if (gm == null) return;
        SplitData = splitDataJson;

        var data = JsonUtility.FromJson<NetworkSplitData>(splitDataJson);
        if (data?.pieces == null) return;

        var splitPieces = new List<PieceModel>();
        foreach (var sp in data.pieces)
        {
            var front = ParseDirections(sp.frontDirs);
            var back = ParseDirections(sp.backDirs);
            var piece = new PieceModel(sp.pieceId, (PlayerSide)sp.owner, (PieceType)sp.pieceType,
                new BoardCoord(-1, -1), front, back, true);
            splitPieces.Add(piece);
        }

        gm.StartRemoteSplitPlacement(splitPieces);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ClientReady()
    {
        if (!HasStateAuthority) return;
        var nm = FindAnyObjectByType<NetworkManager>();
        if (nm != null) nm.OnRemoteClientReady();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_FinishClientSplitPlacement(string placementDataJson)
    {
        if (!HasStateAuthority) return;
        if (gm == null) return;

        var data = JsonUtility.FromJson<NetworkPlacementResult>(placementDataJson);
        if (data?.placements == null) return;

        var placed = new List<PieceModel>();
        foreach (var p in data.placements)
        {
            var front = ParseDirections(p.frontDirs);
            var back = ParseDirections(p.backDirs);
            var piece = new PieceModel(p.pieceId, (PlayerSide)p.owner, (PieceType)p.pieceType,
                new BoardCoord(p.x, p.y), front, back, true);
            gm.PlaceSplitPieceOnBoard(piece, new BoardCoord(p.x, p.y));
            placed.Add(piece);
        }

        gm.FinalizeSplitPlacement(placed);
        SyncState();
    }

    private string BuildJson()
    {
        return SerializedStatePart1.ToString() + SerializedStatePart2.ToString() + SerializedStatePart3.ToString();
    }

    private void ApplyState()
    {
        if (gm == null) return;

        var json = BuildJson();
        var data = JsonUtility.FromJson<NetworkGameState>(json);
        if (data?.pieces == null) return;

        var bv = gm.BoardView ?? FindAnyObjectByType<BoardView>();
        if (bv == null) return;

        bv.ClearAllPieces();
        gm.BoardState.ClearAllPieces();

        foreach (var ps in data.pieces)
        {
            var type = (PieceType)ps.type;
            var owner = (PlayerSide)ps.owner;
            var front = ParseDirections(ps.frontDirs);
            var back = ParseDirections(ps.backDirs);
            var piece = new PieceModel(ps.id, owner, type, new BoardCoord(ps.x, ps.y), front, back, ps.isFrontActive);
            gm.BoardState.AddPiece(piece);
            bv.CreatePieceView(piece);
        }

        gm.SyncPhaseFromNetwork((GamePhase)NetworkedPhase);

        if ((GamePhase)NetworkedPhase == GamePhase.GameOver)
        {
            PlayerSide? winner = NetworkedWinner >= 0 ? (PlayerSide)NetworkedWinner : null;
            var ui = FindAnyObjectByType<UIManager>();
            if (ui != null) ui.ShowGameOver(winner);
        }
    }

    public string SerializeSplitPieces(List<PieceModel> pieces)
    {
        var data = new NetworkSplitData();
        data.pieces = new NetworkSplitPieceState[pieces.Count];
        for (int i = 0; i < pieces.Count; i++)
        {
            var p = pieces[i];
            data.pieces[i] = new NetworkSplitPieceState
            {
                pieceId = p.pieceId,
                owner = (int)p.owner,
                pieceType = (int)p.pieceType,
                frontDirs = StringifyDirections(p.GetCurrentFaceDirections()),
                backDirs = StringifyDirections(p.GetOppositeFaceDirections())
            };
        }
        return JsonUtility.ToJson(data);
    }

    public string SerializePlacementResult(List<PieceModel> placed)
    {
        var data = new NetworkPlacementResult();
        data.placements = new NetworkPlacedPieceState[placed.Count];
        for (int i = 0; i < placed.Count; i++)
        {
            var p = placed[i];
            data.placements[i] = new NetworkPlacedPieceState
            {
                pieceId = p.pieceId,
                owner = (int)p.owner,
                pieceType = (int)p.pieceType,
                x = p.currentPosition.x,
                y = p.currentPosition.y,
                frontDirs = StringifyDirections(p.GetCurrentFaceDirections()),
                backDirs = StringifyDirections(p.GetOppositeFaceDirections())
            };
        }
        return JsonUtility.ToJson(data);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_MakeMove(string moveType, int pieceId, int targetX, int targetY, string extraData)
    {
        if (!HasStateAuthority) return;
        if (gm == null) gm = FindAnyObjectByType<GameManager>();

        switch (moveType)
        {
            case "select":
            {
                var piece = gm.BoardState.GetPieceById(pieceId);
                if (piece != null) gm.OnPieceClicked(piece);
                break;
            }
            case "cell":
            {
                var target = new BoardCoord(targetX, targetY);
                gm.OnCellClicked(target);
                break;
            }
        }
        SyncState();
    }

    public void Cleanup()
    {
        if (gm != null)
            gm.OnGameStateChanged -= SyncState;
        gm = null;
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private string StringifyDirections(List<Direction> dirs)
    {
        if (dirs == null || dirs.Count == 0) return "";
        var parts = new string[dirs.Count];
        for (int i = 0; i < dirs.Count; i++)
            parts[i] = ((int)dirs[i]).ToString();
        return string.Join(",", parts);
    }

    private List<Direction> ParseDirections(string s)
    {
        var list = new List<Direction>();
        if (string.IsNullOrEmpty(s)) return list;
        foreach (var part in s.Split(','))
        {
            if (int.TryParse(part, out int val))
                list.Add((Direction)val);
        }
        return list;
    }
}

[System.Serializable]
public class NetworkGameState
{
    public NetworkPieceState[] pieces;
}

[System.Serializable]
public class NetworkPieceState
{
    public int id;
    public int type;
    public int owner;
    public int x, y;
    public bool isFrontActive;
    public string frontDirs;
    public string backDirs;
}

[System.Serializable]
public class NetworkSplitData
{
    public NetworkSplitPieceState[] pieces;
}

[System.Serializable]
public class NetworkSplitPieceState
{
    public int pieceId;
    public int owner;
    public int pieceType;
    public string frontDirs;
    public string backDirs;
}

[System.Serializable]
public class NetworkPlacementResult
{
    public NetworkPlacedPieceState[] placements;
}

[System.Serializable]
public class NetworkPlacedPieceState
{
    public int pieceId;
    public int owner;
    public int pieceType;
    public int x, y;
    public string frontDirs;
    public string backDirs;
}
