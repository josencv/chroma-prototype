namespace ChromaPrototype.Color.Shapes;

using Godot;

/// <summary>
/// Base class for all color shape prefabs.
/// Provides common functionality for material assignment, spawner configuration, and editor updates.
/// All derived classes must be [Tool] scripts for editor visibility.
/// </summary>
[Tool]
[GlobalClass]
public abstract partial class ColorShapeBase : Node3D
{
    // Material paths for absorption shader (runtime)
    private static readonly string[] AbsorbMaterialPaths =
    {
        "res://materials/color_palette/M_GridAbsorb_Red.tres",
        "res://materials/color_palette/M_GridAbsorb_Orange.tres",
        "res://materials/color_palette/M_GridAbsorb_Yellow.tres",
        "res://materials/color_palette/M_GridAbsorb_Green.tres",
        "res://materials/color_palette/M_GridAbsorb_Blue.tres",
        "res://materials/color_palette/M_GridAbsorb_Purple.tres"
    };

    // Material paths for simple grid shader (editor preview)
    private static readonly string[] EditorMaterialPaths =
    {
        "res://materials/color_palette/M_Grid_Red.tres",
        "res://materials/color_palette/M_Grid_Orange.tres",
        "res://materials/color_palette/M_Grid_Yellow.tres",
        "res://materials/color_palette/M_Grid_Green.tres",
        "res://materials/color_palette/M_Grid_Blue.tres",
        "res://materials/color_palette/M_Grid_Purple.tres"
    };

    // Cached materials (loaded on demand)
    private static ShaderMaterial?[]? _cachedAbsorbMaterials;
    private static ShaderMaterial?[]? _cachedEditorMaterials;

    private LogicalColor _colorId = LogicalColor.Blue;
    private float _capacity = 10.0f;
    private float _density = 1.0f;
    private float _spacing = 0.5f;
    private short _floorId = -1;
    private float _heightOffset = 0.0f;
    private bool _spawnOnReady = true;

    /// <summary>
    /// The logical color of this shape.
    /// </summary>
    [Export]
    public LogicalColor ColorId
    {
        get => _colorId;
        set
        {
            _colorId = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Capacity per probe.
    /// </summary>
    [Export(PropertyHint.Range, "1,100,1")]
    public float Capacity
    {
        get => _capacity;
        set
        {
            _capacity = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Density multiplier per probe.
    /// </summary>
    [Export(PropertyHint.Range, "0.1,5,0.1")]
    public float Density
    {
        get => _density;
        set
        {
            _density = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Spacing between probes.
    /// </summary>
    [Export(PropertyHint.Range, "0.25,5,0.25")]
    public float Spacing
    {
        get => _spacing;
        set
        {
            _spacing = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Optional floor ID for multi-floor scenarios.
    /// </summary>
    [Export]
    public short FloorId
    {
        get => _floorId;
        set
        {
            _floorId = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Height offset applied to all probes.
    /// </summary>
    [Export(PropertyHint.Range, "-5,5,0.1")]
    public float HeightOffset
    {
        get => _heightOffset;
        set
        {
            _heightOffset = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Whether to spawn probes on ready.
    /// </summary>
    [Export]
    public bool SpawnOnReady
    {
        get => _spawnOnReady;
        set => _spawnOnReady = value;
    }

    /// <summary>
    /// Reference to the mesh instance child.
    /// </summary>
    protected MeshInstance3D? MeshNode { get; private set; }

    /// <summary>
    /// Reference to the probe field spawner child.
    /// </summary>
    protected ProbeFieldSpawner? Spawner { get; private set; }

    /// <summary>
    /// The material instance assigned to this shape (for stamp buffer registration).
    /// </summary>
    protected ShaderMaterial? AssignedMaterial { get; private set; }

    public override void _Ready()
    {
        FindChildNodes();
        UpdateShape();

        if (!Engine.IsEditorHint() && SpawnOnReady)
        {
            SpawnProbes();
            RegisterMaterialWithStampBuffer();
        }
    }

    public override void _EnterTree()
    {
        FindChildNodes();
        UpdateShape();
    }

    /// <summary>
    /// Finds and caches child node references.
    /// </summary>
    protected void FindChildNodes()
    {
        MeshNode = GetNodeOrNull<MeshInstance3D>("MeshInstance3D");
        Spawner = GetNodeOrNull<ProbeFieldSpawner>("ProbeFieldSpawner");
    }

    /// <summary>
    /// Called when any exported property changes.
    /// Triggers a full shape update.
    /// </summary>
    protected virtual void OnPropertyChanged()
    {
        if (!IsInsideTree())
            return;

        FindChildNodes();
        UpdateShape();
    }

    /// <summary>
    /// Updates all aspects of the shape: material, mesh dimensions, and spawner config.
    /// </summary>
    protected void UpdateShape()
    {
        UpdateMaterial();
        UpdateMeshDimensions();
        UpdateSpawner();
    }

    /// <summary>
    /// Updates the material on the mesh based on the current color.
    /// </summary>
    protected void UpdateMaterial()
    {
        if (MeshNode == null)
            return;

        var material = GetMaterialForColor(_colorId);
        if (material != null)
        {
            MeshNode.MaterialOverride = material;
            AssignedMaterial = material;
        }
    }

    /// <summary>
    /// Updates the mesh dimensions. Implemented by derived classes.
    /// </summary>
    protected abstract void UpdateMeshDimensions();

    /// <summary>
    /// Updates the spawner configuration. Implemented by derived classes.
    /// </summary>
    protected abstract void UpdateSpawner();

    /// <summary>
    /// Gets the material for a logical color (with caching).
    /// Uses absorb materials at runtime, editor materials in editor.
    /// </summary>
    protected static ShaderMaterial? GetMaterialForColor(LogicalColor color)
    {
        var index = (int)color;
        if (index < 0 || index >= LogicalColorExtensions.ColorCount)
            return null;

        if (Engine.IsEditorHint())
        {
            // Use simple grid materials in editor
            _cachedEditorMaterials ??= new ShaderMaterial?[LogicalColorExtensions.ColorCount];
            if (_cachedEditorMaterials[index] == null)
            {
                _cachedEditorMaterials[index] = GD.Load<ShaderMaterial>(EditorMaterialPaths[index]);
            }
            return _cachedEditorMaterials[index];
        }
        else
        {
            // Use absorb materials at runtime
            _cachedAbsorbMaterials ??= new ShaderMaterial?[LogicalColorExtensions.ColorCount];
            if (_cachedAbsorbMaterials[index] == null)
            {
                _cachedAbsorbMaterials[index] = GD.Load<ShaderMaterial>(AbsorbMaterialPaths[index]);
            }
            return _cachedAbsorbMaterials[index];
        }
    }

    /// <summary>
    /// Registers the assigned material with the stamp buffer for absorption visuals.
    /// </summary>
    protected void RegisterMaterialWithStampBuffer()
    {
        if (Engine.IsEditorHint() || AssignedMaterial == null)
            return;

        var stampBuffer = FindStampBuffer();
        if (stampBuffer != null)
        {
            stampBuffer.RegisterMaterial(AssignedMaterial);
        }
    }

    /// <summary>
    /// Finds the AbsorptionStampBuffer in the scene.
    /// </summary>
    protected AbsorptionStampBuffer? FindStampBuffer()
    {
        var nodes = GetTree().GetNodesInGroup("absorption_stamp_buffer");
        return nodes.Count > 0 ? nodes[0] as AbsorptionStampBuffer : null;
    }

    /// <summary>
    /// Spawns probes at runtime by finding the ColorFieldRuntime in the scene.
    /// </summary>
    public void SpawnProbes()
    {
        if (Engine.IsEditorHint())
            return;

        if (Spawner == null)
        {
            GD.PrintErr($"[{Name}] No ProbeFieldSpawner child found");
            return;
        }

        var runtime = GetTree().Root.FindChild("ColorFieldRuntime", true, false) as ColorFieldRuntime;
        if (runtime == null)
        {
            // Try to find it in the scene tree
            var nodes = GetTree().GetNodesInGroup("color_field_runtime");
            if (nodes.Count > 0)
            {
                runtime = nodes[0] as ColorFieldRuntime;
            }
        }

        if (runtime != null)
        {
            Spawner.SpawnProbes(runtime);
            GD.Print($"[{Name}] Spawned {Spawner.SpawnedProbeIds.Count} probes");
        }
        else
        {
            GD.PrintErr($"[{Name}] Could not find ColorFieldRuntime in scene");
        }
    }
}
