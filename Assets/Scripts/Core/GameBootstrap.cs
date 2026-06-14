using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GameRulesSO gameRules;
    [SerializeField] private InitialPieceLayoutSO initialLayout;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (FindFirstObjectByType<GameBootstrap>() != null) return;
        var obj = new GameObject("GameBootstrap");
        obj.AddComponent<GameBootstrap>();
    }

    private void Awake()
    {
        if (FindObjectsByType<GameBootstrap>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        var gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            gm = gmObj.AddComponent<GameManager>();
        }

        if (gameRules != null)
        {
            var field = typeof(GameManager).GetField("gameRules",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(gm, gameRules);
        }

        if (initialLayout != null)
        {
            var field = typeof(GameManager).GetField("initialLayout",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(gm, initialLayout);
        }
    }
}
