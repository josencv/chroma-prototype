namespace ChromaPrototype.Color.Shapes;

using Godot;

/// <summary>
/// A disk-shaped color patch. Uses a thin cylinder mesh.
/// </summary>
[Tool]
[GlobalClass]
public partial class ColorDisk : ColorShapeBase
{
    private const float DiskHeight = 0.05f;

    private float _radius = 2.0f;

    /// <summary>
    /// Radius of the disk.
    /// </summary>
    [Export(PropertyHint.Range, "0.5,50,0.5")]
    public float Radius
    {
        get => _radius;
        set
        {
            _radius = value;
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
        cylinder.Height = DiskHeight;
        cylinder.RadialSegments = 32;
    }

    protected override void UpdateSpawner()
    {
        if (Spawner == null)
            return;

        Spawner.Shape = SpawnShape.Disk;
        Spawner.Radius = _radius;
        Spawner.Spacing = Spacing;
        Spawner.Color = ColorId;
        Spawner.Capacity = Capacity;
        Spawner.Density = Density;
        Spawner.FloorId = FloorId;
        Spawner.HeightOffset = HeightOffset;
    }
}
