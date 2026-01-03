namespace ChromaPrototype.Color.Shapes;

using System.Collections.Generic;
using Godot;

/// <summary>
/// Spawns probes at runtime in a configured shape pattern.
/// Does not create scene nodes - directly registers probes with ColorFieldRuntime.
/// </summary>
public partial class ProbeFieldSpawner : Node3D
{
    /// <summary>
    /// Shape type for probe spawning.
    /// </summary>
    [Export]
    public SpawnShape Shape { get; set; } = SpawnShape.Rect;

    /// <summary>
    /// Radius for disk shapes.
    /// </summary>
    [Export(PropertyHint.Range, "0.5,50,0.5")]
    public float Radius { get; set; } = 2.0f;

    /// <summary>
    /// Width (X) for rect shapes.
    /// </summary>
    [Export(PropertyHint.Range, "0.5,50,0.5")]
    public float Width { get; set; } = 4.0f;

    /// <summary>
    /// Length (Z) for rect shapes.
    /// </summary>
    [Export(PropertyHint.Range, "0.5,50,0.5")]
    public float Length { get; set; } = 4.0f;

    /// <summary>
    /// Spacing between probes.
    /// </summary>
    [Export(PropertyHint.Range, "0.25,5,0.25")]
    public float Spacing { get; set; } = 0.5f;

    /// <summary>
    /// Logical color for all probes spawned by this field.
    /// </summary>
    [Export]
    public LogicalColor Color { get; set; } = LogicalColor.Blue;

    /// <summary>
    /// Capacity per probe.
    /// </summary>
    [Export(PropertyHint.Range, "1,100,1")]
    public float Capacity { get; set; } = 10.0f;

    /// <summary>
    /// Density multiplier per probe.
    /// </summary>
    [Export(PropertyHint.Range, "0.1,5,0.1")]
    public float Density { get; set; } = 1.0f;

    /// <summary>
    /// Optional floor ID for multi-floor scenarios.
    /// </summary>
    [Export]
    public short FloorId { get; set; } = -1;

    /// <summary>
    /// Height offset applied to all probes.
    /// </summary>
    [Export(PropertyHint.Range, "-5,5,0.1")]
    public float HeightOffset { get; set; } = 0.0f;

    private readonly List<int> _spawnedProbeIds = new();

    /// <summary>
    /// Gets the IDs of all probes spawned by this field.
    /// </summary>
    public IReadOnlyList<int> SpawnedProbeIds => _spawnedProbeIds;

    /// <summary>
    /// Spawns probes and registers them with the color field runtime.
    /// </summary>
    public void SpawnProbes(ColorFieldRuntime runtime)
    {
        _spawnedProbeIds.Clear();

        var basePos = GlobalPosition + new Vector3(0, HeightOffset, 0);

        switch (Shape)
        {
            case SpawnShape.Disk:
                SpawnDisk(runtime, basePos);
                break;
            case SpawnShape.Rect:
                SpawnRect(runtime, basePos);
                break;
        }
    }

    private void SpawnDisk(ColorFieldRuntime runtime, Vector3 basePos)
    {
        // Calculate grid bounds that encompasses the disk
        var radiusSq = Radius * Radius;
        var halfGrid = Mathf.CeilToInt(Radius / Spacing);

        for (var x = -halfGrid; x <= halfGrid; x++)
        {
            for (var z = -halfGrid; z <= halfGrid; z++)
            {
                var localX = x * Spacing;
                var localZ = z * Spacing;
                var distSq = localX * localX + localZ * localZ;

                if (distSq <= radiusSq)
                {
                    var pos = basePos + new Vector3(localX, 0, localZ);
                    var id = runtime.RegisterProbe(pos, Color, Capacity, Density, FloorId);
                    _spawnedProbeIds.Add(id);
                }
            }
        }
    }

    private void SpawnRect(ColorFieldRuntime runtime, Vector3 basePos)
    {
        var halfWidth = Width * 0.5f;
        var halfLength = Length * 0.5f;

        var countX = Mathf.Max(1, Mathf.FloorToInt(Width / Spacing));
        var countZ = Mathf.Max(1, Mathf.FloorToInt(Length / Spacing));

        // Center the grid
        var startX = -halfWidth + (Width - (countX - 1) * Spacing) * 0.5f;
        var startZ = -halfLength + (Length - (countZ - 1) * Spacing) * 0.5f;

        for (var x = 0; x < countX; x++)
        {
            for (var z = 0; z < countZ; z++)
            {
                var localX = startX + x * Spacing;
                var localZ = startZ + z * Spacing;
                var pos = basePos + new Vector3(localX, 0, localZ);
                var id = runtime.RegisterProbe(pos, Color, Capacity, Density, FloorId);
                _spawnedProbeIds.Add(id);
            }
        }
    }

    /// <summary>
    /// Gets the estimated probe count without actually spawning.
    /// </summary>
    public int GetEstimatedProbeCount()
    {
        return Shape switch
        {
            SpawnShape.Disk => EstimateDiskCount(),
            SpawnShape.Rect => EstimateRectCount(),
            _ => 0
        };
    }

    private int EstimateDiskCount()
    {
        var radiusSq = Radius * Radius;
        var halfGrid = Mathf.CeilToInt(Radius / Spacing);
        var count = 0;

        for (var x = -halfGrid; x <= halfGrid; x++)
        {
            for (var z = -halfGrid; z <= halfGrid; z++)
            {
                var localX = x * Spacing;
                var localZ = z * Spacing;
                if (localX * localX + localZ * localZ <= radiusSq)
                    count++;
            }
        }

        return count;
    }

    private int EstimateRectCount()
    {
        var countX = Mathf.Max(1, Mathf.FloorToInt(Width / Spacing));
        var countZ = Mathf.Max(1, Mathf.FloorToInt(Length / Spacing));
        return countX * countZ;
    }
}
