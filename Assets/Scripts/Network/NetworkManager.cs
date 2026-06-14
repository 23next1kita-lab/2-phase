using UnityEngine;
using UnityEngine.UI;
using Fusion;
using Fusion.Sockets;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner runner;
    private GameObject lobbyPanel;
    private InputField roomNameInput;
    private bool returningToHome;
    private bool gameStarting;
    private GameObject waitingPanel;
    private Text waitingMessage;
    private GameObject backButton;

    public System.Action OnConnected;

    public async void ShowLobby()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("LobbyCanvas", typeof(RectTransform));
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        lobbyPanel = new GameObject("LobbyPanel", typeof(RectTransform));
        lobbyPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRt = lobbyPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;

        Image bg = lobbyPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);

        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(lobbyPanel.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.sizeDelta = new Vector2(300, 40);
        labelRt.anchoredPosition = new Vector2(0, 100);
        Text label = labelObj.AddComponent<Text>();
        label.text = "ルーム名";
        label.fontSize = 24;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.font = FontProvider.GetFont();

        GameObject inputObj = new GameObject("RoomInput", typeof(RectTransform));
        inputObj.transform.SetParent(lobbyPanel.transform, false);
        RectTransform inputRt = inputObj.GetComponent<RectTransform>();
        inputRt.sizeDelta = new Vector2(280, 40);
        inputRt.anchoredPosition = new Vector2(0, 40);
        roomNameInput = inputObj.AddComponent<InputField>();
        var inputImg = inputObj.AddComponent<Image>();
        inputImg.color = Color.white;

        GameObject placeholder = new GameObject("Placeholder", typeof(RectTransform));
        placeholder.transform.SetParent(inputObj.transform, false);
        Text phText = placeholder.AddComponent<Text>();
        phText.text = "ルーム名を入力";
        phText.fontSize = 20;
        phText.color = Color.gray;
        phText.font = FontProvider.GetFont();
        roomNameInput.placeholder = phText;

        GameObject textArea = new GameObject("Text", typeof(RectTransform));
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform textAreaRt = textArea.GetComponent<RectTransform>();
        textAreaRt.anchorMin = Vector2.zero;
        textAreaRt.anchorMax = Vector2.one;
        textAreaRt.offsetMin = new Vector2(5, 5);
        textAreaRt.offsetMax = new Vector2(-5, -5);
        Text inputText = textArea.AddComponent<Text>();
        inputText.fontSize = 20;
        inputText.color = Color.black;
        inputText.font = FontProvider.GetFont();
        roomNameInput.textComponent = inputText;
        roomNameInput.text = "Room1";

        CreateLobbyButton("ホストとして開始", new Vector2(0, -40), () => { var _ = HostGame(roomNameInput.text); });
        CreateLobbyButton("参加", new Vector2(0, -120), () => { var _ = JoinGame(roomNameInput.text); });
        CreateLobbyButton("戻る", new Vector2(0, -200), () =>
        {
            Destroy(lobbyPanel);
            var menuObj = new GameObject("MainMenuUI");
            menuObj.AddComponent<MainMenuUI>();
        });

        await Task.Yield();
    }

    private void CreateLobbyButton(string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(label, typeof(RectTransform));
        btnObj.transform.SetParent(lobbyPanel.transform, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280, 60);
        rt.anchoredPosition = pos;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.8f, 0.9f);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.sizeDelta = new Vector2(280, 60);
        txtRt.anchoredPosition = Vector2.zero;

        Text txt = txtObj.AddComponent<Text>();
        txt.text = label;
        txt.fontSize = 24;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = FontProvider.GetFont();

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
    }

    private async Task HostGame(string roomName)
    {
        runner = GetComponent<NetworkRunner>();
        if (runner == null)
            runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        var sceneManager = FindAnyObjectByType<NetworkSceneManagerDefault>();
        if (sceneManager == null)
            sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = roomName,
            Scene = SceneRef.None,
            SceneManager = sceneManager,
            PlayerCount = 2
        });

        if (result.Ok)
        {
            Destroy(lobbyPanel);
            ShowWaitingPanel();
        }
    }

    private async Task JoinGame(string roomName)
    {
        runner = GetComponent<NetworkRunner>();
        if (runner == null)
            runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        var sceneManager = FindAnyObjectByType<NetworkSceneManagerDefault>();
        if (sceneManager == null)
            sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = roomName,
            Scene = SceneRef.None,
            SceneManager = sceneManager,
            PlayerCount = 2
        });

        if (result.Ok)
        {
            Destroy(lobbyPanel);
            ShowJoinedPanel();
            if (!gameStarting)
            {
                gameStarting = true;
                Invoke(nameof(DelayedGameStart), 3f);
            }
        }
    }

    private void ShowWaitingPanel()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("WaitingCanvas", typeof(RectTransform));
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        waitingPanel = new GameObject("WaitingPanel", typeof(RectTransform));
        waitingPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRt = waitingPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;

        Image bg = waitingPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);

        GameObject msgObj = new GameObject("Message", typeof(RectTransform));
        msgObj.transform.SetParent(waitingPanel.transform, false);
        RectTransform msgRt = msgObj.GetComponent<RectTransform>();
        msgRt.anchoredPosition = Vector2.zero;
        msgRt.sizeDelta = new Vector2(400, 80);
        waitingMessage = msgObj.AddComponent<Text>();
        waitingMessage.text = "参加者待ち...";
        waitingMessage.fontSize = 36;
        waitingMessage.color = Color.white;
        waitingMessage.alignment = TextAnchor.MiddleCenter;
        waitingMessage.font = FontProvider.GetFont();
        waitingMessage.fontStyle = FontStyle.Bold;

        GameObject btnObj = new GameObject("BackButton", typeof(RectTransform));
        btnObj.transform.SetParent(waitingPanel.transform, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchoredPosition = new Vector2(0, -120);
        btnRt.sizeDelta = new Vector2(280, 60);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.6f, 0.2f, 0.2f, 0.9f);
        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTxtRt = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRt.sizeDelta = new Vector2(280, 60);
        btnTxtRt.anchoredPosition = Vector2.zero;
        Text btnTxt = btnTxtObj.AddComponent<Text>();
        btnTxt.text = "ホームに戻る";
        btnTxt.fontSize = 24;
        btnTxt.color = Color.white;
        btnTxt.alignment = TextAnchor.MiddleCenter;
        btnTxt.font = FontProvider.GetFont();
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(CancelWaiting);
        backButton = btnObj;
    }

    private void ShowJoinedPanel()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("WaitingCanvas", typeof(RectTransform));
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        waitingPanel = new GameObject("JoinedPanel", typeof(RectTransform));
        waitingPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRt = waitingPanel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;

        Image bg = waitingPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);

        GameObject msgObj = new GameObject("Message", typeof(RectTransform));
        msgObj.transform.SetParent(waitingPanel.transform, false);
        RectTransform msgRt = msgObj.GetComponent<RectTransform>();
        msgRt.anchoredPosition = Vector2.zero;
        msgRt.sizeDelta = new Vector2(400, 80);
        waitingMessage = msgObj.AddComponent<Text>();
        waitingMessage.text = "参加しました！";
        waitingMessage.fontSize = 36;
        waitingMessage.color = Color.white;
        waitingMessage.alignment = TextAnchor.MiddleCenter;
        waitingMessage.font = FontProvider.GetFont();
        waitingMessage.fontStyle = FontStyle.Bold;
    }

    private void DelayedGameStart()
    {
        if (returningToHome) return;
        gameStarting = false;

        if (waitingPanel != null)
            DestroyImmediate(waitingPanel);

        var gm = FindAnyObjectByType<GameManager>();
        if (gm == null) return;

        gm.InitializeGame();

        var handler = FindAnyObjectByType<NetworkGameHandler>();
        if (handler != null)
        {
            handler.SetupOnlineSync();
            if (!handler.HasStateAuthority)
                handler.RPC_ClientReady();
        }

        OnConnected?.Invoke();
    }

    private void CancelWaiting()
    {
        if (returningToHome) return;
        returningToHome = true;

        CancelInvoke(nameof(DelayedGameStart));

        if (waitingPanel != null)
            Destroy(waitingPanel);

        var handler = FindAnyObjectByType<NetworkGameHandler>();
        if (handler != null) handler.Cleanup();

        Destroy(this);

        var menuObj = new GameObject("MainMenuUI");
        menuObj.AddComponent<MainMenuUI>();
    }

    public void OnRemoteClientReady()
    {
        if (!gameStarting && waitingMessage != null)
        {
            gameStarting = true;
            waitingMessage.text = "参加しました！";
            if (backButton != null)
                backButton.SetActive(false);
            Invoke(nameof(DelayedGameStart), 3f);
        }
    }

    private void HandleDisconnect()
    {
        if (returningToHome) return;
        returningToHome = true;

        CancelInvoke(nameof(DelayedGameStart));

        if (waitingPanel != null)
            DestroyImmediate(waitingPanel);

        var ui = FindAnyObjectByType<UIManager>();
        if (ui != null)
        {
            ui.OnReturnToHome();
            return;
        }

        var handler = FindAnyObjectByType<NetworkGameHandler>();
        if (handler != null) handler.Cleanup();

        Destroy(this);

        var menuObj = new GameObject("MainMenuUI");
        menuObj.AddComponent<MainMenuUI>();
    }

    private void Update()
    {
        if (returningToHome || gameStarting || waitingPanel == null || runner == null || !runner.IsRunning)
            return;

        int count = 0;
        foreach (var p in runner.ActivePlayers)
            count++;

        if (count >= 2)
        {
            gameStarting = true;
            if (waitingMessage != null)
                waitingMessage.text = "参加しました！";
            if (backButton != null)
                backButton.SetActive(false);
            Invoke(nameof(DelayedGameStart), 3f);
        }
    }

    // INetworkRunnerCallbacks
    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (player != runner.LocalPlayer && !gameStarting)
        {
            gameStarting = true;
            if (waitingMessage != null)
                waitingMessage.text = "参加しました！";
            if (backButton != null)
                backButton.SetActive(false);
            Invoke(nameof(DelayedGameStart), 3f);
        }
    }
    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (player != runner.LocalPlayer && !returningToHome)
            HandleDisconnect();
    }
    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        if (!returningToHome)
            HandleDisconnect();
    }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    private void OnDestroy()
    {
        if (runner != null)
        {
            runner.RemoveCallbacks(this);
            if (runner.IsRunning)
                runner.Shutdown();
        }
    }
}
