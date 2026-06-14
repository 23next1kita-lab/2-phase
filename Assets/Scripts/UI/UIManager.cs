using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private Text turnText;
    private Text movesRemainingText;
    private Text phaseText;
    private Text timerText;
    private Text messageText;
    private GameObject gameOverPanel;
    private Text gameOverText;
    private GameObject confirmButton;
    private GameObject homeButton;
    private GameObject confirmHomePanel;

    private GameManager gameManager;
    private Canvas cachedCanvas;

    public void Initialize(GameManager gm)
    {
        gameManager = gm;
        EnsureUIElements();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        ClearMessage();
        UpdateUI();
    }

    private void EnsureUIElements()
    {
        EnsureEventSystem();

        cachedCanvas = FindAnyObjectByType<Canvas>();
        if (cachedCanvas == null)
        {
            GameObject canvasObj = new GameObject("GameCanvas", typeof(RectTransform));
            cachedCanvas = canvasObj.AddComponent<Canvas>();
            cachedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        bool mobile = Application.isMobilePlatform;
        if (turnText == null)
        {
            turnText = CreateText("TurnText", cachedCanvas.gameObject, mobile ? new Vector2(-550, 110) : new Vector2(-300, 170), "Turn: 1 - Player1", mobile ? 30 : 20);
            if (mobile)
            {
                turnText.fontStyle = FontStyle.Bold;
                var rt = turnText.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(-550, 110);
                rt.sizeDelta = new Vector2(400, 35);
            }
        }
        if (movesRemainingText == null)
        {
            movesRemainingText = CreateText("MovesText", cachedCanvas.gameObject, mobile ? new Vector2(-550, 80) : new Vector2(-300, 140), "Moves left: 0", mobile ? 27 : 18);
            if (mobile)
            {
                movesRemainingText.fontStyle = FontStyle.Bold;
                var rt = movesRemainingText.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(-550, 80);
                rt.sizeDelta = new Vector2(400, 35);
            }
        }
        if (phaseText == null)
        {
            phaseText = CreateText("PhaseText", cachedCanvas.gameObject, mobile ? new Vector2(-550, 50) : new Vector2(-300, 110), "Phase: Select a piece", mobile ? 24 : 16);
            if (mobile)
            {
                phaseText.fontStyle = FontStyle.Bold;
                var rt = phaseText.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(-550, 50);
                rt.sizeDelta = new Vector2(400, 35);
            }
        }
        if (timerText == null)
        {
            timerText = CreateText("TimerText", cachedCanvas.gameObject, mobile ? new Vector2(0, 130) : new Vector2(0, 170), "", mobile ? 27 : 18);
            if (mobile)
            {
                timerText.fontStyle = FontStyle.Bold;
                var rt = timerText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(550, 80);
                rt.sizeDelta = new Vector2(400, 30);
            }
            timerText.alignment = TextAnchor.MiddleCenter;
        }
        if (messageText == null)
        {
            messageText = CreateText("MessageText", cachedCanvas.gameObject, new Vector2(0, 250), "", 18);
            messageText.alignment = TextAnchor.MiddleCenter;
            var msgRt = messageText.GetComponent<RectTransform>();
            msgRt.sizeDelta = new Vector2(400, 60);
            messageText.gameObject.SetActive(false);
        }
        if (gameOverPanel == null)
        {
            gameOverPanel = CreateGameOverPanel(cachedCanvas.gameObject);
            gameOverPanel.SetActive(false);
        }

        if (homeButton == null)
        {
            homeButton = CreateGameButton("HomeButton", "Home", mobile ? new Vector2(450, 250) : new Vector2(350, 200), () => ShowHomeConfirm());
        }

        if (confirmHomePanel == null)
            confirmHomePanel = CreateHomeConfirmPanel(cachedCanvas.gameObject);
    }

    private GameObject CreateGameButton(string name, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(cachedCanvas.transform, false);
        RectTransform btnRt = btnObj.AddComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(100, 40);
        btnRt.anchoredPosition = pos;
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.7f, 0.2f, 0.2f, 0.85f);

        GameObject btnTxt = new GameObject("Text");
        btnTxt.transform.SetParent(btnObj.transform, false);
        RectTransform btnTxtRt = btnTxt.AddComponent<RectTransform>();
        btnTxtRt.sizeDelta = new Vector2(100, 40);
        btnTxtRt.anchoredPosition = Vector2.zero;
        Text btnText = btnTxt.AddComponent<Text>();
        btnText.text = label;
        btnText.fontSize = 18;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.font = FontProvider.GetFont();

        Button btnComp = btnObj.AddComponent<Button>();
        btnComp.targetGraphic = btnImg;
        btnComp.onClick.AddListener(onClick);
        return btnObj;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    private Text CreateText(string name, GameObject parent, Vector2 pos, string content, int fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);
        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 30);
        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = Color.black;
        text.font = FontProvider.GetFont();
        return text;
    }

    private GameObject CreateGameOverPanel(GameObject canvasObj)
    {
        GameObject panel = new GameObject("GameOverPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 250);
        rt.anchoredPosition = Vector2.zero;
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.8f);

        GameObject textObj = new GameObject("GameOverText");
        textObj.transform.SetParent(panel.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.sizeDelta = new Vector2(400, 200);
        textRt.anchoredPosition = new Vector2(0, 30);
        gameOverText = textObj.AddComponent<Text>();
        gameOverText.text = "Game Over!\nPlayer1 wins!";
        gameOverText.fontSize = 36;
        gameOverText.color = Color.white;
        gameOverText.alignment = TextAnchor.MiddleCenter;
        gameOverText.font = FontProvider.GetFont();

        CreateGameOverButton(panel, "ContinueButton", "Continue", new Vector2(-100, -75), () => OnNewGame());
        CreateGameOverButton(panel, "HomeButton", "Return to Home", new Vector2(100, -75), () => OnReturnToHome());

        return panel;
    }

    private void CreateGameOverButton(GameObject parent, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent.transform, false);
        RectTransform btnRt = btnObj.AddComponent<RectTransform>();
        btnRt.sizeDelta = new Vector2(160, 50);
        btnRt.anchoredPosition = pos;
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.4f, 0.8f, 0.9f);

        GameObject btnTxt = new GameObject("Text");
        btnTxt.transform.SetParent(btnObj.transform, false);
        RectTransform btnTxtRt = btnTxt.AddComponent<RectTransform>();
        btnTxtRt.sizeDelta = new Vector2(160, 50);
        btnTxtRt.anchoredPosition = Vector2.zero;
        Text btnText = btnTxt.AddComponent<Text>();
        btnText.text = label;
        btnText.fontSize = 20;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.font = FontProvider.GetFont();

        Button newGameBtnComp = btnObj.AddComponent<Button>();
        newGameBtnComp.targetGraphic = btnImg;
        newGameBtnComp.onClick.AddListener(onClick);
    }

    public void UpdateUI()
    {
        if (gameManager == null || gameManager.GameRules == null) return;

        if (turnText != null)
            turnText.text = $"Turn: {gameManager.TurnManager.TurnNumber} - {gameManager.TurnManager.CurrentPlayer}";

        if (movesRemainingText != null)
            movesRemainingText.text = $"Moves left: {gameManager.TurnManager.MovesRemaining}";

        if (phaseText != null)
        {
            string phaseStr = gameManager.CurrentPhase switch
            {
                GamePhase.WaitingForPieceSelect => "Select a piece",
                GamePhase.WaitingForDestinationSelect => "Choose destination",
                GamePhase.PlacingSplitPieces => "Placing split pieces",
                GamePhase.GameOver => "Game Over",
                _ => "Unknown"
            };
            phaseText.text = $"Phase: {phaseStr}";
        }

        if (timerText != null)
        {
            if (gameManager.TimeControlEnabled)
            {
                float p1 = gameManager.P1TimeRemaining;
                float p2 = gameManager.P2TimeRemaining;
                timerText.text = $"P1: {FormatTime(p1)}  P2: {FormatTime(p2)}";
                timerText.gameObject.SetActive(true);
            }
            else
            {
                timerText.gameObject.SetActive(false);
            }
        }
    }

    public void ShowSplitPlacementMessage(int count)
    {
        if (messageText != null)
        {
            messageText.text = "駒を載せてください\n（クリックで回転）";
            if (Application.isMobilePlatform)
            {
                messageText.fontSize = 26;
                messageText.fontStyle = FontStyle.Bold;
                messageText.color = Color.white;
                var rt = messageText.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, 100);
                rt.sizeDelta = new Vector2(500, 80);
            }
            messageText.gameObject.SetActive(true);
        }
    }

    public void ShowMessage(string msg)
    {
        if (messageText != null)
        {
            messageText.text = msg;
            messageText.gameObject.SetActive(true);
        }
    }

    public void OnConfirmSplit()
    {
        if (gameManager == null) return;
        if (gameManager.CurrentPlayMode == GameManager.PlayMode.Online &&
            gameManager.TurnManager.CurrentPlayer != gameManager.LocalPlayerSide)
            return;
        gameManager.ConfirmSplitPlacement();
    }

    public void ShowConfirmButton(bool show)
    {
        EnsureConfirmButton();
        if (confirmButton != null)
        {
            confirmButton.SetActive(show);
        }
    }

    private void EnsureConfirmButton()
    {
        if (confirmButton != null) return;

        EnsureUIElements();

        GameObject btnObj = new GameObject("ConfirmButton", typeof(RectTransform));
        btnObj.transform.SetParent(cachedCanvas.transform, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 50);
        rt.anchoredPosition = new Vector2(-400, 0);

        Image img = btnObj.AddComponent<Image>();
        img.color = Color.white;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.sizeDelta = new Vector2(160, 50);
        txtRt.anchoredPosition = Vector2.zero;
        Text txt = txtObj.AddComponent<Text>();
        txt.text = "確定";
        txt.fontSize = 20;
        txt.color = Color.black;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = FontProvider.GetFont();

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => OnConfirmSplit());

        btnObj.SetActive(false);
        confirmButton = btnObj;
    }

    public void ShowHomeConfirm()
    {
        EnsureUIElements();
        if (confirmHomePanel != null)
            confirmHomePanel.SetActive(true);
    }

    public void HideHomeConfirm()
    {
        if (confirmHomePanel != null)
            confirmHomePanel.SetActive(false);
    }

    private GameObject CreateHomeConfirmPanel(GameObject canvasObj)
    {
        GameObject panel = new GameObject("HomeConfirmPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 200);
        rt.anchoredPosition = Vector2.zero;
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.85f);

        GameObject textObj = new GameObject("ConfirmText");
        textObj.transform.SetParent(panel.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.sizeDelta = new Vector2(380, 80);
        textRt.anchoredPosition = new Vector2(0, 40);
        Text confirmText = textObj.AddComponent<Text>();
        confirmText.text = "本当にタイトルに戻りますか？";
        confirmText.fontSize = 26;
        confirmText.color = Color.white;
        confirmText.alignment = TextAnchor.MiddleCenter;
        confirmText.font = FontProvider.GetFont();

        CreateGameOverButton(panel, "YesButton", "はい", new Vector2(-100, -50), () => { HideHomeConfirm(); OnReturnToHome(); });
        CreateGameOverButton(panel, "NoButton", "いいえ", new Vector2(100, -50), () => HideHomeConfirm());

        panel.SetActive(false);
        return panel;
    }

    public void ShowGameOver(PlayerSide? winner)
    {
        EnsureUIElements();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        if (gameOverText != null)
        {
            if (winner.HasValue)
                gameOverText.text = $"Game Over!\n{winner.Value} wins!";
            else
                gameOverText.text = "Game Over!\n引き分け（千日手）";
        }

        UpdateUI();
    }

    public void OnNewGame()
    {
        if (gameManager != null)
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            gameManager.InitializeGame();
        }
    }

    public void OnReturnToHome()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        var boardView = FindAnyObjectByType<BoardView>();
        if (boardView != null) Destroy(boardView.gameObject);

        var clickHandler = FindAnyObjectByType<ClickHandler>();
        if (clickHandler != null) Destroy(clickHandler);

        if (gameManager != null)
        {
            foreach (var cpu in gameManager.GetComponents<CpuPlayer>())
            {
                cpu.Detach();
                Destroy(cpu);
            }

            var nm = gameManager.GetComponent<NetworkManager>();
            if (nm != null) Destroy(nm);

            var handler = gameManager.GetComponent<NetworkGameHandler>();
            if (handler != null) handler.Cleanup();
        }

        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            foreach (Transform child in canvas.transform)
                Destroy(child.gameObject);
        }

        Destroy(gameObject);

        var menuObj = new GameObject("MainMenuUI");
        menuObj.AddComponent<MainMenuUI>();
    }

    public void ClearMessage()
    {
        if (messageText != null)
        {
            messageText.text = "";
            messageText.gameObject.SetActive(false);
        }
    }

    private string FormatTime(float seconds)
    {
        if (seconds <= 0) return "0:00";
        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        return $"{m}:{s:D2}";
    }
}
