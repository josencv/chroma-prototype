namespace ChromaPrototype.Color.Shapes;

using Godot;

/// <summary>
/// A vertical pillar-shaped color object. Uses a cylinder mesh.
/// Probes spawn on the top surface.
/// </summary>
[Tool]
[GlobalClass]
public partial class ColorPillar : ColorShapeBase
{
    private float _radius = 0.5f;
    private float _height = 2.0f;

    /// <summary>
    /// Radius of the pillar.
    /// </summary>
    [Export(PropertyHint.Range, "0.25,5,0.25")]
    public float Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Height of the pillar.
    /// </summary>
    [Export(PropertyHint.Range, "0.5,20,0.5")]
    public float Height
    {
        get => _height;
        set
        {
            _height = value;
            OnPropertyChanged();
        }
    }

    protected override void UpdateMeshDimensions()
    {
        if (MeshNode == null)
            return;

        // Create or update cylinder mesh
        if (MeshNode.Mesh is not CylinderMesh cylinder)
        {
            cylinder = new CylinderMesh();
            MeshNode.Mesh = cylinder;
        }

        cylinder.TopRadius = _radius;
        cylinder.BottomRadius = _radius;
        cylinder.Height = _height;
        cylinder.RadialSegments = 16;

        // Position mesh so bottom is at origin
        MeshNode.Position = new Vector3(0, _height * 0.5f, 0);
    }

    protected override void UpdateSpawner()
    {
        if (Spawner == null)
            return;

        // Probes spawn on top surface
        Spawner.Shape = SpawnShape.Disk;
        Spawner.Radius = _radius;
        Spawner.Spacing = Spacing;
        Spawner.Color = ColorId;
        Spawner.Capacity = Capacity;
        Spawner.Density = Density;
        Spawner.FloorId = FloorId;
        // Place probes at top of pillar
        Spawner.HeightOffset = _height + HeightOffset;
    }
}
