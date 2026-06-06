using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GameRulesSO gameRules;
    [SerializeField] private InitialPieceLayoutSO initialLayout;

    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        var gm = FindObjectOfType<GameManager>();
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
