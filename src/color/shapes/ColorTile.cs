namespace ChromaPrototype.Color.Shapes;

using Godot;

/// <summary>
/// A rectangular tile-shaped color patch. Uses a thin box mesh.
/// </summary>
[Tool]
[GlobalClass]
public partial class ColorTile : ColorShapeBase
{
    private const float TileHeight = 0.05f;

    private float _width = 4.0f;
    private float _length = 4.0f;

    /// <summary>
    /// Width (X axis) of the tile.
    /// </summary>
    [Export(PropertyHint.Range, "0.5,50,0.5")]
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
    /// Length (Z axis) of the tile.
    /// </summary>
    [Export(PropertyHint.Range, "0.5,50,0.5")]
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

        box.Size = new Vector3(_width, TileHeight, _length);
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
