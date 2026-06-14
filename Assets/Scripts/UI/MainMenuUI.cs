using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuUI : MonoBehaviour
{
    private GameObject panelObj;
    private GameManager gameManager;
    private GameObject rulesPanel;
    private int rulesPage;
    private GameObject cpuLevelPanel;
    private bool cpuVsCpuMode;
    private GameObject randomModePanel;
    private GameManager.PlayMode pendingPlayMode;
    private int pendingCpuLevel;
    private bool pendingCpuVsCpu;
    private GameObject timePanel;
    private int pendingTimeSeconds;

    private void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        CreateMenu();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM("menu");
    }

    private void PlayClickSE()
    {
        AudioManager.Instance?.PlaySE("button_click");
    }

    private void CreateMenu()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MenuCanvas", typeof(RectTransform));
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.AddComponent<StandaloneInputModule>();
        }

        panelObj = new GameObject("MainMenuPanel", typeof(RectTransform));
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;

        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);

        CreateButton("オフライン対戦", new Vector2(0, 180), () => ShowRandomModeSelect(GameManager.PlayMode.Offline));
        CreateButton("オンライン対戦", new Vector2(0, 80), () => ShowRandomModeSelect(GameManager.PlayMode.Online));
        CreateButton("CPU戦", new Vector2(0, -20), () => ShowCpuLevelSelect(false));
        CreateButton("CPU vs CPU", new Vector2(0, -120), () => ShowCpuLevelSelect(true));
        CreateButton("ルール説明", new Vector2(0, -220), () => ShowRules());

        rulesPanel = CreateRulesPanel(canvas.gameObject);
        rulesPanel.SetActive(false);

        cpuLevelPanel = CreateCpuLevelPanel(canvas.gameObject);
        cpuLevelPanel.SetActive(false);

        randomModePanel = CreateRandomModePanel(canvas.gameObject);
        randomModePanel.SetActive(false);

        timePanel = CreateTimePanel(canvas.gameObject);
        timePanel.SetActive(false);
    }

    private void CreateButton(string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(label, typeof(RectTransform));
        btnObj.transform.SetParent(panelObj.transform, false);

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
        btn.onClick.AddListener(() => PlayClickSE());
        btn.onClick.AddListener(onClick);
    }

    private string[] rulesPages = new string[]
    {
        "2-Phase ルール説明（1/3）\n\n" +
        "【概要】\n" +
        "2-Phaseは2人用の戦略ボードゲームです。\n" +
        "各プレイヤーは7個の駒を持ち、\n" +
        "駒に書かれた矢印の方向に移動できます。\n\n" +
        "【勝利条件】\n" +
        "・相手の1-Phase駒を捕獲すると勝利\n" +
        "・1-Phase駒を持っていないプレイヤーの負け",

        "2-Phase ルール説明（2/3）\n\n" +
        "【ターンと移動】\n" +
        "・先手（左側・Player1）から開始\n" +
        "・1ターンに2回まで移動可能\n" +
        "　（先手の1ターン目のみ1回）\n" +
        "・駒は矢印の方向にのみ移動\n" +
        "・自分の駒があるマスには移動不可\n" +
        "・捕獲したターンは即終了\n" +
        "　（そのターンの残り手番は消失）",

        "2-Phase ルール説明（3/3）\n\n" +
        "【捕獲と分裂】\n" +
        "・相手の駒へ移動すると捕獲\n" +
        "・捕獲した側が分裂駒を配置\n" +
        "・捕獲された2-Phase駒は\n" +
        "　2つの1-Phase駒に分裂\n" +
        "・分裂駒は空いているマスに配置\n\n" +
        "【駒の種類】\n" +
        "・2-Phase駒：表裏で異なる矢印\n" +
        "　移動後に反転し方向が変化\n" +
        "・1-Phase駒：分裂で生まれる\n" +
        "　捕獲されるとゲームオーバー",

        "2-Phase ルール説明（4/4）\n\n" +
        "【操作方法】\n" +
        "・駒の選択：駒をタップ/クリック\n" +
        "・移動：移動先マスをタップ/クリック\n" +
        "・裏面確認：駒を長押し\n" +
        "　（スマホ：自陣方向に大きく表示）\n" +
        "・分裂駒の配置：\n" +
        "　ドラッグ＆ドロップで移動\n" +
        "・駒の回転：タップ/クリック\n" +
        "　（配置前の向き調整用）"
    };

    private GameObject rulesTextObj;
    private GameObject rulesNextBtn;
    private GameObject rulesPrevBtn;

    private GameObject CreateRulesPanel(GameObject canvasObj)
    {
        GameObject panel = new GameObject("RulesPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.9f);

        rulesTextObj = new GameObject("RulesText", typeof(RectTransform));
        rulesTextObj.transform.SetParent(panel.transform, false);
        RectTransform textRt = rulesTextObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.offsetMin = new Vector2(30, 130);
        textRt.offsetMax = new Vector2(-30, -20);

        Text txt = rulesTextObj.AddComponent<Text>();
        txt.fontSize = 22;
        txt.color = Color.white;
        txt.alignment = TextAnchor.UpperLeft;
        txt.font = FontProvider.GetFont();
        txt.supportRichText = true;

        rulesPrevBtn = CreateRulesNavButton(panel, "PrevButton", "前へ", new Vector2(-150, 80), false, () =>
        {
            if (rulesPage > 0) { rulesPage--; UpdateRulesPage(); }
        });

        rulesNextBtn = CreateRulesNavButton(panel, "NextButton", "次へ", new Vector2(150, 80), false, () =>
        {
            if (rulesPage < rulesPages.Length - 1) { rulesPage++; UpdateRulesPage(); }
        });

        CreateRulesNavButton(panel, "CloseButton", "閉じる", new Vector2(0, 20), true, () => rulesPanel.SetActive(false));

        return panel;
    }

    private GameObject CreateRulesNavButton(GameObject parent, string name, string label, Vector2 pos, bool isClose, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform));
        btnObj.transform.SetParent(parent.transform, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0);
        btnRt.anchorMax = new Vector2(0.5f, 0);
        btnRt.sizeDelta = new Vector2(160, 50);
        btnRt.anchoredPosition = pos;

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = isClose ? new Color(0.6f, 0.2f, 0.2f, 0.9f) : new Color(0.2f, 0.4f, 0.8f, 0.9f);

        GameObject btnTxt = new GameObject("Text", typeof(RectTransform));
        btnTxt.transform.SetParent(btnObj.transform, false);
        RectTransform btnTxtRt = btnTxt.GetComponent<RectTransform>();
        btnTxtRt.sizeDelta = new Vector2(160, 50);
        btnTxtRt.anchoredPosition = Vector2.zero;

        Text btnLabel = btnTxt.AddComponent<Text>();
        btnLabel.text = label;
        btnLabel.fontSize = 24;
        btnLabel.color = Color.white;
        btnLabel.alignment = TextAnchor.MiddleCenter;
        btnLabel.font = FontProvider.GetFont();

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(onClick);

        return btnObj;
    }

    private void ShowRules()
    {
        rulesPage = 0;
        UpdateRulesPage();
        rulesPanel.SetActive(true);
    }

    private GameObject CreateCpuLevelPanel(GameObject canvasObj)
    {
        GameObject panel = new GameObject("CpuLevelPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.9f);

        CreatePanelButton(panel, "レベル1 (初級)", new Vector2(0, 100), () => { panel.SetActive(false); pendingCpuLevel = 1; ShowRandomModeSelect(); });
        CreatePanelButton(panel, "レベル2 (中級)", new Vector2(0, 20), () => { panel.SetActive(false); pendingCpuLevel = 2; ShowRandomModeSelect(); });
        CreatePanelButton(panel, "レベル3 (上級)", new Vector2(0, -60), () => { panel.SetActive(false); pendingCpuLevel = 3; ShowRandomModeSelect(); });
        CreatePanelButton(panel, "レベル4 (最強)", new Vector2(0, -140), () => { panel.SetActive(false); pendingCpuLevel = 4; ShowRandomModeSelect(); });
        CreatePanelButton(panel, "戻る", new Vector2(0, -240), () => panel.SetActive(false));

        return panel;
    }

    private void ShowCpuLevelSelect(bool cpuVsCpu)
    {
        cpuVsCpuMode = cpuVsCpu;
        cpuLevelPanel.SetActive(true);
    }

    private void ShowRandomModeSelect(GameManager.PlayMode mode)
    {
        pendingPlayMode = mode;
        ShowTimeSelect();
    }

    private void ShowRandomModeSelect()
    {
        pendingPlayMode = cpuVsCpuMode ? GameManager.PlayMode.CpuVsCpu : GameManager.PlayMode.CPU;
        ShowTimeSelect();
    }

    private GameObject CreateTimePanel(GameObject canvasObj)
    {
        GameObject panel = new GameObject("TimePanel", typeof(RectTransform));
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.9f);

        Text titleObj = new GameObject("Title", typeof(RectTransform)).AddComponent<Text>();
        titleObj.transform.SetParent(panel.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(300, 60);
        titleRt.anchoredPosition = new Vector2(0, 160);
        titleObj.text = "持ち時間";
        titleObj.fontSize = 28;
        titleObj.color = Color.white;
        titleObj.alignment = TextAnchor.MiddleCenter;
        titleObj.font = FontProvider.GetFont();

        int[] options = { 0, 60, 180, 300, 600 };
        string[] labels = { "なし", "1分", "3分", "5分", "10分" };
        for (int i = 0; i < options.Length; i++)
        {
            int t = options[i];
            int idx = i;
            CreatePanelButton(panel, labels[idx], new Vector2(0, 80 - idx * 70), () =>
            {
                panel.SetActive(false);
                pendingTimeSeconds = t;
                randomModePanel.SetActive(true);
            });
        }
        CreatePanelButton(panel, "戻る", new Vector2(0, 80 - 6 * 70), () => panel.SetActive(false));

        return panel;
    }

    private void ShowTimeSelect()
    {
        timePanel.SetActive(true);
    }

    private GameObject CreateRandomModePanel(GameObject canvasObj)
    {
        GameObject panel = new GameObject("RandomModePanel", typeof(RectTransform));
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.9f);

        CreatePanelButton(panel, "通常配置", new Vector2(0, 60), () =>
        {
            panel.SetActive(false);
            StartGameWithRandomMode(false);
        });
        CreatePanelButton(panel, "ランダム配置", new Vector2(0, -20), () =>
        {
            panel.SetActive(false);
            StartGameWithRandomMode(true);
        });
        CreatePanelButton(panel, "戻る", new Vector2(0, -120), () => panel.SetActive(false));

        return panel;
    }

    private void StartGameWithRandomMode(bool useRandom)
    {
        gameManager.GameRules.randomPieceDirections = useRandom;
        gameManager.GameRules.timeControlSeconds = pendingTimeSeconds;
        StartGame(pendingPlayMode, pendingCpuLevel);
    }

    private void CreatePanelButton(GameObject parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(label, typeof(RectTransform));
        btnObj.transform.SetParent(parent.transform, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280, 50);
        rt.anchoredPosition = pos;
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.8f, 0.9f);
        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.sizeDelta = new Vector2(280, 50);
        txtRt.anchoredPosition = Vector2.zero;
        Text txt = txtObj.AddComponent<Text>();
        txt.text = label;
        txt.fontSize = 24;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = FontProvider.GetFont();
        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => PlayClickSE());
        btn.onClick.AddListener(onClick);
    }

    private void UpdateRulesPage()
    {
        var txt = rulesTextObj.GetComponent<Text>();
        txt.text = rulesPages[rulesPage];

        rulesPrevBtn.SetActive(rulesPage > 0);
        rulesNextBtn.SetActive(rulesPage < rulesPages.Length - 1);
    }

    private void StartGame(GameManager.PlayMode mode, int cpuLevel = 1)
    {
        Destroy(panelObj);
        Destroy(rulesPanel);
        Destroy(cpuLevelPanel);
        Destroy(randomModePanel);
        Destroy(timePanel);
        gameManager.StartGame(mode, cpuLevel);
    }
}
