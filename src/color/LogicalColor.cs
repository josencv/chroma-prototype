namespace ChromaPrototype.Color;

/// <summary>
/// Logical color enum representing the 6 core colors in the game.
/// Values 0..(K-1) where K = 6.
/// </summary>
public enum LogicalColor : byte
{
    Red = 0,
    Orange = 1,
    Yellow = 2,
    Green = 3,
    Blue = 4,
    Purple = 5
}

public static class LogicalColorExtensions
{
    public const int ColorCount = 6;

    /// <summary>
    /// Returns the Godot Color associated with this logical color (for debug visualization).
    /// </summary>
    public static Godot.Color ToGodotColor(this LogicalColor color)
    {
        return color switch
        {
            LogicalColor.Red => new Godot.Color(1.0f, 0.2f, 0.2f),
            LogicalColor.Orange => new Godot.Color(1.0f, 0.6f, 0.2f),
            LogicalColor.Yellow => new Godot.Color(1.0f, 1.0f, 0.2f),
            LogicalColor.Green => new Godot.Color(0.2f, 1.0f, 0.2f),
            LogicalColor.Blue => new Godot.Color(0.2f, 0.4f, 1.0f),
            LogicalColor.Purple => new Godot.Color(0.7f, 0.2f, 1.0f),
            _ => Godot.Colors.White
        };
    }
}
