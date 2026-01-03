namespace ChromaPrototype.Color;

using Godot;

/// <summary>
/// Marker node for authoring color probes in the editor.
/// Place these in scenes to define where color probes exist.
/// At runtime, ColorFieldRuntime scans for these markers and converts them to runtime probes.
/// </summary>
[Tool]
[GlobalClass]
public partial class ColorProbeMarker : Node3D
{
    /// <summary>
    /// The logical color this probe contains.
    /// </summary>
    [Export]
    public LogicalColor Color { get; set; } = LogicalColor.Blue;

    /// <summary>
    /// Maximum amount of color this probe can hold.
    /// </summary>
    [Export(PropertyHint.Range, "0.1,100,0.1")]
    public float Capacity { get; set; } = 10.0f;

    /// <summary>
    /// Richness multiplier - how much color is absorbed per pulse.
    /// Higher density = more color taken per pulse.
    /// </summary>
    [Export(PropertyHint.Range, "0.1,5,0.1")]
    public float Density { get; set; } = 1.0f;

    /// <summary>
    /// Optional floor identifier for multi-floor scenarios.
    /// Use -1 for no floor restriction.
    /// </summary>
    [Export]
    public short FloorId { get; set; } = -1;

    /// <summary>
    /// Debug sphere radius for editor visualization.
    /// </summary>
    [Export(PropertyHint.Range, "0.1,2,0.1")]
    public float DebugRadius { get; set; } = 0.3f;

    /// <summary>
    /// Whether to show debug visualization in editor.
    /// </summary>
    [Export]
    public bool ShowDebug { get; set; } = true;

    public override void _Ready()
    {
        // Add to group so ColorFieldRuntime can find all markers
        AddToGroup("color_probe_markers");
    }


}
