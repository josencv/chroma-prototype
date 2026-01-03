namespace ChromaPrototype.Color.Shapes;

using Godot;

/// <summary>
/// A long narrow color strip. Uses a thin box mesh.
/// Essentially a tile preset for narrow walkways/paths.
/// </summary>
[Tool]
[GlobalClass]
public partial class ColorStrip : ColorShapeBase
{
    private const float StripHeight = 0.05f;

    private float _width = 1.0f;
    private float _length = 8.0f;

    /// <summary>
    /// Width (X axis) of the strip (typically narrow).
    /// </summary>
    [Export(PropertyHint.Range, "0.5,10,0.5")]
    public float Width
    {
        get => _width;
        set
        {
            _width = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Length (Z axis) of the strip (typically long).
    /// </summary>
    [Export(PropertyHint.Range, "1,100,0.5")]
    public float Length
    {
        get => _length;
        set
        {
            _length = value;
            OnPropertyChanged();
        }
    }

    protected override void UpdateMeshDimensions()
    {
        if (MeshNode == null)
            return;

        // Create or update box mesh
        if (MeshNode.Mesh is not BoxMesh box)
        {
            box = new BoxMesh();
            MeshNode.Mesh = box;
        }

        box.Size = new Vector3(_width, StripHeight, _length);
    }

    protected override void UpdateSpawner()
    {
        if (Spawner == null)
            return;

        Spawner.Shape = SpawnShape.Rect;
        Spawner.Width = _width;
        Spawner.Length = _length;
        Spawner.Spacing = Spacing;
        Spawner.Color = ColorId;
        Spawner.Capacity = Capacity;
        Spawner.Density = Density;
        Spawner.FloorId = FloorId;
        Spawner.HeightOffset = HeightOffset;
    }
}
