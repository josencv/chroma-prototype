using Godot;

namespace ChromaPrototype;

/// <summary>
/// Simple camera that follows a target at a fixed offset with quasi-orthogonal settings.
/// </summary>
public partial class FollowCamera3D : Camera3D
{
    [Export] public Node3D? Target { get; set; }

    /// <summary>Distance from target (on the XZ plane projection).</summary>
    [Export] public float Distance { get; set; } = 30.0f;

    /// <summary>Camera pitch in degrees (45-55 for quasi-isometric look).</summary>
    [Export(PropertyHint.Range, "30,80")] public float PitchDegrees { get; set; } = 50.0f;

    /// <summary>Camera yaw in degrees (0 = looking along -Z).</summary>
    [Export] public float YawDegrees { get; set; } = 0.0f;

    /// <summary>Vertical offset from target position.</summary>
    [Export] public Vector3 TargetOffset { get; set; } = new(0, 0.5f, 0);

    /// <summary>Smoothing factor for camera movement (0 = instant, higher = smoother).</summary>
    [Export(PropertyHint.Range, "0,20")] public float Smoothing { get; set; } = 8.0f;

    private Vector3 _currentPosition;

    public override void _Ready()
    {
        _currentPosition = GlobalPosition;
        // Set initial rotation from pitch/yaw
        SetRotationFromAngles();
    }

    public override void _Process(double delta)
    {
        if (Target == null) return;

        var desiredPosition = CalculateDesiredPosition();

        // Smooth position movement
        if (Smoothing > 0)
        {
            _currentPosition = _currentPosition.Lerp(desiredPosition, (float)(Smoothing * delta));
        }
        else
        {
            _currentPosition = desiredPosition;
        }

        GlobalPosition = _currentPosition;

        // Set rotation directly from pitch/yaw instead of using LookAt
        // This keeps the camera orientation fixed regardless of smoothing
        SetRotationFromAngles();
    }

    private void SetRotationFromAngles()
    {
        var pitchRad = Mathf.DegToRad(PitchDegrees);
        var yawRad = Mathf.DegToRad(YawDegrees);

        // Build rotation from yaw (Y-axis) and pitch (X-axis), no roll
        Rotation = new Vector3(-pitchRad, yawRad, 0);
    }

    private Vector3 CalculateDesiredPosition()
    {
        if (Target == null) return GlobalPosition;

        var targetPos = Target.GlobalPosition + TargetOffset;

        var pitchRad = Mathf.DegToRad(PitchDegrees);
        var yawRad = Mathf.DegToRad(YawDegrees);

        // Calculate offset from target
        // At yaw=0, camera looks along -Z, so it's positioned at +Z from target
        var horizontalDist = Distance * Mathf.Cos(pitchRad);
        var verticalDist = Distance * Mathf.Sin(pitchRad);

        var offset = new Vector3(
            horizontalDist * Mathf.Sin(yawRad),
            verticalDist,
            horizontalDist * Mathf.Cos(yawRad)
        );

        return targetPos + offset;
    }
}
