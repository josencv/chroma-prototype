namespace ChromaPrototype.Color;

using Godot;

/// <summary>
/// Runtime probe data structure. Stored in a flat array and referenced by index (probeId).
/// Probes are mutable (remaining changes during absorption).
/// </summary>
public struct Probe
{
    /// <summary>
    /// World position of this probe.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// The logical color this probe contains.
    /// </summary>
    public LogicalColor Color;

    /// <summary>
    /// Current amount remaining (drains on absorption).
    /// </summary>
    public float Remaining;

    /// <summary>
    /// Maximum capacity (used for recovery if enabled).
    /// </summary>
    public float Capacity;

    /// <summary>
    /// Richness multiplier - affects how much is taken per pulse.
    /// Default is 1.0.
    /// </summary>
    public float Density;

    /// <summary>
    /// Optional floor identifier for multi-floor scenarios.
    /// -1 means no floor assignment.
    /// </summary>
    public short FloorId;

    /// <summary>
    /// Owner id for dynamic probes. -1 means static (world probe).
    /// </summary>
    public int OwnerId;

    /// <summary>
    /// Last time this probe was updated (for lazy recovery).
    /// </summary>
    public double LastUpdateTime;

    public Probe(
        Vector3 position,
        LogicalColor color,
        float capacity,
        float density = 1.0f,
        short floorId = -1,
        int ownerId = -1)
    {
        Position = position;
        Color = color;
        Remaining = capacity;
        Capacity = capacity;
        Density = density;
        FloorId = floorId;
        OwnerId = ownerId;
        LastUpdateTime = 0.0;
    }

    /// <summary>
    /// Returns true if this probe has any color remaining.
    /// </summary>
    public readonly bool HasRemaining => Remaining > 0.0001f;

    /// <summary>
    /// Returns true if this is a static (world) probe.
    /// </summary>
    public readonly bool IsStatic => OwnerId == -1;

    /// <summary>
    /// Returns the fill ratio (remaining / capacity).
    /// </summary>
    public readonly float FillRatio => Capacity > 0 ? Remaining / Capacity : 0f;
}
