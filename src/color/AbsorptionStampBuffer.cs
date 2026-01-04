namespace ChromaPrototype.Color;

using Godot;

/// <summary>
/// Recovery mode for visual absorption depletion.
/// </summary>
public enum RecoveryMode
{
    /// <summary>
    /// Mode A: Continuous smooth fade from drained to recovered.
    /// </summary>
    Continuous = 0,

    /// <summary>
    /// Mode B: Recovery in discrete steps at fixed intervals.
    /// </summary>
    Stepped = 1,

    /// <summary>
    /// Mode C: Stays fully drained until recovery time, then snaps back instantly.
    /// </summary>
    AtOnce = 2
}

/// <summary>
/// Represents a single absorption stamp for visual depletion.
/// </summary>
public struct AbsorptionStamp
{
    /// <summary>
    /// Center X position in world space.
    /// </summary>
    public float CenterX;

    /// <summary>
    /// Center Z position in world space.
    /// </summary>
    public float CenterZ;

    /// <summary>
    /// Minimum Y height of the cylinder.
    /// </summary>
    public float YMin;

    /// <summary>
    /// Maximum Y height of the cylinder.
    /// </summary>
    public float YMax;

    /// <summary>
    /// Radius of the cylinder in world units.
    /// </summary>
    public float Radius;

    /// <summary>
    /// Time stamp was created (in game time seconds).
    /// </summary>
    public float T0;

    public AbsorptionStamp(Vector3 center, float radius, float heightDown, float heightUp, float time)
    {
        CenterX = center.X;
        CenterZ = center.Z;
        YMin = center.Y - heightDown;
        YMax = center.Y + heightUp;
        Radius = radius;
        T0 = time;
    }
}

/// <summary>
/// Ring buffer for absorption stamps.
/// Manages stamp data and uploads to shader materials.
/// </summary>
public partial class AbsorptionStampBuffer : Node
{
    /// <summary>
    /// Maximum number of stamps (N = 16).
    /// Must match MAX_STAMPS in absorption_stamps.gdshaderinc.
    /// </summary>
    public const int MaxStamps = 16;

    /// <summary>
    /// Shared settings for recovery and visual depletion.
    /// Should reference the same ColorSystemSettings as ColorFieldRuntime.
    /// </summary>
    [Export]
    public ColorSystemSettings? Settings { get; set; }

    /// <summary>
    /// Enable debug logging.
    /// </summary>
    [Export]
    public bool DebugLogging { get; set; } = false;

    // Ring buffer
    private readonly AbsorptionStamp[] _stamps = new AbsorptionStamp[MaxStamps];
    private int _head = 0;
    private int _count = 0;

    // Shader data arrays (pre-allocated)
    private readonly Vector4[] _stampPosArray = new Vector4[MaxStamps];
    private readonly Vector4[] _stampDataArray = new Vector4[MaxStamps];

    // Cached material references
    private readonly Godot.Collections.Array<ShaderMaterial> _registeredMaterials = new();

    /// <summary>
    /// Gets the current stamp count.
    /// </summary>
    public int StampCount => _count;

    public override void _Ready()
    {
        AddToGroup("absorption_stamp_buffer");

        // Initialize arrays with zeros
        for (var i = 0; i < MaxStamps; i++)
        {
            _stampPosArray[i] = Vector4.Zero;
            _stampDataArray[i] = Vector4.Zero;
        }
    }

    /// <summary>
    /// Registers a material to receive stamp updates.
    /// Call this when creating color shape materials.
    /// </summary>
    public void RegisterMaterial(ShaderMaterial material)
    {
        if (!_registeredMaterials.Contains(material))
        {
            _registeredMaterials.Add(material);

            // Initialize material with current config
            UploadConfigToMaterial(material);

            // Push current stamp data
            UploadToMaterial(material);
        }
    }

    /// <summary>
    /// Unregisters a material from stamp updates.
    /// </summary>
    public void UnregisterMaterial(ShaderMaterial material)
    {
        _registeredMaterials.Remove(material);
    }

    /// <summary>
    /// Updates recovery configuration on all registered materials.
    /// Call this after changing recovery mode or timing parameters.
    /// </summary>
    public void UpdateConfig()
    {
        foreach (var material in _registeredMaterials)
        {
            if (GodotObject.IsInstanceValid(material))
            {
                UploadConfigToMaterial(material);
            }
        }
    }

    private void UploadConfigToMaterial(ShaderMaterial material)
    {
        if (Settings == null)
            return;

        material.SetShaderParameter("u_recovery_mode", (int)Settings.RecoveryMode);
        material.SetShaderParameter("u_recover_seconds", Settings.RecoverySeconds);
        material.SetShaderParameter("u_recovery_delay", Settings.RecoveryDelay);
        material.SetShaderParameter("u_recover_step_interval", Settings.RecoverStepInterval);
        material.SetShaderParameter("u_recover_step_count", Settings.RecoverStepCount);
        material.SetShaderParameter("u_edge_feather", Settings.EdgeFeather);
    }

    /// <summary>
    /// Adds a new absorption stamp from a pulse.
    /// </summary>
    public void AddStamp(Vector3 center, float radius, float heightDown, float heightUp)
    {
        var time = (float)Time.GetTicksMsec() / 1000.0f;
        var stamp = new AbsorptionStamp(center, radius, heightDown, heightUp, time);

        // Write to ring buffer
        _stamps[_head] = stamp;
        _head = (_head + 1) % MaxStamps;
        _count = Mathf.Min(_count + 1, MaxStamps);

        // Update shader arrays
        RebuildShaderArrays();

        // Upload to all registered materials
        UploadToAllMaterials();

        if (DebugLogging)
        {
            GD.Print($"[StampBuffer] Added stamp at ({center.X:F1}, {center.Z:F1}), r={radius:F1}, count={_count}");
        }
    }

    /// <summary>
    /// Adds a stamp using pulse configuration.
    /// </summary>
    public void AddStampFromPulse(Vector3 center, PulseConfig config)
    {
        AddStamp(center, config.Radius, config.HeightDown, config.HeightUp);
    }

    private void RebuildShaderArrays()
    {
        // Convert ring buffer to linear arrays for shader
        for (var i = 0; i < MaxStamps; i++)
        {
            var stamp = _stamps[i];

            // Pack position data: (centerX, centerZ, yMin, yMax)
            _stampPosArray[i] = new Vector4(stamp.CenterX, stamp.CenterZ, stamp.YMin, stamp.YMax);

            // Pack stamp data: (radius, t0, unused, unused)
            _stampDataArray[i] = new Vector4(stamp.Radius, stamp.T0, 0, 0);
        }
    }

    private void UploadToAllMaterials()
    {
        // Remove any freed materials
        for (var i = _registeredMaterials.Count - 1; i >= 0; i--)
        {
            if (!GodotObject.IsInstanceValid(_registeredMaterials[i]))
            {
                _registeredMaterials.RemoveAt(i);
            }
        }

        foreach (var material in _registeredMaterials)
        {
            UploadToMaterial(material);
        }
    }

    private void UploadToMaterial(ShaderMaterial material)
    {
        material.SetShaderParameter("u_stamp_count", _count);
        material.SetShaderParameter("u_stamp_pos", _stampPosArray);
        material.SetShaderParameter("u_stamp_data", _stampDataArray);
    }

    /// <summary>
    /// Clears all stamps.
    /// </summary>
    public void Clear()
    {
        _head = 0;
        _count = 0;

        for (var i = 0; i < MaxStamps; i++)
        {
            _stamps[i] = default;
            _stampPosArray[i] = Vector4.Zero;
            _stampDataArray[i] = Vector4.Zero;
        }

        UploadToAllMaterials();
    }

    /// <summary>
    /// Gets debug info about the buffer state.
    /// </summary>
    public string GetDebugInfo()
    {
        var modeStr = Settings?.RecoveryMode switch
        {
            RecoveryMode.Continuous => "Continuous",
            RecoveryMode.Stepped => $"Stepped({Settings?.RecoverStepCount}x{Settings?.RecoverStepInterval:F1}s)",
            RecoveryMode.AtOnce => "AtOnce",
            _ => "Unknown"
        };
        return $"Stamps: {_count}/{MaxStamps}, Mode: {modeStr}, Materials: {_registeredMaterials.Count}";
    }
}
