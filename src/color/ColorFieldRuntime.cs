namespace ChromaPrototype.Color;

using System.Collections.Generic;
using Godot;

/// <summary>
/// Runtime manager for the color probe system.
/// Handles probe storage, spatial indexing, and absorption queries.
/// </summary>
public partial class ColorFieldRuntime : Node
{
    /// <summary>
    /// Cell size for spatial hashing. Should be approximately equal to pulse radius.
    /// </summary>
    [Export(PropertyHint.Range, "1,10,0.5")]
    public float CellSize { get; set; } = 3.0f;

    /// <summary>
    /// Whether to print debug logs on pulse.
    /// </summary>
    [Export]
    public bool DebugLogging { get; set; } = true;

    /// <summary>
    /// Whether to draw debug gizmos.
    /// </summary>
    [Export]
    public bool DebugDraw { get; set; } = true;

    private readonly List<Probe> _probes = new();
    private SpatialHash2D _spatialHash = null!;

    // Reusable lists to avoid allocations during queries
    private readonly List<int> _queryCandidates = new();
    private readonly List<int> _affectedProbes = new();
    private readonly List<(int probeId, float distance)> _sortBuffer = new();

    /// <summary>
    /// Gets the number of probes currently registered.
    /// </summary>
    public int ProbeCount => _probes.Count;

    /// <summary>
    /// Gets read-only access to all probes.
    /// </summary>
    public IReadOnlyList<Probe> Probes => _probes;

    public override void _Ready()
    {
        _spatialHash = new SpatialHash2D(CellSize);
        ScanAndRegisterMarkers();
    }

    /// <summary>
    /// Scans the scene tree for ColorProbeMarker nodes and registers them as probes.
    /// </summary>
    public void ScanAndRegisterMarkers()
    {
        _probes.Clear();
        _spatialHash.Clear();

        var markers = GetTree().GetNodesInGroup("color_probe_markers");
        foreach (var node in markers)
        {
            if (node is ColorProbeMarker marker)
            {
                RegisterProbe(
                    marker.GlobalPosition,
                    marker.Color,
                    marker.Capacity,
                    marker.Density,
                    marker.FloorId
                );
            }
        }

        if (DebugLogging)
        {
            GD.Print($"[ColorField] Registered {_probes.Count} probes from markers");
        }
    }

    /// <summary>
    /// Registers a new static probe and adds it to the spatial index.
    /// Returns the probe id (index).
    /// </summary>
    public int RegisterProbe(
        Vector3 position,
        LogicalColor color,
        float capacity,
        float density = 1.0f,
        short floorId = -1)
    {
        var probe = new Probe(position, color, capacity, density, floorId);
        var probeId = _probes.Count;
        _probes.Add(probe);
        _spatialHash.Insert(probeId, position);
        return probeId;
    }

    /// <summary>
    /// Queries probes in a cylinder defined by center, radius, and Y height window.
    /// Returns probe ids in the results list.
    /// </summary>
    public void QueryCylinder(
        Vector3 center,
        float radius,
        float yMin,
        float yMax,
        List<int> results,
        short floorIdFilter = -1)
    {
        results.Clear();

        // Get candidates from spatial hash (XZ proximity)
        _spatialHash.QueryCircle(center, radius, _queryCandidates);

        var radiusSquared = radius * radius;

        foreach (var probeId in _queryCandidates)
        {
            ref var probe = ref GetProbeRef(probeId);

            // Skip empty probes
            if (!probe.HasRemaining)
                continue;

            // Height filter
            if (probe.Position.Y < yMin || probe.Position.Y > yMax)
                continue;

            // Floor filter (if enabled)
            if (floorIdFilter >= 0 && probe.FloorId >= 0 && probe.FloorId != floorIdFilter)
                continue;

            // XZ circle test (no sqrt)
            var dx = probe.Position.X - center.X;
            var dz = probe.Position.Z - center.Z;
            var distSq = dx * dx + dz * dz;

            if (distSq <= radiusSquared)
            {
                results.Add(probeId);
            }
        }
    }

    /// <summary>
    /// Performs an absorption pulse at the given center with the specified config.
    /// Uses Policy 1: Total cap, drain nearest-first.
    /// </summary>
    public PulseResult PulseAbsorb(Vector3 center, PulseConfig config)
    {
        var yMin = center.Y - config.HeightDown;
        var yMax = center.Y + config.HeightUp;

        // Query candidates
        _affectedProbes.Clear();
        QueryCylinder(center, config.Radius, yMin, yMax, _affectedProbes, config.FloorIdFilter);

        var candidateCount = _affectedProbes.Count;

        if (candidateCount == 0)
        {
            if (DebugLogging)
            {
                GD.Print("[ColorField] Pulse: no candidates in range");
            }
            return PulseResult.Empty;
        }

        // Sort by distance (nearest first)
        _sortBuffer.Clear();
        foreach (var probeId in _affectedProbes)
        {
            var pos = _probes[probeId].Position;
            var dx = pos.X - center.X;
            var dz = pos.Z - center.Z;
            var dist = Mathf.Sqrt(dx * dx + dz * dz);
            _sortBuffer.Add((probeId, dist));
        }
        _sortBuffer.Sort((a, b) => a.distance.CompareTo(b.distance));

        // Drain probes until maxTakeTotal is reached
        var takenPerColor = new float[LogicalColorExtensions.ColorCount];
        var totalTaken = 0f;
        var probesDrained = 0;
        var drainedList = new List<int>();

        foreach (var (probeId, _) in _sortBuffer)
        {
            if (totalTaken >= config.MaxTakeTotal)
                break;

            ref var probe = ref GetProbeRef(probeId);

            // Calculate how much to take from this probe
            var maxCanTake = config.MaxTakeTotal - totalTaken;
            var available = probe.Remaining * probe.Density;
            var take = Mathf.Min(maxCanTake, available);

            if (take > 0.0001f)
            {
                // Drain the probe
                var actualDrain = take / probe.Density; // Convert back accounting for density
                actualDrain = Mathf.Min(actualDrain, probe.Remaining);
                probe.Remaining -= actualDrain;

                takenPerColor[(int)probe.Color] += take;
                totalTaken += take;
                probesDrained++;
                drainedList.Add(probeId);
            }
        }

        var result = new PulseResult(
            takenPerColor,
            totalTaken,
            candidateCount,
            probesDrained,
            drainedList
        );

        if (DebugLogging)
        {
            GD.Print($"[ColorField] {result}");
        }

        return result;
    }

    /// <summary>
    /// Gets a reference to a probe by id for direct modification.
    /// </summary>
    public ref Probe GetProbeRef(int probeId)
    {
        return ref System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_probes)[probeId];
    }

    /// <summary>
    /// Gets probe data by id (copy).
    /// </summary>
    public Probe GetProbe(int probeId)
    {
        return _probes[probeId];
    }

    /// <summary>
    /// Spawns a grid of test probes for debugging.
    /// </summary>
    public void SpawnTestGrid(
        Vector3 origin,
        int countX,
        int countZ,
        float spacing,
        LogicalColor color,
        float capacity = 10f,
        float density = 1f)
    {
        for (var x = 0; x < countX; x++)
        {
            for (var z = 0; z < countZ; z++)
            {
                var pos = origin + new Vector3(x * spacing, 0, z * spacing);
                RegisterProbe(pos, color, capacity, density);
            }
        }

        if (DebugLogging)
        {
            GD.Print($"[ColorField] Spawned {countX * countZ} test probes at {origin}");
        }
    }

    /// <summary>
    /// Gets debug statistics about the field.
    /// </summary>
    public string GetDebugStats()
    {
        var (cellCount, totalInHash) = _spatialHash.GetDebugInfo();
        var colorCounts = new int[LogicalColorExtensions.ColorCount];
        var totalRemaining = 0f;

        foreach (var probe in _probes)
        {
            colorCounts[(int)probe.Color]++;
            totalRemaining += probe.Remaining;
        }

        var stats = $"Probes: {_probes.Count}, Cells: {cellCount}, Remaining: {totalRemaining:F1}\n";
        for (var i = 0; i < LogicalColorExtensions.ColorCount; i++)
        {
            if (colorCounts[i] > 0)
            {
                stats += $"  {(LogicalColor)i}: {colorCounts[i]}\n";
            }
        }
        return stats;
    }
}
