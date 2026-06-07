using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

public class WeightEvolution : MonoBehaviour
{
    private const string HEADLESS_OBJ_NAME = "__HeadlessEvolution__";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        var args = System.Environment.GetCommandLineArgs();
        if (args.Contains("-runEvolution"))
        {
            var go = new GameObject(HEADLESS_OBJ_NAME);
            Object.DontDestroyOnLoad(go);
            var evo = go.AddComponent<WeightEvolution>();
            evo.RunHeadlessSync();
        }
    }

    public static void RunHeadless()
    {
        var evo = new GameObject(HEADLESS_OBJ_NAME).AddComponent<WeightEvolution>();
        evo.savePath = Path.Combine(Application.persistentDataPath, "best_weights.json");
        evo.RunHeadlessSync();
    }

    [Header("GA Parameters")]
    public int populationSize = 24;
    public int generations = 35;
    public int gamesPerMatch = 3;
    public float mutationRate = 0.25f;
    public float mutationSigma = 0.5f;
    public int eliteCount = 1;
    public int tournamentSize = 3;

    [Header("Display")]
    public bool showProgress = true;

    private List<EvalWeights> population;
    private List<float> fitness;
    private int currentGen;
    private bool running;
    private string savePath;
    private float fitnessBest;
    private float fitnessAvg;

    private void Start()
    {
        savePath = Path.Combine(Application.persistentDataPath, "best_weights.json");
    }

    private void OnGUI()
    {
#if !UNITY_EDITOR
        if (!Debug.isDebugBuild) return;
#endif
        if (!showProgress) return;

        int y = 10;
        GUI.Label(new Rect(10, y, 400, 20), $"Generation: {currentGen}/{generations}  Running: {running}");

        if (!running && currentGen == 0)
        {
            if (GUI.Button(new Rect(10, y + 30, 200, 40), "Start Evolution"))
                StartEvolution();
        }

        if (running)
        {
            if (GUI.Button(new Rect(10, y + 30, 200, 40), "Stop"))
                StopEvolution();
        }
        else if (currentGen > 0)
        {
            if (GUI.Button(new Rect(10, y + 80, 200, 40), "Save Best Weights"))
                SaveBestWeights();

            if (GUI.Button(new Rect(10, y + 130, 200, 40), "Continue Evolution"))
                ContinueEvolution();

            if (fitness != null && fitness.Count > 0)
            {
                float best = fitness.Max();
                float avg = fitness.Average();
                float worst = fitness.Min();

                GUI.Label(new Rect(10, y + 180, 400, 60),
                    $"Best: {best:F3}\nAvg: {avg:F3}\nWorst: {worst:F3}");
            }
        }
    }

    public void StartEvolution()
    {
        population = new List<EvalWeights>();
        for (int i = 0; i < populationSize; i++)
            population.Add(EvalWeights.Random());

        fitness = new List<float>();
        currentGen = 0;
        running = true;
        StartCoroutine(RunEvolution());
    }

    public void StopEvolution()
    {
        running = false;
        StopAllCoroutines();
    }

    public void ContinueEvolution()
    {
        if (population == null || fitness == null) return;
        running = true;
        StartCoroutine(RunEvolution());
    }

    public void RunHeadlessSync()
    {
        population = new List<EvalWeights>();
        for (int i = 0; i < populationSize; i++)
            population.Add(EvalWeights.Random());

        fitness = new List<float>();
        currentGen = 0;
        running = true;

        var sim = new FastGameSimulator();

        for (; currentGen < generations && running; currentGen++)
        {
            EvaluateFitnessSync(sim);
            SaveBestWeights();
            Debug.Log($"[Evolution] GEN {currentGen} best={fitnessBest:F3} avg={fitnessAvg:F3}");

            var nextGen = new List<EvalWeights>();
            for (int i = 0; i < eliteCount && i < population.Count; i++)
                nextGen.Add(population[i].Clone());

            while (nextGen.Count < populationSize)
            {
                var p1 = TournamentSelect();
                var p2 = TournamentSelect();
                var child = EvalWeights.Crossover(p1, p2);
                child.Mutate(mutationRate, mutationSigma);
                nextGen.Add(child);
            }

            for (int r = 0; r < 2 && nextGen.Count > eliteCount; r++)
            {
                int idx = Random.Range(eliteCount, nextGen.Count);
                nextGen[idx] = EvalWeights.Random();
            }

            population = nextGen;
            fitness = null;
        }

        running = false;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(0);
#else
        Application.Quit(0);
#endif
    }

    private System.Collections.IEnumerator RunEvolution()
    {
        for (; currentGen < generations && running; currentGen++)
        {
            yield return StartCoroutine(EvaluateFitness());

            if (!running) yield break;

            SaveBestWeights();

            var nextGen = new List<EvalWeights>();

            for (int i = 0; i < eliteCount && i < population.Count; i++)
                nextGen.Add(population[i].Clone());

            while (nextGen.Count < populationSize)
            {
                var parent1 = TournamentSelect();
                var parent2 = TournamentSelect();
                var child = EvalWeights.Crossover(parent1, parent2);
                child.Mutate(mutationRate, mutationSigma);
                nextGen.Add(child);
            }

            for (int r = 0; r < 2 && nextGen.Count > eliteCount; r++)
            {
                int idx = Random.Range(eliteCount, nextGen.Count);
                nextGen[idx] = EvalWeights.Random();
            }

            population = nextGen;
            fitness = null;

            if (showProgress)
                Debug.Log($"[Evolution] Gen {currentGen}/{generations} best={fitnessBest:F3} avg={fitnessAvg:F3}");
        }

        running = false;
    }

    private System.Collections.IEnumerator EvaluateFitness()
    {
        fitness = new List<float>(new float[population.Count]);
        var sim = new FastGameSimulator();

        int totalPairs = population.Count * (population.Count - 1) / 2;
        int pairsDone = 0;

        for (int i = 0; i < population.Count; i++)
        {
            for (int j = i + 1; j < population.Count; j++)
            {
                if (!running) yield break;

                int iWins = 0;
                int jWins = 0;

                for (int g = 0; g < gamesPerMatch; g++)
                {
                    var w1 = sim.Simulate(population[i], population[j]);
                    if (w1 == PlayerSide.Player1) { iWins += 3; jWins--; }
                    else if (w1 == PlayerSide.Player2) { jWins += 3; iWins--; }

                    var w2 = sim.Simulate(population[j], population[i]);
                    if (w2 == PlayerSide.Player2) { iWins += 3; jWins--; }
                    else if (w2 == PlayerSide.Player1) { jWins += 3; iWins--; }
                }

                fitness[i] += iWins;
                fitness[j] += jWins;
                pairsDone++;

                if (showProgress && pairsDone % 5 == 0)
                {
                    fitnessBest = fitness.Max();
                    fitnessAvg = fitness.Sum() / fitness.Count;
                }

                if (pairsDone % 3 == 0)
                    yield return null;
            }
        }

        var indexed = population.Select((w, i) => (w, f: fitness[i]))
            .OrderByDescending(x => x.f).ToList();

        population = indexed.Select(x => x.w).ToList();
        fitness = indexed.Select(x => x.f).ToList();

        fitnessBest = fitness.Max();
        fitnessAvg = fitness.Sum() / fitness.Count;
    }

    private void EvaluateFitnessSync(FastGameSimulator sim)
    {
        fitness = new List<float>(new float[population.Count]);

        for (int i = 0; i < population.Count; i++)
        {
            for (int j = i + 1; j < population.Count; j++)
            {
                int iWins = 0, jWins = 0;

                for (int g = 0; g < gamesPerMatch; g++)
                {
                    var w1 = sim.Simulate(population[i], population[j]);
                    if (w1 == PlayerSide.Player1) { iWins += 3; jWins--; }
                    else if (w1 == PlayerSide.Player2) { jWins += 3; iWins--; }

                    var w2 = sim.Simulate(population[j], population[i]);
                    if (w2 == PlayerSide.Player2) { iWins += 3; jWins--; }
                    else if (w2 == PlayerSide.Player1) { jWins += 3; iWins--; }
                }

                fitness[i] += iWins;
                fitness[j] += jWins;
            }
        }

        var indexed = population.Select((w, i) => (w, f: fitness[i]))
            .OrderByDescending(x => x.f).ToList();

        population = indexed.Select(x => x.w).ToList();
        fitness = indexed.Select(x => x.f).ToList();

        fitnessBest = fitness.Max();
        fitnessAvg = fitness.Sum() / fitness.Count;
    }

    private EvalWeights TournamentSelect()
    {
        EvalWeights best = null;
        float bestF = float.MinValue;

        for (int i = 0; i < tournamentSize; i++)
        {
            int idx = Random.Range(0, population.Count);
            if (fitness[idx] > bestF)
            {
                bestF = fitness[idx];
                best = population[idx];
            }
        }

        return best;
    }

    public EvalWeights SaveBestWeights()
    {
        if (population == null || population.Count == 0 || fitness == null) return null;
        if (string.IsNullOrEmpty(savePath))
            savePath = Path.Combine(Application.persistentDataPath, "best_weights.json");
        var best = population[0];
        var json = JsonUtility.ToJson(best, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"[Evolution] Best weights saved to {savePath}");
        return best;
    }

    public static EvalWeights LoadBestWeights()
    {
        string path = Path.Combine(Application.persistentDataPath, "best_weights.json");
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonUtility.FromJson<EvalWeights>(json);
    }
}
