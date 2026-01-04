namespace ChromaPrototype.Color;

using Godot;

/// <summary>
/// Shared configuration for the color probe and absorption visual system.
/// Create one instance and reference it from both ColorFieldRuntime and AbsorptionStampBuffer.
/// </summary>
[GlobalClass]
public partial class ColorSystemSettings : Resource
{
    /// <summary>
    /// Time in seconds for full recovery (both gameplay probes and visual).
    /// </summary>
    [Export(PropertyHint.Range, "1,60,0.5")]
    public float RecoverySeconds { get; set; } = 9.0f;

    /// <summary>
    /// Delay in seconds before continuous recovery starts (Continuous mode only).
    /// The surface stays fully drained for this duration before fading back.
    /// </summary>
    [Export(PropertyHint.Range, "0,60,0.5")]
    public float RecoveryDelay { get; set; } = 1.0f;

    /// <summary>
    /// Recovery mode for visual absorption depletion.
    /// </summary>
    [Export]
    public RecoveryMode RecoveryMode { get; set; } = RecoveryMode.Continuous;

    /// <summary>
    /// Interval between recovery steps in seconds (Stepped mode).
    /// </summary>
    [Export(PropertyHint.Range, "0.1,5,0.1")]
    public float RecoverStepInterval { get; set; } = 3f;

    /// <summary>
    /// Number of steps to full recovery (Stepped mode).
    /// </summary>
    [Export(PropertyHint.Range, "2,16,1")]
    public int RecoverStepCount { get; set; } = 3;

    /// <summary>
    /// Edge feather amount in world units for visual depletion.
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.1")]
    public float EdgeFeather { get; set; } = 0.3f;
}
