namespace ChromaPrototype.Color;

using Godot;

public enum DebugDrawMode
{
    Points,
    WireSpheres
}

/// <summary>
/// Debug visualization for the color probe system.
/// Draws probe gizmos in the 3D world.
/// Add this as a child of any Node in your scene and it will automatically find ColorFieldRuntime.
/// </summary>
public partial class ColorFieldDebugDraw : Node
{
    /// <summary>
    /// Reference to the ColorFieldRuntime.
    /// </summary>
    [Export]
    public ColorFieldRuntime? ColorField { get; set; }

    /// <summary>
    /// Whether to auto-find ColorFieldRuntime if not assigned.
    /// </summary>
    [Export]
    public bool AutoFindColorField { get; set; } = true;

    /// <summary>
    /// Draw mode for probes.
    /// </summary>
    [Export]
    public DebugDrawMode DrawMode { get; set; } = DebugDrawMode.Points;

    /// <summary>
    /// Base size of probe visualization.
    /// </summary>
    [Export(PropertyHint.Range, "0.1,1,0.05")]
    public float ProbeSize { get; set; } = 0.25f;

    /// <summary>
    /// Whether to scale probes by their fill ratio.
    /// </summary>
    [Export]
    public bool ScaleByFill { get; set; } = true;

    /// <summary>
    /// Minimum scale when probe is empty.
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.1")]
    public float MinFillScale { get; set; } = 0.3f;

    /// <summary>
    /// Enable spatial culling (only draw probes near camera).
    /// </summary>
    [Export]
    public bool UseSpatialCulling { get; set; } = true;

    /// <summary>
    /// Maximum distance from camera to draw probes.
    /// </summary>
    [Export(PropertyHint.Range, "5,100,5")]
    public float CullDistance { get; set; } = 30.0f;

    private ImmediateMesh? _mesh;
    private MeshInstance3D? _meshInstance;
    private StandardMaterial3D? _material;
    private Camera3D? _camera;

    public override void _Ready()
    {
        // Auto-find ColorFieldRuntime if not assigned
        if (ColorField == null && AutoFindColorField)
        {
            var nodes = GetTree().GetNodesInGroup("color_field_runtime");
            if (nodes.Count > 0)
            {
                ColorField = nodes[0] as ColorFieldRuntime;
            }

            if (ColorField == null)
            {
                GD.PrintErr("[ColorFieldDebugDraw] ColorFieldRuntime not found!");
            }
        }

        _mesh = new ImmediateMesh();
        _meshInstance = new MeshInstance3D
        {
            Mesh = _mesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        AddChild(_meshInstance);

        _material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo = true,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha
        };

        if (DrawMode == DebugDrawMode.Points)
        {
            _material.UsePointSize = true;
            _material.PointSize = 4.0f;
        }
    }

    public override void _Process(double delta)
    {
        if (ColorField == null || _mesh == null || _material == null)
            return;

        // Find camera if using spatial culling
        if (UseSpatialCulling && _camera == null)
        {
            _camera = GetViewport()?.GetCamera3D();
        }

        DrawProbes();
    }

    private void DrawProbes()
    {
        _mesh!.ClearSurfaces();

        var probes = ColorField!.Probes;
        if (probes.Count == 0)
            return;

        Vector3? cameraPos = null;
        if (UseSpatialCulling && _camera != null)
        {
            cameraPos = _camera.GlobalPosition;
        }

        var primitiveType = DrawMode == DebugDrawMode.Points ? Mesh.PrimitiveType.Points : Mesh.PrimitiveType.Lines;
        _mesh.SurfaceBegin(primitiveType, _material);

        var vertexCount = 0;

        foreach (var probe in probes)
        {
            if (!probe.HasRemaining)
                continue;

            // Spatial culling
            if (cameraPos.HasValue)
            {
                var dist = probe.Position.DistanceTo(cameraPos.Value);
                if (dist > CullDistance)
                    continue;
            }

            var color = probe.Color.ToGodotColor();
            color.A = 0.8f * probe.FillRatio + 0.2f;

            var size = ProbeSize;
            if (ScaleByFill)
            {
                size *= Mathf.Lerp(MinFillScale, 1f, probe.FillRatio);
            }

            if (DrawMode == DebugDrawMode.Points)
            {
                DrawPoint(probe.Position, size, color);
                vertexCount++;
            }
            else
            {
                vertexCount += DrawWireSphere(probe.Position, size, color);
            }
        }

        // Only end surface if we actually added vertices
        if (vertexCount > 0)
        {
            _mesh.SurfaceEnd();
        }
    }

    private void DrawPoint(Vector3 position, float size, Color color)
    {
        _mesh!.SurfaceSetColor(color);
        _mesh.SurfaceAddVertex(position);
    }

    private int DrawWireSphere(Vector3 center, float radius, Color color)
    {
        const int segments = 12;
        var angleStep = Mathf.Tau / segments;
        var vertexCount = 0;

        // Draw 3 circles (XY, XZ, YZ planes)
        // XZ plane (horizontal)
        for (var i = 0; i < segments; i++)
        {
            var a1 = i * angleStep;
            var a2 = (i + 1) * angleStep;
            var p1 = center + new Vector3(Mathf.Cos(a1) * radius, 0, Mathf.Sin(a1) * radius);
            var p2 = center + new Vector3(Mathf.Cos(a2) * radius, 0, Mathf.Sin(a2) * radius);
            _mesh!.SurfaceSetColor(color);
            _mesh.SurfaceAddVertex(p1);
            _mesh.SurfaceSetColor(color);
            _mesh.SurfaceAddVertex(p2);
            vertexCount += 2;
        }

        // XY plane (vertical front)
        for (var i = 0; i < segments; i++)
        {
            var a1 = i * angleStep;
            var a2 = (i + 1) * angleStep;
            var p1 = center + new Vector3(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius, 0);
            var p2 = center + new Vector3(Mathf.Cos(a2) * radius, Mathf.Sin(a2) * radius, 0);
            _mesh!.SurfaceSetColor(color);
            _mesh.SurfaceAddVertex(p1);
            _mesh.SurfaceSetColor(color);
            _mesh.SurfaceAddVertex(p2);
            vertexCount += 2;
        }

        // YZ plane (vertical side)
        for (var i = 0; i < segments; i++)
        {
            var a1 = i * angleStep;
            var a2 = (i + 1) * angleStep;
            var p1 = center + new Vector3(0, Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius);
            var p2 = center + new Vector3(0, Mathf.Cos(a2) * radius, Mathf.Sin(a2) * radius);
            _mesh!.SurfaceSetColor(color);
            _mesh.SurfaceAddVertex(p1);
            _mesh.SurfaceSetColor(color);
            _mesh.SurfaceAddVertex(p2);
            vertexCount += 2;
        }

        return vertexCount;
    }
}
