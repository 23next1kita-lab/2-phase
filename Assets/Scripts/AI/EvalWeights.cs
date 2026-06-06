using System;

[Serializable]
public class EvalWeights
{
    public float captureTwoPhase = 1000f;
    public float captureOnePhase = 50000f;
    public float dangerOnePhase = -3000f;
    public float dangerTwoPhase = -400f;
    public float surroundByOnePhase = 30f;
    public float surroundFriendly = 10f;
    public float onePhaseSurroundFriendly = 15f;
    public float groupCohesion = -3f;
    public float onePhaseMobility = 8f;
    public float twoPhaseMobility = 5f;
    public float forwardPushEarly = 20f;
    public int earlyTurnCutoff = 5;
    public float biasSideStrength = 15f;
    public float safeForkCapture = 300f;
    public float riskyForkCapture = 120f;
    public float hiddenFaceOnePhase = -500f;
    public float hiddenFaceTwoPhase = -80f;
    public float centerWeight = -1f;
    public float spawnedTurnPenalty = -30f;
    public float retaliationAdjacent = 80f;
    public float opponentResponseFactor = 0.85f;
    public float forkSafetyBuffer = 20f;
    public float distancePenalty = -3f;
    public float recaptureBonus = 200f;
    public float repetitionPenalty = -500f;
    public float territoryControl = 8f;
    public float pieceCountAdvantage = 100f;
    public float isolationPenalty = -10f;
    public float safeZoneBonus = 3000f;
    public float backtrackPenalty = -1000000f;
    public float dispersionPenalty = -50f;
    public float forwardPressure = 20f;
    public float shelterBonus = 40f;
    public float retreatPenalty = -25f;
    public float safeTwoPhaseCapture = 1000f;
    public float twoPhaseWall = 15f;
    public float exposedOnePhase = -100f;
    public float openingAdvanceBonus = 2000f;
    public float twoPhaseThreat = 5000f;
    public float secondMoveCaptureBonus = 8000f;
    public float safeThreatBonus = 10000f;
    public float earlyBacktrackPenalty = -80000f;
    public float isolatedAdvanceBonus = 5000f;

    public EvalWeights Clone()
    {
        return (EvalWeights)MemberwiseClone();
    }

    public void Mutate(float rate = 0.15f, float sigma = 0.3f)
    {
        MutateField(ref captureTwoPhase, 200, 2000, rate, sigma);
        MutateField(ref captureOnePhase, 10000, 100000, rate, sigma);
        MutateField(ref dangerOnePhase, -5000, -500, rate, sigma);
        MutateField(ref dangerTwoPhase, -1000, -100, rate, sigma);
        MutateField(ref surroundByOnePhase, 0, 100, rate, sigma);
        MutateField(ref surroundFriendly, 0, 40, rate, sigma);
        MutateField(ref onePhaseSurroundFriendly, 0, 50, rate, sigma);
        MutateField(ref groupCohesion, -10, 0, rate, sigma);
        MutateField(ref onePhaseMobility, 0, 25, rate, sigma);
        MutateField(ref twoPhaseMobility, 0, 15, rate, sigma);
        MutateField(ref forwardPushEarly, 0, 60, rate, sigma);
        MutateField(ref biasSideStrength, 0, 50, rate, sigma);
        MutateField(ref safeForkCapture, 100, 600, rate, sigma);
        MutateField(ref riskyForkCapture, 30, 300, rate, sigma);
        MutateField(ref hiddenFaceOnePhase, -1500, -100, rate, sigma);
        MutateField(ref hiddenFaceTwoPhase, -300, -10, rate, sigma);
        MutateField(ref centerWeight, -5, 0, rate, sigma);
        MutateField(ref spawnedTurnPenalty, -100, 0, rate, sigma);
        MutateField(ref retaliationAdjacent, 20, 200, rate, sigma);
        MutateField(ref opponentResponseFactor, 0.3f, 1.0f, rate, sigma);
        MutateField(ref recaptureBonus, 50, 500, rate, sigma);
        MutateField(ref repetitionPenalty, -2000, -100, rate, sigma);
        MutateField(ref territoryControl, 0, 30, rate, sigma);
        MutateField(ref pieceCountAdvantage, 20, 400, rate, sigma);
        MutateField(ref isolationPenalty, -30, 0, rate, sigma);
        MutateField(ref safeZoneBonus, 500, 6000, rate, sigma);
        MutateField(ref backtrackPenalty, -2000000, -10000, rate, sigma);
        MutateField(ref dispersionPenalty, -20, -1, rate, sigma);
        MutateField(ref forwardPressure, 5, 50, rate, sigma);
        MutateField(ref shelterBonus, 10, 100, rate, sigma);
        MutateField(ref retreatPenalty, -80, -5, rate, sigma);
        MutateField(ref safeTwoPhaseCapture, 300, 3000, rate, sigma);
        MutateField(ref twoPhaseWall, 5, 40, rate, sigma);
        MutateField(ref exposedOnePhase, -300, -30, rate, sigma);
        MutateField(ref openingAdvanceBonus, 200, 8000, rate, sigma);
        MutateField(ref twoPhaseThreat, 500, 20000, rate, sigma);
        MutateField(ref secondMoveCaptureBonus, 500, 30000, rate, sigma);
        MutateField(ref safeThreatBonus, 1000, 40000, rate, sigma);
        MutateField(ref earlyBacktrackPenalty, -200000, -5000, rate, sigma);
        MutateField(ref isolatedAdvanceBonus, 500, 20000, rate, sigma);
    }

    private void MutateField(ref float field, float min, float max, float rate, float sigma)
    {
        if (UnityEngine.Random.value < rate)
        {
            float range = max - min;
            field += (float)(UnityEngine.Random.Range(0f, 1f) - 0.5) * sigma * range;
            field = Math.Max(min, Math.Min(max, field));
        }
    }

    public static EvalWeights Random()
    {
        var w = new EvalWeights();
        w.Mutate(1.0f, 0.5f);
        return w;
    }

    public static EvalWeights Crossover(EvalWeights a, EvalWeights b)
    {
        var child = new EvalWeights();
        var fields = typeof(EvalWeights).GetFields();
        foreach (var f in fields)
        {
            if (f.FieldType == typeof(float))
            {
                float va = (float)f.GetValue(a);
                float vb = (float)f.GetValue(b);
                float blend = UnityEngine.Random.Range(0f, 1f);
                float v = va * blend + vb * (1 - blend);
                f.SetValue(child, v);
            }
            else if (f.FieldType == typeof(int))
            {
                int va = (int)f.GetValue(a);
                int vb = (int)f.GetValue(b);
                f.SetValue(child, UnityEngine.Random.value < 0.5f ? va : vb);
            }
        }
        return child;
    }
}
