using System.Collections.Generic;
using Godot;
using MistyInn.Core.Board;
using MistyInn.Core.Content;

namespace MistyInn.Board;

/// <summary>
/// Shows the board from Core's layout (SIM-04): one placeholder tile scene per tile, coloured by zone, the mist gates,
/// and the placement cursor (CAM-13). It also converts between Core's tile space and world space.
/// </summary>
public partial class InnView : Node3D
{
    [Export] public PackedScene TileScene { get; set; } = null!;

    [Export] public PackedScene GateScene { get; set; } = null!;

    [Export] public PackedScene CursorScene { get; set; } = null!;

    [Export] public float WallHeight { get; set; } = 1.4f;

    private static readonly Dictionary<string, Color> ZoneColors = new()
    {
        ["outside"] = new Color("#23282e"),
        ["door"] = new Color("#7a5230"),
        ["entrance"] = new Color("#6a5843"),
        ["common-room"] = new Color("#5c4a39"),
        ["bar"] = new Color("#6d4930"),
        ["kitchen"] = new Color("#575041"),
        ["stage"] = new Color("#4e3f56"),
        ["lounge"] = new Color("#5a3a47"),
        ["room-1"] = new Color("#4a4039"),
        ["room-2"] = new Color("#463b3f"),
        ["cellar-door"] = new Color("#3d3a34"),
    };

    private static readonly Color WallColor = new("#3a2f27");
    private static readonly Color RoomDoorColor = new("#6b4528");
    private static readonly Color ValidCursor = new(0.35f, 0.9f, 0.45f, 0.55f);
    private static readonly Color InvalidCursor = new(0.95f, 0.3f, 0.3f, 0.55f);

    private readonly Dictionary<Color, StandardMaterial3D> _materials = new();
    private readonly Dictionary<string, MistGateView> _gates = new();
    private readonly List<MeshInstance3D> _cursors = new();
    private BoardLayout _layout = null!;
    private StandardMaterial3D _validCursorMaterial = null!;
    private StandardMaterial3D _invalidCursorMaterial = null!;

    /// <summary>Builds the board once, from the session's layout and planes.</summary>
    public void Build(GameContent content)
    {
        _layout = content.Layout;

        foreach (var tile in _layout.AllTiles())
        {
            var kind = _layout.KindAt(tile);
            var view = TileScene.Instantiate<MeshInstance3D>();
            AddChild(view);
            view.Position = TileToWorld(tile);

            if (kind == TileKind.Wall)
            {
                view.Scale = new Vector3(1.04f, WallHeight / 0.1f, 1.04f);
                view.Position += new Vector3(0, WallHeight / 2, 0);
                view.MaterialOverride = Material(WallColor);
            }
            else if (kind == TileKind.RoomDoor)
            {
                view.MaterialOverride = Material(RoomDoorColor);
            }
            else
            {
                view.MaterialOverride = Material(ZoneColors.GetValueOrDefault(_layout.ZoneAt(tile), WallColor));
            }
        }

        foreach (var plane in content.Planes)
        {
            var gate = GateScene.Instantiate<MistGateView>();
            AddChild(gate);
            gate.Position = TileToWorld(content.GateFor(plane.Id));
            gate.SetColor(new Color(plane.MistColor));
            gate.Visible = false;
            _gates[plane.Id] = gate;
        }

        _validCursorMaterial = TransparentMaterial(ValidCursor);
        _invalidCursorMaterial = TransparentMaterial(InvalidCursor);
    }

    /// <summary>The ground rectangle (x, z) the camera may look at.</summary>
    public (Vector2 Min, Vector2 Max) Bounds =>
        (new Vector2(-_layout.Width / 2f, -_layout.Height / 2f), new Vector2(_layout.Width / 2f, _layout.Height / 2f));

    /// <summary>Opens the gates of the converging planes and closes the rest (PLN-04).</summary>
    public void ShowGates(IEnumerable<string> planeIds)
    {
        var open = new HashSet<string>(planeIds);
        foreach (var (planeId, gate) in _gates)
        {
            gate.Visible = open.Contains(planeId);
        }
    }

    public void ShowCursor(Tile tile, bool valid) => ShowCursor(new[] { tile }, valid);

    /// <summary>Shows the placement cursor over every tile a drop would cover, green or red (CAM-13).</summary>
    public void ShowCursor(IReadOnlyList<Tile> tiles, bool valid)
    {
        while (_cursors.Count < tiles.Count)
        {
            var cursor = CursorScene.Instantiate<MeshInstance3D>();
            AddChild(cursor);
            _cursors.Add(cursor);
        }

        for (var i = 0; i < _cursors.Count; i++)
        {
            var cursor = _cursors[i];
            cursor.Visible = i < tiles.Count;
            if (cursor.Visible)
            {
                cursor.Position = TileToWorld(tiles[i]);
                cursor.MaterialOverride = valid ? _validCursorMaterial : _invalidCursorMaterial;
            }
        }
    }

    public void HideCursor()
    {
        foreach (var cursor in _cursors)
        {
            cursor.Visible = false;
        }
    }

    public Vector3 TileToWorld(Tile tile) => PositionToWorld(TilePosition.Center(tile));

    public Vector3 PositionToWorld(TilePosition position) => new(
        (float)(position.X - (_layout.Width - 1) / 2.0),
        0,
        (float)(position.Y - (_layout.Height - 1) / 2.0));

    public Tile WorldToTile(Vector3 world) =>
        new TilePosition(world.X + (_layout.Width - 1) / 2.0, world.Z + (_layout.Height - 1) / 2.0).NearestTile;

    private StandardMaterial3D Material(Color color)
    {
        if (!_materials.TryGetValue(color, out var material))
        {
            material = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.9f };
            _materials[color] = material;
        }

        return material;
    }

    private static StandardMaterial3D TransparentMaterial(Color color) => new()
    {
        AlbedoColor = color,
        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
    };
}
