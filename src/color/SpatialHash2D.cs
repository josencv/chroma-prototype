namespace ChromaPrototype.Color;

using System.Collections.Generic;
using Godot;

/// <summary>
/// 2D spatial hash for XZ plane indexing. Used for cylinder queries aligned to Y axis.
/// Keys are packed (ix, iz) pairs into a single long to avoid tuple allocations.
/// </summary>
public class SpatialHash2D
{
    private readonly float _cellSize;
    private readonly float _inverseCellSize;
    private readonly Dictionary<long, List<int>> _cells = new();

    public SpatialHash2D(float cellSize)
    {
        _cellSize = cellSize;
        _inverseCellSize = 1.0f / cellSize;
    }

    public float CellSize => _cellSize;

    /// <summary>
    /// Inserts a probe id at the given world position.
    /// </summary>
    public void Insert(int probeId, Vector3 position)
    {
        var key = GetCellKey(position.X, position.Z);
        if (!_cells.TryGetValue(key, out var list))
        {
            list = new List<int>();
            _cells[key] = list;
        }
        list.Add(probeId);
    }

    /// <summary>
    /// Removes a probe id from the cell at the given position.
    /// Returns true if found and removed.
    /// </summary>
    public bool Remove(int probeId, Vector3 position)
    {
        var key = GetCellKey(position.X, position.Z);
        if (_cells.TryGetValue(key, out var list))
        {
            return list.Remove(probeId);
        }
        return false;
    }

    /// <summary>
    /// Queries all probe ids in cells that overlap with a circle in XZ plane.
    /// Results may contain duplicates if probes share cells.
    /// </summary>
    public void QueryCircle(Vector3 center, float radius, List<int> results)
    {
        results.Clear();

        // Compute cell range to check
        var minX = (int)Mathf.Floor((center.X - radius) * _inverseCellSize);
        var maxX = (int)Mathf.Floor((center.X + radius) * _inverseCellSize);
        var minZ = (int)Mathf.Floor((center.Z - radius) * _inverseCellSize);
        var maxZ = (int)Mathf.Floor((center.Z + radius) * _inverseCellSize);

        for (var ix = minX; ix <= maxX; ix++)
        {
            for (var iz = minZ; iz <= maxZ; iz++)
            {
                var key = PackKey(ix, iz);
                if (_cells.TryGetValue(key, out var list))
                {
                    results.AddRange(list);
                }
            }
        }
    }

    /// <summary>
    /// Clears all data from the spatial hash.
    /// </summary>
    public void Clear()
    {
        _cells.Clear();
    }

    /// <summary>
    /// Gets the cell key for a world position.
    /// </summary>
    private long GetCellKey(float x, float z)
    {
        var ix = (int)Mathf.Floor(x * _inverseCellSize);
        var iz = (int)Mathf.Floor(z * _inverseCellSize);
        return PackKey(ix, iz);
    }

    /// <summary>
    /// Packs two ints into a single long key.
    /// Supports negative indices.
    /// </summary>
    private static long PackKey(int ix, int iz)
    {
        // Cast to uint first to handle negatives correctly, then pack
        return ((long)(uint)ix << 32) | (uint)iz;
    }

    /// <summary>
    /// Gets debug info about the spatial hash.
    /// </summary>
    public (int cellCount, int totalProbes) GetDebugInfo()
    {
        var totalProbes = 0;
        foreach (var list in _cells.Values)
        {
            totalProbes += list.Count;
        }
        return (_cells.Count, totalProbes);
    }
}
