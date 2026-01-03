namespace ChromaPrototype.Color;

using Godot;

/// <summary>
/// Simple debug HUD for the color system.
/// Shows stored colors and recent pulse info.
/// </summary>
public partial class ColorDebugHud : Control
{
    /// <summary>
    /// Reference to the ColorAbsorber to display info for.
    /// </summary>
    [Export]
    public ColorAbsorber? Absorber { get; set; }

    /// <summary>
    /// Reference to the ColorFieldRuntime for field stats.
    /// </summary>
    [Export]
    public ColorFieldRuntime? ColorField { get; set; }

    /// <summary>
    /// Toggle key to show/hide the HUD.
    /// </summary>
    [Export]
    public Key ToggleKey { get; set; } = Key.F3;

    /// <summary>
    /// Whether the HUD info is currently shown.
    /// </summary>
    [Export]
    public bool ShowInfo { get; set; } = true;

    private Panel? _panel;
    private Label? _label;
    private string _lastPulseInfo = "";
    private double _pulseInfoTimer;

    public override void _Ready()
    {
        // Create semi-transparent background panel
        _panel = new Panel();
        _panel.Position = new Vector2(5, 5);
        _panel.CustomMinimumSize = new Vector2(300, 200);

        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0, 0, 0, 0.7f); // Semi-transparent black
        styleBox.CornerRadiusTopLeft = 4;
        styleBox.CornerRadiusTopRight = 4;
        styleBox.CornerRadiusBottomLeft = 4;
        styleBox.CornerRadiusBottomRight = 4;
        _panel.AddThemeStyleboxOverride("panel", styleBox);
        AddChild(_panel);

        _label = new Label();
        _label.Position = new Vector2(10, 10);
        _label.AddThemeColorOverride("font_color", Colors.White);
        _label.AddThemeColorOverride("font_shadow_color", Colors.Black);
        _label.AddThemeFontSizeOverride("font_size", 14);
        _panel.AddChild(_label);

        // Try to auto-find components
        if (Absorber == null)
        {
            var character = GetTree().Root.FindChild("Character", true, false);
            if (character != null)
            {
                Absorber = character.GetNodeOrNull<ColorAbsorber>("ColorAbsorber");
            }
        }

        if (ColorField == null)
        {
            ColorField = GetTree().Root.FindChild("ColorFieldRuntime", true, false) as ColorFieldRuntime;
        }

        // Connect to absorber signal if available
        if (Absorber != null)
        {
            Absorber.Absorbed += OnAbsorbed;
        }
    }

    public override void _ExitTree()
    {
        if (Absorber != null)
        {
            Absorber.Absorbed -= OnAbsorbed;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == ToggleKey)
        {
            ShowInfo = !ShowInfo;
        }
    }

    public override void _Process(double delta)
    {
        if (_panel == null || _label == null)
            return;

        _panel.Visible = ShowInfo;

        if (!ShowInfo)
            return;

        // Fade out pulse info
        if (_pulseInfoTimer > 0)
        {
            _pulseInfoTimer -= delta;
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        var text = "[Color Debug - F3 to toggle]\n";
        text += "─────────────────────────\n";

        if (Absorber != null)
        {
            text += "STORED COLORS:\n";
            for (var i = 0; i < LogicalColorExtensions.ColorCount; i++)
            {
                var amount = Absorber.StoredColors[i];
                if (amount > 0.01f)
                {
                    var color = (LogicalColor)i;
                    text += $"  {color}: {amount:F1}\n";
                }
            }

            var total = Absorber.GetTotalStored();
            text += $"  Total: {total:F1}\n";
            text += "\n";
        }

        if (ColorField != null)
        {
            text += $"FIELD: {ColorField.ProbeCount} probes\n";
        }

        if (_pulseInfoTimer > 0 && !string.IsNullOrEmpty(_lastPulseInfo))
        {
            text += "\n";
            text += _lastPulseInfo;
        }

        text += "\n[E] to absorb";

        _label!.Text = text;
    }

    private void OnAbsorbed(float[] takenPerColor, float totalTaken)
    {
        if (totalTaken < 0.01f)
        {
            _lastPulseInfo = "PULSE: nothing absorbed";
        }
        else
        {
            _lastPulseInfo = $"PULSE: +{totalTaken:F1} total\n";
            for (var i = 0; i < takenPerColor.Length; i++)
            {
                if (takenPerColor[i] > 0.01f)
                {
                    var color = (LogicalColor)i;
                    _lastPulseInfo += $"  +{takenPerColor[i]:F1} {color}\n";
                }
            }
        }
        _pulseInfoTimer = 3.0; // Show for 3 seconds
    }
}
