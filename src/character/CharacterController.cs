namespace ChromaPrototype.Character;

using Chickensoft.LogicBlocks;
using Godot;

/// <summary>
/// Character controller using CharacterBody3D with LogicBlocks state machine.
/// Handles input (keyboard + gamepad) and transforms it to camera-relative world space.
/// </summary>
public partial class CharacterController : CharacterBody3D
{
    private const float StickDeadzone = 0.2f;

    [Export] public float MaxSpeed { get; set; } = 6.0f;
    [Export] public float Acceleration { get; set; } = 50.0f;
    [Export] public float Deceleration { get; set; } = 80.0f;
    [Export] public float MoveThreshold { get; set; } = 0.1f;

    /// <summary>
    /// Reference to the camera for computing camera-relative movement.
    /// </summary>
    [Export] public Camera3D? Camera { get; set; }

    private CharacterLogic _logic = null!;
    private LogicBlock<CharacterLogic.State>.IBinding _binding = null!;
    private Vector3 _desiredVelocity;

    public override void _Ready()
    {
        _logic = new CharacterLogic();
        _binding = _logic.Bind();

        _binding.Handle((in CharacterLogic.Output.DesiredVelocityChanged output) =>
        {
            _desiredVelocity = output.Velocity;
        });

        _logic.Start();
    }

    public override void _ExitTree()
    {
        _binding.Dispose();
    }

    public override void _PhysicsProcess(double delta)
    {
        // Read and transform input
        var inputDirection = GetInputDirection();
        var worldDirection = TransformToWorldSpace(inputDirection);

        // Update direction in logic
        _logic.Input(new CharacterLogic.Input.SetDesiredDirection(worldDirection));

        // Tick the logic
        var settings = new CharacterLogic.Settings(MaxSpeed, MoveThreshold);
        _logic.Input(new CharacterLogic.Input.Tick(delta, settings));

        // Smooth velocity toward desired
        var rate = _desiredVelocity.LengthSquared() > 0.0001f ? Acceleration : Deceleration;
        Velocity = Velocity.MoveToward(_desiredVelocity, (float)(rate * delta));

        // Apply gravity (simple)
        if (!IsOnFloor())
        {
            Velocity += GetGravity() * (float)delta;
        }

        MoveAndSlide();

        // Rotate character to face movement direction
        if (_desiredVelocity.LengthSquared() > 0.01f)
        {
            var targetRotation = Mathf.Atan2(_desiredVelocity.X, _desiredVelocity.Z);
            Rotation = new Vector3(0, targetRotation, 0);
        }
    }

    /// <summary>
    /// Gets raw input direction using Godot's input action system.
    /// Returns screen-space direction: X = strafe (left/right), Y = forward/back.
    /// Y &lt; 0 = forward (W/up), Y &gt; 0 = backward (S/down).
    /// </summary>
    private Vector2 GetInputDirection()
    {
        var direction = new Vector2(
            Input.GetAxis("move_left", "move_right"),
            Input.GetAxis("move_forward", "move_backward")
        );

        // Normalize if > 1 (can happen with keyboard diagonal)
        if (direction.LengthSquared() > 1f)
        {
            direction = direction.Normalized();
        }

        return direction;
    }

    /// <summary>
    /// Transforms screen-relative input to world-space direction using camera orientation.
    /// </summary>
    /// <param name="input">
    /// Screen-relative input where X = strafe, Y = forward/back (Y &lt; 0 = forward).
    /// </param>
    /// <returns>
    /// World-space direction as Vector2 where X = world X, Y = world Z.
    /// </returns>
    private Vector2 TransformToWorldSpace(Vector2 input)
    {
        if (input.LengthSquared() < 0.0001f)
        {
            return Vector2.Zero;
        }

        // Get camera's forward and right vectors projected onto the XZ plane
        var (forward, right) = GetCameraPlanarVectors();

        // Transform: world = right * input.X + forward * (-input.Y)
        // Negate Y because input uses Y < 0 for forward, but we want forward direction
        var worldDir3 = right * input.X + forward * (-input.Y);

        // Normalize if needed
        if (worldDir3.LengthSquared() > 1f)
        {
            worldDir3 = worldDir3.Normalized();
        }

        // Return as Vector2 (X = world X, Y = world Z)
        return new Vector2(worldDir3.X, worldDir3.Z);
    }

    /// <summary>
    /// Gets the camera's forward and right vectors projected onto the XZ plane.
    /// </summary>
    private (Vector3 Forward, Vector3 Right) GetCameraPlanarVectors()
    {
        if (Camera == null)
        {
            // Fallback: identity (forward = -Z, right = +X)
            return (new Vector3(0, 0, -1), new Vector3(1, 0, 0));
        }

        // Get camera's global transform basis
        var basis = Camera.GlobalTransform.Basis;

        // Camera's forward is -Z in local space
        var cameraForward = -basis.Z;
        // Camera's right is +X in local space
        var cameraRight = basis.X;

        // Project onto XZ plane and normalize
        var forward = new Vector3(cameraForward.X, 0, cameraForward.Z);
        if (forward.LengthSquared() > 0.0001f)
        {
            forward = forward.Normalized();
        }
        else
        {
            forward = new Vector3(0, 0, -1); // Fallback if camera is looking straight down
        }

        var right = new Vector3(cameraRight.X, 0, cameraRight.Z);
        if (right.LengthSquared() > 0.0001f)
        {
            right = right.Normalized();
        }
        else
        {
            right = new Vector3(1, 0, 0);
        }

        return (forward, right);
    }
}
