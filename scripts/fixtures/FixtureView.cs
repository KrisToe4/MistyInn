using System;
using System.Collections.Generic;
using Godot;
using MistyInn.Core.Entities;
using MistyInn.Core.Run;

namespace MistyInn.Fixtures;

/// <summary>
/// A placed fixture: one placeholder part per tile it covers (ART-16), tinted by type, dark when broken (BRD-04).
/// </summary>
public partial class FixtureView : Node3D
{
	[Export] public PackedScene PartScene { get; set; } = null!;

	[Export] public Label3D Label { get; set; } = null!;

	private readonly List<MeshInstance3D> _parts = new();
	private StandardMaterial3D _material = null!;
	private Color _color;

	public FixtureId Id { get; private set; }

	public string DisplayName { get; private set; } = "";

	public void Setup(FixtureSnapshot fixture, Func<MistyInn.Core.Board.Tile, Vector3> tileToWorld)
	{
		Id = fixture.Id;
		DisplayName = fixture.Name;
		_color = ColorFor(fixture.FixtureDefId);
		_material = new StandardMaterial3D { AlbedoColor = _color, Roughness = 0.8f };
		Refresh(fixture, tileToWorld);
	}

	/// <summary>Rebuilds the parts for the fixture's tiles and shows its health.</summary>
	public void Refresh(FixtureSnapshot fixture, Func<MistyInn.Core.Board.Tile, Vector3> tileToWorld)
	{
		foreach (var part in _parts)
		{
			part.QueueFree();
		}

		_parts.Clear();
		if (fixture.Origin is not { } origin)
		{
			Visible = false;
			return;
		}

		Visible = true;
		var originWorld = tileToWorld(origin);
		Position = originWorld;
		foreach (var tile in fixture.Tiles)
		{
			var part = PartScene.Instantiate<MeshInstance3D>();
			AddChild(part);
			part.Position += tileToWorld(tile) - originWorld;
			part.MaterialOverride = _material;
			_parts.Add(part);
		}

		_material.AlbedoColor = fixture.IsBroken ? _color.Darkened(0.7f) : _color;
		Label.Text = fixture.IsBroken ? $"{fixture.Name} (broken)" : fixture.Name;
	}

	private static Color ColorFor(string fixtureDefId) => fixtureDefId switch
	{
		"bar-counter" => new Color("#8a5a35"),
		"table" or "card-table" => new Color("#7a6040"),
		"stage" => new Color("#6a4a7a"),
		"hearth" => new Color("#b8582a"),
		"lantern" => new Color("#e8c060"),
		"ward-circle" => new Color("#6aa0c8"),
		"quiet-corner" or "shadowed-alcove" or "ledger-desk" => new Color("#4a5a6a"),
		"barricade" => new Color("#5a4a3a"),
		"salt-line" => new Color("#d8d8d0"),
		_ => new Color("#808080"),
	};
}
