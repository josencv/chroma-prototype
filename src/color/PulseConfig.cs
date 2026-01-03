namespace ChromaPrototype.Color;

/// <summary>
/// Configuration for an absorption pulse.
/// </summary>
public readonly struct PulseConfig
{
    /// <summary>
    /// XZ radius of the absorption cylinder.
    /// </summary>
    public readonly float Radius;

    /// <summary>
    /// Height above pulse center to include.
    /// </summary>
    public readonly float HeightUp;

    /// <summary>
    /// Height below pulse center to include.
    /// </summary>
    public readonly float HeightDown;

    /// <summary>
    /// Maximum total amount to absorb across all colors.
    /// </summary>
    public readonly float MaxTakeTotal;

    /// <summary>
    /// Optional floor id filter. -1 means no floor filter.
    /// </summary>
    public readonly short FloorIdFilter;

    public PulseConfig(
        float radius = 3.0f,
        float heightUp = 2.0f,
        float heightDown = 1.0f,
        float maxTakeTotal = 5.0f,
        short floorIdFilter = -1)
    {
        Radius = radius;
        HeightUp = heightUp;
        HeightDown = heightDown;
        MaxTakeTotal = maxTakeTotal;
        FloorIdFilter = floorIdFilter;
    }

    /// <summary>
    /// Default pulse configuration.
    /// </summary>
    public static PulseConfig Default => new(
        radius: 3.0f,
        heightUp: 2.0f,
        heightDown: 1.0f,
        maxTakeTotal: 5.0f
    );
}
