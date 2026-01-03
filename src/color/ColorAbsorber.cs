namespace ChromaPrototype.Color;

using Godot;

/// <summary>
/// Component that enables an entity (typically the player) to absorb color from probes.
/// Attach to a Node3D (like CharacterBody3D) to enable absorption.
/// </summary>
public partial class ColorAbsorber : Node
{
    /// <summary>
    /// Reference to the ColorFieldRuntime.
    /// </summary>
    [Export]
    public ColorFieldRuntime? ColorField { get; set; }

    /// <summary>
    /// XZ radius of the absorption cylinder.
    /// </summary>
    [Export(PropertyHint.Range, "1,10,0.5")]
    public float PulseRadius { get; set; } = 3.0f;

    /// <summary>
    /// Height above absorber center to include.
    /// </summary>
    [Export(PropertyHint.Range, "0,5,0.5")]
    public float HeightUp { get; set; } = 2.0f;

    /// <summary>
    /// Height below absorber center to include.
    /// </summary>
    [Export(PropertyHint.Range, "0,5,0.5")]
    public float HeightDown { get; set; } = 1.0f;

    /// <summary>
    /// Maximum total amount to absorb per pulse.
    /// </summary>
    [Export(PropertyHint.Range, "1,50,1")]
    public float MaxTakePerPulse { get; set; } = 5.0f;

    /// <summary>
    /// Cooldown between pulses in seconds.
    /// </summary>
    [Export(PropertyHint.Range, "0,5,0.1")]
    public float PulseCooldown { get; set; } = 0.5f;

    /// <summary>
    /// Input action name for triggering absorption.
    /// </summary>
    [Export]
    public string AbsorbAction { get; set; } = "absorb";

    /// <summary>
    /// Whether to auto-find ColorFieldRuntime if not assigned.
    /// </summary>
    [Export]
    public bool AutoFindColorField { get; set; } = true;

    /// <summary>
    /// Current stored color amounts (indexed by LogicalColor).
    /// </summary>
    public float[] StoredColors { get; private set; } = new float[LogicalColorExtensions.ColorCount];

    /// <summary>
    /// Event emitted when absorption occurs.
    /// </summary>
    [Signal]
    public delegate void AbsorbedEventHandler(float[] takenPerColor, float totalTaken);

    private double _cooldownRemaining;
    private Node3D? _parent;

    public override void _Ready()
    {
        _parent = GetParent<Node3D>();
        if (_parent == null)
        {
            GD.PrintErr("[ColorAbsorber] Must be child of a Node3D!");
            return;
        }

        if (ColorField == null && AutoFindColorField)
        {
            ColorField = GetTree().Root.FindChild("ColorFieldRuntime", true, false) as ColorFieldRuntime;
            if (ColorField == null)
            {
                // Try to find by type in scene
                var nodes = GetTree().GetNodesInGroup("color_field_runtime");
                if (nodes.Count > 0 && nodes[0] is ColorFieldRuntime cf)
                {
                    ColorField = cf;
                }
            }
        }

        if (!InputMap.HasAction(AbsorbAction))
        {
            GD.PrintErr($"[ColorAbsorber] Input action '{AbsorbAction}' not defined in project settings!");
        }
    }

    public override void _Process(double delta)
    {
        if (_cooldownRemaining > 0)
        {
            _cooldownRemaining -= delta;
        }

        if (InputMap.HasAction(AbsorbAction) && Input.IsActionJustPressed(AbsorbAction))
        {
            TryPulse();
        }
    }

    /// <summary>
    /// Attempts to perform an absorption pulse. Returns true if successful.
    /// </summary>
    public bool TryPulse()
    {
        if (_cooldownRemaining > 0)
        {
            GD.Print($"[ColorAbsorber] On cooldown: {_cooldownRemaining:F2}s remaining");
            return false;
        }

        if (ColorField == null)
        {
            GD.PrintErr("[ColorAbsorber] No ColorFieldRuntime assigned!");
            return false;
        }

        if (_parent == null)
            return false;

        var center = _parent.GlobalPosition;
        var config = new PulseConfig(
            radius: PulseRadius,
            heightUp: HeightUp,
            heightDown: HeightDown,
            maxTakeTotal: MaxTakePerPulse
        );

        var result = ColorField.PulseAbsorb(center, config);

        // Add to stored colors
        for (var i = 0; i < LogicalColorExtensions.ColorCount; i++)
        {
            StoredColors[i] += result.TakenPerColor[i];
        }

        // Start cooldown
        _cooldownRemaining = PulseCooldown;

        // Emit signal
        EmitSignal(SignalName.Absorbed, result.TakenPerColor, result.TotalTaken);

        return result.TotalTaken > 0;
    }

    /// <summary>
    /// Gets the total amount of a specific color stored.
    /// </summary>
    public float GetStoredColor(LogicalColor color)
    {
        return StoredColors[(int)color];
    }

    /// <summary>
    /// Gets the total amount of all colors stored.
    /// </summary>
    public float GetTotalStored()
    {
        var total = 0f;
        foreach (var amount in StoredColors)
        {
            total += amount;
        }
        return total;
    }

    /// <summary>
    /// Spends a specific amount of color. Returns actual amount spent.
    /// </summary>
    public float SpendColor(LogicalColor color, float amount)
    {
        var index = (int)color;
        var available = StoredColors[index];
        var spent = Mathf.Min(available, amount);
        StoredColors[index] -= spent;
        return spent;
    }

    /// <summary>
    /// Returns debug info about stored colors.
    /// </summary>
    public string GetDebugInfo()
    {
        var info = "Stored: ";
        for (var i = 0; i < LogicalColorExtensions.ColorCount; i++)
        {
            if (StoredColors[i] > 0.01f)
            {
                info += $"{(LogicalColor)i}:{StoredColors[i]:F1} ";
            }
        }
        if (_cooldownRemaining > 0)
        {
            info += $"[CD:{_cooldownRemaining:F1}s]";
        }
        return info;
    }
}
