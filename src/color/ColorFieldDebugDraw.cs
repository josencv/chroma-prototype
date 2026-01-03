namespace ChromaPrototype.Color;

using Godot;

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
    /// Base size of probe spheres.
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
    /// Whether to show labels with remaining amount.
    /// </summary>
    [Export]
    public bool ShowLabels { get; set; } = false;

    private ImmediateMesh? _mesh;
    private MeshInstance3D? _meshInstance;
    private StandardMaterial3D? _material;

    public override void _Ready()
    {
        // Auto-find ColorFieldRuntime if not assigned
        if (ColorField == null && AutoFindColorField)
        {
            ColorField = GetTree().Root.FindChild("ColorFieldRuntime", true, false) as ColorFieldRuntime;
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
    }

    public override void _Process(double delta)
    {
        if (ColorField == null || _mesh == null || _material == null)
            return;

        DrawProbes();
    }

    private void DrawProbes()
    {
        _mesh!.ClearSurfaces();

        var probes = ColorField!.Probes;
        if (probes.Count == 0)
            return;

        _mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, _material);

        foreach (var probe in probes)
        {
            if (!probe.HasRemaining)
                continue;

            var color = probe.Color.ToGodotColor();
            color.A = 0.8f * probe.FillRatio + 0.2f; // Fade out as it depletes

            var size = ProbeSize;
            if (ScaleByFill)
            {
                size *= Mathf.Lerp(MinFillScale, 1f, probe.FillRatio);
            }

            DrawWireSphere(probe.Position, size, color);
        }

        _mesh.SurfaceEnd();
    }

    private void DrawWireSphere(Vector3 center, float radius, Color color)
    {
        const int segments = 12;
        var angleStep = Mathf.Tau / segments;

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
        }
    }

    /// <summary>
    /// Draws a debug cylinder at the given position (for pulse visualization).
    /// Call from external code when pulsing.
    /// </summary>
    public void DrawPulseCylinder(Vector3 center, float radius, float heightUp, float heightDown, Color color)
    {
        if (_mesh == null || _material == null)
            return;

        // This could be enhanced to show a brief visual on pulse
        // For now, the probes themselves update their appearance
    }
}
