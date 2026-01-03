namespace ChromaPrototype.Color;

using System.Collections.Generic;

/// <summary>
/// Result of an absorption pulse operation.
/// </summary>
public readonly struct PulseResult
{
    /// <summary>
    /// Amount of each color absorbed (indexed by LogicalColor).
    /// </summary>
    public readonly float[] TakenPerColor;

    /// <summary>
    /// Total amount absorbed across all colors.
    /// </summary>
    public readonly float TotalTaken;

    /// <summary>
    /// Number of candidate probes tested.
    /// </summary>
    public readonly int CandidatesTested;

    /// <summary>
    /// Number of probes that were actually drained.
    /// </summary>
    public readonly int ProbesDrained;

    /// <summary>
    /// List of probe indices that were affected (for VFX/debug).
    /// </summary>
    public readonly IReadOnlyList<int> AffectedProbes;

    public PulseResult(
        float[] takenPerColor,
        float totalTaken,
        int candidatesTested,
        int probesDrained,
        List<int> affectedProbes)
    {
        TakenPerColor = takenPerColor;
        TotalTaken = totalTaken;
        CandidatesTested = candidatesTested;
        ProbesDrained = probesDrained;
        AffectedProbes = affectedProbes;
    }

    /// <summary>
    /// Returns an empty result (nothing absorbed).
    /// </summary>
    public static PulseResult Empty => new(
        new float[LogicalColorExtensions.ColorCount],
        0f,
        0,
        0,
        new List<int>()
    );

    /// <summary>
    /// Returns a debug string representation.
    /// </summary>
    public override string ToString()
    {
        var colorStr = "";
        for (var i = 0; i < TakenPerColor.Length; i++)
        {
            if (TakenPerColor[i] > 0.0001f)
            {
                var color = (LogicalColor)i;
                colorStr += $"{color}:{TakenPerColor[i]:F2} ";
            }
        }
        return $"Pulse: total={TotalTaken:F2}, candidates={CandidatesTested}, drained={ProbesDrained} [{colorStr.TrimEnd()}]";
    }
}
